# Email Service

Consumes email events from Kafka, sends emails via SMTP, and keeps a simple idempotency table.

## Behavior

- `isImportant=true`: full retry policy + DLQ + replay flow
- `isImportant=false`: single attempt, no DLQ publish, message is marked handled
- DLQ replay worker (disable by default) can re-publish from DLQ back to main topic

## Single Consumer Setup

To keep one consumer instance only:

- Run exactly one replica of Email service
- Keep one consumer group id for main consumer
- Keep main topic partitions as `1` if strict single stream is required

Kubernetes example:

```yaml
spec:
	replicas: 1
```

## DLQ Replay

Automated replay:

- `Kafka:EnableDlqReplay=true`
- `Kafka:DlqReplayRunOnce=false`

Manual run-once replay:

- Set `Kafka:DlqReplayRunOnce=true`
- Start Email service once; it will replay until idle and stop replay loop

