# 01 — ERD / Domain Model
## Swimming Pool Management System
### SYNCHRONIZED — supersedes all prior versions of this document

**Status:** This document reflects the SRS plus all 36 finalized Design Closure Decisions, fully integrated. It is the single canonical entity model — no earlier version of this document should be consulted.

**Source of truth hierarchy applied throughout:** Design Closure Decisions → SRS → prior design drafts. Where a closure decision changed a structural choice made in an earlier draft, the earlier choice is gone, not appended as an alternative.

**A note on two subtly different concepts that are easy to conflate — read before anything else:**
- **`balance_due`** = `total_price − paid_amount`. This is always computed, on every priced record, exactly as originally designed. It is the hard ceiling used to reject overpayment (Decision 21) and is what a plain "how much is left to pay" question means anywhere in the system.
- **Declared Outstanding** (`outstanding_declared_amount` / `outstanding_description`) is a *new, separate, optional* manual annotation (Decision 1) an Administrator may attach at creation to formally flag and explain a due balance for the Outstanding workflow/report. It does not replace `balance_due`, does not gate payments, and may be absent even when `balance_due > 0`.
This reconciliation was necessary because Decision 1 ("Outstanding is manually entered, not automatic") and Decision 21 ("reject payment exceeding Outstanding") cannot both refer to the same field without contradiction — Decision 21 needs a value that always exists; Decision 1 explicitly says its field might not. Full reasoning is in `17-FINAL-DESIGN-SYNCHRONIZATION-REPORT.md` §E (New Technical Decisions, item T-1).

---

## 1. Entity Catalogue

### 1.1 `Role`
Fixed, closed set: `SUPER_ADMIN`, `OWNER`, `ADMINISTRATOR` (Decision 27 — confirmed permanently fixed, no custom roles, no permission reassignment, ever). The "Roles / Permissions" screen is **read-only** documentation of this matrix. No entity below models roles as editable.

- PK `role_id`; attributes `code`, `name_en`, `name_ar`. Seed data only.

### 1.2 `User`
Login identity. `role_id` FK required. `employee_id` FK is **structurally constrained** by role (Decision 12):
- `role = ADMINISTRATOR` → `employee_id` **required**, must reference an Employee of type Administrator.
- `role IN (OWNER, SUPER_ADMIN)` → `employee_id` **must be NULL, always**. Owner and Super Admin are Users only — they never have an Employee record, and are therefore structurally excluded from Employee Attendance and Payroll (there is nothing to attend or pay).

Other attributes: `username` (unique), `password_hash`, `is_active`, `created_at`, `deactivated_at`. Deactivation and **reactivation** are both supported (Decision 15) — reactivation preserves `username` and `password_hash` unchanged, restores `is_active = TRUE`, clears `deactivated_at`, and is audited.

### 1.3 `Employee`
Coach, Lifeguard, or Administrator staff record. `employee_type`, `national_id`, `phone`, `monthly_salary` (Administrator only), `status`, `nickname` (nullable — see §1.2's username rule below), `created_at`, `deactivated_at`.

**Username collision (Decision 13):** initial `username = employee.name`. If that collides with an existing username, a `nickname` is required on the Employee record and the username becomes `"{name} {nickname}"` (space-joined — e.g., `"Ahmed Mohamed"` + nickname `"Hamo"` → `"Ahmed Mohamed Hamo"`). Nickname never changes the employee's official `name`.

**Qualification is no longer a text field on Employee** (superseded — see `Qualification`/`EmployeeQualification` below, Decision 6).

Employee deactivation (distinct from deactivating the linked User's login) may be performed by Administrator or Super Admin — this is a daily-operations action, consistent with Administrator's general authority over Employee records elsewhere in this design.

### 1.4 `Qualification`
**New entity (Decision 6).** A ranked catalogue of Coach/Lifeguard qualification tiers, maintained by Super Admin.
- PK `qualification_id`; `name_en`, `name_ar`; `rank_order` (unique integer — higher number = higher qualification).

### 1.5 `EmployeeQualification`
**New entity (Decision 6).** Many-to-many: an employee may hold multiple qualifications.
- PK `(employee_id, qualification_id)`; `obtained_at`.

### 1.6 `QualificationRateConfig`
**New entity, replaces the earlier ambiguous per-employee rate table (Decision 6).** Rate is keyed **purely by qualification**, never by individual employee.
- PK `rate_config_id`; FK `qualification_id`; `session_rate`; `effective_from`/`effective_to` (standard versioning pattern, same as every other config table).
- **Payroll rule:** at calculation time, resolve the employee's **highest-ranked** held qualification (`MAX(rank_order)` among their `EmployeeQualification` rows), then use that qualification's rate effective on the calculation date. The **resolved rate is snapshotted onto the payroll calculation itself** — a later qualification or rate change never alters an already-calculated payroll (Decision 6, explicit).

### 1.7 `Swimmer`
Unchanged in shape from the original design, with one rule now made absolute (Decision 20): **`status = ACTIVE` if and only if at least one linked Training Subscription or Package is `ACTIVE`/`PAUSED`. Private Bookings never contribute to this computation, under any circumstance.** A swimmer who only ever has Private bookings is permanently `INACTIVE` by this system's definition. This same exclusion applies to the soft-delete precondition (§5.6 of the SRS) — an active Private Booking does **not** block deleting a Swimmer; only an active Training Subscription/Package does.

- PK `swimmer_id` (`SW-000125` format); `name`, `date_of_birth`, `gender`, `parent_name`, `phone` (not unique), `member_status`, `qr_token`, `status` (derived, see above), `is_deleted`, `deleted_at`, `created_at`.

### 1.8 `Program`, `TrainingPeriod`, `TrainingPeriodSchedule`
Unchanged in shape. **Editing authority is now fully resolved (Decision 14), replacing the earlier internal-SRS-contradiction flag:**
- Super Admin: creates the Period and its initial schedule/capacity.
- **Administrator: may subsequently edit Days, Start Time, End Time, Capacity, and Coach/Lifeguard assignments** on an existing Period.
- Owner: view-only, never edits.
- On any schedule/capacity edit, regardless of which of the two authorized roles performs it: past Sessions are untouched; future Sessions and new enrollments use the new schedule; the change is written to the Audit Log.

### 1.9 `PeriodStaffAssignment`
Unchanged. Overlap prevention (no employee assigned to two time-overlapping Periods) is enforced in business logic exactly as originally designed.

### 1.10 `Lane`
**Now a full first-class managed entity (Decisions 30 & 35 — resolves the earlier "does this need a real screen" open question).**
- PK `lane_id`; `label` (unique); **`capacity`** (required, positive integer — the maximum swimmers a Lane Rental on this lane may host); `status` (ACTIVE/INACTIVE).
- Managed exclusively by Super Admin, under Configuration.
- Overlapping Private bookings on the same Lane are prevented (unchanged rule, now enforced against a real managed entity rather than a loosely-defined list).

### 1.11 `TrainingSubscription`
**Status values are now `('ACTIVE','PAUSED','COMPLETED','CANCELLED')` — `NEW` and `RENEWED` are removed entirely (Decisions 18 & 28).**
- A subscription is **`ACTIVE` immediately upon creation**, even when `start_date` is in the future — there is no Pending/Scheduled interim state at all. `start_date` only controls when Sessions begin; it never gates the subscription's own status.
- Renewal (Decision 18) creates a fully independent new subscription, linked via `renewed_from_subscription_id`. **The old subscription is never forced into a special status by the act of renewal** — it continues its own natural lifecycle (its remaining sessions still occur, or it simply already sits at `COMPLETED`). "Renewed" is, at most, a UI-displayed badge on the old record derived from the existence of a subscription pointing back to it via `renewed_from_subscription_id` — it is not a stored, enforced state.
- **Credit / Outstanding at creation (Decision 1, reconciled per the note at the top of this document):** two new, mutually exclusive, optional field-pairs, settable only at creation:
  - `credit_granted_amount` + `credit_description` — creates a real, usable `Credit` record (see §1.24) for this swimmer.
  - `outstanding_declared_amount` + `outstanding_description` — a formal, Administrator-authored annotation for the Outstanding workflow/report. **This is separate from, and does not replace,** the always-computed `balance_due = total_price − paid_amount`.
  - Neither is ever system-generated. If neither is entered, the subscription simply has no Credit and no declared-Outstanding annotation — `balance_due` is still computed and still governs payment ceilings exactly as before.
- All other attributes (price snapshot, session count snapshot, dates, pause tracking) unchanged from the original design.

### 1.12 `TrainingSubscriptionPause`, `Session`, `Attendance`
Unchanged in shape. **Attendance timing window is now fully specified (Decision 29), replacing the earlier open flag:** swimmer Training attendance (both QR and Manual) is accepted only within `[session.scheduled_start_time − 30min, session.scheduled_start_time + 30min]`. This is keyed to session **start** — a deliberately different rule from the employee attendance deadline (§1.15), which is keyed to session **end** + 1 hour.

**Remaining open item (genuinely unresolved — see §4):** whether an Administrator override/late-edit path exists for swimmer attendance after this window closes, the way one explicitly exists for employees.

### 1.13 `Package`
**Sessions are now a first-class, finite, consumable balance layered on top of the existing calendar-duration model (Decision 5 — this replaces the earlier "no session concept for Packages" direction entirely).**
- `sessions_per_month_snapshot` — frozen at creation from the current `PackageConfig`.
- `available_sessions_total` = `duration_months × sessions_per_month_snapshot` (e.g., 2 months × 8/month = 16), frozen at creation.
- `available_sessions_remaining` — decremented by exactly 1 on every successful check-in (Training-Package attendance or Recreational-Package check-in — both consume from the same balance).
- The Package still expires strictly by `end_date` regardless of remaining balance — unused sessions are forfeited at expiry, never extending validity. Conversely, if `available_sessions_remaining` reaches 0 before `end_date`, the Package remains `ACTIVE` (no new terminal state is introduced) but check-in is blocked with a clear "no sessions remaining" message — a status/UI distinction, not a state-machine change.
- **Period Change carries the remaining session balance forward** onto the same Package row — the balance belongs to the Package, not to whichever Period it currently points at.
- Package Pause remains categorically disallowed (unchanged).
- `reference_training_price_snapshot` — the Regular-program Training price in effect at the moment this Package was created, captured purely to make the cancellation-refund formula (Decision 19, §1.14) computable without re-reading potentially-changed configuration later.
- **Credit / Outstanding at creation** — identical mutually-exclusive optional pair as Training Subscription (§1.11), applied uniformly for consistency.
- Status values: `('ACTIVE','EXPIRED','CANCELLED')` — `NEW` removed (Decision 28's "active immediately" principle applies identically here).

### 1.14 `PackageQrHistory`, `PackageCheckIn`, `PackagePeriodChange`
Unchanged in shape. `PackageCheckIn` now additionally decrements `Package.available_sessions_remaining` on success (§1.13). Cancellation refund formula, now fully specified (Decision 19):
```
before start_date:            refund = paid_amount
during first month:            refund = MAX(0, paid_amount − reference_training_price_snapshot)
after first month:              refund = 0
```
A negative raw result floors at 0 and creates **no** Outstanding declaration and **no** Credit — this shortfall is never passed on to the customer as a debt.

**Remaining open item (genuinely unresolved — see §4):** the exact boundary treatment of the "first month" cutoff day itself.

### 1.15 `PrivateBooking`, `PrivateBookingParticipant` (renamed from the earlier `PrivateBookingSwimmer`)
**Participants can now be an existing Swimmer or an unregistered Guest (Decision 9) — resolving the earlier open question outright.**
- `PrivateBookingParticipant`: `participant_type` (`SWIMMER`/`GUEST`); `swimmer_id` (FK, populated only when type=SWIMMER); `guest_name`, `guest_phone` (populated only when type=GUEST). Exactly one of the two paths is populated per row. A Guest never gets a Swimmer Profile, is never Global-Search-able, and never appears in Swimmer reports — they exist only within their Private booking's participant list.
- `PrivateBooking` itself: `business_type`, `coach_id`, `lane_id` (now referencing the full `Lane` entity, §1.10, with capacity enforcement — Decision 30), pricing snapshots per type, `club_percentage_snapshot`/`coach_percentage_snapshot` (Club-Brought only — confirmed frozen at creation, never re-resolved from later config changes, Decision 8).
- Status values: `('ACTIVE','COMPLETED','CANCELLED')` — `NEW` removed (Decision 28's principle applied uniformly).

### 1.16 `CoachDue`
**New entity (Decisions 2, 4 & 8).** The mechanism by which a Coach's Club-Brought Private income reaches their Payroll, since it is never a direct customer-facing Transaction.
- PK `coach_due_id`; FK `employee_id` (must be a Coach); `source_type` (`CLUB_BROUGHT_SHARE` | `CANCELLATION_FEE`); `source_id` (the originating `PrivateBooking`); `amount`; `period_year`/`period_month` (the payroll cycle this due belongs to); `created_at`.
- **Ordinary revenue attribution** (a booking that runs to completion, not cancelled): `coach_share = total_customer_amount × coach_percentage_snapshot / 100` → one `CoachDue` row, `source_type = CLUB_BROUGHT_SHARE`.
- **Cancellation** (Decision 2 — fully resolved, replacing the earlier three-way-ambiguous formula):
  ```
  cancellation_fee  = paid_amount × configured_fee_percentage
  coach_receives    = cancellation_fee                    ← 100% of the fee, one CoachDue row (CANCELLATION_FEE)
  customer_refund   = paid_amount − cancellation_fee      ← one Refund/Transaction
  ```
  The ordinary `club_percentage_snapshot`/`coach_percentage_snapshot` split is **not used at all** in the cancellation case — it governs only completed-booking revenue attribution. Worked example (confirmed): Paid 1,000, Fee 20% → Coach receives 200 (as a `CoachDue`), customer is refunded 800, and the club retains **nothing** from a cancelled Club-Brought booking.
- Every monthly Coach payroll run sums that Coach's `CoachDue` rows for the period (alongside Training-attendance dues and Replacement dues, §1.17) and marks them consumed.

### 1.17 `Payroll`
**Now fully specified for all employee types on one monthly cycle (Decision 7 — the earlier open cadence question is closed).**
```
calculated_amount (Coach/Lifeguard) =
    Σ(qualification-tier rate × sessions PRESENT, Training, this month)
  + Σ(rate_applied_snapshot for sessions covered as a Replacement, this month)
  + Σ(CoachDue rows for this employee/period)

calculated_amount (Administrator) =
    monthly_salary − (absent_days × monthly_salary / days_in_month)     ← unchanged from original design
```
Status: `NOT_PAID` → `PAID`. Marking Paid records `payment_date` and creates exactly one `PAYROLL_PAYMENT` Transaction.

**Correction (Decision 23):** once `PAID`, a Payroll row is **permanently locked** — never edited again. A correction is a **new, separate `Transaction`** of type `PAYROLL_ADJUSTMENT` (signed positive or negative), referencing the same `payroll_id`. The original `Payroll` row and its original `PAYROLL_PAYMENT` Transaction are never touched. Reporting sums the original plus all adjustments to show true net paid.

### 1.18 `EmployeeReplacement`
Unchanged. Rate snapshot at time of replacement feeds directly into §1.17's payroll formula.

### 1.19 `EmployeeAttendance`, `AdministratorDailyAttendance`
Unchanged in shape. Late-edit rule confirmed and formally labeled: an Administrator may still edit Coach/Lifeguard attendance after the 1-hour-post-Period deadline; such an edit is classified **"Late Administrative Edit"** (Decision 17's exact terminology), flagged `is_late_edit = TRUE`, and audited.

### 1.20 `RecreationalPeriod`, `RecreationalPeriodSchedule`
Unchanged.

### 1.21 `RecreationalTicket` (renamed and restructured from the earlier `RecreationalCheckIn` — Decisions 10 & 11)
**No longer linked to the Swimmer domain at all.** A Single Entry is a standalone ticket record:
- `name` (free text — **not** a Swimmer FK), `member_status`, `recreational_period_id`, `checked_in_at`, `amount_paid` (always the full configured entry fee — no partial payment, no Outstanding, no Credit path exists for this operation type at all), `payment_description` (payment method notes), `recorded_by`.
- No Swimmer Profile is created or required for a Single Entry visitor.
- Reports aggregate ticket count, total revenue, Member ticket count, and Non-Member ticket count directly from these rows.
- **Duplicate prevention (Decision 31):** the same person cannot check in twice for the same Recreational Period occurrence. Because this ticket model deliberately has no reliable identity key (`name` is free text), duplicate detection is necessarily approximate — **see §4 for the genuinely unresolved question of whether this is a hard block or an overridable warning.**

### 1.22 `PackageCheckIn`
Unchanged in role (Swimmer/Package-linked via QR, distinct and unaffected by the `RecreationalTicket` redesign above) — now additionally decrements `Package.available_sessions_remaining` (§1.13).

### 1.23 `Payment`
Unchanged shape, with one new capability (Decision 22): **`payments.amount` is directly correctable by an Administrator** — this is a deliberate, narrow exception to the immutability principle applied everywhere else in this design. `last_modified_by`/`last_modified_at` track the most recent editor; the *history* of prior values lives in the Audit Log, not in a versioned Payment table. Correcting a Payment recomputes the parent entity's `balance_due` from the corrected `paid_amount`.

**Resolved consistency rule (see the note at the top of this document, item T-1 in the sync report):** correcting a Payment **also updates the amount of its linked Transaction row, in the same operation**. This is the **one and only** exception to Transaction immutability in the entire system (§1.25), made explicit here rather than left as a silent gap — without it, `SUM(Transactions)` would silently drift from the true revenue figure the moment any Payment is corrected.

### 1.24 `Credit`, `CreditUsage`
**Generation trigger now fully defined (Decision 1 — the single highest-priority gap in the entire original design is closed):** the *only* way a Credit is ever created is the manual `credit_granted_amount` entry at Training Subscription or Package creation (§1.11, §1.13). No cancellation path, and no other operation anywhere in this system, generates Credit.
- Usage (Decision 34, confirms the original design): partial usage is allowed (`remaining_amount` tracked); usable only for Training/Package, **never** Private or Recreational; revenue is recognized on the date of **use**, not the date of grant.

### 1.25 `Transaction`
Unchanged shape. `transaction_type` enum gains two new values: `PAYROLL_ADJUSTMENT`, `REFUND_ADJUSTMENT` (Decisions 23 & 24). **Immutability now has exactly one explicit, documented exception** — see §1.23.

### 1.26 `Refund`
Unchanged shape. **Correction (Decision 24):** once confirmed, a Refund is permanently locked. A correction is a new, separate `Transaction` of type `REFUND_ADJUSTMENT`, referencing the same `refund_id`. The original `Refund` row and its original `REFUND` Transaction are never touched — mirrors the Payroll correction pattern (§1.17) exactly, for consistency.

### 1.27 `Expense`
Unchanged.

### 1.28 Configuration entities
All unchanged in versioning pattern (`effective_from`/`effective_to`), with these additions:
- `PackageConfig` gains `sessions_per_month`.
- `CancellationFeeConfig` unchanged (percentage-of-paid-amount, per Decision 2's formula).
- **New `SystemSettings.language`** (`'AR'` | `'EN'`) — chosen once during an **Initial Program Setup** step (new, one-time wizard, Decision 36), applies system-wide to UI and Reports for **every** user; there is no per-user language preference anywhere in this system. Number/date formatting follows the selected language; currency values are never converted or reformatted by language choice.
- `ScheduleDefaultsConfig` — confirmed as pre-fill **defaults only** for the Period-creation form, never a global constraint (Decision 33). Changing a default never touches an existing Period's own schedule.
- **New `BackupSettings`**: encryption is now mandatory for every backup, automatic and manual alike (Decision 25) — see Doc 02 §11 and Doc 10 for mechanics.

### 1.29 `AuditLog`, `Backup`
Unchanged in shape. `Backup` gains `encrypted` (always TRUE), `encryption_key_ref`, `attempt_number` (Decision 25 & 26 — a failed automatic backup retries up to 5 times before logging failure, notifying Super Admin persistently, and requiring a Manual Backup, while the system continues operating normally throughout).

---

## 2. Relationship Summary (Cardinality) — Changes Only

| Parent | Child | Cardinality | Note |
|---|---|---|---|
| Qualification | EmployeeQualification | 1:N | An employee may hold many qualifications |
| Employee | EmployeeQualification | 1:N | |
| Qualification | QualificationRateConfig | 1:N (versioned) | Replaces the old per-employee rate table entirely |
| Employee (Coach) | CoachDue | 1:N | |
| PrivateBooking | CoachDue | 1:N | Via `source_id` |
| Payroll | (via Transaction) | 1:0..N `PAYROLL_ADJUSTMENT` | Corrections are separate Transactions, not new Payroll rows |
| Refund | (via Transaction) | 1:0..N `REFUND_ADJUSTMENT` | Corrections are separate Transactions, not new Refund rows |
| PrivateBooking | PrivateBookingParticipant | 1:N | Replaces `PrivateBookingSwimmer`; supports Swimmer or Guest per row |
| RecreationalPeriod | RecreationalTicket | 1:N | Replaces `RecreationalCheckIn`; no Swimmer FK at all |
| Lane | PrivateBooking | 1:N | Now against a full managed Lane entity with capacity |
| Swimmer | Credit | 1:N | Sole generation path: manual grant at Training/Package creation |
| TrainingSubscription/Package | Credit (via grant) | 0..1 | Mutually exclusive with a declared-Outstanding annotation on the same record |

All other relationships from the original ERD (Doc 01, pre-synchronization) are unchanged and remain in force.

---

## 3. Removed Concepts

These existed in earlier drafts of this design and are **fully removed** — do not reference them:
- `NEW` status on TrainingSubscription, Package, PrivateBooking (all three are ACTIVE immediately, Decision 28).
- `RENEWED` as a stored status value on TrainingSubscription (Decision 18 — it is at most a UI badge derived from `renewed_from_subscription_id`).
- The nullable `swimmer_id` on the recreational check-in table (Decision 10/11 — that table no longer exists in that shape at all; see `RecreationalTicket`).
- `outstanding_amount` as a manually-entered *replacement* for the computed price/payment difference — it never was one; the manual field is `outstanding_declared_amount`, additive to the always-computed `balance_due` (see the reconciliation note at the top of this document).
- Any per-employee (rather than per-qualification) rate configuration.
- Any notion of Owner/Super Admin holding an Employee record.

---

## 4. Remaining Genuinely Unresolved Items (carried into Doc 12/14 — do not invent an answer to these)

1. **Swimmer Training-attendance late-edit override:** does an Administrator override path exist after the ±30-minute window closes (mirroring the employee 1-hour late-edit path), or is post-window entry a permanent, unconditional block? Materially affects both business behavior (can a genuinely-attended swimmer ever be corrected) and UI design (hard error vs. an override flow). Not addressed by any closure decision.
2. **Recreational duplicate check-in enforcement mechanics:** Decision 31 requires blocking a duplicate check-in, but Decisions 10/11 removed any reliable identity key (`name` is free text) from the ticket model. Is the block literal and unconditional (risking false positives on shared names), or an overridable warning? Two decisions are in tension here; neither resolves the other.
3. **Package cancellation "first month" boundary:** is the boundary day itself counted as "during" or "after" the first month? Financially relevant, not addressed by Decision 19's text.
4. **Private (Lane Rental / Coach-Brought) cancellation tier boundary:** does "before completing 2 sessions" include exactly 1 completed session, or strictly fewer? Financially relevant, not addressed by any closure decision.

These four are the only genuine business-behavior gaps remaining anywhere in this design set. Everything else previously flagged across Docs 01–14 has been resolved by the closure decisions and is reflected above as final.
