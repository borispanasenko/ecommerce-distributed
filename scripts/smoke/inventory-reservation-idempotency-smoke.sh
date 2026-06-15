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

post_expect_status() {
  local expected_status="$1"
  local url="$2"
  local body="${3:-}"

  local response_file
  response_file="$(mktemp)"

  local actual_status
  if [[ -n "$body" ]]; then
    actual_status="$(
      curl -sS -o "$response_file" -w "%{http_code}" -X POST "$url" \
        -H "Content-Type: application/json" \
        -d "$body"
    )"
  else
    actual_status="$(
      curl -sS -o "$response_file" -w "%{http_code}" -X POST "$url"
    )"
  fi

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

create_order() {
  local label="$1"

  local body
  body="$(cat <<JSON
{
  "customerName": "$label $RUN_ID",
  "customerEmail": "$label-$RUN_ID@example.com",
  "items": [
    {
      "productVariantId": "$PRODUCT_VARIANT_ID",
      "quantity": 1
    }
  ]
}
JSON
)"

  post_json "$ORDERING/api/orders" "$body"
}

reservation_id_from_order_json() {
  jq -r '.items[0].inventoryReservationId'
}

require_command curl
require_command jq

echo "== Inventory reservation idempotency smoke =="
echo "ORDERING=$ORDERING"
echo "INVENTORY=$INVENTORY"
echo "SKU=$SKU"
echo

stock_before_release="$(get_json "$INVENTORY/api/stock/$SKU")"
echo "== Stock before release scenario =="
echo "$stock_before_release" | jq

before_release_on_hand="$(echo "$stock_before_release" | jq -r '.totalOnHandQuantity')"
before_release_reserved="$(echo "$stock_before_release" | jq -r '.totalReservedQuantity')"

release_order_json="$(create_order "Inventory Release Smoke")"
echo "== Create order for release scenario =="
echo "$release_order_json" | jq

RELEASE_ORDER_ID="$(echo "$release_order_json" | jq -r '.id')"
RELEASE_RESERVATION_ID="$(echo "$release_order_json" | reservation_id_from_order_json)"

echo "RELEASE_ORDER_ID=$RELEASE_ORDER_ID"
echo "RELEASE_RESERVATION_ID=$RELEASE_RESERVATION_ID"
echo

cancelled_order="$(post_json "$ORDERING/api/orders/$RELEASE_ORDER_ID/cancel")"
echo "== Cancel order to release reservation =="
echo "$cancelled_order" | jq
assert_eq "Cancelled" "$(echo "$cancelled_order" | jq -r '.status')" "order should be Cancelled"

released_retry="$(post_json "$INVENTORY/api/stock/reservations/$RELEASE_RESERVATION_ID/release")"
echo "== Release already Released reservation directly =="
echo "$released_retry" | jq
assert_eq "Released" "$(echo "$released_retry" | jq -r '.status')" "release retry should return Released"

commit_released_response="$(post_expect_status "400" "$INVENTORY/api/stock/reservations/$RELEASE_RESERVATION_ID/commit")"
echo "== Commit Released reservation should fail =="
echo "$commit_released_response" | jq
assert_eq "reservation_not_active" "$(echo "$commit_released_response" | jq -r '.error')" "commit released reservation should fail"

stock_after_release="$(get_json "$INVENTORY/api/stock/$SKU")"
echo "== Stock after release scenario =="
echo "$stock_after_release" | jq

after_release_on_hand="$(echo "$stock_after_release" | jq -r '.totalOnHandQuantity')"
after_release_reserved="$(echo "$stock_after_release" | jq -r '.totalReservedQuantity')"

assert_number_eq "$before_release_on_hand" "$after_release_on_hand" "on-hand should be unchanged after release scenario"
assert_number_eq "$before_release_reserved" "$after_release_reserved" "reserved should return to original value after release scenario"

stock_before_commit="$(get_json "$INVENTORY/api/stock/$SKU")"
echo "== Stock before commit scenario =="
echo "$stock_before_commit" | jq

before_commit_on_hand="$(echo "$stock_before_commit" | jq -r '.totalOnHandQuantity')"
before_commit_reserved="$(echo "$stock_before_commit" | jq -r '.totalReservedQuantity')"

commit_order_json="$(create_order "Inventory Commit Smoke")"
echo "== Create order for commit scenario =="
echo "$commit_order_json" | jq

COMMIT_ORDER_ID="$(echo "$commit_order_json" | jq -r '.id')"
COMMIT_RESERVATION_ID="$(echo "$commit_order_json" | reservation_id_from_order_json)"

echo "COMMIT_ORDER_ID=$COMMIT_ORDER_ID"
echo "COMMIT_RESERVATION_ID=$COMMIT_RESERVATION_ID"
echo

paid_order="$(post_json "$ORDERING/api/orders/$COMMIT_ORDER_ID/mark-paid")"
echo "== Mark order paid =="
echo "$paid_order" | jq
assert_eq "Paid" "$(echo "$paid_order" | jq -r '.status')" "order should be Paid"

shipped_order="$(post_json "$ORDERING/api/orders/$COMMIT_ORDER_ID/mark-shipped")"
echo "== Mark order shipped to commit reservation =="
echo "$shipped_order" | jq
assert_eq "Shipped" "$(echo "$shipped_order" | jq -r '.status')" "order should be Shipped"

committed_retry="$(post_json "$INVENTORY/api/stock/reservations/$COMMIT_RESERVATION_ID/commit")"
echo "== Commit already Committed reservation directly =="
echo "$committed_retry" | jq
assert_eq "Committed" "$(echo "$committed_retry" | jq -r '.status')" "commit retry should return Committed"

release_committed_response="$(post_expect_status "400" "$INVENTORY/api/stock/reservations/$COMMIT_RESERVATION_ID/release")"
echo "== Release Committed reservation should fail =="
echo "$release_committed_response" | jq
assert_eq "reservation_not_active" "$(echo "$release_committed_response" | jq -r '.error')" "release committed reservation should fail"

stock_after_commit="$(get_json "$INVENTORY/api/stock/$SKU")"
echo "== Stock after commit scenario =="
echo "$stock_after_commit" | jq

after_commit_on_hand="$(echo "$stock_after_commit" | jq -r '.totalOnHandQuantity')"
after_commit_reserved="$(echo "$stock_after_commit" | jq -r '.totalReservedQuantity')"

assert_number_eq "$((before_commit_on_hand - 1))" "$after_commit_on_hand" "on-hand should decrease by 1 exactly once after commit scenario"
assert_number_eq "$before_commit_reserved" "$after_commit_reserved" "reserved should return to original value after commit scenario"

echo
echo "Inventory reservation idempotency smoke passed."

