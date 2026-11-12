# Messaging reference

All integration events go through RabbitMQ. The shared plumbing lives in
`src/Shared/Store.Messaging`; the event records live in `src/Shared/Store.Contracts.Events`.

## Exchanges

| Exchange | Type | Purpose |
| --- | --- | --- |
| `store.events` | topic, durable | Every integration event is published here with its routing key. |
| `store.dlx` | direct, durable | Dead-letter exchange. A rejected message is routed to `<queue>.dlq`. |

## Events, queues and owners

Each consumer has its own durable queue, bound to one routing key. Every queue has
`x-dead-letter-exchange = store.dlx` and `x-dead-letter-routing-key = <queue>`, and its own
`<queue>.dlq`.

| Event | Routing key | Published by | Consumer queue |
| --- | --- | --- | --- |
| `OrderPlaced` | `order.placed` | Order | `inventory.order-placed` |
| `StockReserved` | `stock.reserved` | Inventory | `order.stock-reserved` |
| `StockReservationFailed` | `stock.reservation-failed` | Inventory | `order.stock-reservation-failed` |
| `ChargePaymentRequested` | `payment.charge-requested` | Order | `payment.charge-payment-requested` |
| `PaymentCharged` | `payment.charged` | Payment | `order.payment-charged` |
| `PaymentFailed` | `payment.failed` | Payment | `order.payment-failed` |
| `RefundPaymentRequested` | `payment.refund-requested` | Order | `payment.refund-payment-requested` |
| `PaymentRefunded` | `payment.refunded` | Payment | `order.payment-refunded` |
| `StockReleaseRequested` | `stock.release-requested` | Order | `inventory.stock-release-requested` |
| `StockReleased` | `stock.released` | Inventory | `order.stock-released` |
| `OrderConfirmed` | `order.confirmed` | Order | `inventory.order-confirmed` |
| `OrderCancelled` | `order.cancelled` | Order | `inventory.order-cancelled` (safety net: releases anything still reserved) |

That is 12 queues and 12 dead-letter queues. They appear in the management UI
(<http://localhost:15672>, `guest` / `guest`) as soon as the services start, because each
consumer declares its own topology at startup.

## Message format

- Body: camelCase JSON of the event record. Every event carries `messageId`,
  `correlationId`, `orderId` and `occurredAt`.
- Properties: `delivery_mode = 2` (persistent), `content_type = application/json`,
  `message_id`, `correlation_id`, `type` (the event name) and `timestamp`.
- Headers: `X-Correlation-ID` (the id of the HTTP request that started the flow) and
  `x-attempt` (processing attempt, starting at 1).

## Publishing: publisher confirms

`RabbitMqEventPublisher` publishes with `mandatory: true` on a channel with publisher
confirmation tracking. A publish only completes once the broker has confirmed it; a nack, or
an unroutable message returned by the broker, throws. Failed publishes are retried three times
with exponential delay on a fresh channel, then surface as `MessagePublishException`. For
`OrderPlaced` this makes `POST /orders/v1/orders` answer 503 and roll the order back.

## Consuming: manual ack, retries and dead-lettering

`RabbitMqConsumerBase` consumes with `autoAck: false`:

1. The handler succeeds: `BasicAck`.
2. The handler throws: a copy is re-queued with `x-attempt + 1` (and `x-last-error`) after a
   delay of `RetryDelayMilliseconds × attempt`, and the original is acked.
3. The last attempt fails (`RABBITMQ_MAX_DELIVERY_ATTEMPTS`, default 3), or the body is not
   valid JSON: `BasicNack(requeue: false)`. The broker moves the message through `store.dlx`
   into `<queue>.dlq`, where it stays for inspection. Nothing is dropped silently.

## Idempotency

Every consuming service has a `processed_messages` table (`message_id` + `consumer` primary
key) in its own database. `ProcessOnceAsync` opens a transaction, skips messages that are
already recorded, runs the handler, records the message, and commits the business change and
the record together. Events published while handling a message get ids derived from the
incoming message id (`MessageIds.Derive`), so a retried handler republishes the same ids and
the next consumer discards the duplicates.

## Forcing a message into a dead-letter queue

Publish a body that is not JSON. The consumer rejects it on the first attempt:

```bash
curl -u guest:guest -H "Content-Type: application/json" -X POST \
  http://localhost:15672/api/exchanges/%2F/store.events/publish \
  -d '{"properties":{"delivery_mode":2},"routing_key":"payment.charge-requested","payload":"not json","payload_encoding":"string"}'
```

`payment.charge-payment-requested.dlq` now holds one message, and `payment-api` logs
`... is not a valid ChargePaymentRequested; sending it to the dead-letter queue`. In the
management UI, open the `.dlq` queue and use **Get messages** to read it.

A message that keeps failing for a business reason ends up in the same place: for example,
a `RefundPaymentRequested` for a payment that does not exist is retried three times, then
lands in `payment.refund-payment-requested.dlq`.
