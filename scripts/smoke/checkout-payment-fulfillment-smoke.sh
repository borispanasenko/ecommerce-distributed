#!/usr/bin/env bash
set -euo pipefail

ORDERING="${ORDERING:-http://localhost:5002}"
PAYMENT="${PAYMENT:-http://localhost:5004}"
FULFILLMENT="${FULFILLMENT:-http://localhost:5006}"
INVENTORY="${INVENTORY:-http://localhost:5003}"

PRODUCT_VARIANT_ID="${PRODUCT_VARIANT_ID:-9572fb9d-f059-401e-9041-7fc75f8cb414}"
SKU="${SKU:-ARM-BLK}"
AMOUNT_MINOR="${AMOUNT_MINOR:-6900}"
CURRENCY="${CURRENCY:-USD}"

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

post_expect_status() {
  local expected_status="$1"
  local url="$2"
  local body="${3:-}"

  local response_file
  response_file="$(mktemp)"

  local actual_status
  actual_status="$(
    curl -sS -o "$response_file" -w "%{http_code}" -X POST "$url" \
      -H "Content-Type: application/json" \
      -d "$body"
  )"

  cat "$response_file"
  rm -f "$response_file"

  if [[ "$actual_status" != "$expected_status" ]]; then
    echo "Expected HTTP $expected_status but got HTTP $actual_status for $url" >&2
    exit 1
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

echo "== Checkout -> Payment -> Fulfillment smoke =="
echo "ORDERING=$ORDERING"
echo "PAYMENT=$PAYMENT"
echo "FULFILLMENT=$FULFILLMENT"
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
  "customerName": "Full Smoke $RUN_ID",
  "customerEmail": "full-smoke-$RUN_ID@example.com",
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

create_payment_body="$(cat <<JSON
{
  "orderId": "$ORDER_ID",
  "amountMinor": $AMOUNT_MINOR,
  "currency": "$CURRENCY",
  "provider": "Manual"
}
JSON
)"

payment_json="$(post_json "$PAYMENT/api/payments" "$create_payment_body")"
echo "== Create payment =="
echo "$payment_json" | jq

PAYMENT_ID="$(echo "$payment_json" | jq -r '.id')"

if [[ -z "$PAYMENT_ID" || "$PAYMENT_ID" == "null" ]]; then
  echo "Failed to read payment id." >&2
  exit 1
fi

echo "PAYMENT_ID=$PAYMENT_ID"
echo

succeed_payment_body="$(cat <<JSON
{
  "providerReference": "SMOKE-APPROVED-$RUN_ID"
}
JSON
)"

payment_succeeded="$(post_json "$PAYMENT/api/payments/$PAYMENT_ID/succeed" "$succeed_payment_body")"
echo "== Succeed payment =="
echo "$payment_succeeded" | jq
assert_eq "Succeeded" "$(echo "$payment_succeeded" | jq -r '.status')" "payment should be Succeeded"

order_after_payment="$(get_json "$ORDERING/api/orders/$ORDER_ID")"
echo "== Order after payment =="
echo "$order_after_payment" | jq
assert_eq "Paid" "$(echo "$order_after_payment" | jq -r '.status')" "order should be Paid after payment succeeds"

stock_after_payment="$(get_json "$INVENTORY/api/stock/$SKU")"
echo "== Stock after payment =="
echo "$stock_after_payment" | jq

after_payment_on_hand="$(echo "$stock_after_payment" | jq -r '.totalOnHandQuantity')"
after_payment_reserved="$(echo "$stock_after_payment" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_on_hand" "$after_payment_on_hand" "on-hand should not decrease after payment"
assert_number_eq "$((before_reserved + 1))" "$after_payment_reserved" "reservation should remain allocated after payment"

create_shipment_body="$(cat <<JSON
{
  "orderId": "$ORDER_ID",
  "carrier": "Manual",
  "trackingNumber": "SMOKE-TRACK-$RUN_ID"
}
JSON
)"

shipment_json="$(post_json "$FULFILLMENT/api/shipments" "$create_shipment_body")"
echo "== Create shipment =="
echo "$shipment_json" | jq

SHIPMENT_ID="$(echo "$shipment_json" | jq -r '.id')"

if [[ -z "$SHIPMENT_ID" || "$SHIPMENT_ID" == "null" ]]; then
  echo "Failed to read shipment id." >&2
  exit 1
fi

echo "SHIPMENT_ID=$SHIPMENT_ID"
echo

ship_shipment_body="$(cat <<JSON
{
  "carrier": "Manual",
  "trackingNumber": "SMOKE-SHIPPED-$RUN_ID"
}
JSON
)"

shipment_shipped="$(post_json "$FULFILLMENT/api/shipments/$SHIPMENT_ID/ship" "$ship_shipment_body")"
echo "== Ship shipment =="
echo "$shipment_shipped" | jq
assert_eq "Shipped" "$(echo "$shipment_shipped" | jq -r '.status')" "shipment should be Shipped"

retry_ship_body="$(cat <<JSON
{
  "carrier": "Manual",
  "trackingNumber": "SMOKE-RETRY-$RUN_ID"
}
JSON
)"

retry_ship_response="$(post_expect_status "400" "$FULFILLMENT/api/shipments/$SHIPMENT_ID/ship" "$retry_ship_body")"
echo "== Retry ship should fail at Fulfillment manual API =="
echo "$retry_ship_response" | jq
assert_eq "shipment_cannot_be_shipped" "$(echo "$retry_ship_response" | jq -r '.error')" "retry ship should be rejected by Fulfillment"

order_after_shipment="$(get_json "$ORDERING/api/orders/$ORDER_ID")"
echo "== Order after shipment =="
echo "$order_after_shipment" | jq
assert_eq "Shipped" "$(echo "$order_after_shipment" | jq -r '.status')" "order should be Shipped after shipment ships"

stock_after_shipment="$(get_json "$INVENTORY/api/stock/$SKU")"
echo "== Stock after shipment =="
echo "$stock_after_shipment" | jq

after_shipment_on_hand="$(echo "$stock_after_shipment" | jq -r '.totalOnHandQuantity')"
after_shipment_reserved="$(echo "$stock_after_shipment" | jq -r '.totalReservedQuantity')"

assert_number_eq "$((before_on_hand - 1))" "$after_shipment_on_hand" "on-hand should decrease by 1 after shipment"
assert_number_eq "$before_reserved" "$after_shipment_reserved" "reserved should return to original value after shipment"

echo
echo "Checkout -> Payment -> Fulfillment smoke passed."

