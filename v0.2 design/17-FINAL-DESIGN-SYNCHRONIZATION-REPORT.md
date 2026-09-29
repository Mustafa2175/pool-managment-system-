# 17 — Final Design Synchronization Report
## Swimming Pool Management System

**Source-of-truth hierarchy applied throughout this pass:** `Design_Closure_Decisions.md` (primary) → `15-Design-Closure-Amendments.md` (decision-by-decision reasoning) → SRS (`Swimming_Pool_Management_System_SRS_v1_0.md`) → prior design documents (01–14, pre-closure). Where a closure decision changed a structural choice made in an earlier draft, the earlier choice was removed from the affected documents, not appended alongside it.

---

## A. Documents Reviewed

All of the following were reviewed in full against the 36 finalized closure decisions:

`Swimming_Pool_Management_System_SRS_v1_0.md`, `Design_Closure_Decisions.md`, `01-ERD.md`, `02-Database-Schema.md`, `03-System-Architecture.md`, `04-UI-UX-Specification.md`, `05-Business-Logic.md`, `06-Validation-Rules.md`, `07-State-Machines.md`, `08-Security-and-Authorization.md`, `09-Audit-Log.md`, `10-Backup-Restore-and-Migration.md`, `11-Cross-Module-Workflows.md`, `12-Edge-Cases-and-Decisions.md`, `13-Requirements-Traceability.md`, `14-Implementation-Readiness-Review.md`, `15-Design-Closure-Amendments.md`, `15-Design-Closure-Addendum.md` (an independently-produced alternative, rated on request and cross-checked for reconciliation value), `16-Implementation-Task-List.md` (same).

---

## B. Documents Modified

Every one of Docs 01–14 required modification; none were left as pure pre-closure text. Summary of what changed in each:

| Document | Nature of change |
|---|---|
| `01-ERD.md` | Full rewrite. New entities (`Qualification`, `EmployeeQualification`, `QualificationRateConfig`, `CoachDue`, `Lane` upgraded to full entity, `PrivateBookingParticipant` replacing `PrivateBookingSwimmer`, `RecreationalTicket` replacing `RecreationalCheckIn`). Removed `NEW`/`RENEWED` status values across three entities. Added the Credit/Outstanding reconciliation note as the document's opening section, since it governs how every other document must read those two fields. |
| `02-Database-Schema.md` | Full rewrite of every changed table's DDL. Dropped `employee_rate_configs`, `recreational_checkins`, `private_booking_swimmers`. Added `qualifications`, `employee_qualifications`, `qualification_rate_configs`, `coach_dues`, `recreational_tickets`, `private_booking_participants`. Added `balance_due` as a generated column distinct from the new manual `outstanding_declared_amount`/`credit_granted_amount` pair. Documented the one sanctioned Transaction-immutability exception explicitly in schema comments. |
| `03-System-Architecture.md` | Targeted edits: harmonized entity/repository naming to match the finalized ERD (`QualificationRateConfig` not `EmployeeRateConfig`, `CoachDue` not `CoachDueEntry`), added the Initial Program Setup use case, corrected the Payment-correction sequence diagram's field reference from `outstanding_amount` to `balance_due`. |
| `04-UI-UX-Specification.md` | Targeted edits: rewrote the Credit/Outstanding subsection of subscription creation to reflect the reconciled two-field model instead of a single ambiguous override; corrected a passage that had incorrectly *asserted* a swimmer attendance late-edit override exists (removed the invented behavior, replaced with an explicit "genuinely unresolved" note and a hard-stop-by-default UI); harmonized `QualificationTier` → `Qualification` naming. Confirmed the document already covered the ticket-based Recreational redesign, Lane Management screen, Qualifications section, User Reactivation/Password Reset flows, and Payroll/Refund Adjustment and Payment Correction UI correctly. |
| `05-Business-Logic.md` | Targeted edits across nine subsections: rewrote the Credit/Outstanding logic (§2.4) to the reconciled model; fixed the Payroll rate-resolution function, which had an inverted `rank_order` convention (was resolving to the *lowest*-ranked qualification, contradicting the ERD's "higher number = higher qualification" rule); corrected `coach_dues.source_type` values from an inconsistent `PRIVATE_SHARE`/`PRIVATE_CANCELLATION_FEE` naming to the schema-canonical `CLUB_BROUGHT_SHARE`/`CANCELLATION_FEE`; removed references to now-nonexistent `payroll_adjustments`/`refund_adjustments` tables, replacing them with the correct "new Transaction row only, no new table" mechanism; resolved the Password Reset resulting-value question as a technical default; narrowed the consolidated remaining-`DECISION REQUIRED` list from an inconsistent count down to the 4 genuine items. |
| `06-Validation-Rules.md` | Full rewrite. Added validation rows for Credit/Outstanding mutual exclusivity, Package session-balance check-in, Lane capacity, Guest participant entry, Username-nickname collision, Password Reset identity matching, User Reactivation, Payment/Payroll/Refund correction and locking rules, System Language setting. Corrected the overpayment rule to reference `balance_due` explicitly rather than the ambiguous `outstanding_amount`. |
| `07-State-Machines.md` | Full rewrite. Removed `NEW`/`RENEWED` states from all three affected diagrams. Added Payment's new correctable-in-place behavior, Payroll's and Refund's lock-then-adjust patterns, User reactivation, Period edit-authority resolution, Backup's retry/encryption states. Corrected the Swimmer-status coupling table to permanently and explicitly exclude Private Bookings. |
| `08-Security-and-Authorization.md` | Targeted edits: fully specified Password Reset (two flows) and its resulting value, User Reactivation authorization, mandatory Backup encryption (removing the prior open question), five new authorization rules (`CORRECT_PAYMENT`, `ADJUST_PAYROLL`/`ADJUST_REFUND`, `RESET_PASSWORD`, `REACTIVATE_USER`, `MANAGE_LANES`, `MANAGE_QUALIFICATIONS_AND_RATES`), rewrote the summary table to move three items from RED to GREEN. |
| `09-Audit-Log.md` | Targeted edits: added roughly a dozen new event codes across Training, Packages, Recreational, Private, Employees/Payroll, Finance, Users, and Backup categories. Substantially narrowed (though did not fully close) the audit-coverage-scope question using the closure decisions' own explicit examples (Overpayment, Duplicate Check-in, Password Reset failures) as precedent. Documented the one Transaction-immutability exception explicitly and confirmed it does not extend to `audit_logs`, which remains unconditionally insert-only. |
| `10-Backup-Restore-and-Migration.md` | Targeted edits: resolved mandatory encryption (removing the RED item), fully specified the 5-attempt retry/failure/notification flow (removing the RED item), added a new §5 documenting the adopted key-management technical approach, updated the summary table. |
| `11-Cross-Module-Workflows.md` | Full rewrite, most significantly the **Financial Integrity Appendix** (§10), which now uses an explicit `transaction_type` → financial-meaning classification table and formula instead of any form of "sum of positive transactions," per the synchronization brief's specific requirement. Added new sequence diagrams for Package session consumption, Payment Correction, and the fully-resolved Club-Brought cancellation flow. |
| `12-Edge-Cases-and-Decisions.md` | Targeted edits: corrected two items (C5, F3) that had been marked ✅ resolved by *inventing* an answer the closure decisions never gave — downgraded both back to 🟡 genuinely open, per the "do not invent" instruction. Resolved four of the five narrow flags the closure decisions themselves surfaced (N1, N2, N4, N5) as technical decisions; left the fifth (N3) correctly folded into the still-open F3 rather than silently closed. Updated all summary counts. |
| `13-Requirements-Traceability.md` | Full rewrite. Added roughly 20 new/changed rows covering every closure decision with schema/logic/UI impact, corrected all entity and table names to the synchronized ERD/Schema, added three new module groupings implied by the decisions (Coach Dues report, Recreational Ticket report, Qualifications & Rates configuration). |
| `14-Implementation-Readiness-Review.md` | Targeted edits: moved all 4 previously-critical RED items to GREEN, moved 4 of 5 narrow closure-decision flags from a pending-RED framing to resolved-YELLOW/GREEN, rewrote the consistency-check table to reflect the actual reconciliation work (not just a restated intention to reconcile), rewrote the recommended path forward to reflect that implementation may begin immediately. |

---

## C. Decisions Propagated

Every one of the 36 finalized decisions was traced to its concrete effect on the design and propagated into every document it touches. Full decision-by-decision detail (including the exact schema DDL, business-logic pseudocode, and audit events each decision produces) lives in `15-Design-Closure-Amendments.md`, cross-checked against the final synchronized Docs 01–13 during this pass; the table below is a compact index, not a duplicate of that detail.

| Decision | Documents affected |
|---|---|
| D1 Credit/Outstanding | 01, 02, 04, 05, 06, 09, 11, 12, 13 |
| D2 Club-Brought cancellation fee | 01, 02, 05, 06, 07, 09, 11, 13 |
| D3 Training price = flat total | 01, 02, 05, 06, 13 |
| D4 Club-Brought coach income → dues → payroll | 01, 02, 03, 05, 07, 09, 11, 13 |
| D5 Package sessions | 01, 02, 04, 05, 06, 07, 09, 11, 13 |
| D6 Qualification-based rate | 01, 02, 03, 04, 05, 06, 08, 13 |
| D7 Monthly Coach/Lifeguard payroll | 01, 02, 05, 07, 11, 13 |
| D8 Private split snapshot | 01, 02, 05, 12 (confirmed, no change needed) |
| D9 Private participants (Swimmer/Guest) | 01, 02, 04, 05, 06, 13 |
| D10/D11 Recreational ticket model, full payment | 01, 02, 04, 05, 06, 09, 11, 13 |
| D12 Owner/Super Admin are Users only | 01, 02, 06, 07, 13 |
| D13 Username collision → Nickname | 01, 02, 06, 08, 13 |
| D14 Training Period editing authority | 01, 05, 06, 07, 13 |
| D15 User reactivation | 01, 06, 07, 08, 09, 13 |
| D16 Password reset | 05, 06, 08, 09, 13 |
| D17 Late attendance edit | 01, 05 (confirmed, no change needed) |
| D18 Training renewal, no forced status | 01, 02, 05, 07, 13 |
| D19 Package cancellation refund floor | 01, 05, 06, 11, 12 |
| D20 Swimmer status excludes Private | 01, 05, 06, 07, 13 |
| D21 Payment overpayment (reconciled with D1) | 01, 02, 05, 06, 09, 12, 14 |
| D22 Payment correction (Transaction sync resolved) | 01, 02, 05, 06, 07, 09, 12, 13, 14 |
| D23 Paid Payroll correction | 01, 02, 05, 06, 07, 09, 11, 12, 13 |
| D24 Refund correction | 01, 02, 05, 06, 07, 09, 13 |
| D25 Backup encryption | 01, 02, 08, 10, 12, 14 |
| D26 Automatic backup failure | 01, 07, 09, 10 |
| D27 Fixed roles/permissions | 01, 08, 12 (confirmed) |
| D28 Training subscription active immediately | 01, 02, 05, 06, 07, 13 |
| D29 Training attendance window | 01, 04, 05, 06, 09, 13 |
| D30 Lane Rental capacity | 01, 02, 05, 06, 13 |
| D31 Recreational duplicate check-in | 01, 02, 05, 06, 09, 12, 13 |
| D32 Administrator attendance | 01, 13 (confirmed) |
| D33 Schedule defaults | 01, 06 (confirmed) |
| D34 Credit usage, partial allowed | 01, 05, 11, 12 (confirmed) |
| D35 Lane management | 01, 02, 04, 13 |
| D36 Language/numerals | 01, 02, 04, 13 |

---

## D. Contradictions Found

Four genuine contradictions were found and resolved during this pass — two between the finalized decisions themselves, and two introduced by an earlier, less rigorous draft of the synchronized documents.

### D-1. Decision 1 ("Outstanding is manual") vs. Decision 21 ("reject Payment > Outstanding")
- **Old behavior (pre-closure):** `outstanding_amount` was a single, automatically-computed field (`total_price − paid_amount`), used both as the payment ceiling and as the general "amount owed" figure.
- **Conflicting instructions:** Decision 1 says Outstanding is now manually entered by the Administrator and may not exist at all ("if neither is entered, the subscription has no Credit or Outstanding"). Decision 21 requires rejecting any payment exceeding "Outstanding" — but if Outstanding might not exist, there is no ceiling to check against.
- **Resolution:** two distinct fields. `balance_due` (`total_price − paid_amount`) remains always-computed and is what Decision 21's ceiling refers to. `outstanding_declared_amount` is Decision 1's new, separate, optional, additive annotation for the Outstanding report/workflow. Neither decision is reinterpreted — this connects them without contradicting either.
- **Affected documents:** 01, 02, 04, 05, 06, 09, 11, 12, 13, 14.

### D-2. Decision 22 ("no separate correction transaction required") vs. the pre-existing Transaction-immutability principle
- **Old behavior:** `transactions` rows were unconditionally insert-only across the entire system (documented explicitly in the pre-closure Doc 09 §5).
- **Conflicting instructions:** Decision 22 requires that correcting a Payment directly updates the Payment record, with "no separate correction transaction required" — but the ledger's revenue-reconciliation invariant (Doc 11 §10.9) requires that `SUM(classified transactions)` always equal recognized revenue, which would silently break if the Payment's linked Transaction were left at its old, now-wrong amount.
- **Resolution:** one explicit, narrow, documented exception — correcting a Payment also updates its one linked Transaction's amount, in the same operation. This is the only code path in the entire system permitted to `UPDATE` a `transactions` row; Payroll and Refund corrections (Decisions 23, 24) deliberately do **not** use this pattern — they insert new adjustment Transactions instead, per those decisions' own text.
- **Affected documents:** 01, 02, 03, 05, 06, 07, 09, 12, 13, 14.

### D-3. An earlier draft of Doc 12 had *invented* a resolution to the swimmer attendance late-edit question
- **Old behavior (a defect introduced during an earlier, less rigorous synchronization attempt, not part of any official prior deliverable):** Doc 12 item C5 had been marked ✅ resolved, asserting "entry outside the window requires an administrative override, audited by analogy to the employee late-edit rule" — but no closure decision says this. Doc 04's UI spec had likewise been written to *assume* this override exists.
- **Resolution:** reverted to 🟡 genuinely open in both documents. This is not a new business decision — it is the removal of a previously invented one, restoring compliance with the "do not invent a business rule" instruction. The UI now defaults to a hard stop at the window boundary, pending confirmation.
- **Affected documents:** 01, 04, 05, 12.

### D-4. An earlier draft of Doc 12 had similarly *softened* the Recreational duplicate-check-in rule without authorization
- **Old behavior:** Doc 12 item F3 had been marked "✅ (with a caveat)," implicitly treating a recommended softening (warning-with-override, to compensate for the ticket model's unreliable name-only identity key) as though it were settled.
- **Resolution:** reverted to 🟡 genuinely open. Decision 31's literal text (hard rejection) is applied as written pending confirmation, since Decisions 10/11 (removing the identity key) and Decision 31 (requiring the rejection) are two closure decisions in real tension with each other — neither resolves the other, and picking a side is a genuine product choice, not a technical implementation detail.
- **Affected documents:** 01, 05, 06, 12.

---

## E. New Technical Decisions

These are implementation defaults that resolve necessary mechanical questions without changing any business-visible behavior, money, or permission. They are clearly separated from the business decisions in Section D above and from the genuinely-open business questions in Section F below.

| ID | Technical Decision | Why it's technical, not business |
|---|---|---|
| T-1a | Payment correction (D22) also updates its one linked Transaction's amount, in the same operation — the sole exception to Transaction immutability in the system. | Necessary purely to keep the ledger internally consistent; changes no dollar amount or permission, only *how* consistency is maintained. |
| T-1b | Overpayment (D21) is checked against `balance_due`, a field distinct from D1's `outstanding_declared_amount`. | A naming/field-mapping clarification, not a reinterpretation of either decision's stated behavior. |
| T-2 | Password reset (D16) reseeds the password to the employee's National ID. | The closure decision specifies the verification mechanism, not the resulting value; National ID matches the pre-existing account-creation convention and requires no new UI for displaying a random password. |
| T-3 | Backup encryption (D25) uses an OS-secured key store (DPAPI/Keychain/permissioned key file), referenced per-backup. | The closure decision mandates encryption; it does not specify a mechanism. This choice affects only implementation internals, never what the Super Admin sees or does. |
| T-4 | Qualification `rank_order` is a Super-Admin-maintained ordered list, not derived from any external source. | Needed to make "highest qualification" computable at all; consistent with every other Super-Admin-configured list in this system. |
| T-5 | `PAYROLL_ADJUSTMENT`/`REFUND_ADJUSTMENT` are added as new `transaction_type` enum values rather than reusing `PAYROLL_PAYMENT`/`REFUND`. | Reporting clarity only — lets Reports distinguish an original run from a later correction; does not change any computed total. |
| T-6 | A Coach's Club-Brought ordinary revenue share (D4) accrues to `coach_dues` as one lump entry at booking creation, with an offsetting entry inserted on cancellation, rather than accruing per-session. | D4 specifies the destination (Payroll dues) and the cancellation math (D2) separately; this is the simplest timing model consistent with both, and matches Club-Brought's own flat (non-session-metered) customer pricing. |

---

## F. Remaining `DECISION REQUIRED` — Genuinely Unresolved Business Decisions

Exactly **4 items** remain — every one of them narrowly scoped to a single screen or formula's edge case, none affecting the core financial model, data structure, or permission system, and none blocking the start of implementation on any module.

1. **Swimmer Training-attendance late-edit override.** Does an Administrator override path exist after the ±30-minute window closes (Decision 29), mirroring the employee 1-hour late-edit pattern (Decision 17)? Not addressed by any closure decision. *Affects:* the Attendance screen's edit-permission logic only.
2. **Recreational duplicate-check-in enforcement mechanics.** Decision 31 requires blocking a duplicate check-in; Decisions 10/11 removed the only reliable identity key from the ticket model (it is free-text `name` only). Is the block literal and unconditional, or an overridable warning? Two closure decisions are in genuine tension; neither resolves the other. *Affects:* the Recreational check-in screen's UX only.
3. **Package cancellation "first month" boundary.** Is the boundary day itself (e.g., day 30 or 31) counted as "during" or "after" the first month for the refund formula (Decision 19)? Financially relevant, not re-confirmed by the closure decisions beyond the SRS's original worked example. *Affects:* the Package cancellation formula's edge case only.
4. **Private (Lane Rental/Coach-Brought) cancellation tier boundary.** Does "before completing 2 sessions" (governing the 50%-refund tier) include exactly 1 completed session, or strictly fewer? Financially relevant, not addressed by any closure decision. *Affects:* the Private cancellation formula's edge case only.

No other business decision remains open anywhere in this design. All four items above are tracked identically in `01-ERD.md` §4, `05-Business-Logic.md` §12, `06-Validation-Rules.md` (inline), and `12-Edge-Cases-and-Decisions.md` §"Remaining Open Item Count" — there is exactly one canonical list, referenced consistently, not four different lists that could drift apart.

---

## G. Implementation Readiness

# **GREEN**

The 4 previously-critical blocking items (Credit generation trigger, Club-Brought cancellation math, Training price granularity, Private income vs. Payroll) are fully resolved. The 2 genuine cross-decision contradictions found during this synchronization pass (Section D) are resolved with documented, non-arbitrary reconciliations. The 2 previously-invented "resolutions" found in an earlier draft (Section D-3, D-4) have been reverted to honestly-open status rather than left as silent errors. All 6 new technical decisions (Section E) are implementation defaults that change no business behavior. The 4 remaining genuine business gaps (Section F) are narrow, named, and non-blocking.

Every one of Docs 01–14 has been directly edited in place — none rely on "see Doc 15/17 for the real answer" as a substitute for stating the resolved rule where it belongs.

---

## H. AI Coding Agent Handoff

**Authoritative documents, in priority order:**
1. `Design_Closure_Decisions.md` — the finalized business decisions themselves; never contradict these.
2. `01-ERD.md` through `14-Implementation-Readiness-Review.md` (this synchronized set) — the complete, internally-consistent technical design. Read `01-ERD.md`'s opening reconciliation note before touching anything related to Credit, Outstanding, or payment ceilings — it is the single most consequential clarification in the whole package.
3. `15-Design-Closure-Amendments.md` — supplementary decision-by-decision reasoning; useful for *why*, but where it conflicts with 01–14 on naming or on the Credit/Outstanding model, **01–14 wins** (see the notice at the top of Doc 15 itself).
4. `12-Edge-Cases-and-Decisions.md` §"Remaining Open Item Count" and this report's Section F — the complete, exhaustive list of what is *not yet* decided. There are exactly 4 such items in the entire system.

**Rules for the coding agent:**
- Do not invent a resolution to any of the 4 items in Section F. If implementation of a specific screen/formula requires an answer to one of them, stop and ask, rather than guessing a default — even though this report offers no default for them, unlike the technical decisions in Section E which *are* safe to rely on as-is.
- Do not reintroduce `NEW` or `RENEWED` as status values anywhere — they were deliberately removed (Decisions 28, 18).
- Do not conflate `balance_due` (always computed, governs payment ceilings) with `outstanding_declared_amount`/`credit_granted_amount` (optional, manual, additive annotations) — this is the single most likely source of a financial-correctness bug if misread.
- Do not add a second exception to Transaction immutability beyond the one documented (Payment correction, T-1a). Payroll and Refund corrections use new adjustment Transactions, never in-place edits.
- Treat `audit_logs` as unconditionally insert-only — it has zero exceptions, unlike `transactions`, which has exactly one.

**What must be tested (minimum bar before considering any module complete):**
- The revenue-reconciliation invariant (`11-Cross-Module-Workflows.md` §10.9): classified Transaction sums always equal the Profit report, for every period, including after a Payment correction.
- Every state machine transition table in `07-State-Machines.md`, both allowed and rejected transitions.
- The Club-Brought cancellation worked example (Paid 1,000, Fee 20% → Coach 200, Customer refund 800, Club 0) as a literal regression test.
- Package session-pool exhaustion blocking check-in while calendar time remains, and calendar expiry forfeiting any remaining session balance.
- Every row in `06-Validation-Rules.md`, both the accept and reject paths.
- Crash-injection mid-transaction for at least one multi-step financial workflow (e.g., Subscription creation with a Credit grant), confirming the Unit-of-Work either commits everything or nothing.

**Build order recommendation** (no blocking dependencies remain): `14-Implementation-Readiness-Review.md` §5 gives the full sequencing; in short — Foundation (Users/Roles/Employees/Qualifications/Configuration/Lanes/Language Setup/Audit/Backup) → Core (Swimmers/Training/Finance including Credit-Outstanding and Payment Correction) → Packages (session pool) → Private (Guest participants, Club-Brought dues) → Recreational (ticket model) → Payroll (three-source monthly aggregation) → Reports.
