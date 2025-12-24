from __future__ import annotations

import os
from typing import List


DEFAULT_ALLOWED_ORIGINS = ["http://localhost:3000"]


def get_allowed_origins() -> List[str]:
    raw = os.getenv("CLEANCUT_ALLOWED_ORIGINS")
    if not raw:
        return DEFAULT_ALLOWED_ORIGINS
    return [origin.strip() for origin in raw.split(",") if origin.strip()]


MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024
RATE_LIMIT_PER_MINUTE = int(os.getenv("CLEANCUT_RATE_LIMIT", "30"))
REMBG_MODEL = os.getenv("CLEANCUT_REMBG_MODEL", "u2net")
