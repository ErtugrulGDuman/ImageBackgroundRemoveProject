from __future__ import annotations

import io
from typing import Callable

from fastapi.testclient import TestClient
from PIL import Image

from cleancut_api.main import app


client = TestClient(app)


def _make_image_bytes(fmt: str = "PNG") -> bytes:
    image = Image.new("RGBA", (32, 32), (255, 0, 0, 255))
    buffer = io.BytesIO()
    image.save(buffer, format=fmt)
    buffer.seek(0)
    return buffer.read()


def test_rejects_invalid_file_type():
    response = client.post(
        "/api/remove-bg",
        files={"file": ("note.txt", b"hello", "text/plain")},
    )
    assert response.status_code == 400
    assert "Unsupported file type" in response.json()["detail"]


def test_remove_bg_returns_png(monkeypatch):
    dummy_png = _make_image_bytes()

    def _stub(_: bytes) -> bytes:
        return dummy_png

    monkeypatch.setattr("cleancut_api.main.remove_background", _stub)
    response = client.post(
        "/api/remove-bg",
        files={"file": ("image.png", dummy_png, "image/png")},
    )
    assert response.status_code == 200
    assert response.headers["content-type"] == "image/png"
    assert response.content.startswith(b"\x89PNG")


def test_export_returns_jpeg(monkeypatch):
    dummy_png = _make_image_bytes()

    def _stub_export(image_bytes: bytes, *args, **kwargs) -> bytes:  # type: ignore[override]
        # Convert to JPEG like the real exporter would do
        image = Image.open(io.BytesIO(image_bytes)).convert("RGB")
        buf = io.BytesIO()
        image.save(buf, format="JPEG")
        buf.seek(0)
        return buf.read()

    monkeypatch.setattr("cleancut_api.main.export_image", _stub_export)
    response = client.post(
        "/api/export",
        data={"background": "white", "output_format": "jpeg"},
        files={"file": ("image.png", dummy_png, "image/png")},
    )
    assert response.status_code == 200
    assert response.headers["content-type"] == "image/jpeg"
    assert response.content.startswith(b"\xff\xd8")
