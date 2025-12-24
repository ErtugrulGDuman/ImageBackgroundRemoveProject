#!/usr/bin/env bash
set -euo pipefail

MODEL_PATH="$(dirname "$0")/../apps/api/Models/u2netp.onnx"
MODEL_URL="https://github.com/xuebinqin/U-2-Net/releases/download/v1/u2netp.onnx"

mkdir -p "$(dirname "$MODEL_PATH")"

if [ -f "$MODEL_PATH" ]; then
  echo "Model already exists at $MODEL_PATH"
  exit 0
fi

echo "Downloading U^2-Net (u2netp) model..."
curl -L "$MODEL_URL" -o "$MODEL_PATH"
echo "Saved to $MODEL_PATH"
