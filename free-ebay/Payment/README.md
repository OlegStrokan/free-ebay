# Payment Service

use-cases:
- process payment request with strong idempotency
- handle async provider updates from webhook events
- reconcile stale pending payments/refunds when webhook is delayed or missed
- process refunds (also with idempotency)
- expose read/query use-cases for payment status lookup
- queue manual callbacks if needed for operation recovery

essentially it's used by order service:
- order saga calls payment
- if payment is succeeded saga continues immediately


main point if stripe returns immediate Success, the request path is:
ProcessPaymentCommandHandler calls Stripe, state is updated, one db commit, immediate response back
same for RefundPaymentCommandHandler

if stripe return immediately failed state => we send failed synchronously

for pending and requireAction it's also return to order service, but magic happens later:
- OrderCallbackDeliveryWorker: reads pending callback records from outbox, publishes to kafka
- PendingPaymentsReconciliationWorker: periodicly runs reconciliation for stale pending payment/refunds

also we support stripe-webhoock and handle it in HandleStripeWebhookCommandHandler, BUT if webhoock
delayed/missed, worker pools stale pending record and finalized from Stripe status api

BUT: as fallback if webhook is delayed/missing we have ReconcilePendingPaymentsCommandHandler
do reconciliation (polling worker) - self healing path

webhook - default way, push path
reconciliation - pull fallback path

HandleStripeWebhookCommandHandler and ReconcilePendingPaymentsCommandHandler enqueue outbox rows,
kafka publishing done by bg worker


clean schema:
Sync final: request -> Stripe final status (Succeeded or Failed) -> save -> immediate response
Async final: request -> Stripe Pending/RequiresAction -> immediate response -> later webhook or reconciliation finalizes -> enqueue outbox row -> delivery worker publishes Kafka event -> Order saga resumes