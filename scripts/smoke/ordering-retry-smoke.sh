#!/usr/bin/env bash
set -euo pipefail

ORDERING="${ORDERING:-http://localhost:5002}"
INVENTORY="${INVENTORY:-http://localhost:5003}"

PRODUCT_VARIANT_ID="${PRODUCT_VARIANT_ID:-9572fb9d-f059-401e-9041-7fc75f8cb414}"
SKU="${SKU:-ARM-BLK}"

RUN_ID="$(date +%s)"

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 1
  fi
}

get_json() {
  curl -sS --fail "$1"
}

post_json() {
  local url="$1"
  local body="${2:-}"

  if [[ -n "$body" ]]; then
    curl -sS --fail -X POST "$url" \
      -H "Content-Type: application/json" \
      -d "$body"
  else
    curl -sS --fail -X POST "$url"
  fi
}

assert_eq() {
  local expected="$1"
  local actual="$2"
  local message="$3"

  if [[ "$expected" != "$actual" ]]; then
    echo "Assertion failed: $message" >&2
    echo "Expected: $expected" >&2
    echo "Actual:   $actual" >&2
    exit 1
  fi
}

assert_number_eq() {
  local expected="$1"
  local actual="$2"
  local message="$3"

  if (( expected != actual )); then
    echo "Assertion failed: $message" >&2
    echo "Expected: $expected" >&2
    echo "Actual:   $actual" >&2
    exit 1
  fi
}

require_command curl
require_command jq

echo "== Ordering retry smoke =="
echo "ORDERING=$ORDERING"
echo "INVENTORY=$INVENTORY"
echo "SKU=$SKU"
echo

stock_before="$(get_json "$INVENTORY/api/stock/$SKU")"
echo "== Stock before =="
echo "$stock_before" | jq

before_on_hand="$(echo "$stock_before" | jq -r '.totalOnHandQuantity')"
before_reserved="$(echo "$stock_before" | jq -r '.totalReservedQuantity')"

create_order_body="$(cat <<JSON
{
  "customerName": "Ordering Retry Smoke $RUN_ID",
  "customerEmail": "ordering-retry-smoke-$RUN_ID@example.com",
  "items": [
    {
      "productVariantId": "$PRODUCT_VARIANT_ID",
      "quantity": 1
    }
  ]
}
JSON
)"

order_json="$(post_json "$ORDERING/api/orders" "$create_order_body")"
echo "== Create order =="
echo "$order_json" | jq

ORDER_ID="$(echo "$order_json" | jq -r '.id')"

if [[ -z "$ORDER_ID" || "$ORDER_ID" == "null" ]]; then
  echo "Failed to read order id." >&2
  exit 1
fi

echo "ORDER_ID=$ORDER_ID"
echo

stock_after_order="$(get_json "$INVENTORY/api/stock/$SKU")"
echo "== Stock after order creation =="
echo "$stock_after_order" | jq

after_order_on_hand="$(echo "$stock_after_order" | jq -r '.totalOnHandQuantity')"
after_order_reserved="$(echo "$stock_after_order" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_on_hand" "$after_order_on_hand" "on-hand should not change after order reservation"
assert_number_eq "$((before_reserved + 1))" "$after_order_reserved" "reserved should increase by 1 after order creation"

paid_first="$(post_json "$ORDERING/api/orders/$ORDER_ID/mark-paid")"
echo "== Mark paid first time =="
echo "$paid_first" | jq
assert_eq "Paid" "$(echo "$paid_first" | jq -r '.status')" "first mark-paid should return Paid"

paid_retry="$(post_json "$ORDERING/api/orders/$ORDER_ID/mark-paid")"
echo "== Mark paid retry =="
echo "$paid_retry" | jq
assert_eq "Paid" "$(echo "$paid_retry" | jq -r '.status')" "mark-paid retry should return Paid"

stock_after_paid_retry="$(get_json "$INVENTORY/api/stock/$SKU")"
echo "== Stock after mark-paid retry =="
echo "$stock_after_paid_retry" | jq

after_paid_on_hand="$(echo "$stock_after_paid_retry" | jq -r '.totalOnHandQuantity')"
after_paid_reserved="$(echo "$stock_after_paid_retry" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_on_hand" "$after_paid_on_hand" "on-hand should not change after mark-paid retry"
assert_number_eq "$((before_reserved + 1))" "$after_paid_reserved" "reservation should stay allocated after mark-paid retry"

shipped_first="$(post_json "$ORDERING/api/orders/$ORDER_ID/mark-shipped")"
echo "== Mark shipped first time =="
echo "$shipped_first" | jq
assert_eq "Shipped" "$(echo "$shipped_first" | jq -r '.status')" "first mark-shipped should return Shipped"

shipped_retry="$(post_json "$ORDERING/api/orders/$ORDER_ID/mark-shipped")"
echo "== Mark shipped retry =="
echo "$shipped_retry" | jq
assert_eq "Shipped" "$(echo "$shipped_retry" | jq -r '.status')" "mark-shipped retry should return Shipped"

stock_after_shipped_retry="$(get_json "$INVENTORY/api/stock/$SKU")"
echo "== Stock after mark-shipped retry =="
echo "$stock_after_shipped_retry" | jq

after_shipped_on_hand="$(echo "$stock_after_shipped_retry" | jq -r '.totalOnHandQuantity')"
after_shipped_reserved="$(echo "$stock_after_shipped_retry" | jq -r '.totalReservedQuantity')"

assert_number_eq "$((before_on_hand - 1))" "$after_shipped_on_hand" "on-hand should decrease by 1 exactly once"
assert_number_eq "$before_reserved" "$after_shipped_reserved" "reserved should return to original value after shipment"

echo
echo "Ordering retry smoke passed."

