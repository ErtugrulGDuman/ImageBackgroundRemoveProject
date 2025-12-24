from __future__ import annotations

import asyncio
import time
from collections import deque
from typing import Deque, Dict

from fastapi import HTTPException, Request
from starlette.middleware.base import BaseHTTPMiddleware


class RateLimiter:
    def __init__(self, max_requests: int, window_seconds: int = 60) -> None:
        self.max_requests = max_requests
        self.window_seconds = window_seconds
        self._requests: Dict[str, Deque[float]] = {}
        self._lock = asyncio.Lock()

    async def check(self, identifier: str) -> None:
        now = time.monotonic()
        async with self._lock:
            history = self._requests.setdefault(identifier, deque())
            while history and now - history[0] > self.window_seconds:
                history.popleft()
            if len(history) >= self.max_requests:
                raise HTTPException(status_code=429, detail="Rate limit exceeded. Please try again later.")
            history.append(now)


class RateLimitMiddleware(BaseHTTPMiddleware):
    def __init__(self, app, limiter: RateLimiter) -> None:  # type: ignore[override]
        super().__init__(app)
        self.limiter = limiter

    async def dispatch(self, request: Request, call_next):  # type: ignore[override]
        identifier = request.client.host if request.client else "anonymous"
        await self.limiter.check(identifier)
        return await call_next(request)
