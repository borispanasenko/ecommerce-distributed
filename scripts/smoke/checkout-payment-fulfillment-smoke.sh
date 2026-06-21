#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

# shellcheck source=scripts/testing/test-helper.sh
source "$ROOT_DIR/scripts/testing/test-helper.sh"

require_command curl
require_command jq

RUN_ID="$(date +%s)_$RANDOM"

echo "== Checkout -> Payment -> Fulfillment smoke =="
echo "ORDERING=$ORDERING"
echo "PAYMENT=$PAYMENT"
echo "FULFILLMENT=$FULFILLMENT"
echo "INVENTORY=$INVENTORY"
echo "SKU=$SKU"
echo

read -r WAREHOUSE_ID LOCATION_ID < <(inventory_stock_seed "$SKU" 10 "MAIN-$RUN_ID" "MAIN-$RUN_ID-01")

stock_before="$(get_json "$INVENTORY/api/stock/$SKU")"
before_on_hand="$(echo "$stock_before" | jq -r '.totalOnHandQuantity')"
before_reserved="$(echo "$stock_before" | jq -r '.totalReservedQuantity')"

order_json="$(create_order "Full Smoke $RUN_ID" "$PRODUCT_VARIANT_ID" 1)"
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

payment_succeeded="$(post_json "$PAYMENT/api/payments/$PAYMENT_ID/succeed" "$(cat <<JSON
{
  "providerReference": "SMOKE-APPROVED-$(date +%s)"
}
JSON
)")"

assert_eq "Succeeded" "$(echo "$payment_succeeded" | jq -r '.status')" "payment should be Succeeded"

order_after_payment="$(get_json "$ORDERING/api/orders/$ORDER_ID")"
assert_eq "Paid" "$(echo "$order_after_payment" | jq -r '.status')" "order should be Paid after payment succeeds"

stock_after_payment="$(get_json "$INVENTORY/api/stock/$SKU")"
after_payment_on_hand="$(echo "$stock_after_payment" | jq -r '.totalOnHandQuantity')"
after_payment_reserved="$(echo "$stock_after_payment" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_on_hand" "$after_payment_on_hand" "on-hand should not decrease after payment"
assert_number_eq "$((before_reserved + 1))" "$after_payment_reserved" "reservation should remain allocated after payment"

shipment_json="$(post_json "$FULFILLMENT/api/shipments" "$(cat <<JSON
{
  "orderId": "$ORDER_ID",
  "carrier": "Manual",
  "trackingNumber": "SMOKE-TRACK-$(date +%s)"
}
JSON
)")"

SHIPMENT_ID="$(echo "$shipment_json" | jq -r '.id')"
assert_eq "Pending" "$(echo "$shipment_json" | jq -r '.status')" "new shipment should be Pending"

shipment_shipped="$(post_json "$FULFILLMENT/api/shipments/$SHIPMENT_ID/ship" "$(cat <<JSON
{
  "carrier": "Manual",
  "trackingNumber": "SMOKE-SHIPPED-$(date +%s)"
}
JSON
)")"

assert_eq "Shipped" "$(echo "$shipment_shipped" | jq -r '.status')" "shipment should be Shipped"

order_after_shipment="$(get_json "$ORDERING/api/orders/$ORDER_ID")"
assert_eq "Shipped" "$(echo "$order_after_shipment" | jq -r '.status')" "order should be Shipped after shipment ships"

stock_after_shipment="$(get_json "$INVENTORY/api/stock/$SKU")"
after_shipment_on_hand="$(echo "$stock_after_shipment" | jq -r '.totalOnHandQuantity')"
after_shipment_reserved="$(echo "$stock_after_shipment" | jq -r '.totalReservedQuantity')"

assert_number_eq "$((before_on_hand - 1))" "$after_shipment_on_hand" "on-hand should decrease by 1 after shipment"
assert_number_eq "$before_reserved" "$after_shipment_reserved" "reserved should return to original value after shipment"

echo "Checkout -> Payment -> Fulfillment smoke passed."
