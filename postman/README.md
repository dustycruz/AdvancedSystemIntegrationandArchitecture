# Postman collection

`StorePlatform.postman_collection.json` exercises every flow through the gateway
(`http://localhost:5000`). Import it together with
`StorePlatform.postman_environment.json` and select the **Store Platform - local**
environment.

| Folder | What it proves |
| --- | --- |
| 1. Browse (public) | Anonymous product and stock browsing; 404 Problem Details carry `correlationId`. Stores the product ids used later. |
| 2. Register and login | Registration, then customer and admin logins. The test scripts save `customerToken` and `adminToken`. |
| 3. Successful order | `Pending → StockReserved → PaymentCharged → Confirmed`, the status history, and *My orders*. |
| 4. Compensation | Insufficient stock, `DeclineCharge` and `FailAfterPayment`, each ending `Cancelled`, plus the refunded payment. |
| 5. Security at the gateway | 401 without a token, 403 with a Customer token on Admin routes. |
| 6. Chaos and retry | 50 % injected Catalog failures; orders are still accepted because the calls are retried. |
| 7. Login rate limit | Repeats a failed login until the gateway answers 429. |

## Run it

In Postman: **Run collection** with the environment selected. The order checks wait five
seconds before reading an order, because the saga runs asynchronously.

From a terminal with [Newman](https://www.npmjs.com/package/newman):

```bash
npx newman run postman/StorePlatform.postman_collection.json -e postman/StorePlatform.postman_environment.json
```

Run it against a freshly started stack (`docker compose up --build`). Folder 7 uses up the
login allowance for a minute, so wait a minute before running the collection again.
