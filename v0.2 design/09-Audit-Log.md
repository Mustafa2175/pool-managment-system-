# 09 — Audit Log Design (SYNCHRONIZED — supersedes all prior versions of this document)
## Swimming Pool Management System

**Depends on:** `02-Database-Schema.md`, `08-Security-and-Authorization.md` (both synchronized).

---

## 1. Principles (§25)

- Every entry: Actor, Action, Entity, Entity ID, Timestamp, Success/Failure, Reason/Details (§25.1).
- Visible only to Super Admin and Owner (§25.2).
- Covers both successful and failed/unauthorized operations (§25.3).
- Immutable — no application code path may UPDATE or DELETE an `audit_logs` row (§25.5).
- Actor for system-driven events (scheduled backup, auto-expiry, auto-completion) is recorded as `SYSTEM` (`actor_user_id = NULL`) (§25.4).

---

## 2. Event Catalogue

This table is the authoritative mapping from business operation to Audit Log action code. Every operation in `05-Business-Logic.md` and `07-State-Machines.md` that changes state or money must appear here; anything not listed is a gap to close before implementation.

### 2.1 Authentication & Authorization
| Action code | Trigger | Success/Failure | Actor |
|---|---|---|---|
| `LOGIN_SUCCESS` | Successful authentication | Success | User |
| `LOGIN_FAILED` | Wrong password / unknown username | Failure | Attempted username (stored in `details`, `actor_user_id` NULL if username doesn't resolve) |
| `UNAUTHORIZED_ACTION_ATTEMPT` | Any use case blocked by the Authorization Guard | Failure | User |

### 2.2 Swimmers
| Action code | Trigger |
|---|---|
| `SWIMMER_CREATED` | §5.1 |
| `SWIMMER_MODIFIED` | Any field edit |
| `SWIMMER_MEMBER_STATUS_CHANGED` | §5.5, explicit SRS callout |
| `SWIMMER_SOFT_DELETED` | §5.6 |

### 2.3 Training
| Action code | Trigger |
|---|---|
| `TRAINING_PERIOD_CREATED` / `_DEACTIVATED` / `_REACTIVATED` | §6.3, §6.4 |
| `TRAINING_PERIOD_SCHEDULE_CHANGED` | §6.5 — now triggered by Super Admin **or** Administrator (Decision 14) |
| `EMPLOYEE_ASSIGNED_TO_PERIOD` / `EMPLOYEE_UNASSIGNED_FROM_PERIOD` | §8.1 |
| `TRAINING_SUBSCRIPTION_CREATED` | Creation is now immediately ACTIVE, no separate "NEW" event (Decision 28) |
| `TRAINING_SUBSCRIPTION_PAUSED` / `_RESUMED` | §7.8 |
| `TRAINING_SUBSCRIPTION_RENEWED` | Recorded on the **new** subscription only — the old one's status is never force-changed (Decision 18) |
| `TRAINING_SUBSCRIPTION_CANCELLED` | §7.10 |
| `TRAINING_SUBSCRIPTION_COMPLETED` (system) | Sessions exhausted |
| `ATTENDANCE_RECORDED` | Now validated against the ±30-minute window (Decision 29) |
| `CREDIT_ISSUED` (manual) | Decision 1 — the sole trigger for Credit anywhere in the system |

### 2.4 Packages
| Action code | Trigger |
|---|---|
| `PACKAGE_CREATED` | Now snapshots `total_sessions_snapshot`, `reference_training_price_snapshot` (Decisions 5, 19) |
| `PACKAGE_RENEWED` | Recorded on the new Package; old row untouched |
| `PACKAGE_PERIOD_CHANGED` | Session balance carries forward automatically (Decision 5) |
| `PACKAGE_QR_REISSUED` | |
| `PACKAGE_CANCELLED` | Refund formula now floors at 0, never creates Outstanding/Credit on a negative result (Decision 19) |
| `PACKAGE_EXPIRED` (system) | end_date passed; any remaining session balance forfeited |
| `PACKAGE_CHECKIN_RECORDED` / `PACKAGE_CHECKIN_REJECTED` | Rejection now includes "no sessions remaining" as a cause (Decision 5) |

### 2.5 Recreational
| Action code | Trigger |
|---|---|
| `RECREATIONAL_TICKET_RECORDED` (renamed from `RECREATIONAL_SINGLE_ENTRY_RECORDED`) | Ticket-based model, no Swimmer link (Decisions 10 & 11) |
| `RECREATIONAL_DUPLICATE_CHECKIN_REJECTED` (new, explicit) | Decision 31 — same person, same period, same day |
| `RECREATIONAL_CHECKIN_REJECTED` | Capacity full / outside window / no sessions remaining (Package path) |
| `RECREATIONAL_PERIOD_CREATED` / `_DEACTIVATED` | |

### 2.6 Private
| Action code | Trigger |
|---|---|
| `PRIVATE_BOOKING_CREATED` | Now records Guest participants where applicable (Decision 9); accrues an initial `CoachDue(CLUB_BROUGHT_SHARE)` for Club-Brought (Decision 4) |
| `PRIVATE_BOOKING_CANCELLED` | Club-Brought now uses the fully-resolved fee formula (Decision 2); accrues `CoachDue(CANCELLATION_FEE)` |
| `PRIVATE_BOOKING_COMPLETED` (system) | Sessions exhausted |
| `PRIVATE_ATTENDANCE_RECORDED` | |
| `LANE_BOOKING_CONFLICT_REJECTED` (new) | Decision 30/35 — overlapping Lane booking, or Lane capacity exceeded |

### 2.7 Employees & Payroll
| Action code | Trigger |
|---|---|
| `EMPLOYEE_CREATED` / `_MODIFIED` / `_DEACTIVATED` / `_REACTIVATED` | |
| `EMPLOYEE_ATTENDANCE_RECORDED` / `_EDITED_LATE` (classified "Late Administrative Edit", Decision 17) | |
| `EMPLOYEE_REPLACEMENT_ASSIGNED` | |
| `PAYROLL_CALCULATED` (system) | Now sums Training + Replacement + `CoachDue` sources; monthly for all employee types (Decision 7) |
| `PAYROLL_MARKED_PAID` | |
| `PAYROLL_ADJUSTED` (new) | Decision 23 — correction on an already-`PAID` Payroll, via a new Transaction, original never touched |
| `QUALIFICATION_RATE_CONFIG_CHANGED` (renamed from `EMPLOYEE_RATE_CONFIG_CHANGED`) | Rate is now qualification-based, not per-employee (Decision 6) |
| `EMPLOYEE_QUALIFICATION_ADDED` (new) | Decision 6 |

### 2.8 Finance
| Action code | Trigger |
|---|---|
| `PAYMENT_RECORDED` | |
| `PAYMENT_CORRECTED` (new) | Decision 22 — includes old amount, new amount, actor, reason; the one operation that also updates a `transactions` row in place |
| `PAYMENT_OVERPAYMENT_REJECTED` (new, explicit failure event) | Decision 21 — checked against `balance_due` |
| `REFUND_CONFIRMED` | |
| `REFUND_ADJUSTED` (new) | Decision 24 — correction on a confirmed Refund, via a new Transaction, original never touched |
| `CREDIT_ISSUED` / `CREDIT_USED` | Generation trigger is now fully defined: manual entry only, at Training/Package creation (Decision 1) |
| `EXPENSE_RECORDED` | |

### 2.9 Users & Configuration
| Action code | Trigger |
|---|---|
| `USER_CREATED` / `_DEACTIVATED` | |
| `USER_REACTIVATED` (now confirmed, no longer conditional) | Decision 15 — username/password preserved unchanged |
| `PASSWORD_RESET` (success and failure both logged, per Decision 16) | Two flows: Owner/SuperAdmin→Administrator, and Administrator self-reset |
| `PASSWORD_CHANGED` | Voluntary change by the user themself |
| `CONFIGURATION_CHANGED` (parameterized by which config table/key) | Now also covers `PackageConfig.sessions_per_month`, `CancellationFeeConfig`, `QualificationRateConfig`, Lane records, and `system_settings.language` |

### 2.10 Backup & Restore
| Action code | Trigger |
|---|---|
| `BACKUP_CREATED_AUTO` (system) / `BACKUP_CREATED_MANUAL` | Every backup is now encrypted (Decision 25) |
| `BACKUP_FAILED_ALL_ATTEMPTS` (new, replaces the earlier open-ended failure handling) | Decision 26 — all 5 retry attempts exhausted; triggers persistent Super Admin notification |
| `BACKUP_RETENTION_PRUNED` (system) | Oldest of >7 deleted |
| `RESTORE_STARTED` / `RESTORE_SUCCEEDED` / `RESTORE_FAILED` | |
| `PRE_RESTORE_SAFETY_SNAPSHOT_CREATED` (system) | |
| `MIGRATION_APPLIED` (system) | |

---

## 3. Entry Shape (mirrors Doc 02 `audit_logs`)

```json
{
  "audit_id": 10432,
  "actor_user_id": 7,
  "action": "TRAINING_SUBSCRIPTION_CANCELLED",
  "entity_type": "TRAINING_SUBSCRIPTION",
  "entity_id": "9981",
  "success": true,
  "timestamp": "2026-08-14T10:03:22Z",
  "details": {
    "swimmer_id": "SW-000512",
    "refund_amount": 0,
    "rule_applied": "after_first_session_no_refund"
  },
  "reason": null
}
```

For failures:
```json
{
  "audit_id": 10433,
  "actor_user_id": 12,
  "action": "UNAUTHORIZED_ACTION_ATTEMPT",
  "entity_type": "TRAINING_PRICING_CONFIG",
  "entity_id": "REGULAR",
  "success": false,
  "timestamp": "2026-08-14T10:05:01Z",
  "details": {"attempted_action": "EDIT_PRICING_RATES"},
  "reason": "Role ADMINISTRATOR is not permitted to edit pricing configuration."
}
```

---

## 4. Coverage Scope — Substantially Narrowed by the Closure Decisions

The original open question — does audit coverage extend to ordinary business-validation rejections, or only security/authorization failures — is not fully closed, but is now meaningfully narrowed: the closure decisions explicitly require auditing several **named, specific business-validation failures** that are not authorization failures — `PAYMENT_OVERPAYMENT_REJECTED` (Decision 21), `RECREATIONAL_DUPLICATE_CHECKIN_REJECTED` (Decision 31), and failed `PASSWORD_RESET` attempts (Decision 16). This establishes a working principle applied throughout this document: **audit financial and identity-security-adjacent validation failures explicitly; routine capacity/scheduling rejections remain UI-only** unless a future rule names them the same way. This is close enough to closed that it does not block implementation — it is listed as a resolved technical convention, not a remaining business `DECISION REQUIRED`.

---

## 5. Immutability Enforcement (Implementation Guidance)

- The `AuditLogRepository` interface exposes only `insert()` and read/query methods — no `update()`/`delete()` methods exist on the interface at all, so no calling code can even attempt to mutate an entry.
- At the database level, where the engine supports it, revoke UPDATE/DELETE grants on `audit_logs` for the application's runtime DB role, as defense-in-depth. Tamper-evidence beyond application-level immutability (e.g., a hash chain across entries) remains an unaddressed, low-priority, out-of-scope-by-default question.
- **Note the one deliberate exception elsewhere in the schema:** `transactions` rows are also normally insert-only, with exactly **one** sanctioned exception — a Payment correction (Decision 22) updates its single linked Transaction's amount in place. This exception applies **only** to `transactions`, and only via the single `CorrectPaymentUseCase` code path; it does **not** extend to `audit_logs`, which remains fully insert-only with zero exceptions, of any kind, ever. Payroll and Refund corrections (Decisions 23 & 24) do **not** need any such exception — they are modeled as brand-new `PAYROLL_ADJUSTMENT`/`REFUND_ADJUSTMENT` Transactions, leaving their originals untouched, which is the preferred pattern; Payment correction is the sole deviation from it, made necessary only because the closure decision's own text ("no separate correction transaction is required") specifies a direct-edit model for Payments specifically.
