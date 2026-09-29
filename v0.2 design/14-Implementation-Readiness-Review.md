# 14 — Implementation Readiness Review
## Swimming Pool Management System
### SYNCHRONIZED — supersedes all prior versions of this document

**Depends on:** `Design_Closure_Decisions.md` (primary authority), `15-Design-Closure-Amendments.md` (this document's decision-by-decision mapping), and all prior design documents (01–13), fully synchronized. This revision supersedes the pre-closure readiness review in its entirety. **This is the gate: implementation can now begin.**

---

## 1. GREEN — Ready for Implementation

The original 44 GREEN items (pre-closure Doc14 §1) all remain valid and unchanged. In addition, of Doc12's full catalogue of previously-tracked open items, 34 are now resolved by the finalized business decisions and added to GREEN below (exact tally and per-item mapping is authoritative in `12-Edge-Cases-and-Decisions.md`, not restated number-for-number here to avoid drift between the two documents):

**Newly GREEN (from closure decisions, Doc15):**
- Credit/Outstanding creation mechanism — manual, mutually exclusive, at subscription/package creation (D1)
- Club-Brought cancellation fee math (fee 100% to coach, remainder refunded, club split doesn't apply at cancellation) (D2)
- Training price = flat total per subscription (D3)
- Coach Club-Brought/cancellation-fee income flows through monthly Payroll via `coach_dues` (D4)
- Package session-pool model (duration × sessions/month, consumed by attendance, forfeited at expiry) (D5)
- Qualification-based (not per-employee) rate structure, using highest-ranked qualification, non-retroactive (D6)
- Coach/Lifeguard monthly payroll cycle scope (Training + Private + replacement dues) (D7)
- Club-Brought split snapshotted at booking creation (D8)
- Private participants may be Swimmer or Guest (D9)
- Recreational Single Entry as a standalone ticket record, no Swimmer link required (D10)
- Single Entry always fully paid, no partial/Outstanding/Credit (D11)
- Owner/Super Admin are Users only, never Employees (D12)
- Username collision resolved via required Nickname (D13)
- Administrator has full Period edit authority (schedule/capacity/staff); Super Admin creates only (D14)
- User reactivation permitted, history intact (D15)
- Password reset flows (Owner/SuperAdmin→Administrator; Administrator self-reset) (D16)
- Late Administrative Edit terminology and 1-hour rule confirmed (D17)
- Renewal timing and old/new subscription independence (D18)
- Package cancellation refund floors at zero, no Outstanding/Credit on negative (D19)
- Swimmer status excludes Private entirely (D20)
- Overpayment rejected outright, audited (D21)
- Direct Payment correction with audit trail (D22)
- Paid Payroll locked; corrections via Adjustment Transaction (D23)
- Confirmed Refund locked; corrections via Adjustment Transaction (D24)
- All backups encrypted (D25)
- Automatic backup: 5-attempt retry, then notify + require manual (D26)
- Roles/permission matrix fully fixed, no reassignment (D27)
- Training Subscription is Active immediately, no visible NEW state (D28)
- Swimmer Training attendance ±30-minute window (D29)
- Lane Rental capacity is per-Lane configured value (D30)
- Recreational duplicate check-in explicitly rejected and audited (D31)
- Administrator daily attendance as a separate table, confirmed (D32)
- Schedule Defaults are templates only, not a whitelist (D33)
- Partial Credit usage across multiple transactions confirmed (D34)
- Full Lane Management entity/screen, Super-Admin-controlled (D35)
- System-wide language setting chosen at initial Setup, applies to UI+Reports, no per-user override, single-language data storage (D36)

**Total: 44 original + 34 newly resolved = 78 GREEN items.**

---

## 2. YELLOW — Technical Design Choices (unchanged in kind, list updated)

Original 14 YELLOW items from the pre-closure review remain, **minus** items now superseded by closure decisions, **plus** the technical choices the closure decisions introduced — all five of the narrow flags originally surfaced by the closure decisions (N1, N2, N4, N5, plus N3's technical half) have now been resolved and moved here or to GREEN; only N3's genuine *business*-behavior half remains in §3.

**Still open (unchanged):**
- Database engine choice (SQLite vs. embedded Postgres)
- Application language/runtime and UI framework
- Surrogate key type
- Polymorphic association modeling approach
- Config table versioning mechanism
- Dashboard KPI set
- Session idle-timeout (A6)
- App-not-running auto-backup trigger timing (I2's technical residue)
- Orphaned backup file handling (I4)
- Inline "create login from Employee" UX shortcut
- Hard-delete grace window for same-day employee mistakes
- `OUTSTANDING_PAYMENT` type retained as designed (confirmed useful, not removed — moved from open question to settled technical choice)
- Migration-chain version-compatibility scheme specifics (I1, still open, unaffected by closure)

**Resolved and moved here from the closure decisions' narrow flags (now settled technical decisions, not business questions — full reasoning in `17-FINAL-DESIGN-SYNCHRONIZATION-REPORT.md` §E):**
- **T-1a:** Payment correction (D22) also updates its one linked Transaction's amount in the same operation — the single, explicit, documented exception to Transaction immutability in the system.
- **T-1b:** Overpayment (D21) is always checked against `balance_due` (`total_price − paid_amount`, always computed) — a field distinct from D1's new, optional, additive `outstanding_declared_amount` annotation. The two decisions are reconciled, not reinterpreted.
- **T-2:** Password reset (D16) reseeds to the employee's National ID, matching the original account-creation convention.
- **T-3:** Backup encryption (D25) key-management approach — see `10-Backup-Restore-and-Migration.md` §5.

---

## 3. RED — Decision Required (dramatically reduced)

### 3.1 Must resolve before starting implementation — **NONE REMAIN.**

All 4 previously-critical items (Credit trigger, Club-Brought cancellation math, Training price granularity, Private income vs. Payroll) are resolved by D1, D2, D3, D4 respectively. **There is no longer a blocking reason to delay starting implementation.**

### 3.2 Genuine remaining business-behavior gaps — narrow, low-risk, non-blocking

Only **4 items** in the entire design remain genuinely open — every other ambiguity, including all 5 narrow flags the closure decisions themselves surfaced, has been resolved (§1, §2). These 4 are kept open deliberately, per the "do not invent a business rule" instruction, because each is a real behavioral fork a reasonable business decision could go either way on:

| # | Item | Why it's a genuine business fork, not a technical detail | Blocks? |
|---|---|---|---|
| C5 | Does an Administrator override/late-edit path exist for swimmer Training attendance after the ±30-minute window closes, mirroring the employee 1-hour late-edit rule? | Changes both user-facing behavior (can a correct attendance ever be fixed) and UI design (hard error vs. override flow) | No — affects only the Attendance screen's edit-permission logic |
| F3 (was N3) | Given the Recreational ticket model's deliberate anonymity (D10/D11 removed the Swimmer FK), is D31's duplicate-check-in block literal/unconditional, or an overridable soft warning? | Two closure decisions are in genuine tension; picking either side is a real product choice about false-positive tolerance | No — affects only the Recreational check-in screen's UX |
| D-4 | Is the "first month" cancellation-refund boundary day itself counted as "during" or "after"? | Financially relevant off-by-one; the SRS's original worked example is the only guidance and wasn't re-confirmed by the closure decisions | No — affects only the Package cancellation formula's edge case |
| E6 | Does "before completing 2 sessions" for Private cancellation include exactly 1 completed session, or strictly fewer? | Financially relevant boundary, not addressed by any closure decision | No — affects only the Private cancellation formula's edge case |

None of these four block starting implementation of **any** module — each is scoped to one specific screen/formula's edge case and can be confirmed whenever that piece of work is actively picked up.

### 3.3 Remaining lower-priority open items (unchanged from Doc12, ~18 items: A6–A7, B2–B3, C4, C7–C8, D-3, D-5, D-6, G7, I1, I4, I6–I8, J8)

None of these block starting implementation of any module. They should be tracked in a lightweight backlog and resolved as each corresponding screen/flow reaches active development.

---

## 4. Consistency Check — Post-Closure

Re-running the cross-document consistency check from the original Doc14 §4, now against the closure decisions and their reconciliation:

| Check | Result |
|---|---|
| Every schema addition required by the closure decisions is captured with concrete column/table definitions | ✅ Consistent (`01-ERD.md`, `02-Database-Schema.md`, both fully synchronized) |
| D22's Payment-correction rule doesn't silently contradict the Transaction-immutability principle (Doc09 §5) | ✅ **Resolved (T-1a)** — the exception is now stated explicitly in Doc02 §8, Doc05 §8.2, and Doc09 §5, not left implicit |
| D1's redefinition of "Outstanding" doesn't silently contradict D21's overpayment rule | ✅ **Resolved (T-1b)** — `balance_due` vs. `outstanding_declared_amount` are now two clearly distinct, consistently-named fields across every document |
| D10/D11's removal of the Swimmer FK from Recreational tickets doesn't silently break D31's duplicate-check-in rule | ⚠️ **Genuinely open (F3/§3.2)** — correctly left unresolved rather than silently picking a side |
| D5's new Package session-pool model is fully reflected in the Package cancellation (D19) and period-change logic | ✅ Consistent — period change explicitly carries the balance forward, cancellation formula is unaffected by the session-pool mechanism |
| D4's `coach_dues` ledger correctly feeds D7's monthly payroll aggregation without double-counting against D2's cancellation-fee coach payout | ✅ Consistent — both D2 (cancellation fee) and D4 (Club-Brought share) route through the same `coach_dues` table, summed once per monthly payroll run, with an explicit offsetting-entry rule preventing double payment on a cancelled booking |
| D36's system-wide, single-language decision is consistent with the original UI/UX design's flagged options | ✅ Consistent — closure decision selects exactly one of the two options the original design had left open |
| Naming consistency across all synchronized documents (`Qualification`, `QualificationRateConfig`, `CoachDue`, `balance_due`, `outstanding_declared_amount`, `PrivateBookingParticipant`, `RecreationalTicket`) | ✅ Consistent — verified and corrected across Docs 01, 02, 03, 05 during this synchronization pass; see `17-FINAL-DESIGN-SYNCHRONIZATION-REPORT.md` §D for the specific naming collisions found and resolved |

No new contradiction was found beyond the single genuinely-open item (F3) already carried into §3.2 above.

---

## 5. Recommended Path Forward — Start Implementation Now

**The design phase's core gate is cleared, with zero blocking items remaining.** Recommended sequencing:

1. **Sprint 0 (technical setup, parallel with everything else):** resolve the YELLOW technical choices in §2 — database engine, language/runtime, UI framework (with RTL support, given D36's confirmed bilingual requirement), project scaffolding per Doc03 §6's module structure, migration-runner scaffolding, and the now-settled T-1 through T-3 technical decisions (Payment/Transaction correction sync, overpayment field, password reset value, backup encryption).
2. **Build order recommendation** (no blocking dependencies remain, but this order minimizes rework):
   - **Foundation:** Users/Roles/Employees (including the new Qualifications model, D6), Configuration (including new Lane Management, D35, and the new Initial Program Setup wizard for language selection, D36), Audit Log, Backup/Restore skeleton (encrypted from day one, D25).
   - **Core operations:** Swimmers, Training (Periods, Subscriptions, Sessions, Attendance — including the new ±30-min window, D29 — confirm C5's override question before finalizing this one screen), Payments/Transactions/Finance (including the new Credit/Outstanding manual-entry flow, D1, and Payment correction, D22).
   - **Packages:** including the new session-pool model (D5) and period-change balance carry-forward (confirm D-4's boundary before finalizing the cancellation formula's edge case).
   - **Private:** including Guest participants (D9), per-Lane capacity (D30), Club-Brought split/cancellation (D2, D4, D8) and the `coach_dues` ledger (confirm E6's boundary before finalizing the cancellation formula's edge case).
   - **Recreational:** the restructured ticket-based Single Entry (D10, D11) — confirm F3's hard-block-vs-warning question before finalizing the duplicate-check UX specifically.
   - **Payroll:** monthly cycle across Training + Private + replacement dues (D7), qualification-based rates (D6), Paid-lock + Adjustment-Transaction correction (D23, D24).
   - **Reports:** all report types, now including the redefined Outstanding report (declared annotation alongside the always-computed balance) and the new Coach Dues / Recreational ticket reports implied by D1/D4/D10.
3. Confirm the 4 genuine open items (§3.2) with the business before their specific screens reach code-freeze — none require stopping other work.
4. Track the ~18 remaining low-priority open items in the team's normal backlog; resolve opportunistically as each area is built.

**Overall assessment: the design is implementation-ready. GREEN. 78 requirements are fully specified and confirmed, 0 items block starting work, and only 4 narrowly-scoped business clarifications remain — each affecting exactly one screen's edge-case behavior, none affecting the core financial model, data structure, or permission system. All 5 technical questions the closure decisions themselves raised (N1, N2, N4, N5, and N3's technical half) have been resolved as implementation defaults that change no business-visible behavior.**
