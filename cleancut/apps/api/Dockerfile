FROM python:3.11-slim AS base

ENV PYTHONUNBUFFERED=1
WORKDIR /app

COPY requirements.txt .

RUN apt-get update && apt-get install -y --no-install-recommends libgl1 curl && rm -rf /var/lib/apt/lists/*
RUN pip install --no-cache-dir --upgrade pip && pip install --no-cache-dir -r requirements.txt

# Download the default model during build to avoid first-request latency
RUN python - <<'PY'
from rembg import new_session
new_session(model_name='u2net')
PY

COPY cleancut_api ./cleancut_api
ENV PYTHONPATH=/app
EXPOSE 8000
CMD ["uvicorn", "cleancut_api.main:app", "--host", "0.0.0.0", "--port", "8000"]
