# 06 — Validation Rules Specification
## Swimming Pool Management System
### SYNCHRONIZED — supersedes all prior versions of this document

**Depends on:** `05-Business-Logic.md` (synchronized). **Format:** Field/Operation | Rule | Error Condition | User-facing Message | Layer(s)
**Layer key:** C = Client/UI-side (fast feedback, not trusted alone), S = Server/Service-side (authoritative, always enforced regardless of UI), B = Both.

---

## 1. Swimmers

| Field/Operation | Rule | Error Condition | User-facing message | Layer |
|---|---|---|---|---|
| Name | Required, non-empty | Empty | "Name is required." | B |
| Date of Birth | Required, must be a real past date | Missing/future date | "Enter a valid date of birth." | B |
| Gender | Required, from fixed list | Missing | "Select a gender." | B |
| Phone | Optional — format still `DECISION REQUIRED` (not addressed by closure decisions; genuinely open, low priority) | — | — | B |
| Member/Non-Member change | Any value change is allowed at any time | n/a | Confirmation, not error: "This affects pricing of new subscriptions only." | B |
| Delete Swimmer | Swimmer must have no Training Subscription/Package in an active state — **an active Private Booking never blocks deletion** (Decision 20, resolves the earlier "pending" note) | Active Training/Package link exists | "This swimmer has an active subscription and cannot be deleted." | S |
| Global Search | At least 1 character; matches Name/ID/Phone/Parent | — | "No swimmers match your search." (empty state, not an error) | B |

---

## 2. Training Periods & Subscriptions

| Field/Operation | Rule | Error Condition | User-facing message | Layer |
|---|---|---|---|---|
| Period schedule | end_time > start_time per day row | Invalid range | "End time must be after start time." | B |
| Period capacity | Integer > 0 | ≤ 0 or non-integer | "Capacity must be a positive number." | B |
| Period edit authority | Administrator or Super Admin may edit Days/Start/End/Capacity/Coaches/Lifeguards; Owner never can (Decision 14) | Owner attempts edit | "You do not have permission to edit this period." | S |
| Subscription: Period selection | Period.status = ACTIVE | Inactive period selected | "This period is not available for new subscriptions." | S |
| Subscription: Start Date | ≥ today | Past date | "Start date cannot be in the past." | B |
| Subscription: Start Date | Must fall on one of the Period's scheduled days | Mismatched day | "Start date must match one of this period's scheduled days: [list]." | S |
| Subscription: Capacity | `active_subscriptions_count < period.capacity` | Full | "This period has no available capacity." | S |
| Subscription: Schedule overlap | No overlapping active Training/Package/Private for the same swimmer | Overlap found | "This swimmer already has an active enrollment that overlaps this schedule ([conflicting item])." | S |
| Subscription: Price override | Administrator cannot edit the computed **flat total** price (Decision 3) | Attempted override | Field rendered read-only; no error needed if UI never allows input | C (by omission) + S (reject if bypassed) |
| Subscription: Paid Amount | 0 ≤ paid_amount ≤ total_price. **Whether exactly 0 is a valid Paid Amount remains genuinely open** (not addressed by any closure decision — Doc12 item C4) | Negative or exceeds total | "Paid amount cannot exceed the total price." | B |
| Subscription: Credit/Outstanding entry | Mutually exclusive — providing both `credit_granted_amount` and `outstanding_declared_amount` is rejected (Decision 1) | Both provided | "Choose either Credit or Outstanding, not both." | B |
| Subscription: Credit amount | If provided, requires a non-empty Description | Amount without description | "A description is required when granting Credit." | B |
| Subscription: Outstanding declaration | If provided, requires a non-empty Description | Amount without description | "A description is required when declaring Outstanding." | B |
| Coach/Lifeguard assignment | No overlapping active assignment for the same employee across periods | Overlap | "This employee is already assigned to an overlapping period: [name/time]." | S |
| Pause | Subscription must be ACTIVE | Wrong state | "Only active subscriptions can be paused." | S |
| Resume | Subscription must be PAUSED | Wrong state | "Only paused subscriptions can be resumed." | S |
| Renewal | Old subscription must exist. Whether a `CANCELLED` subscription may be renewed is not addressed by any closure decision — recommended default (safety-first, unconfirmed) remains: reject. | Cancelled subscription | "Cancelled subscriptions cannot be renewed." | S |
| Cancellation | Subscription must be ACTIVE or PAUSED (not already Cancelled/Completed) | Wrong state | "This subscription cannot be cancelled from its current state." | S |
| Training Attendance window | Accepted only within `[session.scheduled_start_time − 30min, +30min]` (Decision 29) | Outside window | "Attendance can only be recorded between [time] and [time] for this session." | S |
| Training Attendance late-edit | Whether an Administrator override exists after the window closes is genuinely unresolved (Doc12 item C5) — pending confirmation, no override path is exposed in the UI by default | n/a | n/a | S |

---

## 3. Packages

| Field/Operation | Rule | Error Condition | User-facing message | Layer |
|---|---|---|---|---|
| Training Package: Program | Must be Regular | Star/Team selected | Not selectable in UI; server rejects if bypassed: "Training packages are only available for the Regular program." | C + S |
| Package: Start Date | ≥ today | Past date | "Start date cannot be in the past." | B |
| Package: Period | Must be Active, with capacity, no schedule conflict | Any violation | Same messages as Training Subscription equivalents | S |
| Package: Duplicate active | No duplicate active Package on the same Period for the same swimmer | Duplicate found | "This swimmer already has an active package on this period." | S |
| Package: Pause | Not permitted at all | Any attempt | No Pause control exists in UI; server rejects: "Packages cannot be paused." | C (by omission) + S |
| Package: Session check-in | `sessions_consumed < total_sessions_snapshot` (Decision 5) | No sessions remaining | "No sessions remaining on this package. Renew or contact the swimmer." | S |
| Package: Period Change | Only while status = Active; new Period Active + capacity + no conflict; session balance always carries forward automatically | Any violation | "Cannot change period: [specific reason]." | S |
| Package: Renewal (Expired) | Administrator-chosen Start Date must be ≥ today | Past date | "Start date cannot be in the past." | B |
| Package: QR reissue | Always allowed for the package owner while not Cancelled; invalidates prior QR | n/a | Confirmation: "Reissuing invalidates the current QR." | S |
| Package: Cancellation | Cannot cancel an already-Cancelled or Expired package | Wrong state | "This package cannot be cancelled from its current state." | S |
| Package: Credit/Outstanding entry | Identical mutual-exclusivity rule as Training Subscription (§2), applied for consistency | Both provided | "Choose either Credit or Outstanding, not both." | B |

---

## 4. Recreational

| Field/Operation | Rule | Error Condition | User-facing message | Layer |
|---|---|---|---|---|
| Ticket: Name | Required, free text — **no Swimmer link or lookup of any kind** (Decisions 10 & 11) | Empty | "Name is required." | B |
| Ticket: Amount Paid | Must equal the full configured entry fee exactly — no partial payment, no Outstanding, no Credit path exists for this operation at all | Any amount other than the full fee | "Full payment is required for Single Entry." | S |
| Single Entry check-in | Within `[period.start_time − 30min, +30min]` | Outside window | "Check-in is only allowed between [time] and [time]." | S |
| Single Entry check-in | Capacity available | Full | "This period is at full capacity." | S |
| Single Entry: duplicate | A person cannot check in twice for the same Recreational Period occurrence (Decision 31). **Enforcement mechanics (hard block given the name-only identity key, vs. an overridable warning) are genuinely unresolved** (Doc12 item F3) — the literal decision text is a block, applied here pending confirmation of whether an override should exist | Duplicate `(period, name, date)` | "[Name] has already checked in for this session." | S |
| Package check-in | QR valid, Package Active & not expired, correct Period, capacity available, sessions remaining (Decision 5) | Any failure | Specific per-cause message: "QR invalid," "Package expired," "Wrong period for this package," "This period is at full capacity," "No sessions remaining on this package." | S |

---

## 5. Private

| Field/Operation | Rule | Error Condition | User-facing message | Layer |
|---|---|---|---|---|
| Coach selection | Exactly one coach; employee_type = COACH | Non-coach selected | "Only coaches can be assigned to a Private booking." | S |
| Coach schedule conflict | No overlap with the coach's other Private bookings or Training Period assignments | Overlap | "This coach is unavailable at the selected time: conflicts with [item]." | S |
| Lane conflict | No overlap with another Private booking on the same Lane | Overlap | "This lane is already booked at the selected time." | S |
| Lane Rental participant count | Must not exceed the selected Lane's configured `capacity` (Decision 30) | Over capacity | "This lane's capacity ([N]) has been reached." | S |
| Participant entry | Each row is either an existing Swimmer (searchable) or a Guest (Name + Phone required, no Swimmer record created) (Decision 9) | Neither provided | "Select an existing swimmer or enter guest details." | B |
| Club-Brought split config | `club_percentage + coach_percentage = 100` exactly | Sum ≠ 100 | "Club and coach percentages must add up to 100%." | B (live validation on the Configuration form) |
| Club-Brought cancellation | Cancellation Fee formula (`fee% × paid_amount`, 100% to Coach, remainder refunded) is used exclusively for cancellation — the ordinary club/coach split is **never** applied here (Decision 2) | n/a | Confirmation shows the fee formula explicitly, never the ordinary split percentages | S |
| Lane Rental / Coach-Brought cancellation tier | "Before completing 2 sessions" boundary (exactly 1 vs. strictly fewer completed sessions) is genuinely unresolved (Doc12 item E6) | n/a | n/a | S |
| Cancellation | Booking must not already be Cancelled/Completed | Wrong state | "This booking cannot be cancelled from its current state." | S |

---

## 6. Finance

| Field/Operation | Rule | Error Condition | User-facing message | Layer |
|---|---|---|---|---|
| Payment amount | > 0 | ≤ 0 | "Enter a valid payment amount." | B |
| Payment amount | Cannot exceed `balance_due` (`total_price − paid_amount`, always computed — this is the ceiling Decision 21 refers to, independent of any separate declared-Outstanding annotation; see `01-ERD.md` reconciliation note) | Overpayment | "Payment exceeds the outstanding balance." Rejected outright — no Payment or Transaction row created (Decision 21). | S |
| Payment correction | Administrator may directly correct a Payment's amount; the linked Transaction is updated in the same operation (Decision 22, resolved consistency rule — see `01-ERD.md` §1.23) | n/a | Confirmation shows the resulting change to the parent's balance: "Correcting this payment from X to Y reduces Outstanding by Z." | S |
| Refund amount | Never user-editable — always system-computed | Attempted manual override | Field is non-editable in UI; if an API bypass is attempted: "Refund amount cannot be modified." | C (by omission) + S |
| Refund correction | A confirmed Refund is permanently locked; corrections are a new, separate `REFUND_ADJUSTMENT` Transaction, never an edit to the original (Decision 24) | Attempted direct edit | "This refund is locked. Use Add Correction instead." | S |
| Expense amount | > 0 | ≤ 0 | "Enter a valid expense amount." | B |
| Expense description | Required | Empty | "Description is required." | B |
| Manual transaction creation | Never permitted anywhere | n/a | No such form exists in the UI. | C (by omission) — no server check needed since no code path exists |
| Payroll correction (Paid) | A `PAID` Payroll row is permanently locked; corrections are a new, separate `PAYROLL_ADJUSTMENT` Transaction (Decision 23) | Attempted direct edit | "This payroll is locked. Use Add Adjustment instead." | S |

---

## 7. Configuration

| Field/Operation | Rule | Error Condition | User-facing message | Layer |
|---|---|---|---|---|
| Any price/rate/fee/duration field | Must be a valid positive number | Negative/zero/non-numeric | "Enter a valid positive value." | B |
| Club-Brought percentages | Sum = 100 exactly | Sum ≠ 100 | "Club and coach percentages must add up to 100%." | B |
| Cancellation Fee type | Must be Percentage or Fixed | Missing | "Select a fee type." | B |
| Cancellation Fee value | If Percentage: 0–100; if Fixed: > 0 | Out of range | "Enter a value between 0 and 100." / "Enter a positive amount." | B |
| Any config change | Applies to future records only — always shown as a confirmation, never an error | n/a | "This change applies to future subscriptions/bookings only." | B (informational, not a blocking validation) |
| Config editor permissions | Pricing/Rates & General Configuration: Super Admin only; Cancellation Fee: Super Admin + Owner | Wrong role attempts | Screen/action not shown to unauthorized roles; server returns 403-equivalent if bypassed | S |
| Package sessions-per-month | Must be a positive integer (Decision 5) | ≤ 0 or non-integer | "Enter a valid number of sessions per month." | B |
| Qualification rank_order | Must be unique across all qualifications (Decision 6) | Duplicate rank | "This rank is already assigned to another qualification." | S |
| Lane capacity | Must be a positive integer (Decision 30) | ≤ 0 or non-integer | "Enter a valid lane capacity." | B |
| Lane deactivation | Blocked while a currently-Active Private booking references this Lane | Active booking exists | "This lane has an active booking and cannot be deactivated." | S |
| System language | Must be exactly `AR` or `EN`; set once at Initial Program Setup (Decision 36); whether it can be changed later is `DECISION REQUIRED (minor, technical)` — not addressed | Invalid value | "Select a language." | B |

---

## 8. Employees, Users & Permissions

| Field/Operation | Rule | Error Condition | User-facing message | Layer |
|---|---|---|---|---|
| Employee: National ID | Required (used as initial password, and for password-reset identity verification, Decision 16) | Missing | "National ID is required." | B |
| Employee: Monthly Salary | Required if employee_type = ADMINISTRATOR; forbidden otherwise | Mismatch | "Monthly salary is required for Administrator employees." | B |
| Employee: Qualifications | A Coach/Lifeguard may hold zero or more; payroll rate resolution requires at least one qualification to compute a rate (Decision 6) | No qualification held, payroll runs | "This employee has no qualification on record — payroll cannot compute a rate." | S |
| User creation (Owner) | Role must be Administrator only | Attempted Owner/Super Admin creation | Role selector restricted to Administrator in UI; server rejects: "Owners can only create Administrator users." | C + S |
| User creation | Administrator-role user must be linked to an Employee of type Administrator. **Owner/Super Admin must never be linked to any Employee record** (Decision 12) | Missing/mismatched link, or an Owner/Super Admin with an Employee link | "Select an Administrator employee to link this account to." | S |
| Username | Unique. On collision, a Nickname is required and appended: `"{name} {nickname}"` (Decision 13) | Duplicate, no nickname provided | "This name is already in use. Enter a nickname to create a unique username." | S |
| User deactivation | Deactivated user cannot log in | n/a | Enforced at authentication, not a form validation | S |
| User reactivation | Restores `is_active`; username and password are both left unchanged (Decision 15) | n/a | Confirmation: "This restores login access. Username and password are unchanged." | S |
| Password reset (Owner/Super Admin → Administrator) | Requires the target Administrator's National ID, matched against their Employee record (Decision 16) | Mismatch | "These details do not match our records." (generic, does not reveal which part failed) | S |
| Password reset (Administrator self-reset) | Requires own Username + National ID, both matched | Mismatch | "These details do not match our records." | S |
| Every protected action | Actor's role must be permitted per the fixed permission matrix (Decision 27 — no custom roles, no reassignment, ever) | Unauthorized | "You do not have permission to perform this action." + logged as unauthorized attempt | S |

---

## 9. Backup / Restore

| Field/Operation | Rule | Error Condition | User-facing message | Layer |
|---|---|---|---|---|
| Manual backup destination | Must be a writable path (local/USB/external/network) | Unwritable/unreachable | "Cannot write to the selected destination." | S |
| Backup encryption | Every backup, automatic and manual, is always encrypted — there is no unencrypted option (Decision 25) | n/a | n/a — not user-configurable | S |
| Automatic backup retry | Up to 5 attempts on failure before declaring the automatic backup failed (Decision 26) | All 5 fail | Persistent Super Admin notification: "Automatic backup failed after 5 attempts. Please perform a manual backup." | S |
| Restore: version check | `backup.system_version` must be ≤ running app version | Newer backup than app | "This backup was created with a newer version of the software. Please upgrade before restoring." | S |
| Restore: safe-restore | A pre-restore snapshot is always taken automatically before proceeding | n/a | Informational, not blocking: "We'll back up your current data before restoring, in case anything goes wrong." | S |

---

## 10. QR Validation (cross-cutting)

| Context | Rule | Error Condition | User-facing message | Layer |
|---|---|---|---|---|
| Training attendance QR | Resolves to an active subscription whose session matches the currently open Period/date, within the ±30-minute window | Mismatch/inactive/outside window | "This QR does not match an active subscription for this session." | S |
| Package check-in QR | Package Active, not expired, correct Period, capacity available, sessions remaining | Any failure | Cause-specific message (see §4 above) | S |
| General principle | QR is identification only — never itself grants access; every scan re-runs full business validation | n/a | n/a — architectural rule, not a single message | S |

---

## 11. Summary: Client vs. Server Validation Policy

- **Every rule in this document is enforced server/service-side (S), without exception** — the Application layer is the single point that must be correct regardless of which screen or future integration calls it.
- **Client-side (C) duplication** is added only where it improves responsiveness (e.g., date pickers that grey out ineligible days, live percentage-sum validation, live Credit/Outstanding mutual-exclusivity toggling) — never a substitute for the server check.
- Fields marked "C (by omission)" indicate the UI simply never presents the option (e.g., no Pause button on Packages, no Refund-amount input field) rather than presenting it and then rejecting it.
- **Four rows in this document remain flagged as genuinely unresolved** (Training Attendance late-edit override, §2; Single Entry duplicate enforcement mechanics, §4; the "first month"/"2 sessions" boundary questions live in `05-Business-Logic.md` §3.5/§5.6 rather than as standalone validation rows, since they're formula edge cases, not field-level checks) — these are the same four items tracked in `12-Edge-Cases-and-Decisions.md` §3.2 and `01-ERD.md` §4.
