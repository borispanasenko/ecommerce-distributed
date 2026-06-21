#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

# shellcheck source=scripts/testing/test-helper.sh
source "$ROOT_DIR/scripts/testing/test-helper.sh"

require_command curl
require_command jq

RUN_ID="$(date +%s)_$RANDOM"

echo "== Inventory reservation idempotency smoke =="
echo "ORDERING=$ORDERING"
echo "INVENTORY=$INVENTORY"
echo "SKU=$SKU"
echo

read -r WAREHOUSE_ID LOCATION_ID < <(inventory_stock_seed "$SKU" 10 "MAIN-$RUN_ID" "MAIN-$RUN_ID-01")

stock_before="$(get_json "$INVENTORY/api/stock/$SKU")"
before_on_hand="$(echo "$stock_before" | jq -r '.totalOnHandQuantity')"
before_reserved="$(echo "$stock_before" | jq -r '.totalReservedQuantity')"

release_order_json="$(create_order "Inventory Release Smoke $RUN_ID" "$PRODUCT_VARIANT_ID" 1)"
RELEASE_ORDER_ID="$(echo "$release_order_json" | jq -r '.id')"
RELEASE_RESERVATION_ID="$(echo "$release_order_json" | jq -r '.items[0].inventoryReservationId')"

cancelled_order="$(post_json "$ORDERING/api/orders/$RELEASE_ORDER_ID/cancel")"
assert_eq "Cancelled" "$(echo "$cancelled_order" | jq -r '.status')" "order should be Cancelled"

released_retry="$(post_json "$INVENTORY/api/stock/reservations/$RELEASE_RESERVATION_ID/release")"
assert_eq "Released" "$(echo "$released_retry" | jq -r '.status')" "release retry should return Released"

commit_released_response="$(post_expect_status "400" "$INVENTORY/api/stock/reservations/$RELEASE_RESERVATION_ID/commit")"
assert_eq "reservation_not_active" "$(echo "$commit_released_response" | jq -r '.error')" "commit released reservation should fail"

stock_after_release="$(get_json "$INVENTORY/api/stock/$SKU")"
after_release_on_hand="$(echo "$stock_after_release" | jq -r '.totalOnHandQuantity')"
after_release_reserved="$(echo "$stock_after_release" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_on_hand" "$after_release_on_hand" "on-hand should be unchanged after release scenario"
assert_number_eq "$before_reserved" "$after_release_reserved" "reserved should return to original value after release scenario"

stock_before_commit="$(get_json "$INVENTORY/api/stock/$SKU")"
before_commit_on_hand="$(echo "$stock_before_commit" | jq -r '.totalOnHandQuantity')"
before_commit_reserved="$(echo "$stock_before_commit" | jq -r '.totalReservedQuantity')"

commit_order_json="$(create_order "Inventory Commit Smoke $RUN_ID" "$PRODUCT_VARIANT_ID" 1)"
COMMIT_ORDER_ID="$(echo "$commit_order_json" | jq -r '.id')"
COMMIT_RESERVATION_ID="$(echo "$commit_order_json" | jq -r '.items[0].inventoryReservationId')"

paid_order="$(post_json "$ORDERING/api/orders/$COMMIT_ORDER_ID/mark-paid")"
assert_eq "Paid" "$(echo "$paid_order" | jq -r '.status')" "order should be Paid"

shipped_order="$(post_json "$ORDERING/api/orders/$COMMIT_ORDER_ID/mark-shipped")"
assert_eq "Shipped" "$(echo "$shipped_order" | jq -r '.status')" "order should be Shipped"

committed_retry="$(post_json "$INVENTORY/api/stock/reservations/$COMMIT_RESERVATION_ID/commit")"
assert_eq "Committed" "$(echo "$committed_retry" | jq -r '.status')" "commit retry should return Committed"

release_committed_response="$(post_expect_status "400" "$INVENTORY/api/stock/reservations/$COMMIT_RESERVATION_ID/release")"
assert_eq "reservation_not_active" "$(echo "$release_committed_response" | jq -r '.error')" "release committed reservation should fail"

stock_after_commit="$(get_json "$INVENTORY/api/stock/$SKU")"
after_commit_on_hand="$(echo "$stock_after_commit" | jq -r '.totalOnHandQuantity')"
after_commit_reserved="$(echo "$stock_after_commit" | jq -r '.totalReservedQuantity')"

assert_number_eq "$((before_commit_on_hand - 1))" "$after_commit_on_hand" "on-hand should decrease by 1 exactly once after commit scenario"
assert_number_eq "$before_commit_reserved" "$after_commit_reserved" "reserved should return to original value after commit scenario"

echo "Inventory reservation idempotency smoke passed."
