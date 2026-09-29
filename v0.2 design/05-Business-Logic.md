# 05 — Business Logic Specification (SYNCHRONIZED — v2, post Design Closure)
## Swimming Pool Management System

**Depends on:** `01-ERD.md` (v2), `02-Database-Schema.md` (v2). This is the authoritative business logic document — every formula and rule below reflects the finalized Design Closure Decisions. Where the original v1 document carried a `DECISION REQUIRED` flag that is now resolved, the resolved rule replaces it outright (no "see addendum" pointers).

---

## 1. Swimmers

Unchanged from v1 except:

### 1.4 Derived Active/Inactive Status (finalized decision #20)
```
status = ACTIVE  iff  EXISTS a TrainingSubscription OR Package for this swimmer with status IN ('ACTIVE','PAUSED')
Private Bookings are NEVER part of this computation, regardless of their state.
```
This also governs the Soft-Delete precondition (§5.6 of the SRS): only an active Training Subscription/Package blocks deletion — an active Private Booking never does.

---

## 2. Training

### 2.1–2.3 Period Creation, Editing, Coach/Lifeguard Assignment (finalized decision #14)

**Resolved authority (replaces v1's flagged SRS-internal contradiction):** Super Admin creates the Period and its initial schedule/capacity. **Administrator may subsequently edit** Days, Start Time, End Time, Capacity, Coach assignments, and Lifeguard assignments. Owner is view-only. On any schedule/capacity change: historical Sessions are never modified; only future Session generation and new enrollments use the updated schedule; the change is written to Audit Log (`TRAINING_PERIOD_SCHEDULE_CHANGED`) regardless of which of the two eligible roles made it.

### 2.4 Training Subscription Creation

**Validation (unchanged from v1):** Period Active; start date ≥ today and matches a scheduled day; capacity available; no schedule overlap across Training/Package/Private for the swimmer.

**Pricing (finalized decision #3):**
```
total_price = TrainingPricingConfig[program, swimmer.member_status].price   # a FLAT TOTAL, never per-session x count
unit_price_snapshot = total_price   # frozen at creation
configured_session_count_snapshot = current global Training Session Count
```

**Credit / Outstanding at creation (finalized decision #1, fully reconciled — no longer a DECISION REQUIRED item):**
```
balance_due = total_price - paid_amount   # ALWAYS computed, unconditionally, on every record.
                                           # This is the hard payment ceiling (sec 8.1) and the plain
                                           # "how much is left to pay" figure everywhere in the system.
                                           # Nothing below ever replaces or disables this computation.

IF administrator provides credit_granted_amount (with credit_description, required together):
    assert outstanding_declared_amount is not also provided (mutually exclusive)
    insert credits row (amount=credit_granted_amount, remaining_amount=credit_granted_amount,
                         description=credit_description, generated_from_type='TRAINING_SUBSCRIPTION',
                         generated_from_id=subscription_id, status='AVAILABLE')
    audit: CREDIT_ISSUED (manual, actor=current_user)
ELIF administrator provides outstanding_declared_amount (with outstanding_description, required together):
    store outstanding_declared_amount/outstanding_description on the subscription row as a formal,
    described annotation for the Outstanding Report/workflow -- this is ADDITIONAL TO, not a
    replacement for, balance_due above.
ELSE:
    no Credit granted, no formal Outstanding annotation -- balance_due still exists and still governs
    payments exactly as if this whole optional section didn't exist.
```
This resolves the tension between decision #1's "Outstanding is manual" framing and decision #21's "reject payment exceeding Outstanding" (sec 8.1): they refer to two different fields. `balance_due` is the ever-present ceiling; `outstanding_declared_amount` is the new optional annotation. Full reasoning in `17-FINAL-DESIGN-SYNCHRONIZATION-REPORT.md` item T-1.

**Status (finalized decision #28):** the subscription is `ACTIVE` **immediately** on creation, regardless of whether `start_date` is in the future. There is no `NEW`/Pending/Scheduled state. Session generation and the subscription's own lifecycle both proceed exactly as before; only the **label** for the initial state changes (was `NEW`, is now `ACTIVE` from the first moment).

### 2.5 Session Generation
Unchanged.

### 2.6 Training Attendance — Window (finalized decision #29, replaces v1's undefined swimmer deadline)
```
Attendance (QR or Manual) is accepted only within:
    [session.scheduled_start_time - 30min, session.scheduled_start_time + 30min]
Outside this window: reject with the specific window shown to the user.
```
**Genuinely unresolved (§12 item 1 below):** whether an Administrator override ("Late Edit") exists for swimmer attendance past this window, mirroring the employee 1-hour-post-end override (decision #17). Not addressed by any closure decision, and — per the "do not invent" instruction — no default is assumed here; the current UI (Doc04 §4.10) presents the window closing as a hard stop pending confirmation.

### 2.7 Pause/Resume
Unchanged.

### 2.8 Renewal (finalized decision #18)
```
IF old subscription has remaining (unattended, non-cancelled) sessions:
    new_start = first available Period occurrence AFTER the old subscription's last scheduled session
ELSE (old subscription fully COMPLETED):
    new_start = first available Period occurrence on/after the renewal request date

new subscription: fully independent row, own price snapshot (at CURRENT config, priced at the moment
    of actual creation -- resolves the earlier "theoretical vs actual timing" ambiguity), own sessions,
    own Credit/Outstanding handling per sec 2.4, linked via renewed_from_subscription_id.

old subscription: status is NEVER force-changed by the renewal event. It continues its own natural
    lifecycle (to COMPLETED, or stays whatever it already was). The 'RENEWED' status value does not
    exist -- the renewed_from_subscription_id link alone carries the relationship.
```

### 2.9 Cancellation
Unchanged from v1 (before-first-session = full refund of paid amount, no fee, no credit; after-first-session = no refund, no credit, paid amount stays as revenue). Refund, when applicable, follows the standard confirm-only flow (§8.2).

---

## 3. Packages

### 3.1 Creation — Session Balance Model (finalized decision #5, replaces v1's "no session concept" framing)
```
total_sessions_snapshot = duration_in_months x package_configs.sessions_per_month   # frozen at creation
sessions_consumed = 0
reference_training_price_snapshot = current Regular-program Training total price   # for the cancellation formula, sec 3.5
total_price = PackageConfig[package_type, swimmer.member_status].price   # unchanged: independent of chosen Period
```
Credit/Outstanding entry at Package creation follows the identical pattern as sec 2.4, extended for consistency (finalized decision #1 names "subscription creation" specifically; applying the same mechanism to Packages is a technical consistency choice, not a new business rule, since Package pricing already otherwise mirrors Training's pattern throughout this design).

**Status (finalized decision #28's principle applied consistently):** `ACTIVE` immediately on creation; no `NEW` state.

### 3.2 Check-in — Session Consumption (finalized decision #5)
```
ON check-in attempt (Training-Package OR Recreational-Package path):
    validate QR / Active / not-past-end_date / correct-Period / capacity   (unchanged checks)
    IF package.sessions_consumed >= package.total_sessions_snapshot:
        reject: "No sessions remaining on this package."
    ELSE:
        insert package_checkins row
        package.sessions_consumed += 1
```
Calendar `end_date` remains the hard ceiling regardless of sessions remaining — reaching it forfeits unused sessions (no extension). Exhausting sessions before `end_date` leaves the Package `ACTIVE` (no new terminal state) but blocks further check-ins with the message above.

### 3.3 Period Change (finalized decision #5, explicit)
Unchanged mechanics (old period governs through day before `change_date`, new from `change_date`) **plus**: `sessions_consumed`/`total_sessions_snapshot` are properties of the Package row itself and are never reset or recalculated by a Period Change — the remaining balance carries forward automatically since nothing about the Package row's session-tracking columns is touched by this event.

### 3.4 Renewal
Same two-branch date logic as Training (sec 2.8); new Package is fully independent, including a **new** `total_sessions_snapshot` computed fresh from current config — the old Package's balance does not carry into a renewal (only a Period Change carries balance, per sec 3.3; renewal starts fresh, consistent with it being an independent new row).

### 3.5 Cancellation (finalized decision #19, resolves v1's floor-at-zero question)
```
IF today < start_date:
    refund = paid_amount   # full refund, no credit
ELIF today <= start_date + 1 month - 1 day:   # "first month" -- SRS worked example (Sep 10 -> Oct 9) stands as the operative boundary
    refund = MAX(0, paid_amount - reference_training_price_snapshot)   # floored at 0, confirmed
    # If the raw result is negative: refund = 0, and NO Outstanding or Credit is created for the shortfall.
ELSE:
    refund = 0   # after first month: no refund, no credit
```

---

## 4. Recreational

### 4.1 Single Entry — Ticket Model (finalized decisions #10, #11, replaces v1's Swimmer-linked design entirely)
```
Fields recorded: visitor_name (free text, NEVER a Swimmer FK), member_status, recreational_period_id,
                 checked_in_at, amount_paid, payment_description, recorded_by.

Validation:
    amount_paid MUST equal the full configured entry fee -- no partial payment, no Outstanding, no
    Credit path exists for this operation at all (finalized, explicit).
    Duplicate check: reject if a recreational_tickets row already exists for
        (recreational_period_id, visitor_name, DATE(checked_in_at))  -- finalized decision #31.
        (Technical caveat, non-business: visitor_name is a weak dedup key given the deliberately
         anonymous ticket model -- false positives on common names are a known, accepted limitation.)

On success: insert recreational_tickets row, insert Transaction(RECREATIONAL_TICKET_PAYMENT, positive).
Reports (ticket count, revenue, Member vs Non-Member counts) read directly off recreational_tickets,
with no Swimmer-table join required at all.
```

### 4.2 Package-Holder Check-in
Unaffected by 4.1's redesign — remains Swimmer/Package-linked via QR exactly as originally designed, now decrementing the Package's session balance (sec 3.2) on success.

### 4.3 Capacity
Unchanged — capacity is shared across both paths for a given Period occurrence, never frees on departure.

---

## 5. Private

### 5.1 Participants (finalized decision #9, replaces v1's Swimmer-only participant model)
```
Each participant row is EITHER:
    {participant_type: SWIMMER, swimmer_id: <existing swimmer>}
    OR
    {participant_type: GUEST, guest_name, guest_phone}   -- no Swimmer record ever created for a Guest
Lane Rental participant count must not exceed the selected Lane's configured capacity (finalized decision #30).
```

### 5.2 Lane Rental
Pricing unchanged (fixed club fee regardless of participant count, up to the Lane's capacity). Lane is now a real managed entity (finalized decision #35) — see sec 5.7.

### 5.3 Coach-Brought
Unchanged: `club_revenue = participant_count × fee_per_swimmer_snapshot`.

### 5.4 Club-Brought — Ordinary Revenue (finalized decision #8, confirms snapshotting)
```
club_share = total_customer_amount x club_percentage_snapshot / 100
coach_share = total_customer_amount x coach_percentage_snapshot / 100
Both percentages frozen at booking creation; later Configuration changes never affect this booking.
On booking creation: insert coach_dues row (source_type='CLUB_BROUGHT_SHARE', amount=coach_share,
    employee_id=coach_id) -- this is how the Coach's ordinary income reaches monthly Payroll
    (finalized decision #4 -- the mechanism the original SRS never described).
```

### 5.5 Attendance
Unchanged — manual only, no QR path for Private.

### 5.6 Cancellation

**Lane Rental / Coach-Brought (unchanged from v1):** before first session = full refund; before completing 2 sessions = 50% refund/50% club-retained; after completing 2 sessions = no refund.

**Club-Brought (finalized decision #2 — fully resolves v1's three-way ambiguity):**
```
# Applies once at least one session has occurred (Club-Brought only)
cancellation_fee = cancellation_fee_configs[current].fee_value_as_percentage x paid_amount / 100
coach_share = cancellation_fee                          # 100% of the fee -> Coach, in full
refund_to_customer = paid_amount - cancellation_fee      # remainder -> customer
# Worked example (finalized): paid=1000, fee%=20 -> coach=200, refund=800.
# The club receives NOTHING from a cancelled Club-Brought booking. club_percentage_snapshot /
# coach_percentage_snapshot are NEVER used in this calculation -- those percentages govern ONLY
# ordinary, non-cancelled revenue (sec 5.4). The two formulas are structurally separate.

On cancellation: insert coach_dues row (source_type='CANCELLATION_FEE', amount=coach_share).
IF a 'CLUB_BROUGHT_SHARE' row was already accrued for this booking at creation (sec 5.4), insert an
    offsetting NEGATIVE coach_dues row (source_type='CLUB_BROUGHT_SHARE', amount=-original_share)
    so the Coach is never paid both the full ordinary share AND the cancellation fee for one booking.
insert refunds row (calculated_amount=refund_to_customer) + Transaction(REFUND, negative).
```

### 5.7 Lane Management (finalized decisions #30, #35)
Lanes are full entities: label, capacity, status — Super-Admin-managed via Configuration. Overlap prevention (unchanged mechanism) now runs against a real managed resource rather than a loosely-defined list.

---

## 6. Employees

### 6.1 Qualification & Rate (finalized decision #6, replaces v1's per-employee/per-tier ambiguity)
```
An employee may hold multiple Qualification rows (via employee_qualifications).
current_rate_for(employee_id, as_of_date):
    highest_qualification = the employee_qualifications row with the HIGHEST rank_order
                             (rank_order convention: higher number = higher qualification, per Doc01/02)
    RETURN qualification_rate_configs[qualification_id=highest_qualification, effective_from <= as_of_date, effective_to IS NULL or > as_of_date].session_rate
Historical payroll/coach_dues snapshot this resolved rate at calculation/accrual time and are
    NEVER retroactively affected by later qualification or rate changes.
```

### 6.2 Creation, Deactivation
Unchanged from v1, with one confirmation: Owner and Super Admin **never** have an Employee record (finalized decision #12) — this is enforced structurally (Doc02 §1 CHECK), not just by convention, so Employee Attendance and Payroll queries never need to filter these roles out — they simply have no rows to find.

---

## 7. Payroll

### 7.1 Coach/Lifeguard Payroll — Merged Formula (finalized decisions #4, #6, #7, replaces v1's Training-attendance-only formula)
```
FOR employee in (COACH, LIFEGUARD), for target (year, month):
    training_dues = SUM(current_rate_for(employee, session.date)) for each session PRESENT this month
                     (0 for ABSENT/EXCUSED)
    replacement_dues = SUM(rate_applied_snapshot) for each employee_replacements row where this
                        employee was the REPLACEMENT this month
    private_dues = SUM(coach_dues.amount) WHERE employee_id=this AND consumed_in_payroll_id IS NULL
                   AND source_type IN ('CLUB_BROUGHT_SHARE','CANCELLATION_FEE') this month
    calculated_amount = training_dues + replacement_dues + private_dues
    insert payrolls row (status='NOT_PAID')
    UPDATE consumed coach_dues rows: SET consumed_in_payroll_id = this payroll's id  -- prevents double-counting
                                                                                    across payroll runs
    audit: PAYROLL_CALCULATED (system)
```
**Cadence confirmed monthly for all employee types** (finalized decision #7 — previously unconfirmed for Coach/Lifeguard; Administrator was already monthly).

### 7.2 Administrator Payroll
Unchanged: `net = monthly_salary − (absent_days × daily_value)`.

### 7.3 Mark Paid
Unchanged: Super Admin/Owner/Administrator can all mark Paid; creates the `PAYROLL_PAYMENT` Transaction.

### 7.4 Paid Payroll Correction (finalized decision #23, new capability)
```
Once payrolls.status = 'PAID', the row is PERMANENTLY LOCKED -- no in-place edit, ever.
AdjustPayrollUseCase(payroll_id, adjustment_amount, reason):
    insert Transaction(PAYROLL_ADJUSTMENT, amount=adjustment_amount [signed], related_entity_id=payroll_id,
                        created_by=actor)   -- no separate adjustment table; the Transaction row IS the record
    audit: PAYROLL_ADJUSTED {payroll_id, adjustment_amount, reason, actor}
Original payrolls row and its original PAYROLL_PAYMENT transaction are NEVER touched.
Payroll Report must sum original PAYROLL_PAYMENT + all PAYROLL_ADJUSTMENT transactions per
    employee/period (via related_entity_id) to show the true net paid.
```

### 7.5 Employee Attendance Edit Deadline (finalized decision #17, confirms v1)
1-hour post-Period-end deadline for Coach/Lifeguard attendance; edits after that are `is_late_edit = TRUE`, classified "Late Administrative Edit," and audited. Unchanged.

### 7.6 Replacement
Unchanged.

### 7.7 Administrator Attendance (finalized decision #32, confirms v1)
Separate `administrator_daily_attendance` table (Administrator, Date, Present/Absent); Payroll consumes `absent_days` from it. Unchanged.

---

## 8. Finance

### 8.1 Payments & Overpayment (finalized decision #21)
```
RecordPaymentUseCase(payable, amount):
    IF amount > balance_due:   # balance_due = total_price - paid_amount, ALWAYS computed (sec 2.4) --
                                # this is the ceiling decision #21 refers to, regardless of whether a
                                # separate outstanding_declared_amount annotation exists on the record.
        REJECT -- no Payment row, no Transaction row created
        audit: PAYMENT_OVERPAYMENT_REJECTED (failure, success=false)
    ELSE:
        insert payments row
        insert Transaction (type per payable: TRAINING_PAYMENT / PACKAGE_PAYMENT / PRIVATE_PAYMENT
                             / OUTSTANDING_PAYMENT if this is a later payment against an existing balance)
        balance_due recomputes automatically (generated column, sec 2.4)
```

### 8.2 Payment Correction (finalized decision #22, new capability — architectural exception)
```
CorrectPaymentUseCase(payment_id, new_amount, reason):
    UPDATE payments SET amount=new_amount, last_modified_by, last_modified_at
    UPDATE the ONE linked transactions row SET amount=new_amount  -- technical necessity, not a new
        business rule: preserves the SUM(transactions)=Profit reconciliation invariant (Doc11 sec 8.9)
        that the finalized decision text does not itself address, since it says "no separate
        correction transaction is required" but is silent on whether the existing one should be
        left stale (which would silently break revenue reporting).
    parent.balance_due recomputes automatically from the corrected paid_amount (generated column)
    audit: PAYMENT_CORRECTED {old_amount, new_amount, actor, timestamp, reason}
```
This is the **only** code path in the entire system permitted to UPDATE a `transactions` row.

### 8.3 Refunds & Refund Correction (finalized decision #24, new capability)
Refund confirmation flow unchanged (system-computed, admin confirms only, never edits the number). **New:**
```
Once a Refund is confirmed, it is PERMANENTLY LOCKED.
AdjustRefundUseCase(refund_id, adjustment_amount, reason):
    insert Transaction(REFUND_ADJUSTMENT, amount=adjustment_amount [signed], related_entity_id=refund_id,
                        created_by=actor)   -- no separate adjustment table; mirrors the Payroll pattern exactly
    audit: REFUND_ADJUSTED {refund_id, adjustment_amount, reason, actor}
Original refunds row and its original REFUND transaction are NEVER touched.
```

### 8.4 Credit (finalized decision #1 — resolves v1's "no generation trigger" gap entirely)
```
Credit is NEVER system-computed and NEVER produced by any cancellation path (Training/Package/Private
cancellations continue to resolve to Refund or nothing, exactly as the SRS states literally -- that
literal reading is now confirmed correct).
Credit's ONLY generation path: a manual, Administrator-entered amount + required description, offered
    as an option at Training Subscription or Package creation (sec 2.4, 3.1), mutually exclusive with
    a declared-Outstanding annotation on the same creation event. generated_from_type is set to
    'TRAINING_SUBSCRIPTION' or 'PACKAGE' matching whichever entity the grant was made on
    (generated_from_id = that entity's own PK) -- consistent with every other polymorphic
    type/id pair used elsewhere in this schema (e.g. payments.payable_type).
```

### 8.5 Credit Usage (finalized decision #34, confirms v1)
Partial usage allowed (Credit 500, use 300, remaining 200). Usable **only** for Training/Package, **never** Private or Recreational (Single Entry's full-payment-only rule, sec 4.1, wouldn't accept it regardless). Revenue recognized at usage date via `Transaction(CREDIT_USAGE, positive)`, not at grant date.

### 8.6 Expenses
Unchanged.

### 8.7 Revenue / Profit — see `11-Cross-Module-Workflows.md` §8 for the full, corrected derivation (this is the section that directly answers the "Revenue must not be naive SUM(all positive transactions)" requirement).

---

## 9. Users

### 9.1 Creation, Username Collision (finalized decision #13, replaces v1's numeric-suffix recommendation)
```
username = employee.name  (normally)
ON collision against an existing users.username:
    require employee.nickname (prompt if not already set on the Employee record)
    username = employee.name + ' ' + employee.nickname   -- e.g. "Ahmed Mohamed" + "Hamo" -> "Ahmed Mohamed Hamo"
    (nickname does NOT alter employee.name, the official record)
    IF this regenerated username is ITSELF a collision (rare): resurface the error, no auto-numbering.
```

### 9.2 Deactivation & Reactivation (finalized decision #15, adds Reactivation)
```
Deactivate: is_active=FALSE, deactivated_at=now(). (unchanged)
Reactivate: is_active=TRUE, deactivated_at=NULL. Username and password_hash are BOTH preserved
    unchanged -- no forced reset. Audited: USER_REACTIVATED.
```

### 9.3 Password Reset (finalized decision #16, new capability)
```
Flow A -- Owner/Super Admin resets an Administrator's password:
    input: target Administrator's National ID
    validate against employees.national_id for the Employee linked to that User
    mismatch -> reject, audit PASSWORD_RESET (success=false)
    match -> reissue password, audit PASSWORD_RESET (success=true)

Flow B -- Administrator self-reset:
    input: own Username + own National ID
    both must match the Administrator's own record
    same success/failure audit pattern as Flow A

Password is NEVER displayed except the standard one-time reveal.
Resulting value (technical decision, not a business rule — see 17-FINAL-DESIGN-SYNCHRONIZATION-REPORT.md
    item T-2): reset reseeds the password hash to the employee's National ID, identical to the original
    account-creation convention (SRS sec 4.1). This was chosen because it requires no new secure-random-
    display UI, matches an already-established pattern, and does not change any user-facing permission
    or financial behavior — it is an implementation default, not a reopened business question.
```

---

## 10. Backup / Restore (finalized decisions #25, #26)

### 10.1 Encryption
All backups (Automatic and Manual, every destination type) are **always** encrypted. Restore decrypts as its first step, before version-compatibility checking.

### 10.2 Automatic Backup Retry
```
attempt = 1
WHILE attempt <= 5:
    result = try_create_backup()
    IF result.success: audit BACKUP_CREATED_AUTO; DONE
    ELSE: attempt += 1   -- immediate retry, no described backoff delay
IF attempt > 5 (all 5 failed):
    audit BACKUP_FAILED_ALL_ATTEMPTS (system, details={attempts:5})
    surface a PERSISTENT warning to Super Admin (until acknowledged or a successful Manual Backup occurs)
    system continues operating normally -- never blocks normal use
```

---

## 11. Language (finalized decision #36)

System-wide `system_settings.system_language`, set during Initial Program Setup, applies uniformly to UI and Reports for every user — there is no per-user language field anywhere in the domain model. See `04-UI-UX-Specification.md` §1–2 for the UI-level treatment.

---

## 12. Consolidated Remaining `DECISION REQUIRED` Items in This Document

Only genuine, unresolved **business-behavior** gaps are listed here — every item that could be closed as a non-business technical default (Outstanding computation, password reset value) has been resolved above and removed from this list.

1. **Swimmer attendance late-edit override** — does an Administrator override path exist after the ±30-minute window closes, mirroring the employee 1-hour late-edit pattern, or is post-window entry a permanent, unconditional block? (§2.6)
2. **Recreational duplicate check-in enforcement mechanics** — given the ticket model's deliberate anonymity (§4.1), is the block on a repeat `(period, name, date)` combination literal/unconditional, or an overridable soft warning? Decisions #10/#11 (anonymity) and #31 (duplicate blocking) are in tension and neither resolves the other.
3. **Package cancellation "first month" boundary** — is the boundary day itself counted as "during" or "after" the first month? (§3.5)
4. **Private Lane Rental / Coach-Brought cancellation tier boundary** — does "before completing 2 sessions" include exactly 1 completed session, or strictly fewer? (§5.6)

All four are non-blocking, narrowly scoped, and can be resolved by whoever owns each specific screen when it is actively built, without stalling any other module.
