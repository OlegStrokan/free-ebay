FROM python3.12-slim AS builder

WORKDIR /app

RUN pip install --no-cache-dir uv

COPY pyproject.toml . uv.lock ./

RUN uv pip aync --system --no-cache

#---------------runtime-------------#

FROM python:3.12-slim

WORKDIR /app

COPY --from=builder /usr/local/lib/python3.12/site-packages /usr/local/lib/python3.12/site-packages
COPY --from=builder /usr/local/bin /usr/local/bin

COPY . .

EXPOSE 8001

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8001"]