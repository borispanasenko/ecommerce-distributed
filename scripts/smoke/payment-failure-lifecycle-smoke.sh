#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

# shellcheck source=scripts/testing/test-helper.sh
source "$ROOT_DIR/scripts/testing/test-helper.sh"

require_command curl
require_command jq

RUN_ID="$(date +%s)_$RANDOM"

echo "== Payment failure lifecycle smoke =="
echo "ORDERING=$ORDERING"
echo "PAYMENT=$PAYMENT"
echo "INVENTORY=$INVENTORY"
echo "SKU=$SKU"
echo

read -r WAREHOUSE_ID LOCATION_ID < <(inventory_stock_seed "$SKU" 10 "MAIN-$RUN_ID" "MAIN-$RUN_ID-01")

stock_before="$(get_json "$INVENTORY/api/stock/$SKU")"
before_on_hand="$(echo "$stock_before" | jq -r '.totalOnHandQuantity')"
before_reserved="$(echo "$stock_before" | jq -r '.totalReservedQuantity')"

order_json="$(create_order "Payment Failure Smoke $RUN_ID" "$PRODUCT_VARIANT_ID" 1)"
ORDER_ID="$(echo "$order_json" | jq -r '.id')"
assert_eq "PendingPayment" "$(echo "$order_json" | jq -r '.status')" "new order should be PendingPayment"

payment_json="$(post_json "$PAYMENT/api/payments" "$(cat <<JSON
{
  "orderId": "$ORDER_ID",
  "amountMinor": $AMOUNT_MINOR,
  "currency": "$CURRENCY",
  "provider": "Manual"
}
JSON
)")"

PAYMENT_ID="$(echo "$payment_json" | jq -r '.id')"
assert_eq "Pending" "$(echo "$payment_json" | jq -r '.status')" "new payment should be Pending"

payment_failed="$(post_json "$PAYMENT/api/payments/$PAYMENT_ID/fail" "$(cat <<JSON
{
  "failureReason": "Payment failure smoke $(date +%s)"
}
JSON
)")"

assert_eq "Failed" "$(echo "$payment_failed" | jq -r '.status')" "payment should be Failed"

order_after_payment_failure="$(get_json "$ORDERING/api/orders/$ORDER_ID")"
assert_eq "PendingPayment" "$(echo "$order_after_payment_failure" | jq -r '.status')" "order should remain PendingPayment after payment failure"

stock_after_payment_failure="$(get_json "$INVENTORY/api/stock/$SKU")"
after_failure_on_hand="$(echo "$stock_after_payment_failure" | jq -r '.totalOnHandQuantity')"
after_failure_reserved="$(echo "$stock_after_payment_failure" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_on_hand" "$after_failure_on_hand" "on-hand should not change after payment failure"
assert_number_eq "$((before_reserved + 1))" "$after_failure_reserved" "reservation should remain allocated after payment failure"

expired_order="$(post_json "$ORDERING/api/orders/$ORDER_ID/expire")"
assert_eq "Expired" "$(echo "$expired_order" | jq -r '.status')" "order should be Expired after manual expiration"

stock_after_expire="$(get_json "$INVENTORY/api/stock/$SKU")"
after_expire_on_hand="$(echo "$stock_after_expire" | jq -r '.totalOnHandQuantity')"
after_expire_reserved="$(echo "$stock_after_expire" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_on_hand" "$after_expire_on_hand" "on-hand should not change after expiration release"
assert_number_eq "$before_reserved" "$after_expire_reserved" "reserved should return to original value after expiration"

echo "Payment failure lifecycle smoke passed."
