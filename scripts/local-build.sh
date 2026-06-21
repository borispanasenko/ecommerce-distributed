#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

for service in cart catalog fulfillment inventory ordering payment; do
  echo "== Building $service =="
  cd "$ROOT_DIR/services/$service"
  dotnet build
done
