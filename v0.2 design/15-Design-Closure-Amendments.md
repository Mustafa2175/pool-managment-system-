# 15 — Design Closure Amendments
## Swimming Pool Management System

**Source:** `Design_Closure_Decisions.md` (business-final, supersedes any conflicting recommendation in Docs 01–14)
**Purpose:** Translate each of the 36 closure decisions into concrete amendments against the existing design — which document/table/rule it changes, what new schema is needed, and (where the closure decision itself creates a small new implementation question) a narrowly-scoped flag rather than a silent assumption, per the closure document's own instruction #3.

**Status after this document:** of the 36 original RED items in `12-Edge-Cases-and-Decisions.md`, **34 are now fully resolved**. Two narrow implementation-detail points are newly surfaced by the decisions themselves and are flagged in §3. A small number of pre-existing low-priority items that the closure document didn't address remain open (carried in the revised Doc 12).

**⚠️ TERMINOLOGY & RECONCILIATION NOTICE (added during final synchronization):** This document's decision-by-decision *reasoning* remains accurate background reading, but two things below are superseded by the final synchronized document set and should not be read as authoritative on their own:
1. **Field/table naming:** wherever this document's naming differs from `01-ERD.md`/`02-Database-Schema.md` (both fully synchronized), the ERD/Schema names win. Notably: this document's `available_sessions_total`/`available_sessions_remaining` (Package sessions, Decision 5) are `total_sessions_snapshot`/`sessions_consumed` in the final schema.
2. **Decision 1 (Credit/Outstanding):** this document's §1 originally proposed making `outstanding_amount` a purely manual column, replacing automatic computation entirely. **This is superseded.** The final, reconciled model (needed to make Decision 1 consistent with Decision 21's overpayment rule) keeps an always-computed `balance_due` (`total_price − paid_amount`) and adds a *separate*, optional, additive `outstanding_declared_amount` annotation. See `01-ERD.md`'s opening note and `17-FINAL-DESIGN-SYNCHRONIZATION-REPORT.md` item T-1 for the authoritative version.

---

## 1. Decision-by-Decision Amendments

### D1. Credit and Outstanding (amends Doc01 §1.25, Doc02 §5/§8, Doc05 §2.4/§3.1/§8.3, Doc11 §8.4)

**Resolves:** H1 (Credit generation trigger) — the single highest-priority open item.

**New rule (replaces the "automatic outstanding = total − paid" assumption used in the original design):**
- `outstanding_amount` on `training_subscriptions`/`packages` is **no longer a derived/computed column**. It becomes a manually-entered, optional field set by the Administrator at creation time, with its own `outstanding_description`.
- A parallel, mutually exclusive manual `credit_amount` + `credit_description` pair may instead be entered at creation.
- If neither is entered, the record carries no Outstanding and no Credit, **regardless of the arithmetic difference between `total_price` and `paid_amount`.**
- The system never auto-generates either value from any other business event (cancellation, etc.) — decision D1's "not automatically generated" line, combined with the absence of any other SRS-described Credit-generating event, means **this creation-time manual entry is the only path that ever creates Credit**, closing the gap identified in Doc05 §8.3.

**Schema changes (Doc02 §4 `training_subscriptions`, §5 `packages`):**
- Change `outstanding_amount` from a `GENERATED ALWAYS AS` column to a plain nullable `DECIMAL(12,2)` column, manually set.
- Add `outstanding_description TEXT NULL`.
- Add `credit_amount DECIMAL(12,2) NULL`, `credit_description TEXT NULL`.
- Add `CHECK (NOT (outstanding_amount IS NOT NULL AND credit_amount IS NOT NULL))` — enforces mutual exclusivity at the DB level, not just in the UI.
- If `credit_amount` is entered, insert a linked `credits` row (Doc02 §8) at creation time referencing the subscription/package as `generated_from_type/id`, so the existing `Credit`/`CreditUsage` machinery (Doc01 §1.25–1.26, confirmed still valid by D34 below) picks it up for later use.

**UI (amends Doc04 §3.3, §3.4):** the Add Subscription/Add Package forms gain two optional, mutually exclusive fields — "Outstanding" (amount + reason) and "Credit" (amount + reason) — presented as a single toggle group, not two independent checkboxes, to make the mutual exclusivity visually obvious.

**Reporting impact:** the Outstanding Report (§21.4, Doc13 §12) now reports only records where the Administrator explicitly declared an Outstanding amount — it is a report of *declared* debts, not of arithmetic gaps between price and payment. This is a deliberate, business-confirmed behavior, not a defect.

---

### D2. Club-Brought Private Cancellation Fee (amends Doc05 §5.6, Doc11 §8.7–8.8)

**Resolves:** E4 (Club-Brought cancellation math order-of-operations) — the second critical open item.

**New rule (fully replaces the ambiguous §15.3 reading):**
```
cancellation_fee = paid_amount × configured_fee_percentage
coach_receives    = cancellation_fee   (100% of the fee, not the configured club/coach split)
customer_refund   = paid_amount − cancellation_fee
```
The club's configured percentage split (§14.3 / D4 below) **does not apply at cancellation** — it governs revenue recognition for a booking that runs to completion, not the cancellation payout. This is now unambiguous: worked example confirms Paid 1,000 / Fee 20% → Coach 200, Refund 800, **zero retained as club revenue on a cancelled Club-Brought booking.**

**Business logic (Doc05 §5.6 replacement):** the coach's 200 EGP is not a Refund-table entry — it is a **coach due**, recorded via the new `coach_dues` ledger (see D4) and paid out through the coach's monthly payroll, not as an immediate Transaction against the customer. The customer-facing Transaction is a single `REFUND` of 800.

**Financial integrity check (amends Doc11 §8.8):** the invariant is preserved — the 1,000 paid resolves to exactly one REFUND transaction (800) plus one deferred payroll-ledger entry (200), never both counted as club revenue and never double-paid.

---

### D3. Training Price Granularity (amends Doc05 §2.4)

**Resolves:** C2.

**New rule:** the configured Training price **is the total price for the whole subscription** (all sessions included), confirming the original design's default assumption. No change to schema; Doc05 §2.4's `DECISION REQUIRED` marker is removed — `total_price = unit_price_snapshot` directly, with `unit_price_snapshot` representing the whole-subscription price, not a per-session rate.

---

### D4. Club-Brought Private Coach Income (amends Doc01 §1.28, Doc02 §9, Doc05 §7.1, Doc11 §8.7)

**Resolves:** E5 (does coach income from Private flow through Payroll?) and formalizes E1/D8's snapshotting.

**New rule:** Yes — a coach's Club-Brought share (§14.3's `coach_share`) and any Cancellation Fee income (D2) are **both** added to the coach's monthly financial dues and paid through the regular monthly Payroll run, not through any separate payment mechanism.

**New entity — `coach_dues`** (addition to Doc02 §9):

| Column | Type | Notes |
|---|---|---|
| coach_due_id | INTEGER PK | |
| employee_id | INTEGER FK→employees | employee_type = COACH |
| source_type | TEXT | CHECK IN ('CLUB_BROUGHT_SHARE','CANCELLATION_FEE') |
| source_id | INTEGER | FK to the originating `private_bookings` row |
| amount | DECIMAL(12,2) | |
| period_year, period_month | INTEGER | which payroll cycle this due belongs to (the cycle in which the booking/cancellation event occurred) |
| created_at | TIMESTAMP | |

**Payroll calculation amendment (Doc05 §7.1, Doc07 §8):** monthly Coach payroll = Σ(session rate × present sessions, Training) + Σ(replacement sessions at replacement's rate) + Σ(`coach_dues` rows for this employee/period). This closes the Doc05§7.1/§11 gap identified in the original design.

---

### D5. Package Attendance / Sessions (amends Doc01 §1.13/§1.19a, Doc02 §5, Doc05 §3.1, Doc07 §2)

**Resolves:** D1 (do Packages generate Sessions?) — with a **third answer**, distinct from both options the original design weighed.

**New rule:** Packages are **not** purely duration-based with no session concept (as D2 of the original ERD guessed), nor do they get a full pre-generated calendar like Training (the other option considered). Instead:
- `available_sessions = duration_months × configured_sessions_per_month` snapshot at creation (e.g., 2 months × 8/month = 16).
- Each attendance/check-in **consumes exactly one** available session (`available_sessions_remaining` decrements).
- The Package still **expires by calendar date** regardless of unused sessions — unused sessions are forfeited at expiry, they do not extend validity (explicit, closes a variant of the "package holder not arriving" edge case D5 from Doc12).
- Package Pause remains disallowed (confirms Doc07 §2, no change).
- **Package Period Change now explicitly carries the remaining session balance forward** to the new Period (amends Doc05 §3.3 — previously silent on this point).

**Schema changes (Doc02 §5 `packages`):**
- Add `sessions_per_month_snapshot INTEGER NOT NULL`.
- Add `available_sessions_total INTEGER NOT NULL` (= duration_months × sessions_per_month_snapshot).
- Add `available_sessions_remaining INTEGER NOT NULL`, decremented on each `package_checkins` insert; `CHECK (available_sessions_remaining >= 0)`.
- `package_period_changes` (Doc02 §5) requires no new column — the balance simply carries via `packages.available_sessions_remaining`, which is untouched by a period change.

---

### D6. Employee Qualification and Rate (amends Doc01 §1.4, Doc02 §1/§9)

**Resolves:** G1 (per-employee vs. per-qualification rate).

**New rule:** rate is purely **qualification-based**, not employee-specific. An employee may hold multiple qualifications; payroll always uses the rate tied to their **highest-ranked** qualification. Historical payroll always keeps the rate that was in effect **when that payroll was calculated** (confirms/closes G4's non-retroactivity question explicitly, not just by extrapolation).

**Schema changes (replaces the single `employee_rate_configs` design in Doc02 §9):**

`qualifications`
| Column | Type | Notes |
|---|---|---|
| qualification_id | INTEGER PK | |
| name_en, name_ar | TEXT | |
| rank | INTEGER | Higher number = higher qualification; used to pick the "highest" one an employee holds |

`employee_qualifications` (M:N)
| Column | Type | Notes |
|---|---|---|
| employee_id | FK→employees | |
| qualification_id | FK→qualifications | |
| PK | (employee_id, qualification_id) | |

`qualification_rate_configs` (replaces per-employee rate config; same versioning pattern as other configs, Doc02 §10)
| Column | Type | Notes |
|---|---|---|
| rate_config_id | INTEGER PK | |
| qualification_id | FK→qualifications | |
| session_rate | DECIMAL(12,2) | |
| effective_from, effective_to | DATE | Standard versioning pattern |

**Payroll calculation amendment (Doc05 §7.1):** `applicable_rate(employee, date) = qualification_rate_configs row effective on `date` for the qualification with MAX(rank) among the employee's `employee_qualifications`.` Snapshot this resolved rate onto each `Attendance`-derived payroll line item at calculation time (not just at read time) so a later qualification change cannot retroactively alter an already-run payroll — this satisfies the "historical payroll keeps the rate applicable when calculated" rule structurally, the same snapshot pattern used everywhere else in the schema (Doc02 §12).

---

### D7. Coach/Lifeguard Payroll Cycle (amends Doc01 §1.28, Doc05 §7.1, Doc07 §8)

**Resolves:** G2.

**New rule:** monthly cycle, confirmed identical to Administrator's cycle. Scope explicitly includes Training session dues + Private dues (`coach_dues`, D4) + replacement-session dues, all in one monthly `payrolls` row per employee. No schema change beyond D4's `coach_dues` table — `payrolls.calculated_amount` aggregation logic (Doc02 §9) now explicitly sums three sources instead of one.

---

### D8. Club-Brought Private Split Snapshot (amends Doc01 §1.15, Doc02 §7, Doc05 §5.4)

**Resolves:** E1.

**New rule:** confirms the original design's recommendation exactly — `club_percentage_snapshot`/`coach_percentage_snapshot` on `private_bookings` (already in Doc02 §7) are frozen at booking creation; later Configuration changes never affect existing bookings; new bookings use the then-current configured percentages. No schema change — the `DECISION REQUIRED` marker on this field is simply removed.

---

### D9. Private Participants (amends Doc01 §1.16, Doc02 §7)

**Resolves:** E2.

**New rule:** a Private participant may be either a linked Swimmer **or** an unlinked Guest recorded only within the booking (no Swimmer Profile created for guests).

**Schema changes (`private_booking_swimmers` renamed/restructured to `private_booking_participants`, Doc02 §7):**

| Column | Type | Notes |
|---|---|---|
| private_booking_participant_id | INTEGER PK | |
| private_booking_id | FK→private_bookings | |
| participant_type | TEXT | CHECK IN ('SWIMMER','GUEST') |
| swimmer_id | FK→swimmers, NULL | Required when participant_type='SWIMMER' |
| guest_name | TEXT, NULL | Required when participant_type='GUEST' |
| CHECK | | exactly one of (swimmer_id, guest_name) populated, matching participant_type |

---

### D10 & D11. Recreational Single Entry (amends Doc01 §1.21, Doc02 §6, Doc05 §4.1, Doc06 §4)

**Resolves:** F1, F2.

**New rule:** Single Entry is a lightweight **ticket record**, not a Swimmer-linked transaction. It captures Name, Member/Non-Member, Period, date/time, amount paid, and payment-method description directly on the ticket — no Swimmer Profile is created or required. Reports aggregate ticket counts and revenue (total, Member, Non-Member) directly from these ticket records.

Payment is **always full** at entry — partial payment, Outstanding, and Credit are all explicitly disallowed for Single Entry (this also resolves the ambiguity in Doc12 F2 definitively: no, it can never be partial).

**Schema changes (`recreational_checkins` restructured, Doc02 §6):**

| Column | Type | Notes |
|---|---|---|
| checkin_id | INTEGER PK | |
| recreational_period_id | FK→recreational_periods | |
| name | TEXT NOT NULL | Free text, no Swimmer FK |
| member_status | TEXT NOT NULL | CHECK IN ('MEMBER','NON_MEMBER') |
| checked_in_at | TIMESTAMP | |
| amount_paid | DECIMAL(12,2) NOT NULL | Always the full configured entry fee — CHECK against config at insert time |
| payment_description | TEXT | Payment method/details |
| recorded_by | FK→users | |

The `swimmer_id` FK is **removed entirely** from this table — a clean break from the original design's nullable-FK compromise.

---

### D12. Owner and Super Admin Employee Records (amends Doc01 §1.2, Doc02 §1)

**Resolves:** A2.

**New rule:** confirmed — Owner and Super Admin are Users only, never Employees, and are excluded from Employee Attendance and Payroll entirely. `users.employee_id` remains nullable and is now understood to be **structurally never populated** for these two roles (not just optionally empty) — Doc02 §1's `users` table CHECK constraint should be tightened: `CHECK (role via roles.code = 'ADMINISTRATOR' AND employee_id IS NOT NULL) OR (role_code IN ('OWNER','SUPER_ADMIN') AND employee_id IS NULL)`.

---

### D13. Username Collision (amends Doc01 §1.2, Doc02 §1, Doc05 §9.1)

**Resolves:** A3.

**New rule:** on collision, a required Nickname is appended to form the username (`Ahmed Mohamed` + Nickname `Hamo` → username `Ahmed Mohamed Hamo`). The Nickname does not alter the employee's official name field.

**Schema change:** add `nickname TEXT NULL` to `employees` (Doc02 §1), populated only when a collision occurs; `users.username` generation logic checks for an existing match on plain name first, and only prompts for/appends Nickname when a collision is actually detected.

---

### D14. Training Period Editing Authority (amends Doc05 §2.2, Doc07 §5)

**Resolves:** C1.

**New rule:** Administrator can edit Days, Start/End Time, Capacity, Coaches, and Lifeguards on an existing Period (a materially wider authority than the original SRS text's ambiguous phrasing suggested) — Super Admin's role is limited to **initial creation**. All the existing historical-preservation rules (§6.5: past sessions frozen, future sessions follow new schedule, changes audited) apply unchanged regardless of which role makes the edit. Owner remains view-only, confirmed.

**Amendment:** Doc08 §2.2's authorization table row for `EDIT_TRAINING_PERIOD` / schedule fields is confirmed as `SUPER_ADMIN, ADMINISTRATOR` — no change needed beyond removing the `DECISION REQUIRED` marker.

---

### D15. User Reactivation (amends Doc07 §7, Doc08 §6)

**Resolves:** A5.

**New rule:** a deactivated User can be reactivated (by whoever has the original deactivation authority per §4.2's role rules), keeping username and password unchanged, with all history intact and the reactivation itself audited (`USER_REACTIVATED`, already anticipated as a conditional entry in Doc09 §2.9).

---

### D16. Password Reset (amends Doc08 §1.2)

**Resolves:** A4.

**New rule — two distinct flows:**
1. **Owner/Super Admin resets an Administrator's password:** requires entering the Administrator's National ID, validated against the Employee Profile; mismatch rejects the reset. Both success and failure are audited.
2. **Administrator self-reset:** requires Username + National ID, both validated against the Administrator's own records.

**Business logic addition (new use case, Doc05 §9):** `ResetPassword(target_user, provided_national_id, actor)` — validates `provided_national_id == employees.national_id` for the Employee linked to `target_user`; on match, reseeds the password hash (to a new value — `DECISION REQUIRED` narrowly scoped: does the reset reseed to the National ID again, matching the original account-creation convention, or generate a fresh temporary password the user must then change? The closure decision specifies the **verification** input but not the **resulting** password value — see §3 below); on mismatch, reject and audit the failure. Passwords remain never-displayed regardless of outcome.

---

### D17. Late Attendance Edit (amends Doc05 §7.4, Doc09 §2.7)

**Resolves:** confirms the original design's 1-hour rule exactly, formalizing the label "Late Administrative Edit." No schema/logic change — `is_late_edit` flag and its audit entry (`EMPLOYEE_ATTENDANCE_EDITED_LATE`) are confirmed as designed; only the display label in the UI (Doc04 §3.7) should read "Late Administrative Edit" verbatim to match the business's chosen terminology.

---

### D18. Training Renewal (amends Doc05 §2.8, Doc07 §1)

**Resolves:** C6.

**New rule:** confirms both branches exactly as originally designed (remaining-sessions → start after old subscription's last session, on the Period's first available occurrence; fully-completed → start on the Period's first available occurrence from the renewal date). Old and new subscriptions **remain separate historical records** — this resolves the ambiguity about forced status transitions: **the old subscription is not forced into `RENEWED` status at the moment of renewal.** It continues its own natural lifecycle (remaining sessions still occur and are attended, or it simply sits as already `COMPLETED`), and the `renewed_from_subscription_id` link on the new record is what connects the two historically — a status-based `RENEWED` label is not required for correctness, though the UI may still display "Renewed" as a badge on the old record once the link exists, for clarity (a presentation choice, not a state-machine requirement). Doc07 §1's `RENEWED` state can be treated as an optional/informational label layered on top of `COMPLETED`, not a distinct enforced transition.

---

### D19. Package Cancellation Refund (amends Doc05 §3.5, Doc11 §3)

**Resolves:** D2 (floor at zero) and D4 (first-month boundary is not more precisely specified, but the floor-at-zero and no-outstanding/no-credit-on-negative rules are now explicit).

**New rule:** `refund = MAX(0, paid_amount − reference_training_price_snapshot)`. If the raw result is negative, refund = 0, and — critically — **this shortfall does not create an Outstanding or Credit record either** (explicitly closing a path that D1's new manual Outstanding/Credit mechanism might otherwise have been mistakenly wired into). Before-start and after-first-month rules are unchanged (full refund / no refund respectively).

---

### D20. Swimmer Status and Private (amends Doc01 §1.5, Doc07 §4)

**Resolves:** B1.

**New rule:** confirmed — Swimmer Active/Inactive status depends **only** on Training Subscription and Package state; Private activity never affects it. A swimmer who only ever has Private bookings remains permanently Inactive by this system's definition. Doc05 §1.4's recompute rule is updated to explicitly exclude Private from the `EXISTS` check.

---

### D21. Payment Overpayment (amends Doc06 §6, Doc09 §2.8)

**Resolves:** H3.

**New rule:** confirmed — payment exceeding the current Outstanding (note: under D1, "Outstanding" now means the *manually declared* balance, not an arithmetic gap; see §3 for the resulting interaction) is rejected outright, no Payment/Transaction is created, and the failed attempt is audited (`PAYMENT_OVERPAYMENT_REJECTED`, new Doc09 event code).

---

### D22. Payment Correction (amends Doc02 §8, Doc03 §2.3, Doc09 §5, Doc11 §8.1)

**Resolves:** H4 — but see the flagged consistency point in §3, since this is the one decision that most directly touches the "Transactions are append-only/immutable" principle treated as foundational (GREEN) in the original design.

**New rule:** Administrator may directly correct a previously recorded Payment's amount; the Payment row itself is updated in place (not superseded by a new row); no separate correction Transaction is created; Audit Log captures old amount, new amount, actor, timestamp, and reason.

**Schema change:** `payments` (Doc02 §8) becomes an updatable table (drop the "append-only" characterization from Doc02/Doc03 for this one table specifically — it remains append-only for `transactions` and `audit_logs`, which are unaffected by this decision).

---

### D23. Paid Payroll Correction (amends Doc02 §9, Doc07 §8)

**Resolves:** G5.

**New rule:** once `payrolls.status = PAID` and its Transaction exists, the row is locked (no further edits). Corrections use a new `payroll_adjustments` entry producing its own `PAYROLL_ADJUSTMENT` Transaction (positive or negative), leaving the original Payroll row untouched.

**Schema addition:**

`payroll_adjustments`
| Column | Type | Notes |
|---|---|---|
| adjustment_id | INTEGER PK | |
| original_payroll_id | FK→payrolls | |
| amount | DECIMAL(12,2) | Signed |
| reason | TEXT | |
| created_by | FK→users | |
| created_at | TIMESTAMP | |
| resulting_transaction_id | FK→transactions | New transaction_type value: `PAYROLL_ADJUSTMENT` |

---

### D24. Refund Correction (amends Doc02 §8)

**Resolves:** a corollary of H4, not previously separately numbered.

**New rule:** parallels D23 exactly — a confirmed Refund is locked; corrections use a new Adjustment/Correction Transaction; the original Refund remains in history; the correction is audited.

**Schema addition:** `refund_adjustments`, same shape as `payroll_adjustments` (§D23), referencing `refunds.refund_id`; new `transaction_type` value `REFUND_ADJUSTMENT`.

---

### D25. Backup Encryption (amends Doc08 §4, Doc10 §8)

**Resolves:** I5.

**New rule:** all backups (Automatic and Manual) are encrypted; not intended to be readable/restorable outside the system's controlled Restore mechanism. `DECISION REQUIRED` (narrow, technical): specific encryption algorithm/key-management approach (e.g., where is the encryption key itself stored/derived, given this is a fully offline system with no external key-management service?) — this is a Doc03 technology-choice-level decision, not a business one, and does not block starting implementation of other modules.

---

### D26. Automatic Backup Failure (amends Doc10 §1)

**Resolves:** I2, I3.

**New rule:** up to 5 total attempts with automatic retry between each; on exhausting all 5, log the failure (Audit/System Log), notify Super Admin, keep the main system operational, and require a Manual Backup to be performed.

**Schema addition:** `backup_attempts` (id, scheduled_run_id, attempt_number, attempted_at, success, failure_reason) — or, more simply, extend `backups` with a `failed_attempts_json`/counter on a single pending-run record; either is a technical implementation choice (Doc03-level), not a business one. Business rule (5 attempts, then notify + require manual) is now fixed.

---

### D27. Roles and Permissions (amends Doc01 §1.1, Doc04 §3.13, Doc08 §2)

**Resolves:** A1.

**New rule:** confirmed — roles are fixed, the permission matrix is fixed, no new roles can be created, no permission reassignment between roles. The "Roles/Permissions" screen (Doc04 §3.13) is therefore **read-only display** of the fixed matrix, not an editable feature — resolving the tension flagged in the original ERD document definitively in favor of the read-only interpretation.

---

### D28. Training Subscription Status (amends Doc05 §2.4, Doc07 §1)

**Resolves:** C3.

**New rule:** a newly created Training Subscription is Active immediately, even with a future Start Date — there is no visible Pending/Scheduled interim state. This confirms the original design's tentative recommendation and removes the `NEW` status's ambiguity entirely: **`NEW` is removed from the visible/enforced state set** (Doc07 §1's diagram simplifies to `[*] → ACTIVE → ...`, dropping the `NEW → ACTIVE` transition as a real step — creation goes directly to ACTIVE).

---

### D29. Training Attendance Window (amends Doc01 §1.14, Doc05 §2.6, Doc06 §2)

**Resolves:** C5.

**New rule:** swimmer Training attendance follows the **same ±30-minute window** pattern already used for Recreational Single Entry (§13.3) — 30 minutes before session start to 30 minutes after. Outside this window, normal attendance entry is rejected (an Administrator could presumably still use a "late edit" override analogous to the employee 1-hour rule, though the closure document doesn't explicitly extend the "late edit" *label* to swimmer attendance — treat the ±30-minute rule as the hard normal-entry boundary, and any entry attempted outside it as requiring the same late-edit-style administrative override and audit flag used for employee attendance, by direct analogy).

---

### D30. Lane Rental Capacity (amends Doc01 §1.17/§1.15, Doc02 §3/§7, Doc05 §5.2)

**Resolves:** part of E3 — supersedes the original "maximum swimmers per lane, not specified" gap with a **configured-per-lane** answer rather than a single global constant.

**New rule:** each Lane has its own configured Capacity (max swimmers); Lane Rental bookings are validated against their selected Lane's specific capacity, not a system-wide constant.

**Schema change:** add `capacity INTEGER NOT NULL` to `lanes` (Doc02 §3); Lane Rental creation validation (Doc06 §5) checks `COUNT(private_booking_participants for this booking) <= lane.capacity`.

---

### D31. Recreational Duplicate Check-in (amends Doc05 §4.1, Doc06 §4, Doc09 §2.5)

**Resolves:** F3.

**New rule:** confirmed — explicit rejection (not a silent no-op), no second ticket/Payment Transaction created, and the duplicate attempt is audited (`RECREATIONAL_CHECKIN_DUPLICATE_REJECTED`, new Doc09 event). Detection key: same `name` + `recreational_period_id` + same Period occurrence date — `DECISION REQUIRED` (narrow): since D10/D11 removed the Swimmer FK from Single Entry tickets, duplicate detection now keys on free-text `name` rather than a stable Swimmer ID — this is inherently fuzzier (e.g., two different people named "Mohamed Ali") and is flagged in §3.

---

### D32. Administrator Attendance (amends Doc01 §1.29, Doc02 §9)

**Resolves:** G3 — confirms the original design's recommended `administrator_daily_attendance` table exactly, as designed in Doc02 §9. No changes needed; the `DECISION REQUIRED` marker is removed.

---

### D33. Schedule Defaults (amends Doc01 §1.31, Doc02 §10)

**Resolves:** J10.

**New rule:** confirmed — `schedule_defaults_configs` (Doc02 §10) is a **template/default-value source** for the Period creation form, not a global whitelist constraint. Each Period keeps a fully independent schedule once created; changing the Default never retroactively affects existing Periods. Removes the `DECISION REQUIRED` marker.

---

### D34. Credit Usage (amends Doc01 §1.25, Doc02 §8)

**Resolves:** H2.

**New rule:** confirms partial usage is explicitly allowed (500 credit, use 300, 200 remains) — validates the original `remaining_amount`-based `credits`/`credit_usages` design exactly as built in Doc02 §8. Usable only for Training/Package, never Private/Recreational; revenue recognized on date of use — both already correctly designed. No schema change; `DECISION REQUIRED` marker removed.

---

### D35. Lane Management (amends Doc01 §1.17, Doc02 §3, Doc04 §3.12)

**Resolves:** J9.

**New rule:** Lanes are full first-class entities with identity/number and their own Capacity (D30), managed via Configuration by Super Admin, with overlap prevention already designed in Doc02 §7/Doc05 §5.1. This confirms a full Lane Management screen is needed in the UI (add to Doc04 §3.12's Configuration screen list, previously only implied) rather than a simple flat list.

---

### D36. Language and Numerals (amends Doc04 §1, Doc02 §13)

**Resolves:** J4, J5, J6, J7 — all four bilingual open items, together.

**New rule:** language is a **single, system-wide setting** chosen once during initial Program Setup (not per-user, closing J4 definitively in favor of the system-wide option Doc04 §1 had flagged as one of two possibilities). It applies uniformly to UI and Reports for all users (closing J6 — yes, reports render in the selected language) — there is no per-user override. Number/date formatting conventions follow the selected language (closing J5 by tying numeral/date format to the language choice rather than leaving it as a separate independent setting); currency values themselves are unaffected by language choice (amounts don't convert or reformat their numeric value, only the surrounding locale formatting/labels change).

**Implication for J7 (bilingual data storage):** since there is only ever one active system language at a time, **user-entered data does not need dual-language storage** — a swimmer/employee's name is captured once, in whatever the operator types, consistent with the single-free-text-field default the original design already recommended (Doc02 §13). This closes J7 in favor of the simpler design.

**New requirement (Setup Wizard):** an initial "Program Setup" flow must exist (not previously specified as a distinct screen in Doc04) that captures the system language choice, presumably alongside other first-run Configuration (Club Info, etc.) — add to Doc04 as a new first-run-only screen, distinct from the ongoing Configuration module.

---

## 2. Resolution Status Summary

| Doc12 item | Resolved by | Doc12 item | Resolved by |
|---|---|---|---|
| A1 | D27 | E1 | D8 |
| A2 | D12 | E2 | D9 |
| A3 | D13 | E3 | D30 (partially, see §3) |
| A4 | D16 | E4 | D2 |
| A5 | D15 | E5 | D4 |
| B1 | D20 | F1 | D10 |
| C1 | D14 | F2 | D11 |
| C2 | D3 | F3 | D31 (see §3 caveat) |
| C3 | D28 | G1 | D6 |
| C5 | D29 | G2 | D7 |
| C6 | D18 | G3 | D32 |
| D1 (Doc12 letter-D-item-1) | D5 | G4 | D6 |
| D2 (Doc12 letter-D-item-2) | D19 | G5 | D23 |
| H1 | D1 (closure) | H2 | D34 |
| H3 | D21 | H4 | D22 (see §3 caveat) |
| I2 | D26 | I3 | D26 |
| I5 | D25 (technical sub-item remains, see §3) | J4–J7 | D36 |
| J9 | D35 | J10 | D33 |

**Items from the original Doc12 that the closure document did not address** (carried forward unchanged into the revised Doc12): B2, B3, C4, C7, C8, C9, D3–D6 (Doc12 lettered items under "Packages," distinct from this document's "D#" numbering — renamed for clarity in the revised Doc12), E6, E7, F4, G6, G7, H5, H6, H7 (already resolved by design), I1, I4, I6–I8, J1 (partially clarified, see below), J2–J3 (already resolved by design), J8.

**J1 clarification (not a full resolution, but meaningfully narrowed):** the closure decisions explicitly call for auditing several specific *business-validation* failures that are not authorization failures — overpayment rejection (D21), duplicate check-in rejection (D31), and failed password-reset attempts (D16). This establishes a working principle: **audit financial and identity-security-adjacent validation failures explicitly, even though they are not permission violations**, while routine capacity/scheduling rejections remain a UI-only concern unless similarly named. J1 is downgraded from a fully open question to a "apply this principle by analogy to any similarly consequential future case" guidance note.

---

## 3. New (Narrow) Flags Introduced by the Closure Decisions Themselves

Per the closure document's own instruction #3 ("if an implementation issue conflicts with these decisions, stop and mark it as `DECISION REQUIRED`"), the following are not left-over ambiguities from the original SRS — they are small new questions that the act of resolving the original ones has surfaced. None of these block starting implementation broadly; they are scoped to specific, identifiable pieces of work.

| # | Flag | Why it's newly surfaced | Affects |
|---|---|---|---|
| N1 | **D22 (Payment Correction) says "no separate correction transaction is required" and the Payment row is updated directly — but does the linked `Transaction` row's `amount` also get updated to match, or does the ledger (`transactions`) now silently disagree with its source `Payment` row?** The financial-integrity design (Doc11 §8.1, §8.9) relies on `SUM(transactions) = reported revenue`; if a Payment is corrected but its Transaction isn't, the ledger becomes wrong. If the Transaction *is* updated in place, that's a deliberate, narrow exception to the Transaction-immutability principle (Doc09 §5) that should be stated explicitly as such, not left implicit. | `payments`/`transactions` consistency, Doc11 §8.9's reconciliation invariant | Recommend: the Transaction linked to a corrected Payment is updated in place to the new amount as part of the same correction operation, and this is the **one and only** exception to Transaction immutability in the whole system — worth a one-line explicit carve-out in Doc09 §5 rather than silently assuming it. |
| N2 | **D1 (Credit/Outstanding) redefines "Outstanding" as a manual declaration, but D21 (Overpayment) says payment exceeding "Outstanding" is rejected.** If a subscription has no manually-declared Outstanding (D1's default case — "if neither is entered"), does D21's overpayment check compare against `total_price − paid_amount` (the old arithmetic definition) instead, or does an un-declared-Outstanding subscription simply have no overpayment ceiling other than `total_price` itself? | Payment validation logic (Doc06 §6) | Recommend: overpayment validation always compares against `total_price − paid_amount` regardless of whether a manual Outstanding record exists — D1's Outstanding field is a *reporting/description* overlay, while `total_price` remains the hard payment ceiling everywhere else (subscription creation's own paid-amount validation, Doc06 §2, already caps paid_amount at `total_price`). This keeps D1 and D21 consistent without contradiction, but should be explicitly confirmed since neither decision states it in these terms. |
| N3 | **D31's duplicate-check-in detection**, now keyed on free-text `name` (since D10/D11 removed the Swimmer FK from Single Entry tickets), is inherently less precise than ID-based detection — two genuinely different people sharing a common name could be incorrectly blocked. | Recreational check-in UX (Doc04 §3.6) | Recommend: duplicate check is a **soft warning with an override**, not a hard block, given the name-only matching — "A person named [X] already checked in for this period. Continue anyway?" — rather than an unconditional rejection, to avoid turning a same-name coincidence into a customer-service incident. This is a UX refinement recommendation, not a reversal of D31's core "duplicates are rejected and audited" rule. |
| N4 | **D16's password reset specifies the verification input (National ID) but not the resulting new password value** — reseed to National ID again (matching original account creation) or issue a fresh temporary password? | Doc05 new Password Reset use case | Needs a one-line business answer; does not block other modules. |
| N5 | **D25's backup encryption** needs a concrete key-management approach for a fully offline system (no external KMS). | Doc03 technology choices | Technical decision, not business — does not block starting implementation of any business module. |

None of N1–N5 are large enough to justify delaying the start of implementation on Swimmers, Training, Employees, Packages, Recreational, Private, or Reports — they are scoped narrowly to Finance-correction logic (N1), Payment validation (N2), Recreational UX (N3), the new Password Reset use case (N4), and Backup infrastructure (N5) respectively, and can be resolved in parallel with early development.
