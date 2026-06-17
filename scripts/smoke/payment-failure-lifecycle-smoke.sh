#!/usr/bin/env bash
set -euo pipefail

ORDERING="${ORDERING:-http://localhost:5002}"
PAYMENT="${PAYMENT:-http://localhost:5004}"
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

echo "== Payment failure lifecycle smoke =="
echo "ORDERING=$ORDERING"
echo "PAYMENT=$PAYMENT"
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
  "customerName": "Payment Failure Smoke $RUN_ID",
  "customerEmail": "payment-failure-smoke-$RUN_ID@example.com",
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

assert_eq "PendingPayment" "$(echo "$order_json" | jq -r '.status')" "new order should be PendingPayment"

echo "ORDER_ID=$ORDER_ID"
echo

stock_after_order="$(get_json "$INVENTORY/api/stock/$SKU")"
echo "== Stock after order creation =="
echo "$stock_after_order" | jq

after_order_on_hand="$(echo "$stock_after_order" | jq -r '.totalOnHandQuantity')"
after_order_reserved="$(echo "$stock_after_order" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_on_hand" "$after_order_on_hand" "on-hand should not change after reservation"
assert_number_eq "$((before_reserved + 1))" "$after_order_reserved" "reserved should increase by 1 after order creation"

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

assert_eq "Pending" "$(echo "$payment_json" | jq -r '.status')" "new payment should be Pending"

echo "PAYMENT_ID=$PAYMENT_ID"
echo

fail_payment_body="$(cat <<JSON
{
  "failureReason": "Payment failure smoke $RUN_ID"
}
JSON
)"

payment_failed="$(post_json "$PAYMENT/api/payments/$PAYMENT_ID/fail" "$fail_payment_body")"
echo "== Fail payment =="
echo "$payment_failed" | jq

assert_eq "Failed" "$(echo "$payment_failed" | jq -r '.status')" "payment should be Failed"

order_after_payment_failure="$(get_json "$ORDERING/api/orders/$ORDER_ID")"
echo "== Order after payment failure =="
echo "$order_after_payment_failure" | jq

assert_eq "PendingPayment" "$(echo "$order_after_payment_failure" | jq -r '.status')" "order should remain PendingPayment after payment failure"

stock_after_payment_failure="$(get_json "$INVENTORY/api/stock/$SKU")"
echo "== Stock after payment failure =="
echo "$stock_after_payment_failure" | jq

after_failure_on_hand="$(echo "$stock_after_payment_failure" | jq -r '.totalOnHandQuantity')"
after_failure_reserved="$(echo "$stock_after_payment_failure" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_on_hand" "$after_failure_on_hand" "on-hand should not change after payment failure"
assert_number_eq "$((before_reserved + 1))" "$after_failure_reserved" "reservation should remain allocated after payment failure"

expired_order="$(post_json "$ORDERING/api/orders/$ORDER_ID/expire")"
echo "== Expire unpaid order =="
echo "$expired_order" | jq

assert_eq "Expired" "$(echo "$expired_order" | jq -r '.status')" "order should be Expired after manual expiration"

stock_after_expire="$(get_json "$INVENTORY/api/stock/$SKU")"
echo "== Stock after expiration =="
echo "$stock_after_expire" | jq

after_expire_on_hand="$(echo "$stock_after_expire" | jq -r '.totalOnHandQuantity')"
after_expire_reserved="$(echo "$stock_after_expire" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_on_hand" "$after_expire_on_hand" "on-hand should not change after expiration release"
assert_number_eq "$before_reserved" "$after_expire_reserved" "reserved should return to original value after expiration"

echo
echo "Payment failure lifecycle smoke passed."
