#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

CATALOG="${CATALOG:-http://localhost:5001}"
ORDERING="${ORDERING:-http://localhost:5002}"
INVENTORY="${INVENTORY:-http://localhost:5003}"
PAYMENT="${PAYMENT:-http://localhost:5004}"
CART="${CART:-http://localhost:5005}"
FULFILLMENT="${FULFILLMENT:-http://localhost:5006}"

PRODUCT_VARIANT_ID="${PRODUCT_VARIANT_ID:-9572fb9d-f059-401e-9041-7fc75f8cb414}"
SKU="${SKU:-ARM-BLK}"
AMOUNT_MINOR="${AMOUNT_MINOR:-6900}"
CURRENCY="${CURRENCY:-USD}"

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1" >&2
    exit 1
  fi
}

json_get() {
  jq -r "$1"
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

inventory_stock_seed() {
  local sku="$1"
  local quantity="${2:-10}"
  local warehouse_code="${3:-MAIN}"
  local location_code="${4:-MAIN-01}"

  local warehouse_id location_id

  warehouse_id="$(
    curl -sS --fail -X POST "$INVENTORY/api/warehouses" \
      -H "Content-Type: application/json" \
      -d "{\"code\":\"$warehouse_code\",\"name\":\"Main Warehouse\"}" \
    | jq -r '.id'
  )"

  location_id="$(
    curl -sS --fail -X POST "$INVENTORY/api/locations" \
      -H "Content-Type: application/json" \
      -d "{\"warehouseId\":\"$warehouse_id\",\"code\":\"$location_code\"}" \
    | jq -r '.id'
  )"

  curl -sS --fail -X POST "$INVENTORY/api/stock/receipts" \
    -H "Content-Type: application/json" \
    -d "{\"sku\":\"$sku\",\"warehouseId\":\"$warehouse_id\",\"locationId\":\"$location_id\",\"quantity\":$quantity,\"reason\":\"seed\"}" \
    >/dev/null

  printf '%s %s\n' "$warehouse_id" "$location_id"
}

create_order() {
  local label="$1"
  local variant_id="${2:-$PRODUCT_VARIANT_ID}"
  local quantity="${3:-1}"

  local body
  body="$(cat <<JSON
{
  "customerName": "$label",
  "customerEmail": "${label// /-}@example.com",
  "items": [
    {
      "productVariantId": "$variant_id",
      "quantity": $quantity
    }
  ]
}
JSON
)"

  post_json "$ORDERING/api/orders" "$body"
}
