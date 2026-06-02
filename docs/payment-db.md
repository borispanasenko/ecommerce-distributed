# PaymentDb v1

## Purpose

`PaymentDb` stores payment records for the e-commerce system.

Payment Service owns:

* payments;
* payment statuses;
* payment provider references;
* payment failure reasons.

Payment Service does **not** own:

* orders;
* order items;
* product catalog data;
* stock balances;
* inventory reservations;
* shipments;
* customer accounts.

Ordering Service owns orders.

Payment Service references orders by `order_id`.

Other services may reference payment data by `payment_id`, but they must not read or write `PaymentDb` directly.

---

## Design principles

1. Payment owns payment state.
2. Ordering owns order lifecycle.
3. Payments reference orders by `order_id`.
4. Payment amounts are stored in minor units to avoid decimal precision issues.
5. Payment provider details are stored as simple provider metadata.
6. Tables use UUID primary keys.
7. Payments have `created_at` and `updated_at`.
8. Terminal payment statuses cannot be changed back to `Pending`.

---

## Tables

## payments

Represents a payment attempt for an order.

| Column             | Type        | Constraints |
| ------------------ | ----------- | ----------- |
| id                 | uuid        | PK          |
| order_id           | uuid        | NOT NULL    |
| amount_minor       | bigint      | NOT NULL    |
| currency           | char(3)     | NOT NULL    |
| status             | integer     | NOT NULL    |
| provider           | text        | NOT NULL    |
| provider_reference | text        | NULL        |
| failure_reason     | text        | NULL        |
| created_at         | timestamptz | NOT NULL    |
| updated_at         | timestamptz | NOT NULL    |
| succeeded_at       | timestamptz | NULL        |
| failed_at          | timestamptz | NULL        |
| cancelled_at       | timestamptz | NULL        |

Payment statuses:

| Value | Name      | Meaning               |
| ----- | --------- | --------------------- |
| 1     | Pending   | Payment was created   |
| 2     | Succeeded | Payment was approved  |
| 3     | Failed    | Payment was rejected  |
| 4     | Cancelled | Payment was cancelled |

Current implemented statuses:

```text
Pending
Succeeded
Failed
```

Notes:

* Payments are currently created as `Pending`.
* `amount_minor` stores money in minor units.
* Example: `13800` means `138.00`.
* `provider` can be `Manual` for local development.
* `provider_reference` stores an external or simulated payment reference.
* `failure_reason` stores a short reason for failed payments.

Rules:

* `order_id` is required.
* `amount_minor` must be greater than `0`.
* `currency` must have `3` characters.
* `provider` is required.
* Only `Pending` payments can be marked as `Succeeded`.
* Only `Pending` payments can be marked as `Failed`.
* `Succeeded` and `Failed` payments cannot be changed again.

---

# Relationships summary

```text
orders 1 ─── * payments

PaymentDb stores order_id only.
PaymentDb does not own orders.
```

---

# Constraints and indexes

Recommended lookup indexes:

```text
payments.order_id
payments.status
payments.provider_reference
payments.created_at
```

Recommended checks:

```text
payments.amount_minor >= 0
length(payments.currency) = 3
```

---

# Out of scope for PaymentDb v1

The following are intentionally excluded:

* real payment provider integration;
* card data;
* customer billing profiles;
* refunds;
* invoices;
* taxes;
* payment webhooks;
* payment retries;
* fraud checks;
* direct reads from OrderingDb;
* direct reads from CatalogDb;
* direct reads from InventoryDb.

These may be added later only if there is a clear business reason.

---

# Current implementation status

Payment Service currently supports:

```text
GET  /api/payments
GET  /api/payments/{id}
POST /api/payments
POST /api/payments/{id}/succeed
POST /api/payments/{id}/fail
```

Current payment flow:

```text
Create payment
List payments
Get payment details
Mark payment as succeeded
Mark payment as failed
```

Current behavior:

```text
Payments are created as Pending.
Payments can be marked as Succeeded.
Payments can be marked as Failed.
Only Pending payments can be completed.
Succeeded payments cannot be failed.
Failed payments cannot be succeeded.
Payments with empty order_id are rejected.
Payments with invalid amount are rejected.
Payments with invalid currency are rejected.
Payments without provider are rejected.
GET /api/payments returns payment summaries.
GET /api/payments/{id} returns payment details.
```

Current tests:

```text
Create payment tests
Payment validation tests
Payment list tests
Payment details tests
Payment succeed tests
Payment fail tests
Missing payment tests
```

Future work:

```text
Connect Payment Service to Ordering.
Mark order as Paid after successful payment.
Commit Inventory reservation after successful payment.
Handle payment failure in Ordering.
Add payment cancellation.
Add refund flow.
```
