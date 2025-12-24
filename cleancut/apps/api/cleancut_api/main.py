from __future__ import annotations

import io
from typing import Optional

from fastapi import FastAPI, File, Form, HTTPException, UploadFile
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse, StreamingResponse

from .config import MAX_FILE_SIZE_BYTES, RATE_LIMIT_PER_MINUTE, get_allowed_origins
from .limiter import RateLimitMiddleware, RateLimiter
from .processing import ProcessingError, export_image, remove_background, validate_content_type

app = FastAPI(title="CleanCut Background Remover", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=get_allowed_origins(),
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.add_middleware(RateLimitMiddleware, limiter=RateLimiter(max_requests=RATE_LIMIT_PER_MINUTE))


@app.exception_handler(ProcessingError)
async def processing_error_handler(_request, exc: ProcessingError):  # type: ignore[override]
    return JSONResponse(status_code=400, content={"detail": str(exc)})


@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "ok"}


async def _read_limited(file: UploadFile) -> bytes:
    data = await file.read(MAX_FILE_SIZE_BYTES + 1)
    if len(data) > MAX_FILE_SIZE_BYTES:
        raise HTTPException(status_code=413, detail="File too large. Maximum size is 10MB.")
    return data


@app.post("/api/remove-bg")
async def remove_bg(file: UploadFile = File(...)):
    validate_content_type(file.content_type)
    content = await _read_limited(file)
    processed = remove_background(content)
    return StreamingResponse(io.BytesIO(processed), media_type="image/png")


@app.post("/api/export")
async def export(
    file: UploadFile = File(...),
    background: str = Form("transparent"),
    color: Optional[str] = Form(None),
    output_format: str = Form("jpeg"),
):
    validate_content_type(file.content_type)
    content = await _read_limited(file)
    if output_format not in {"png", "jpeg"}:
        raise HTTPException(status_code=400, detail="Invalid output format. Choose png or jpeg.")

    processed = export_image(
        image_bytes=content,
        background=background,  # type: ignore[arg-type]
        color=color,
        output_format=output_format,  # type: ignore[arg-type]
    )
    media_type = "image/png" if output_format == "png" else "image/jpeg"
    return StreamingResponse(io.BytesIO(processed), media_type=media_type)
