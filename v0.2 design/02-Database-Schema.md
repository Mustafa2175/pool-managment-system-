# 02 — Database Schema
## Swimming Pool Management System
### SYNCHRONIZED — supersedes all prior versions of this document

**Depends on:** `01-ERD.md` (synchronized version).
**Conventions unchanged from the original design:** surrogate integer PKs, `created_at` on every table, soft-delete via `is_deleted`/`deactivated_at` never physical DELETE, versioned config tables (`effective_from`/`effective_to`), snapshot columns on every priced/rated transactional row so historical values never drift when configuration changes (this remains the single most important structural rule in the whole schema, unchanged by the closure decisions).

This document only restates tables that **changed**. Every table not listed below is unchanged from the pre-synchronization schema and remains in force exactly as originally specified.

---

## 1. Identity & Access — Changed Tables

### `employees` (changed)
```sql
CREATE TABLE employees (
    employee_id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    employee_type TEXT NOT NULL CHECK (employee_type IN ('ADMINISTRATOR','COACH','LIFEGUARD')),
    national_id TEXT NOT NULL UNIQUE,
    phone TEXT,
    nickname TEXT NULL,                          -- NEW: populated only on username collision (Decision 13)
    monthly_salary DECIMAL(12,2),
    status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE')),
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    deactivated_at TIMESTAMP NULL,
    CHECK (employee_type <> 'ADMINISTRATOR' OR monthly_salary IS NOT NULL)
);
-- REMOVED: qualification_certificate column (superseded by employee_qualifications, Decision 6)
```

### `users` (changed — CHECK tightened per Decision 12)
```sql
CREATE TABLE users (
    user_id INTEGER PRIMARY KEY,
    username TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    role_id INTEGER NOT NULL REFERENCES roles(role_id),
    employee_id INTEGER NULL REFERENCES employees(employee_id),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_by_user_id INTEGER NULL REFERENCES users(user_id),
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    deactivated_at TIMESTAMP NULL
    -- Application-layer CHECK (engine-enforced where the DB supports cross-table CHECKs,
    -- otherwise enforced in the CreateUser/EditUser use cases without exception):
    --   role='ADMINISTRATOR'        => employee_id IS NOT NULL (and that Employee.employee_type='ADMINISTRATOR')
    --   role IN ('OWNER','SUPER_ADMIN') => employee_id IS NULL, always
);
```
Reactivation (Decision 15) is an `UPDATE`: `is_active=TRUE, deactivated_at=NULL` — `username`/`password_hash` untouched.

---

## 2. New Tables — Employee Qualifications & Rates (replaces the old `employee_rate_configs`)

```sql
CREATE TABLE qualifications (
    qualification_id INTEGER PRIMARY KEY,
    name_en TEXT NOT NULL,
    name_ar TEXT NOT NULL,
    rank_order INTEGER NOT NULL UNIQUE            -- higher = higher qualification
);

CREATE TABLE employee_qualifications (
    employee_id INTEGER NOT NULL REFERENCES employees(employee_id),
    qualification_id INTEGER NOT NULL REFERENCES qualifications(qualification_id),
    obtained_at DATE NOT NULL,
    PRIMARY KEY (employee_id, qualification_id)
);

CREATE TABLE qualification_rate_configs (
    rate_config_id INTEGER PRIMARY KEY,
    qualification_id INTEGER NOT NULL REFERENCES qualifications(qualification_id),
    session_rate DECIMAL(12,2) NOT NULL,
    effective_from DATE NOT NULL,
    effective_to DATE NULL
);
-- Partial-unique pattern (as used by every other config table): only one row per
-- qualification_id may have effective_to IS NULL at a time.
```
**`employee_rate_configs` is dropped entirely** — replaced by the two tables above. Any payroll calculation resolves an employee's rate via: `MAX(rank_order)` among their `employee_qualifications` → the `qualification_rate_configs` row effective on the calculation date for that qualification. The resolved value is snapshotted onto the payroll line item, never re-resolved later.

---

## 3. Training Subscriptions — Changed Table

### `training_subscriptions` (changed)
```sql
CREATE TABLE training_subscriptions (
    subscription_id INTEGER PRIMARY KEY,
    swimmer_id TEXT NOT NULL REFERENCES swimmers(swimmer_id),
    program_id INTEGER NOT NULL REFERENCES programs(program_id),
    period_id INTEGER NOT NULL REFERENCES training_periods(period_id),
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    unit_price_snapshot DECIMAL(12,2) NOT NULL,          -- flat total price for the whole subscription (Decision 3)
    configured_session_count_snapshot INTEGER NOT NULL,
    total_price DECIMAL(12,2) NOT NULL,
    paid_amount DECIMAL(12,2) NOT NULL DEFAULT 0,
    -- balance_due is ALWAYS computed, never manually entered — this is the hard payment
    -- ceiling used by the overpayment check (Decision 21) and every general "how much is
    -- left" display. It is NOT the same field as the new declared-Outstanding pair below.
    balance_due DECIMAL(12,2) GENERATED ALWAYS AS (total_price - paid_amount) STORED,
    credit_granted_amount DECIMAL(12,2) NULL,             -- NEW (Decision 1)
    credit_description TEXT NULL,
    outstanding_declared_amount DECIMAL(12,2) NULL,       -- NEW (Decision 1) — a manual annotation,
    outstanding_description TEXT NULL,                    --   NOT a replacement for balance_due
    status TEXT NOT NULL DEFAULT 'ACTIVE'
        CHECK (status IN ('ACTIVE','PAUSED','COMPLETED','CANCELLED')),  -- 'NEW','RENEWED' REMOVED
    renewed_from_subscription_id INTEGER NULL REFERENCES training_subscriptions(subscription_id),
    created_by INTEGER NOT NULL REFERENCES users(user_id),
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    CHECK (credit_granted_amount IS NULL OR outstanding_declared_amount IS NULL)  -- mutual exclusivity
);
```
`renewed_from_subscription_id` is the **only** mechanism linking a renewal to its predecessor — there is no forced status change on the old row (Decision 18).

---

## 4. Packages — Changed Table

### `packages` (changed)
```sql
CREATE TABLE packages (
    package_id INTEGER PRIMARY KEY,
    swimmer_id TEXT NOT NULL REFERENCES swimmers(swimmer_id),
    package_type TEXT NOT NULL CHECK (package_type IN ('TRAINING','RECREATIONAL')),
    program_id INTEGER NULL REFERENCES programs(program_id),      -- Training only, must be Regular
    training_period_id INTEGER NULL REFERENCES training_periods(period_id),
    recreational_period_id INTEGER NULL REFERENCES recreational_periods(recreational_period_id),
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    duration_snapshot_days INTEGER NOT NULL,
    sessions_per_month_snapshot INTEGER NOT NULL,            -- NEW (Decision 5)
    available_sessions_total INTEGER NOT NULL,               -- NEW: duration_months * sessions_per_month_snapshot
    available_sessions_remaining INTEGER NOT NULL,           -- NEW: decremented per check-in
    reference_training_price_snapshot DECIMAL(12,2) NOT NULL,-- NEW (Decision 19): Regular Training price at creation
    total_price DECIMAL(12,2) NOT NULL,
    paid_amount DECIMAL(12,2) NOT NULL DEFAULT 0,
    balance_due DECIMAL(12,2) GENERATED ALWAYS AS (total_price - paid_amount) STORED,
    credit_granted_amount DECIMAL(12,2) NULL,                -- NEW (Decision 1), same pattern as subscriptions
    credit_description TEXT NULL,
    outstanding_declared_amount DECIMAL(12,2) NULL,
    outstanding_description TEXT NULL,
    qr_token TEXT NULL UNIQUE,
    status TEXT NOT NULL DEFAULT 'ACTIVE'
        CHECK (status IN ('ACTIVE','EXPIRED','CANCELLED')),  -- 'NEW' REMOVED
    renewed_from_package_id INTEGER NULL REFERENCES packages(package_id),
    created_by INTEGER NOT NULL REFERENCES users(user_id),
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    CHECK (available_sessions_remaining >= 0),
    CHECK (credit_granted_amount IS NULL OR outstanding_declared_amount IS NULL),
    CHECK (
        (package_type='TRAINING' AND training_period_id IS NOT NULL AND recreational_period_id IS NULL) OR
        (package_type='RECREATIONAL' AND recreational_period_id IS NOT NULL AND training_period_id IS NULL)
    )
);
```

### `package_configs` (changed)
```sql
ALTER TABLE package_configs ADD COLUMN sessions_per_month INTEGER NOT NULL;
```

### `package_checkins` (unchanged shape, changed behavior)
Every successful insert into `package_checkins` is now paired, in the same transaction, with `UPDATE packages SET available_sessions_remaining = available_sessions_remaining - 1 WHERE package_id = ?` — enforced by the `CHECK (available_sessions_remaining >= 0)` above rejecting the check-in if none remain.

---

## 5. Recreational Single Entry — Replaced Table

### `recreational_tickets` (replaces `recreational_checkins` entirely — Decisions 10 & 11)
```sql
CREATE TABLE recreational_tickets (
    ticket_id INTEGER PRIMARY KEY,
    recreational_period_id INTEGER NOT NULL REFERENCES recreational_periods(recreational_period_id),
    name TEXT NOT NULL,                              -- free text, NOT a swimmer_id FK
    member_status TEXT NOT NULL CHECK (member_status IN ('MEMBER','NON_MEMBER')),
    checked_in_at TIMESTAMP NOT NULL DEFAULT now(),
    amount_paid DECIMAL(12,2) NOT NULL CHECK (amount_paid > 0),   -- always the full configured fee
    payment_description TEXT NULL,
    recorded_by INTEGER NOT NULL REFERENCES users(user_id)
);
-- No swimmer_id column exists. This table is fully independent of the Swimmer domain.
```
Duplicate-check-in enforcement against `(recreational_period_id, name, DATE(checked_in_at))` is a business-logic concern, not a DB unique constraint, because the exact enforcement mechanics (hard block vs. overridable warning) are one of the four genuinely unresolved items carried in Doc 12/14.

---

## 6. Private — Changed Tables

### `lanes` (changed)
```sql
CREATE TABLE lanes (
    lane_id INTEGER PRIMARY KEY,
    label TEXT NOT NULL UNIQUE,
    capacity INTEGER NOT NULL CHECK (capacity > 0),   -- NEW (Decisions 30 & 35)
    status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','INACTIVE'))
);
```

### `private_bookings` (changed — status enum only)
```sql
-- status CHECK changed from ('NEW','ACTIVE','COMPLETED','CANCELLED')
--                        to  ('ACTIVE','COMPLETED','CANCELLED')   -- Decision 28
```
`club_percentage_snapshot`/`coach_percentage_snapshot` (Club-Brought only) are unchanged in shape, now explicitly confirmed as frozen-forever-at-creation (Decision 8) — never re-read from config after booking creation, for any purpose including cancellation math.

### `private_booking_participants` (replaces `private_booking_swimmers` entirely — Decision 9)
```sql
CREATE TABLE private_booking_participants (
    participant_id INTEGER PRIMARY KEY,
    private_booking_id INTEGER NOT NULL REFERENCES private_bookings(private_booking_id),
    participant_type TEXT NOT NULL CHECK (participant_type IN ('SWIMMER','GUEST')),
    swimmer_id TEXT NULL REFERENCES swimmers(swimmer_id),
    guest_name TEXT NULL,
    guest_phone TEXT NULL,
    CHECK (
        (participant_type='SWIMMER' AND swimmer_id IS NOT NULL AND guest_name IS NULL) OR
        (participant_type='GUEST' AND guest_name IS NOT NULL AND swimmer_id IS NULL)
    )
);
```
Lane Rental participant count for a booking must not exceed the selected Lane's `capacity` (Decision 30) — enforced in business logic at booking creation and whenever a participant is added.

---

## 7. New Table — Coach Dues (Decisions 2, 4 & 8)

```sql
CREATE TABLE coach_dues (
    coach_due_id INTEGER PRIMARY KEY,
    employee_id INTEGER NOT NULL REFERENCES employees(employee_id),   -- must be employee_type='COACH'
    source_type TEXT NOT NULL CHECK (source_type IN ('CLUB_BROUGHT_SHARE','CANCELLATION_FEE')),
    source_id INTEGER NOT NULL REFERENCES private_bookings(private_booking_id),
    amount DECIMAL(12,2) NOT NULL,
    period_year INTEGER NOT NULL,
    period_month INTEGER NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    consumed_in_payroll_id INTEGER NULL REFERENCES payrolls(payroll_id)  -- set once a payroll run consumes it
);
```
This table is what monthly Coach/Lifeguard payroll sums for Private-derived income — Training-session dues and Replacement dues continue to be computed directly from `employee_attendances`/`employee_replacements` respectively; they are **not** duplicated into this ledger.

---

## 8. Finance — Changed Tables

### `payments` (changed)
```sql
ALTER TABLE payments ADD COLUMN last_modified_by INTEGER NULL REFERENCES users(user_id);
ALTER TABLE payments ADD COLUMN last_modified_at TIMESTAMP NULL;
-- payments.amount is now directly UPDATE-able by an authorized CorrectPaymentUseCase
-- (Decision 22) — this is the ONE deliberate, narrow exception to the immutability
-- principle applied to every other financial table in this schema.
```

### `transactions` (changed — enum extended, one narrow mutability exception)
```sql
-- transaction_type CHECK gains two new values:
--   'PAYROLL_ADJUSTMENT'   (Decision 23)
--   'REFUND_ADJUSTMENT'    (Decision 24)
--
-- IMMUTABILITY EXCEPTION (resolves the gap neither closure document addressed —
-- see 17-FINAL-DESIGN-SYNCHRONIZATION-REPORT.md item T-1):
-- When a Payment is corrected (Decision 22), the ONE linked Transaction row
-- (transaction_type IN ('TRAINING_PAYMENT','PACKAGE_PAYMENT','PRIVATE_PAYMENT'))
-- has its `amount` updated in the SAME operation, to the corrected value.
-- This is the only code path anywhere in the system permitted to UPDATE a
-- transactions row. Every other financial correction (Payroll, Refund) uses a
-- brand-new adjustment Transaction instead, leaving the original row untouched.
-- Without this one exception, SUM(transactions) would silently diverge from
-- true revenue the moment any Payment is corrected.
```

### `refunds` (unchanged shape; correction pattern added)
Correction (Decision 24) never touches the `refunds` row or its original `REFUND` Transaction — a new `REFUND_ADJUSTMENT` Transaction is inserted instead, referencing `related_entity_id = refund_id`.

### `payrolls` (unchanged shape; correction pattern added)
Correction (Decision 23) never touches a `PAID` `payrolls` row or its original `PAYROLL_PAYMENT` Transaction — a new `PAYROLL_ADJUSTMENT` Transaction is inserted instead, referencing `related_entity_id = payroll_id`.

### `credits` (changed)
```sql
ALTER TABLE credits ADD COLUMN description TEXT NULL;
-- generated_from_type CHECK unchanged in shape: ('TRAINING_SUBSCRIPTION','PACKAGE') —
-- these values already correctly identify the parent entity type; the sole generation
-- path is now the manual credit_granted_amount entry on that parent (Decision 1).
```

---

## 9. Configuration — Changed / New

```sql
-- package_configs: see §4 above (sessions_per_month added)

CREATE TABLE system_settings (
    setting_key TEXT PRIMARY KEY,
    setting_value TEXT NOT NULL
);
-- Seeded with ('language', 'AR' | 'EN') at Initial Program Setup (Decision 36).
-- System-wide only — there is no per-user language column anywhere (not on `users`,
-- not anywhere else). Editable later only by Super Admin, via Configuration.

-- schedule_defaults_configs: unchanged in shape; confirmed as template/pre-fill values
-- only (Decision 33) — no CHECK against it is ever enforced on actual Period schedules.
```

---

## 10. Backup — Changed Table

### `backups` (changed)
```sql
ALTER TABLE backups ADD COLUMN encrypted BOOLEAN NOT NULL DEFAULT TRUE;   -- Decision 25: always true, every backup
ALTER TABLE backups ADD COLUMN encryption_key_ref TEXT NULL;             -- technical: see Doc 03 §5 for key-management approach
ALTER TABLE backups ADD COLUMN attempt_number INTEGER NOT NULL DEFAULT 1;-- Decision 26: which of up to 5 attempts succeeded
```

---

## 11. Dropped Tables / Columns — Full List

- `employee_rate_configs` — dropped, replaced by `qualification_rate_configs` (§2).
- `employees.qualification_certificate` — dropped, replaced by `employee_qualifications` (§2).
- `recreational_checkins` — dropped, replaced by `recreational_tickets` (§5).
- `private_booking_swimmers` — dropped, replaced by `private_booking_participants` (§6).
- `'NEW'` value from `training_subscriptions.status`, `packages.status`, `private_bookings.status` CHECKs.
- `'RENEWED'` value from `training_subscriptions.status` CHECK.

## 12. Historical-Truth Rule — Unchanged, Now Also Covers New Tables

The schema-wide rule from the original design stands, extended to every table introduced above: `qualification_rate_configs`, `coach_dues`, `packages` (all new snapshot columns), `training_subscriptions` (Credit/Outstanding pair) all either snapshot their values at the moment of the triggering event or are themselves append-only ledger entries — none of them are ever silently recomputed from current configuration after the fact.
