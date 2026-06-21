#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

# shellcheck source=scripts/testing/test-helper.sh
source "$ROOT_DIR/scripts/testing/test-helper.sh"

wait_for_health() {
  local url="$1"
  local label="$2"
  local attempts="${3:-60}"

  for _ in $(seq 1 "$attempts"); do
    if curl -fsS "$url" >/dev/null; then
      echo "$label is healthy"
      return 0
    fi
    sleep 2
  done

  echo "Timed out waiting for $label at $url" >&2
  exit 1
}

echo "== Up =="
(cd "$ROOT_DIR" && docker compose up -d --build)

echo "== Wait for services =="
wait_for_health "$CATALOG/health" "Catalog"
wait_for_health "$INVENTORY/health" "Inventory"
wait_for_health "$ORDERING/health" "Ordering"
wait_for_health "$PAYMENT/health" "Payment"
wait_for_health "$CART/health" "Cart"
wait_for_health "$FULFILLMENT/health" "Fulfillment"

echo "== Smoke: ordering retry =="
"$ROOT_DIR/scripts/smoke/ordering-retry-smoke.sh"

echo "== Smoke: checkout/payment/fulfillment =="
"$ROOT_DIR/scripts/smoke/checkout-payment-fulfillment-smoke.sh"

echo "== Smoke: inventory idempotency =="
"$ROOT_DIR/scripts/smoke/inventory-reservation-idempotency-smoke.sh"

echo "== Smoke: order expiration =="
"$ROOT_DIR/scripts/smoke/order-expiration-smoke.sh"

echo "== Smoke: payment failure lifecycle =="
"$ROOT_DIR/scripts/smoke/payment-failure-lifecycle-smoke.sh"

echo "All checks passed."
