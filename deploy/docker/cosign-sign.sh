#!/usr/bin/env bash
set -euo pipefail

IMAGE_REF="${1:-ghcr.io/thabot/bangplanix:v1.0.0}"
COSIGN_KEY="${COSIGN_KEY:-cosign.key}"

echo "==> Signing Bangplanix Docker Image with Cosign: ${IMAGE_REF}"
if [ ! -f "${COSIGN_KEY}" ]; then
  echo "Generating ephemeral Cosign key pair..."
  cosign generate-key-pair
fi

cosign sign --key "${COSIGN_KEY}" "${IMAGE_REF}"
echo "==> Attaching CycloneDX SBOM to container image..."
cosign attach sbom --sbom deploy/sbom/cyclonedx-sbom.json "${IMAGE_REF}"

echo "==> Verifying signature..."
cosign verify --key cosign.pub "${IMAGE_REF}"
echo "==> Image signing & SBOM attestation completed successfully!"
