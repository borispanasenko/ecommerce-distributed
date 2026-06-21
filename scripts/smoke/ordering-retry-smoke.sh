#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

# shellcheck source=scripts/testing/test-helper.sh
source "$ROOT_DIR/scripts/testing/test-helper.sh"

require_command curl
require_command jq

RUN_ID="$(date +%s)_$RANDOM"

echo "== Ordering retry smoke =="
echo "ORDERING=$ORDERING"
echo "INVENTORY=$INVENTORY"
echo "SKU=$SKU"
echo

read -r WAREHOUSE_ID LOCATION_ID < <(inventory_stock_seed "$SKU" 10 "MAIN-$RUN_ID" "MAIN-$RUN_ID-01")

echo "WAREHOUSE_ID=$WAREHOUSE_ID"
echo "LOCATION_ID=$LOCATION_ID"
echo

stock_before="$(get_json "$INVENTORY/api/stock/$SKU")"
before_on_hand="$(echo "$stock_before" | jq -r '.totalOnHandQuantity')"
before_reserved="$(echo "$stock_before" | jq -r '.totalReservedQuantity')"

order_json="$(create_order "Ordering Retry Smoke $RUN_ID" "$PRODUCT_VARIANT_ID" 1)"
ORDER_ID="$(echo "$order_json" | jq -r '.id')"

assert_eq "PendingPayment" "$(echo "$order_json" | jq -r '.status')" "new order should be PendingPayment"

stock_after_order="$(get_json "$INVENTORY/api/stock/$SKU")"
after_order_on_hand="$(echo "$stock_after_order" | jq -r '.totalOnHandQuantity')"
after_order_reserved="$(echo "$stock_after_order" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_on_hand" "$after_order_on_hand" "on-hand should not change after order reservation"
assert_number_eq "$((before_reserved + 1))" "$after_order_reserved" "reserved should increase by 1 after order creation"

paid_first="$(post_json "$ORDERING/api/orders/$ORDER_ID/mark-paid")"
assert_eq "Paid" "$(echo "$paid_first" | jq -r '.status')" "first mark-paid should return Paid"

paid_retry="$(post_json "$ORDERING/api/orders/$ORDER_ID/mark-paid")"
assert_eq "Paid" "$(echo "$paid_retry" | jq -r '.status')" "mark-paid retry should return Paid"

stock_after_paid_retry="$(get_json "$INVENTORY/api/stock/$SKU")"
after_paid_on_hand="$(echo "$stock_after_paid_retry" | jq -r '.totalOnHandQuantity')"
after_paid_reserved="$(echo "$stock_after_paid_retry" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_on_hand" "$after_paid_on_hand" "on-hand should not change after mark-paid retry"
assert_number_eq "$((before_reserved + 1))" "$after_paid_reserved" "reservation should stay allocated after mark-paid retry"

shipped_first="$(post_json "$ORDERING/api/orders/$ORDER_ID/mark-shipped")"
assert_eq "Shipped" "$(echo "$shipped_first" | jq -r '.status')" "first mark-shipped should return Shipped"

shipped_retry="$(post_json "$ORDERING/api/orders/$ORDER_ID/mark-shipped")"
assert_eq "Shipped" "$(echo "$shipped_retry" | jq -r '.status')" "mark-shipped retry should return Shipped"

stock_after_shipped_retry="$(get_json "$INVENTORY/api/stock/$SKU")"
after_shipped_on_hand="$(echo "$stock_after_shipped_retry" | jq -r '.totalOnHandQuantity')"
after_shipped_reserved="$(echo "$stock_after_shipped_retry" | jq -r '.totalReservedQuantity')"

assert_number_eq "$((before_on_hand - 1))" "$after_shipped_on_hand" "on-hand should decrease by 1 exactly once"
assert_number_eq "$before_reserved" "$after_shipped_reserved" "reserved should return to original value after shipment"

echo "Ordering retry smoke passed."
