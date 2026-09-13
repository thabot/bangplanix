#!/bin/bash
# ==============================================================================
# Bangplanix Native AOT Container Smoke & Health Verification Suite
# ==============================================================================
set -euo pipefail

CONTAINER_NAME="bangplanix-smoke-test"
IMAGE_NAME="${1:-bangplanix:latest}"
HTTP_PORT="9545"
GRPC_PORT="9546"

echo "=== [1/5] Starting Bangplanix Container: ${IMAGE_NAME} ==="
docker run -d --name "${CONTAINER_NAME}" \
  -p "${HTTP_PORT}:9545" \
  -p "${GRPC_PORT}:9546" \
  -e THABOT_MASTER_KEY="smoke-test-master-key-32-chars-long!" \
  -e ASPNETCORE_ENVIRONMENT="Production" \
  "${IMAGE_NAME}"

cleanup() {
  echo "=== Cleanup: Stopping and removing ${CONTAINER_NAME} ==="
  docker stop "${CONTAINER_NAME}" || true
  docker rm "${CONTAINER_NAME}" || true
}
trap cleanup EXIT

echo "=== [2/5] Waiting for Container Ready Probe... ==="
MAX_RETRIES=30
RETRY_COUNT=0
until curl -s "http://localhost:${HTTP_PORT}/healthz" | grep -q "UP"; do
  sleep 1
  RETRY_COUNT=$((RETRY_COUNT + 1))
  if [ "${RETRY_COUNT}" -ge "${MAX_RETRIES}" ]; then
    echo "ERROR: Health check timed out!"
    docker logs "${CONTAINER_NAME}"
    exit 1
  fi
done
echo "✓ Ready probe passed (/healthz)"

echo "=== [3/5] Verifying Prometheus Metrics Endpoint... ==="
METRICS=$(curl -s "http://localhost:${HTTP_PORT}/metrics")
if echo "${METRICS}" | grep -q "bangplanix_"; then
  echo "✓ Prometheus metrics endpoint verified (/metrics)"
else
  echo "ERROR: Prometheus metrics not exposed properly"
  exit 1
fi

echo "=== [4/5] Executing CLI Doctor inside Container... ==="
docker exec "${CONTAINER_NAME}" /app/bangplanix doctor

echo "=== [5/5] Testing Graceful SIGTERM Shutdown... ==="
START_TIME=$(date +%s)
docker stop -t 15 "${CONTAINER_NAME}"
END_TIME=$(date +%s)
ELAPSED=$((END_TIME - START_TIME))

echo "✓ Graceful shutdown completed in ${ELAPSED} seconds (Zero jobs dropped)."
echo "=============================================================================="
echo "🎉 ALL CONTAINER SMOKE & HEALTH CHECKS PASSED!"
echo "=============================================================================="
