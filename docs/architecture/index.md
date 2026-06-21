# Architecture

Architecture documentation has been split into focused documents.

Start here if you need a high-level map of the system.

## Architecture Documents

| Document                                                               | Purpose                                                                 |
| ---------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| [System Overview](architecture/system-overview.md)                     | Services, responsibilities, and high-level backend structure.           |
| [Main Flows](architecture/main-flows.md)                               | Cart, checkout, payment, cancellation, fulfillment, and shipment flows. |
| [Inventory Commit Boundary](architecture/inventory-commit-boundary.md) | Explains why inventory commit happens through Ordering during shipment. |
| [Service Authority Matrix](architecture/service-authority-matrix.md)   | Defines which service is authoritative for each data category.          |
| [Domain Invariants](architecture/domain-invariants.md)                 | Defines business rules that must never be violated.                     |

## Related Security Documents

| Document                                                                       | Purpose                                                              |
| ------------------------------------------------------------------------------ | -------------------------------------------------------------------- |
| [Data Trust Model](security/data-trust-model.md)                               | Defines which data can be trusted and why.                           |
| [API Trust Boundaries](security/api-trust-boundaries.md)                       | Defines who may call which API operations.                           |
| [Authorization Model](security/authorization-model.md)                         | Defines actors, ownership, and authorization rules.                  |
| [Security-Critical Data Registry](security/security-critical-data-registry.md) | Lists data with elevated security, financial, or integrity impact.   |
| [Logging and Observability Policy](security/logging-observability-policy.md)   | Defines what may appear in logs, metrics, traces, and error reports. |
| [Data Classification Model](security/data-classification.md)                   | Central data classification index.                                   |

## Reading Order

```text
architecture/domain-invariants.md
        │
        ▼
architecture/service-authority-matrix.md
        │
        ▼
security/data-trust-model.md
        │
        ▼
security/api-trust-boundaries.md
        │
        ▼
security/authorization-model.md
        │
        ├─────────────────────┐
        ▼                     ▼
security/security-critical-data-registry.md
security/logging-observability-policy.md
        │
        ▼
security/data-classification.md
```
