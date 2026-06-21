#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

# shellcheck source=scripts/testing/test-helper.sh
source "$ROOT_DIR/scripts/testing/test-helper.sh"

require_command curl
require_command jq

RUN_ID="$(date +%s)_$RANDOM"

echo "== Order expiration smoke =="
echo "ORDERING=$ORDERING"
echo "INVENTORY=$INVENTORY"
echo "SKU=$SKU"
echo

read -r WAREHOUSE_ID LOCATION_ID < <(inventory_stock_seed "$SKU" 10 "MAIN-$RUN_ID" "MAIN-$RUN_ID-01")

stock_before="$(get_json "$INVENTORY/api/stock/$SKU")"
before_on_hand="$(echo "$stock_before" | jq -r '.totalOnHandQuantity')"
before_reserved="$(echo "$stock_before" | jq -r '.totalReservedQuantity')"

order_json="$(create_order "Expiration Smoke $RUN_ID" "$PRODUCT_VARIANT_ID" 1)"
ORDER_ID="$(echo "$order_json" | jq -r '.id')"

stock_after_order="$(get_json "$INVENTORY/api/stock/$SKU")"
after_order_on_hand="$(echo "$stock_after_order" | jq -r '.totalOnHandQuantity')"
after_order_reserved="$(echo "$stock_after_order" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_on_hand" "$after_order_on_hand" "on-hand should not change after order reservation"
assert_number_eq "$((before_reserved + 1))" "$after_order_reserved" "reserved should increase by 1 after order creation"

expired_first="$(post_json "$ORDERING/api/orders/$ORDER_ID/expire")"
assert_eq "Expired" "$(echo "$expired_first" | jq -r '.status')" "first expire should return Expired"

stock_after_expire="$(get_json "$INVENTORY/api/stock/$SKU")"
after_expire_on_hand="$(echo "$stock_after_expire" | jq -r '.totalOnHandQuantity')"
after_expire_reserved="$(echo "$stock_after_expire" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_on_hand" "$after_expire_on_hand" "on-hand should not change after expiration release"
assert_number_eq "$before_reserved" "$after_expire_reserved" "reserved should return to original value after expiration"

expired_retry="$(post_json "$ORDERING/api/orders/$ORDER_ID/expire")"
assert_eq "Expired" "$(echo "$expired_retry" | jq -r '.status')" "expire retry should return Expired"

stock_after_retry="$(get_json "$INVENTORY/api/stock/$SKU")"
after_retry_on_hand="$(echo "$stock_after_retry" | jq -r '.totalOnHandQuantity')"
after_retry_reserved="$(echo "$stock_after_retry" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_on_hand" "$after_retry_on_hand" "on-hand should remain unchanged after expire retry"
assert_number_eq "$before_reserved" "$after_retry_reserved" "reserved should remain unchanged after expire retry"

order_after_retry="$(get_json "$ORDERING/api/orders/$ORDER_ID")"
assert_eq "Expired" "$(echo "$order_after_retry" | jq -r '.status')" "order should stay Expired"

echo "Order expiration smoke passed."
