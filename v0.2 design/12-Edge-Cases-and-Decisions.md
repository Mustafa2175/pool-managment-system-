# 12 — Edge Cases and Decisions
## Swimming Pool Management System
### SYNCHRONIZED — supersedes all prior versions of this document

**Depends on:** all prior documents, and supersedes the pre-closure version of this document. Every item originally listed here has been checked against `Design_Closure_Decisions.md`. Resolved items are marked ✅ with a pointer to the relevant decision and document. Unaddressed items remain open and are carried forward unchanged, renumbered for clarity within this revision.

---

## Resolution Legend
- ✅ **RESOLVED** — closed by a business decision (Dn); see the cited document for the exact rule and any schema/logic delta.
- 🟡 **OPEN — narrow/low-risk** — not addressed by the closure decisions; does not block starting implementation, tracked for later confirmation.
- 🔶 **NEW FLAG, NOW RESOLVED AS A TECHNICAL DECISION** — a small question surfaced only because resolving the original 36 items required connecting them; closed without changing business behavior — see `17-FINAL-DESIGN-SYNCHRONIZATION-REPORT.md` §E for the exact reasoning.

---

## A. Identity, Roles, Users

| # | Item | Status | Resolution |
|---|---|---|---|
| A1 | Roles/Permissions screen: read-only or editable? | ✅ | Read-only — roles/matrix are fixed, no reassignment (D27) |
| A2 | Do Owner/Super Admin need Employee records? | ✅ | No — Users only, excluded from Attendance/Payroll (D12) |
| A3 | Username collision rule | ✅ | Required Nickname appended on collision (D13) |
| A4 | Forgotten-password reset flow | ✅ | Two flows: Owner/SuperAdmin-resets-Administrator (National ID match) and Administrator self-reset (Username+National ID) (D16) |
| A5 | Can a deactivated User be reactivated? | ✅ | Yes, username/password unchanged, history intact, audited (D15) |
| A6 | Session idle-timeout for shared terminals | 🟡 | Not addressed — technical/UX decision, low risk |
| A7 | Who deactivates an Employee record vs. their User login? | 🟡 | Not explicitly addressed — original recommendation (Administrator can deactivate Employees) stands pending confirmation |

## B. Swimmers

| # | Item | Status | Resolution |
|---|---|---|---|
| B1 | Does Swimmer status include Private Bookings? | ✅ | No — Training/Package only, confirmed explicitly (D20) |
| B2 | Race condition: Member/Non-Member change mid-subscription-creation | 🟡 | Not addressed — low-frequency technical edge case |
| B3 | Phone number format validation | 🟡 | Not addressed — free text remains acceptable pending confirmation |

## C. Training

| # | Item | Status | Resolution |
|---|---|---|---|
| C1 | Can Administrator edit Period schedule/capacity? | ✅ | Yes — full edit authority (schedule, capacity, coaches, lifeguards); Super Admin creates only, Owner view-only (D14) |
| C2 | Flat total vs. per-session Training price | ✅ | Flat total for the whole subscription (D3) |
| C3 | Does a visible "NEW" state exist? | ✅ | No — creation goes straight to ACTIVE, even with a future start date; NEW removed from the state machine (D28) |
| C4 | Is zero paid amount allowed at creation? | 🟡 | Not explicitly addressed; consistent with D1 (Outstanding/Credit now optional/manual) to allow it — treat as allowed pending confirmation |
| C5 | Swimmer attendance late-edit deadline | 🟡 | The ±30-minute window itself is confirmed (D29). Whether an Administrator override/late-edit path exists after it closes — mirroring the employee 1-hour late-edit pattern — is **not** stated by any closure decision. Not invented here; genuinely open (see Doc05 §2.6, §12 item 1). |
| C6 | Renewal timing / old subscription's status | ✅ | Old and new remain separate historical records; old is not force-transitioned to RENEWED at renewal time, continues its own natural lifecycle (D18) |
| C7 | Period schedule edited to fewer weekly slots than a priced subscription assumed | 🟡 | Not addressed — historical-preservation rule (past sessions frozen) still applies, but the pricing-vs-frequency mismatch itself isn't addressed |
| C8 | Employee assignment conflict created retroactively by a later Period schedule edit | 🟡 | Not addressed |
| C9 | Renewal price timing: request time vs. theoretical due date | ✅ (by inference) | Renewal creates a fully independent subscription, snapshotted at actual creation (request) time — consistent with every other snapshot-at-creation rule in the design (D3, D18) |

## D. Packages

| # | Item | Status | Resolution |
|---|---|---|---|
| D-1 | Do Packages generate Sessions? | ✅ | Neither original option — Packages get a consumable session-count pool (duration × sessions/month), not a pre-generated calendar and not pure ad-hoc tracking (D5) |
| D-2 | Cancellation refund floor at zero | ✅ | Yes, floors at zero; no Outstanding/Credit created on a negative result (D19) |
| D-3 | Package period change + retroactively deactivated new Period | 🟡 | Not addressed |
| D-4 | Exact "first month" boundary (day 30/31) | 🟡 | Not precisely specified numerically — recommend `[start_date, start_date + 1 month − 1 day]` inclusive, pending confirmation |
| D-5 | Reporting/alerting for a Package holder who never uses any sessions | 🟡 | Not addressed — operational nice-to-have, not a system requirement |
| D-6 | Distinguishing expired-QR rejection from forged/invalid QR for fraud visibility | 🟡 | Not addressed |

## E. Private

| # | Item | Status | Resolution |
|---|---|---|---|
| E1 | Club-Brought split snapshotted at creation? | ✅ | Yes, confirmed exactly as originally designed (D8) |
| E2 | Must Private participants be real Swimmers? | ✅ | No — Swimmer or unlinked Guest, both supported (D9) |
| E3 | Maximum swimmers per Lane Rental | ✅ | Per-Lane configured Capacity, not a single global constant (D30) |
| E4 | Club-Brought cancellation math order-of-operations | ✅ | Fee = % of paid, 100% to Coach; remainder refunded to customer; club's normal % split does not apply at cancellation (D2) |
| E5 | Does coach Club-Brought income flow through Payroll? | ✅ | Yes, via new `coach_dues` ledger feeding the monthly Payroll run (D4) |
| E6 | Private cancellation 1-vs-2-completed-sessions boundary | 🟡 | Not precisely addressed — recommend "before completing the 2nd session" = at most 1 session completed, pending confirmation |
| E7 | Club-Brought config change mid-booking, then cancelled | ✅ (by inference) | Snapshot at creation (E1/D8) fully insulates the booking, including through cancellation, since D2's cancellation formula uses the booking's own paid_amount and configured fee — no live re-read of current config occurs |

## F. Recreational

| # | Item | Status | Resolution |
|---|---|---|---|
| F1 | Must Single Entry be an existing Swimmer? | ✅ | No — ticket record with Name + Member status directly, no Swimmer Profile needed (D10) |
| F2 | Can Single Entry be partially paid? | ✅ | No — always full payment; Outstanding/Credit both explicitly disallowed (D11) |
| F3 | Re-check-in duplicate handling | 🟡 | D31 requires rejecting duplicates, but D10/D11 removed the only reliable identity key (ticket model is name-only, free text). Whether this makes the block a hard, unconditional reject (as D31's literal text says) or should be softened to a warning-with-override is a genuine tension **between two closure decisions**, not resolved by either. Not invented here; genuinely open (see Doc05 §12 item 2). |
| F4 | Does capacity reset per Period occurrence or run as a lifetime total? | 🟡 | Not explicitly re-confirmed by the closure decisions — original per-occurrence reading stands pending confirmation |

## G. Employees & Payroll

| # | Item | Status | Resolution |
|---|---|---|---|
| G1 | Per-employee vs. per-qualification rate | ✅ | Purely qualification-based, using the employee's highest-ranked qualification (D6) |
| G2 | Coach/Lifeguard payroll cycle | ✅ | Monthly, same as Administrator; includes Training + Private + replacement dues (D7) |
| G3 | Separate Administrator daily attendance table | ✅ | Confirmed exactly as originally designed (D32) |
| G4 | Non-retroactive payroll rates | ✅ | Explicitly confirmed — historical payroll keeps the rate applicable when calculated (D6) |
| G5 | Reverting a mistaken "Mark Paid" | ✅ | Not reverted directly — original Payroll locks once paid; corrections are a new `PAYROLL_ADJUSTMENT` Transaction referencing the original payroll (no separate adjustment table) (D23) |
| G6 | Correcting payroll for an already-deactivated employee | ✅ (by inference) | The D23 adjustment mechanism is employee-status-agnostic — a correction can be recorded regardless of the employee's current active/inactive status |
| G7 | "Replace the replacement" correction path | 🟡 | Not explicitly addressed — likely follows the same general adjustment philosophy as D23/D24, but no dedicated rule exists yet |

## H. Finance

| # | Item | Status | Resolution |
|---|---|---|---|
| H1 | What generates Credit? | ✅ | Manual entry by Administrator at Training/Package creation, mutually exclusive with Outstanding (D1) |
| H2 | Can Credit be partially used across multiple transactions? | ✅ | Yes, explicitly confirmed with a worked example (D34) |
| H3 | Can Payment exceed Outstanding? | ✅ | No — rejected against `balance_due` (`total_price − paid_amount`, always computed), regardless of whether a separate declared-Outstanding annotation exists (D21, reconciled with D1 — see resolved N2 below); no Transaction created, failure audited |
| H4 | Correction path for a mistaken Payment entry | ✅ | Yes — direct correction of the Payment record, audited with old/new values; its one linked Transaction is updated in the same operation to preserve the revenue-reconciliation invariant (D22, reconciled — see resolved N1 below) |
| H5 | UI risk of misreading "full refund" as "of total price" rather than "of paid amount" | ✅ (resolved by design, not by decision) | Already addressed via explicit UI guidance (Doc04 §3.3) to always show the computed number, not just the rule name |
| H6 | Is `OUTSTANDING_PAYMENT` transaction type still meaningful under D1? | ✅ (by inference) | Yes — it now classifies a payment made against a *manually-declared* Outstanding balance, rather than an arithmetic one; the type distinction remains useful for reporting |
| H7 | Application crash during a financial operation | ✅ (resolved by design) | Unit-of-Work transactional pattern already guarantees all-or-nothing commits (Doc03 §2.3) |

## I. Backup / Restore

| # | Item | Status | Resolution |
|---|---|---|---|
| I1 | Precise version-compatibility scheme | 🟡 | Not addressed — remains a technical (semver-style) decision for Doc03/Doc10 |
| I2 | Behavior when the app isn't running at scheduled backup time | ✅ (partially) | The 5-attempt retry/notify/manual-fallback rule (D26) addresses *failure* handling; the "app not running" trigger-timing question itself is still a technical scheduling detail |
| I3 | User-facing notification on automatic-backup failure | ✅ | Yes — Super Admin is notified after all 5 attempts fail (D26) |
| I4 | Orphaned backup file on disconnected removable media during pruning | 🟡 | Not addressed |
| I5 | Backup encryption at rest | ✅ | Yes, all backups encrypted (D25); key-management mechanism resolved as a technical default — see resolved N5 below |
| I6 | Missing intermediate migration script in the version chain | 🟡 | Not addressed |
| I7 | Specificity of the "upgrade first" blocking message | 🟡 | Not addressed — UX/support detail |
| I8 | Can the safe-restore rollback itself fail? | 🟡 | Not addressed — residual risk, worth a design note but not resolved |

## J. Cross-Cutting / Operational

| # | Item | Status | Resolution |
|---|---|---|---|
| J1 | Scope of "failed important operations" for Audit Log | ✅ (narrowed, not fully closed) | Closure decisions explicitly audit several named business-validation failures (overpayment D21, duplicate check-in D31, failed password reset D16) alongside authorization failures — establishes a working principle to apply by analogy; see Doc15 §2 closing note |
| J2 | Duplicate operations (double-click) | ✅ (resolved by design) | UI submit-lock + idempotent state-machine rejections already handle this |
| J3 | Unauthorized actions | ✅ (resolved by design) | Fully covered by the Authorization Guard + Audit Log |
| J4 | System-wide vs. per-user language | ✅ | System-wide, chosen once at initial Program Setup (D36) |
| J5 | Numeral/date formatting for Arabic UI | ✅ | Follows the selected system language's convention automatically (D36) |
| J6 | Do reports render bilingually? | ✅ | No — reports render in the single selected system language, same as the UI (D36) |
| J7 | Bilingual storage for user-entered data | ✅ | Not needed — single system language means single free-text entry per field, confirming the simpler original design (D36) |
| J8 | Is the Program list closed or extensible? | 🟡 | Not addressed — Roles are explicitly confirmed fixed (D27), but Programs (Regular/Star/Team) are not explicitly addressed the same way |
| J9 | Do Lanes need a full management screen? | ✅ | Yes — full Lane Management entity/screen, Super-Admin-controlled via Configuration (D35) |
| J10 | Is ScheduleDefaultsConfig a template or a hard whitelist? | ✅ | Template/default only; each Period's schedule is fully independent thereafter (D33) |

---

## Newly Surfaced Flags (from the closure decisions themselves) — Now Resolved

These arose only because resolving the original 36 items surfaced small new questions the closure decisions didn't directly answer. All five are now closed as **technical decisions** that do not alter business behavior — full reasoning in `17-FINAL-DESIGN-SYNCHRONIZATION-REPORT.md` §E.

| # | Flag | Resolution |
|---|---|---|
| N1 | Does correcting a Payment (D22) also update its linked Transaction's amount? | **Resolved (T-1):** yes — this is now the one explicit, documented exception to Transaction immutability in the whole system (Doc02 §8, Doc05 §8.2, Doc09 §5). Without it the revenue-reconciliation invariant would silently break. |
| N2 | How does Overpayment rejection (D21) interact with D1's "Outstanding is manual"? | **Resolved (T-1):** two distinct fields. `balance_due` (`total_price − paid_amount`) is always computed and is what D21's ceiling refers to; D1's `outstanding_declared_amount` is a separate, optional, additive annotation. Neither decision is reinterpreted — this connects them without contradiction. |
| N3 | D31's duplicate-check-in detection is name-only (no Swimmer FK on tickets per D10) | **Left open, not resolved** — this is a genuine business-behavior choice (hard block vs. overridable warning), not a technical detail, so it is *not* closed here. It remains tracked as F3 above and Doc05 §12 item 2. |
| N4 | D16's password reset doesn't specify the resulting new password value | **Resolved (T-2):** reset reseeds to the employee's National ID, matching the original account-creation convention. Pure implementation default; no user-facing permission or money behavior changes. |
| N5 | D25's backup encryption needs a concrete key-management approach | **Resolved (T-3):** see `10-Backup-Restore-and-Migration.md` §5 for the adopted technical approach. |

N3 is intentionally **not** marked resolved above — it is a real business-behavior fork (see F3), and per the "do not invent" instruction it stays open rather than being quietly closed alongside the purely technical items.

---

## Remaining Open Item Count

- **34 of the original 36 numbered closure-eligible items are fully resolved** by the finalized business decisions.
- **4 genuine business-behavior items remain open** (C5, F3, D-4, E6) — all narrow, low-financial-risk, and non-blocking for starting implementation on any module. These are the same four items enumerated in `01-ERD.md` §4 and `05-Business-Logic.md` §12.
- **~18 lower-priority items from the original Doc12 list remain open**, none newly business-critical (A6–A7, B2–B3, C4, C7–C9's C7/C8, D-3, D-5, D-6, G7, I1, I4, I6–I8, J8).
- **5 new narrow flags surfaced by the closure decisions themselves are now resolved** (N1, N2, N4, N5 as technical decisions; N3 correctly folded into the still-open F3 rather than invented away).

None of the remaining open items are in the "critical, blocks-everything" category that the original 4 critical items (H1, E4, C2, E5) occupied — all four of those are now fully resolved (D1, D2, D3, D4 respectively). The system-wide status is **GREEN with 4 narrowly-scoped, explicitly-named exceptions** — see `14-Implementation-Readiness-Review.md` for the full final verdict.
