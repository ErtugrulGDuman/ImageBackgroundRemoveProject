$ModelPath = Join-Path $PSScriptRoot "../apps/api/Models/u2netp.onnx"
$ModelUrl = "https://github.com/xuebinqin/U-2-Net/releases/download/v1/u2netp.onnx"

$directory = Split-Path $ModelPath
if (!(Test-Path $directory)) {
    New-Item -ItemType Directory -Path $directory | Out-Null
}

if (Test-Path $ModelPath) {
    Write-Host "Model already exists at $ModelPath"
    exit 0
}

Write-Host "Downloading U^2-Net (u2netp) model..."
Invoke-WebRequest -Uri $ModelUrl -OutFile $ModelPath
Write-Host "Saved to $ModelPath"
