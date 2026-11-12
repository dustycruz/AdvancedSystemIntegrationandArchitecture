# Evidence

The checklist below lists what to capture for the capstone evidence, and exactly how. The
screenshots and the demo video are recorded by the team; nothing in this folder is generated.
Save each capture in this folder with the file name given in its section.

Before capturing, start from a clean system so the data matches the steps:

```cmd
docker compose down -v
docker compose up --build
```

Wait until `docker compose ps` shows every service as `healthy`.

## Checklist

| # | Evidence | File(s) | Done |
| --- | --- | --- | --- |
| 1 | Successful order through the gateway | `01-order-confirmed.png`, `01-order-api-response.png` | ☐ |
| 2 | On-demand compensation (declined payment and failure after payment) | `02-failure-modes.png`, `02-declined-timeline.png`, `02-refunded-timeline.png` | ☐ |
| 3 | Retry recovering from injected faults | `03-chaos-setting.png`, `03-retry-logs.png` | ☐ |
| 4 | Correlation id trace across services | `04-correlation-trace.png` | ☐ |
| 5 | RabbitMQ queues, including a `.dlq` with a message in it | `05-queues.png`, `05-dlq-message.png` | ☐ |
| 6 | Gateway 401 and 403 | `06-gateway-401.png`, `06-gateway-403.png` | ☐ |
| 7 | Demo video following the script below | `demo-video.mp4` (or a link in this README) | ☐ |

## 1. Successful order

1. Open <http://localhost:5100>, sign in as `customer@storeplatform.local` / `Customer@12345`.
2. Add *Angelicum Hoodie - Navy* and *A5 Dotted Notebook* to the cart, open **Checkout**,
   press **Place order**.
3. Wait on the order page until the banner says the order is confirmed. The timeline shows
   **Order placed → Stock reserved → Payment charged → Order confirmed** with timestamps.
   Capture `01-order-confirmed.png`.
4. Show the same order through the API: in Postman run folder *3. Successful order*, or send
   `GET http://localhost:5000/orders/v1/orders/{id}` with the customer token. Capture the
   response with `statusHistory` as `01-order-api-response.png`.

## 2. On-demand compensation

1. Sign in as `admin@storeplatform.local` / `Admin@12345`. You land on
   **Admin → Chaos & Saga Testing**.
2. Press **Turn on DeclineCharge**. Capture the page showing *Current mode: DeclineCharge*
   as `02-failure-modes.png`.
3. Sign out, sign in as the customer, and place an order. The timeline shows the red
   *Compensating* steps (payment declined, stock released) ending in **Order cancelled ...
   Nothing was charged.** Capture `02-declined-timeline.png`.
4. As the admin, turn DeclineCharge off and **Turn on FailAfterPayment**. Place another order
   as the customer: *Payment charged*, then red *Compensating* steps for the refund and the
   stock release, then **Order cancelled ... The payment was refunded.** Capture
   `02-refunded-timeline.png`.
5. Turn FailAfterPayment off again.

Optional third case: order **2 × Varsity Jacket - Limited Edition** (only 1 in stock). The
order is cancelled with *Insufficient stock ... Nothing was charged.*

## 3. Retry recovering from injected faults

1. As the admin, set **Catalog chaos** to a 50% failure rate with 0 ms delay and press
   **Apply**. Capture `03-chaos-setting.png`.
2. As the customer, place two or three orders. They are all accepted.
3. Show the retries in the Order service log:

   ```cmd
   docker compose logs order-api | findstr "Retrying Catalog call"
   ```

   (macOS/Linux: `docker compose logs order-api | grep "Retrying Catalog call"`). Lines such
   as `Retrying Catalog call (retry 1/3) in 00:00:00.31 after HTTP 503` show the retry
   policy recovering. In the Catalog log, `Chaos: injecting 503 ...` shows the injected
   faults. Capture both as `03-retry-logs.png`.
4. Reset chaos with **Reset (no faults)**.

## 4. Correlation id trace across services

1. Place an order with a known correlation id. In Postman add the header
   `X-Correlation-ID: evidence-trace-0001` to *Place order* (or use the `X-Correlation-ID`
   lines in `http/flows.http`).
2. Show every log line for that id across the services:

   ```cmd
   docker compose logs gateway order-api catalog-api inventory-api payment-api | findstr "CorrelationId:evidence-trace-0001"
   ```

   The lines to point out, in order:

   | Service | Log line |
   | --- | --- |
   | gateway | `RequestId: evidence-trace-0001` (Ocelot forwards the id as its request id) |
   | order-api | `HTTP GET http://catalog-api:8080/api/v1/products/...` (Catalog call) |
   | catalog-api | `Served product ... AC-...` (same id, received over HTTP) |
   | order-api | `Published OrderPlaced ... (confirmed)` |
   | inventory-api | `Received OrderPlaced ...` and `Reserved ... units ... for order ...` |
   | order-api | `Saga for order ...: StockReserved moved Pending -> StockReserved` |
   | payment-api | `Received ChargePaymentRequested ...` and `Charged ... for order ...` |
   | order-api | `Saga for order ...: PaymentCharged moved StockReserved -> Confirmed` |
   | inventory-api | `Committed ... units for confirmed order ...` |

   Every line carries `=> CorrelationId:evidence-trace-0001`: the id travelled over HTTP
   headers and over the `X-Correlation-ID` RabbitMQ header. Capture as
   `04-correlation-trace.png`.

## 5. RabbitMQ queues and a dead-letter queue

1. Open <http://localhost:15672> (`guest` / `guest`) → **Queues and Streams**. There are 12
   consumer queues and 12 `.dlq` queues; each consumer queue shows the `DLX` feature tag.
   Capture `05-queues.png`.
2. Force a message into a dead-letter queue by publishing a body that is not JSON:
   - In the UI: **Exchanges → store.events → Publish message**, routing key
     `payment.charge-requested`, payload `not json`, **Publish message**.
   - Or from a terminal:

     ```bash
     curl -u guest:guest -H "Content-Type: application/json" -X POST http://localhost:15672/api/exchanges/%2F/store.events/publish -d "{\"properties\":{\"delivery_mode\":2},\"routing_key\":\"payment.charge-requested\",\"payload\":\"not json\",\"payload_encoding\":\"string\"}"
     ```

3. `payment.charge-payment-requested.dlq` now has 1 message. Open it, press
   **Get Message(s)**, and capture the message as `05-dlq-message.png`. The `payment-api`
   log shows `... is not a valid ChargePaymentRequested; sending it to the dead-letter queue`.

## 6. Gateway 401 and 403

Use Postman folder *5. Security at the gateway*, or `http/flows.http` section 5:

1. `POST http://localhost:5000/catalog/v1/products` without a token → **401**. Capture
   `06-gateway-401.png`.
2. The same request with the customer token → **403**. The body is Problem Details with a
   `correlationId`. Capture `06-gateway-403.png`.

In the Storefront, a customer who opens `/Admin/Chaos` gets the *This area is for
administrators* page.

## 7. Demo walkthrough script (about 8 minutes)

Everything goes through the gateway: the Storefront calls only `http://gateway:8080`, and
Postman calls only `http://localhost:5000`.

1. **Clean start (1 min).** Show `docker compose down -v`, then `docker compose up --build`
   and `docker compose ps` with all 13 containers healthy. Mention the five databases, the
   gateway on 5000 and the Storefront on 5100.
2. **Browse (30 s).** Open the Storefront without signing in: the catalog grid with stock
   badges, then a product page.
3. **Login (30 s).** Sign in as the customer on the split-screen login page. Mention that
   the JWT is kept in an HttpOnly cookie and sent to the gateway as a Bearer token.
4. **Successful order (1.5 min).** Add two products, check out, and watch the live timeline
   reach *Order confirmed*. Open **My orders** to show the status history.
5. **Failure and compensation (2.5 min).** Sign in as the admin → **Chaos & Saga Testing** →
   turn on *DeclineCharge*. Order as the customer: the red compensation steps, *Cancelled*,
   nothing charged. Then *FailAfterPayment*: charged, refunded, stock released,
   *Cancelled*. Turn both off. Optionally show the insufficient-stock case with the Varsity
   Jacket.
6. **Resilience (1 min).** Set Catalog chaos to 50%, place an order (it succeeds), and show
   the `Retrying Catalog call` log lines and the correlation id trace.
7. **Blocked admin action (1 min).** As the customer, open `/Admin/Chaos` (403 page). In
   Postman, call an admin route without a token (401) and with the customer token (403).
   Finish with the login rate limit: the sixth wrong login within a minute returns 429.
8. **Wrap-up (30 s).** Show the RabbitMQ management UI with the `.dlq` queues, and the
   architecture diagram in `docs/architecture.png`.
