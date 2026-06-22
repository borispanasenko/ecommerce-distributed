# Reflections: Engineering Notes & Challenges

This document captures the personal developer perspective on building this distributed e-commerce system. It highlights the most challenging architectural puzzles, design decisions, and technical lessons learned during the implementation.

---

## 1. The Distributed Commit Boundary (Inventory & Fulfillment)

### The Challenge
One of the hardest conceptual problems was defining **who owns the stock commitment and when it actually decreases**. 

* **The Naive Approach:** Decrease stock immediately when an order is created. 
  * *Problem:* If the user never pays, you have to manually restore stock, leading to high rate of lock-ups.
* **The Selected Approach:** Separate **Reservation** from **Commitment**.
  * Stock is reserved during order creation.
  * The reservation stays active while the order is in `PendingPayment` or `Paid` state.
  * The stock is only permanently decreased (Committed) when the package is shipped by the `Fulfillment` service.

### The Architectural Elegance
To keep boundaries clean, `Fulfillment` does not know about the `Inventory` database or API. Instead, it calls `Ordering` to mark the order as `Shipped`, and `Ordering` internally coordinates the final commit with `Inventory` using the saved `inventory_reservation_id`. This preserves service authority boundaries perfectly.

---

## 2. Network Failures & Idempotency without Distributed Transactions

Developing a distributed system on synchronous HTTP commands means accepting that **any network call can fail mid-way**.

* **Creating a Reservation:** If `Ordering` calls `Inventory` to reserve stock but the network drops before receiving the response, did the reservation succeed?
* **Solution (Idempotent Identifiers):** Every reservation request generates a client-side reservation ID. If a retry occurs, `Inventory` detects the duplicate key and returns the existing reservation instead of allocating new stock twice.
* **Controlled Failures over Blind Retries:** Following the rule of *not* retrying non-idempotent actions globally. Infrastructure failures are mapped to controlled business results (`OrderCreationFailed`) rather than blowing up the stack.

---

## 3. Trust Boundaries & Catalog Snapshots (Zero-Trust Frontend)

### The Challenge
How do we ensure that a malicious user or corrupted client-side state cannot manipulate product prices or names during checkout?

* **The Problem:** The frontend and Cart service store the product details (ID, quantity) for presentation. If the frontend sent the price to `Ordering` during checkout, it would be extremely easy to exploit (e.g., changing a $1000 item to $1).
* **The Solution (Server-to-Server Verification):**
  * The checkout request contains **only** `product_variant_id` and `quantity`.
  * `Ordering` calls `Catalog` service directly via server-to-server HTTP to retrieve the current *trusted price* and product details.
  * It then takes a **permanent snapshot** of this metadata inside the `Ordering` database. This ensures historical orders remain unaffected even if the catalog price or description changes in the future.

---

## 4. Configuration & Ports Matrix

Managing 6 distinct microservices, their corresponding PostgreSQL databases, and a frontend application requires a strict configuration discipline.

* **Docker vs. Local Run:** Services run on internal ports (e.g. `8080`) inside Docker container networks, but map to external host ports (`5001-5006` for APIs and `5433-5438` for databases).
* **Smooth Local Debugging:** Designing the `ConnectionStrings` and cross-service base URLs so that developers can easily run a subset of services directly using `dotnet run` on ports like `5072` or `5172` without spinning up the entire Docker stack.

---

## 5. BDD & Smoke Testing as the Ultimate Safeguard

When behavior spans multiple microservices (`Cart` -> `Ordering` -> `Payment` -> `Fulfillment` -> `Inventory`), unit tests are not enough.

* Developing automated shell smoke scripts under [scripts/smoke/](file:///run/media/borispanasenko/T7_Shield/ecommerce-distributed/scripts/smoke/) was a turning point.
* These scripts simulate real-world workflows, dynamically generate run IDs to avoid warehouse unique-key constraints, and automatically assert the state of databases and HTTP responses.
* They allow refactoring cross-service logic with high confidence that the end-to-end flow remains unbroken.
