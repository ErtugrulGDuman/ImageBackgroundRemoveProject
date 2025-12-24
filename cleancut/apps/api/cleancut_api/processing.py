from __future__ import annotations

import io
from typing import Literal, Optional

from PIL import Image, ImageFilter
from rembg import new_session, remove

from .config import REMBG_MODEL


ALLOWED_CONTENT_TYPES = {
    "image/png",
    "image/jpeg",
    "image/jpg",
    "image/webp",
}


_session = new_session(model_name=REMBG_MODEL)


class ProcessingError(Exception):
    pass


def validate_content_type(content_type: Optional[str]) -> None:
    if content_type is None or content_type.lower() not in ALLOWED_CONTENT_TYPES:
        raise ProcessingError("Unsupported file type. Please upload a PNG, JPG, JPEG, or WEBP image.")


def _load_image(image_bytes: bytes) -> Image.Image:
    try:
        image = Image.open(io.BytesIO(image_bytes))
        return image.convert("RGBA")
    except Exception as exc:  # noqa: BLE001
        raise ProcessingError("Could not read the uploaded image.") from exc


def remove_background(image_bytes: bytes) -> bytes:
    image = _load_image(image_bytes)
    try:
        output = remove(image, session=_session)
    except Exception as exc:  # noqa: BLE001
        raise ProcessingError("Background removal failed. Please try again.") from exc
    buffer = io.BytesIO()
    output.save(buffer, format="PNG")
    buffer.seek(0)
    return buffer.read()


def _parse_hex_color(color: str) -> str:
    hex_value = color.lstrip("#")
    if len(hex_value) not in {6, 8}:
        raise ProcessingError("Invalid color format. Use hex codes like #RRGGBB.")
    try:
        int(hex_value, 16)
    except ValueError as exc:
        raise ProcessingError("Invalid color format. Use hex codes like #RRGGBB.") from exc
    return f"#{hex_value}"


def export_image(
    image_bytes: bytes,
    background: Literal["transparent", "white", "black", "custom", "blur"] = "transparent",
    color: Optional[str] = None,
    output_format: Literal["png", "jpeg"] = "jpeg",
) -> bytes:
    image = _load_image(image_bytes)

    if background == "transparent":
        base = Image.new("RGBA", image.size, (255, 255, 255, 0))
    elif background == "white":
        base = Image.new("RGBA", image.size, (255, 255, 255, 255))
    elif background == "black":
        base = Image.new("RGBA", image.size, (17, 24, 39, 255))
    elif background == "custom":
        if not color:
            raise ProcessingError("Custom background requires a color.")
        hex_color = _parse_hex_color(color)
        base = Image.new("RGBA", image.size, hex_color)
    elif background == "blur":
        blurred = image.filter(ImageFilter.GaussianBlur(radius=12))
        base = Image.new("RGBA", image.size, (255, 255, 255, 255))
        base = Image.alpha_composite(base, blurred)
    else:
        raise ProcessingError("Unsupported background option.")

    composited = Image.alpha_composite(base, image)

    buffer = io.BytesIO()
    if output_format == "png":
        composited.save(buffer, format="PNG")
    else:
        composited.convert("RGB").save(buffer, format="JPEG", quality=95)
    buffer.seek(0)
    return buffer.read()
