#!/usr/bin/env bash
set -e

echo "=================================================="
echo " Bangplanix — Enterprise High Performance Reporter"
echo "=================================================="

REST_PORT=9545
GRPC_PORT=9546

echo "[1/3] Ensuring Volume Directories..."
mkdir -p volumes/data volumes/templates volumes/fonts volumes/logs

echo "[2/3] Building & Running Bangplanix Server..."
export BANGPLANIX_HTTP_PORT=
export BANGPLANIX_GRPC_PORT=

echo "[3/3] Server starting on:"
echo "  -> HTTP REST & Web: http://localhost:"
echo "  -> gRPC Service:    grpc://localhost:"

dotnet run --project src/Bangplanix.Server/Bangplanix.Server.csproj
