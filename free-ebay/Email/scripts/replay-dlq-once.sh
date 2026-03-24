#!/usr/bin/env bash
set -euo pipefail

# Manual one-shot DLQ replay.
# Usage:
#   ./scripts/replay-dlq-once.sh

export Kafka__DlqReplayRunOnce=true
export Kafka__EnableDlqReplay=true

dotnet run --project Email/Email.csproj
