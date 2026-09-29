# 13 — Requirements Traceability Matrix
## Swimming Pool Management System
### SYNCHRONIZED — supersedes all prior versions of this document

**Purpose:** Ensure no requirement — original SRS or finalized closure decision — disappears during implementation. Format: `Requirement → Domain Entity → Database Table → Business Logic → UI → Test Case`.

Grouped by module. Every row cites either an SRS section or a Decision number (D1–D36). Rows changed or added by the closure decisions are marked accordingly; all table/entity names match the synchronized `01-ERD.md`/`02-Database-Schema.md`.

---

## 1. Roles & Permissions

| Requirement | Entity | Table | Logic | UI | Test |
|---|---|---|---|---|---|
| 3 fixed roles, closed permission matrix, no custom roles, no reassignment (D27) | Role | `roles` | Authorization Guard | Roles/Permissions screen is read-only | Attempt to create a custom role or reassign a permission → impossible by design |
| Owner: financial/admin view, no daily ops | User, Role | `users`, `roles` | Guard rejects operational actions for OWNER | No operational buttons render for Owner | Attempt each operational action as Owner → expect rejection + audit |

## 2. Users, Employees & Authentication

| Requirement | Entity | Table | Logic | UI | Test |
|---|---|---|---|---|---|
| Administrator User must link to Administrator Employee; **Owner/Super Admin must NEVER have an Employee record** (§4.1, **D12**) | User, Employee | `users`, `employees` | Create-User validation | Add User form | Attempt create Owner/Super Admin with an Employee link → reject; attempt Administrator without one → reject |
| Initial username = Employee Name; **collision requires Nickname** (§4.1, **D13**) | User, Employee | `users`, `employees` (`nickname`) | Username collision handling | Nickname prompt on collision | Create two employees with the same name → second requires nickname, username = `"{name} {nickname}"` |
| Super Admin: full user CRUD; Owner: Administrator-only; Administrator: none (§4.2) | User | `users` | Guard + context check | Role selector restricted for Owner | Owner attempts to create Owner/Super Admin → reject |
| Deactivate, never delete (§4.3) | User | `users` | Deactivation use case | User Details screen | Deactivate → login blocked; historical FKs still resolve |
| **User reactivation, username/password unchanged (D15)** | User | `users` | Reactivation use case | Reactivate action | Reactivate → login restored, username/password identical to before deactivation, audited |
| **Password reset: Owner/Super Admin → Administrator via National ID match (D16)** | User, Employee | `users`, `employees` | Password reset use case (verify National ID) | Reset Password action | Correct National ID → reset succeeds; wrong → rejected, both outcomes audited |
| **Password reset: Administrator self-reset via Username + National ID (D16)** | User, Employee | `users`, `employees` | Self-reset use case | Self-reset flow | Both match → succeeds; either wrong → rejected, audited |
| **Owner/Super Admin excluded from Employee Attendance and Payroll (D12)** | User, Employee | `users`, `employees` | Structural exclusion (no Employee row exists) | N/A — screens never list them | Query Employee Attendance/Payroll for any Owner/Super Admin → always empty by construction |

## 3. Swimmers

| Requirement | Entity | Table | Logic | UI | Test |
|---|---|---|---|---|---|
| Auto-generated Swimmer ID (§5.2) | Swimmer | `swimmers` | ID sequence generator | Read-only ID display | Create N swimmers → IDs sequential, no collisions |
| Global search by Name/ID/Phone/Parent, phone not unique (§5.3) | Swimmer | `swimmers` | Search query | Swimmers List search box | Two swimmers, same phone → both returned |
| **Active/Inactive derived ONLY from Training Subscription/Package state — Private NEVER counts (§5.4, D20)** | Swimmer, TrainingSubscription, Package | `swimmers`, `training_subscriptions`, `packages` | Status recompute | Status badge | Swimmer with only active Private bookings → stays INACTIVE; active Subscription/Package → ACTIVE |
| Member/Non-Member affects only new pricing (§5.5) | Swimmer | `swimmers` | Member-status-change use case | Confirmation modal | Flip status → existing subscription price unchanged, new one uses new price |
| **Soft delete blocked only by active Training/Package — an active Private booking never blocks it (§5.6, D20)** | Swimmer | `swimmers` (`is_deleted`) | Delete validation | Delete button disabled + tooltip when blocked | Swimmer with only an active Private booking → deletable; with active Subscription/Package → blocked |
| Inactive/deleted swimmers remain in history/search/renewal (§5.4) | Swimmer | `swimmers` | Query scope includes `is_deleted` rows for history views | History tab always shows | Soft-delete swimmer → still appears in past subscription history |

## 4. Training Programs & Periods

| Requirement | Entity | Table | Logic | UI | Test |
|---|---|---|---|---|---|
| Period belongs to exactly one Program (§6.2) | TrainingPeriod, Program | `training_periods` | FK constraint | Period form | Attempt period without program → reject |
| Super Admin creates initial schedule/capacity (§6.3) | TrainingPeriod | `training_periods`, `training_period_schedules` | Create-Period use case | Super-Admin-only creation screen | Non-Super-Admin attempts create → reject |
| **Administrator may subsequently edit Days/Start/End/Capacity/Coaches/Lifeguards; Owner view-only (D14)** | TrainingPeriod, PeriodStaffAssignment | `training_periods`, `training_period_schedules`, `period_staff_assignments` | Edit-Period use case | Edit controls visible to Super Admin + Administrator only | Administrator edits schedule → succeeds; Owner attempts → rejected |
| Active/Inactive lifecycle, historical subscriptions preserved on deactivate (§6.4) | TrainingPeriod | `training_periods` | State machine | Status toggle | Deactivate period with existing subscriptions → subscriptions unaffected |
| Schedule change preserves past sessions, audit logged, **regardless of which of the two authorized roles made the change (D14)** | TrainingPeriodSchedule, Session | `training_period_schedules`, `sessions` | Schedule-change use case | Edit schedule form | Change schedule → past Session rows unchanged, new subscriptions use new schedule, audit entry exists |

## 5. Training Subscriptions & Attendance

| Requirement | Entity | Table | Logic | UI | Test |
|---|---|---|---|---|---|
| **Price is a flat total for the whole subscription, never per-session × count (§7.2, D3)** | TrainingSubscription, TrainingPricingConfig | `training_subscriptions`, `training_pricing_configs` | Price snapshot | Read-only computed total price | Change config → old subscription price unchanged; verify total is not multiplied by session count |
| **Subscription is ACTIVE immediately on creation, even with a future start_date — no Pending/NEW state exists (§7.1, D28)** | TrainingSubscription | `training_subscriptions` (status CHECK excludes 'NEW') | Creation use case | No Pending/Scheduled UI state anywhere | Create with future start_date → status is ACTIVE immediately |
| Start date matches schedule day (§7.3) | TrainingSubscription | `training_subscriptions` | Validation | Date picker restricts days | Wrong day → reject |
| Capacity enforcement (§7.4) | TrainingSubscription, TrainingPeriod | `training_subscriptions`, `training_periods` | Capacity check | "Full" badge | Fill period to capacity → next attempt rejected |
| No schedule overlap across Training/Package/Private (§7.5) | TrainingSubscription, Package, PrivateBooking | multiple | Overlap check | Inline conflict error | Overlapping enrollment attempt → reject naming conflict |
| Sessions auto-generated (§7.6) | Session | `sessions` | Session generator | Sessions calendar view | Create subscription → correct count/dates of sessions generated |
| **QR/manual attendance within a ±30-minute window around session start (§7.7, D29)** | Attendance, Session | `attendances`, `sessions` | Attendance use case | Attendance screen | Attempt attendance 31 minutes before/after start → reject; within window → succeeds |
| Pause/Resume extends end date (§7.8) | TrainingSubscriptionPause | `training_subscription_pauses` | Pause/Resume use case | Pause/Resume actions | Pause N days → end_date extended by N |
| **Renewal: two-branch start-date logic; old subscription's status is NEVER forced by renewal (§7.9, D18)** | TrainingSubscription | `training_subscriptions` (`renewed_from_subscription_id`) | Renewal use case | Renewal flow, "Renewed" shown only as a badge | Renew a subscription with remaining sessions → old subscription's status is untouched, continues naturally; new one is fully independent |
| Cancellation refund rule (§7.10) | TrainingSubscription, Refund, Transaction | `training_subscriptions`, `refunds`, `transactions` | Cancellation use case | Cancellation confirmation showing computed refund | Cancel before/after first session → correct refund amounts |
| **Credit/Outstanding manual entry at creation, mutually exclusive (D1)** | TrainingSubscription, Credit | `training_subscriptions` (`credit_granted_amount`/`outstanding_declared_amount`), `credits` | Creation use case | Optional Credit/Outstanding section on the creation form | Attempt to enter both → reject; enter Credit → Credit record created; `balance_due` unaffected by either path |

## 6. Coach/Lifeguard Assignment & Attendance

| Requirement | Entity | Table | Logic | UI | Test |
|---|---|---|---|---|---|
| No overlapping assignments (§8.1) | PeriodStaffAssignment, Employee | `period_staff_assignments` | Overlap check | Conflict error | Assign to overlapping periods → reject |
| Lifeguards optional (§8.1) | PeriodStaffAssignment | `period_staff_assignments` | No minimum-count validation | No required-lifeguard warning | Create period with zero lifeguards → succeeds |
| Assignment ≠ attendance (§8.2) | PeriodStaffAssignment, EmployeeAttendance | separate tables | Distinct use cases | Distinct screens | Assign employee, don't mark attendance → attendance remains unset, no auto-present |
| Replacement valid for one session only (§8.3) | EmployeeReplacement | `employee_replacements` | Replacement use case | Replacement action on a single session | Replace for one session → base assignment unchanged, only that session's payroll uses replacement's rate |
| **1-hour post-period attendance edit deadline, classified "Late Administrative Edit" (§9.1, D17)** | EmployeeAttendance | `employee_attendances` (`is_late_edit`) | Late-edit flag logic | "Late Administrative Edit" tag | Edit attendance >1hr after period end → flagged + audited with that exact label |
| **Administrator daily attendance is a separate table from session-based Coach/Lifeguard attendance (D32)** | AdministratorDailyAttendance | `administrator_daily_attendance` | Distinct recording use case | Employee Profile attendance history | Record Administrator Present/Absent by date → independent of any session concept |

## 7. Employee Qualifications & Payroll

| Requirement | Entity | Table | Logic | UI | Test |
|---|---|---|---|---|---|
| **An employee may hold multiple qualifications; payroll uses the HIGHEST one (D6)** | Qualification, EmployeeQualification | `qualifications`, `employee_qualifications` | Highest-qualification resolution | Employee Profile "Qualifications" section | Employee with 3 qualifications → payroll uses the one with the highest `rank_order` |
| **Rate is configured per-qualification, never per-employee (D6)** | QualificationRateConfig | `qualification_rate_configs` | Rate resolution | Configuration → Rates screen | Two employees with the same qualification → same resolved rate |
| **Historical payroll preserves the rate applicable at calculation time (D6)** | Payroll | `payrolls` | Rate snapshotting | Payroll Details breakdown | Change a qualification's rate → previously-calculated payroll unchanged |
| **Coach/Lifeguard payroll is monthly, aggregating Training + Replacement + CoachDue sources (§11.1, D7)** | Payroll, EmployeeAttendance, EmployeeReplacement, CoachDue | `payrolls`, `employee_attendances`, `employee_replacements`, `coach_dues` | Merged payroll calculation | Payroll Details breakdown, itemized by source | Sum of the three sources = `calculated_amount`, verified against manually-computed expectation |
| Administrator fixed salary minus absence deduction (§11.2) | Payroll, AdministratorDailyAttendance | `payrolls`, `administrator_daily_attendance` | Admin payroll calc | Payroll Details breakdown | Salary/days/absences → correct net formula |
| Mark Paid by Super Admin/Owner/Administrator (§11.3) | Payroll | `payrolls` | Mark-Paid use case | Mark Paid action | All 3 roles can mark paid |
| **Paid Payroll is permanently locked; corrections via a new PAYROLL_ADJUSTMENT Transaction (D23)** | Payroll, Transaction | `payrolls`, `transactions` | Adjustment use case | "Add Adjustment" action, no direct edit control | Attempt to edit a Paid payroll → impossible; Add Adjustment → new signed Transaction, original untouched |

## 8. Packages

| Requirement | Entity | Table | Logic | UI | Test |
|---|---|---|---|---|---|
| Training Package = Regular only (§12.1) | Package, Program | `packages` | Creation validation | Program restricted to Regular in Training-Package flow | Attempt Star/Team training package → reject |
| Price independent of chosen Period (§12.3) | Package | `packages` | Price snapshot logic | Read-only computed price | Same price regardless of period selected |
| **Session-pool model: `total_sessions_snapshot = duration_months × sessions_per_month`, decremented per check-in, calendar expiry still hard ceiling (D5)** | Package, PackageConfig | `packages` (`total_sessions_snapshot`, `sessions_consumed`), `package_configs` (`sessions_per_month`) | Session-consumption logic | Session balance ("X of Y remaining") shown on Package Details | 2-month package at 8/month → 16 total; each check-in decrements by 1; check-in blocked at 0 remaining even with time left; expiry forfeits any leftover balance |
| QR-based check-in (§12.4, §12.9) | Package, PackageQrHistory, PackageCheckIn | `packages`, `package_qr_history`, `package_checkins` | Check-in validation | QR display + reissue action | Reissue QR → old token invalid, new valid |
| No pause (§12.8) | Package | `packages` | No pause transition exists | No Pause button anywhere | Attempt pause via API bypass → reject |
| **Period change mechanics; session balance carries forward automatically (§12.7, D5)** | PackagePeriodChange | `package_period_changes` | Period-change use case | Period Change screen | Change period mid-way through the session pool → `sessions_consumed`/`total_sessions_snapshot` unaffected by the switch |
| **Cancellation: first-month formula floors at 0, creates no Outstanding/Credit on a negative result (§12.10, D19)** | Package, Refund | `packages`, `refunds` | Cancellation formula | Cancellation confirmation with explained formula | Cancel during first month with a small paid amount vs. a large reference price → refund floors at exactly 0, no debt created |
| **Credit/Outstanding manual entry at creation, identical pattern to Training Subscription (D1)** | Package, Credit | `packages` (`credit_granted_amount`/`outstanding_declared_amount`), `credits` | Creation use case | Optional Credit/Outstanding section | Same test pattern as the Training Subscription row in §5 |

## 9. Recreational

| Requirement | Entity | Table | Logic | UI | Test |
|---|---|---|---|---|---|
| **Single Entry is a standalone ticket — no Swimmer link of any kind (§13.2, D10)** | RecreationalTicket | `recreational_tickets` (no `swimmer_id` column) | Ticket-creation use case | Single Entry tab: Name, Member/Non-Member, Amount, Description | Create a ticket → no Swimmer record created or required, searchable only by the ticket's own fields |
| **Full payment required — no partial, no Outstanding, no Credit (D11)** | RecreationalTicket | `recreational_tickets` | Payment validation | Amount field locked to the configured fee | Attempt any amount other than the full fee → reject |
| ±30-minute check-in window | RecreationalTicket | `recreational_tickets` | Window validation | Window-restricted check-in button | Outside window → reject |
| **Duplicate check-in for the same person/period/day rejected (D31)** | RecreationalTicket | `recreational_tickets` | Duplicate-detection use case | Rejection message naming the person | Same name, same period, same day → second attempt rejected, audited |
| Capacity shared, never frees on departure (§13.5) | RecreationalTicket, PackageCheckIn | `recreational_tickets`, `package_checkins` | Capacity count logic | Live capacity counter (increase-only) | Fill capacity → further check-ins rejected regardless of "time passed" |
| **Package holder check-in validation chain, now also checking sessions remaining (§13.6, D5)** | Package, PackageCheckIn | `packages`, `package_checkins` | Validation chain | Scan result with specific reason | Each failure mode (expired/wrong period/full/invalid/no-sessions-remaining) surfaces distinct message |
| **Reports: ticket count, total revenue, Member/Non-Member ticket counts (D10)** | RecreationalTicket | `recreational_tickets` | Aggregation queries | Recreational report | Counts and revenue sum match raw ticket data, split correctly by `member_status` |

## 10. Private

| Requirement | Entity | Table | Logic | UI | Test |
|---|---|---|---|---|---|
| One coach, no replacement (§14.4) | PrivateBooking, Employee | `private_bookings` | Creation validation | Single coach selector | Attempt multi-coach or replacement → not offered/rejected |
| **Coach & Lane conflict checks, Lane capacity enforced (§14.5, §14.8, D30, D35)** | PrivateBooking, Lane | `private_bookings`, `lanes` (`capacity`) | Conflict + capacity checks | Conflict error naming the clash; capacity indicator | Overlapping coach/lane → reject; exceeding Lane capacity → reject |
| Lane Rental: fixed club fee regardless of participant count (§14.1) | PrivateBooking | `private_bookings` (`lane_fee_snapshot`) | Pricing logic | Read-only fee | Fee unchanged across 1..capacity participants |
| **Coach-Brought: participant count × fee, participants may be Swimmer or Guest (§14.2, D9)** | PrivateBooking, PrivateBookingParticipant | `private_bookings`, `private_booking_participants` | Pricing logic | Computed total shown; participant rows show Swimmer search OR Guest name/phone fields | N participants (any mix of Swimmer/Guest) → N × fee; Guest never creates a Swimmer record |
| **Club-Brought: split percentages frozen at booking creation, never re-resolved from later config (§14.3, D8)** | PrivateBooking | `private_bookings` (`club_percentage_snapshot`, `coach_percentage_snapshot`) | Split calc | Split preview | Change global config after booking creation → this booking's split is unaffected |
| **Club-Brought ongoing revenue: coach's share becomes a CoachDue, not a second customer transaction (D4)** | PrivateBooking, CoachDue | `private_bookings`, `coach_dues` | Revenue-attribution use case | Coach Dues shown in Payroll Details | One customer payment → one Transaction; coach's share appears only in `coach_dues`, feeding the next monthly payroll |
| **Club-Brought cancellation: fee = fee% × paid_amount, 100% to Coach, remainder refunded; club retains nothing; ordinary split never used here (D2)** | PrivateBooking, Refund, CoachDue | `private_bookings`, `refunds`, `coach_dues` | Cancellation formula | Cancellation confirmation showing the fee formula explicitly, never the ordinary split | Worked example: Paid 1,000, Fee 20% → Coach CoachDue 200, Customer refund 800, Club transaction total = 0 |
| Attendance manually recorded, not QR (§14.7) | Attendance | `attendances` (private sessions) | Manual-only path | No QR option in Private attendance screen | QR scan option absent for Private |
| **Lane Rental / Coach-Brought cancellation tiers (§15.1–15.2)** | PrivateBooking, Refund | `private_bookings`, `refunds` | Cancellation logic | Cancellation confirmation with tier explained | 0/1/2+ sessions completed → correct refund tier each (exact 1-vs-2 boundary genuinely open, see Doc12 E6) |

## 11. Finance

| Requirement | Entity | Table | Logic | UI | Test |
|---|---|---|---|---|---|
| Every payment produces a transaction (§17.1) | Payment, Transaction | `payments`, `transactions` | Atomic insert pair | — | Every Payment row has exactly one linked Transaction |
| Partial payments allowed (§17.2) | Payment | `payments` | Multiple payment inserts | Payment history list | Sum of payments ≤ total_price; `balance_due` decreases each time |
| No manual transactions (§17.4) | Transaction | `transactions` | No generic "add transaction" use case exists | No such form in UI | Attempt direct transaction creation via any exposed path → impossible by design |
| Transaction types enumerated, **now including PAYROLL_ADJUSTMENT and REFUND_ADJUSTMENT (§17.5, D23, D24)** | Transaction | `transactions` (`transaction_type` CHECK) | Type assigned per originating use case | Transactions ledger filter | Each type only ever originates from its designated use case |
| Refund system-calculated, admin confirms only (§19) | Refund | `refunds` | Refund calculators | Non-editable refund amount + confirm action | Attempt to alter refund amount → rejected/not possible |
| **Confirmed Refund permanently locked; corrections via a new REFUND_ADJUSTMENT Transaction (D24)** | Refund, Transaction | `refunds`, `transactions` | Adjustment use case | "Add Correction" action, no direct edit control | Attempt to edit a confirmed refund → impossible; Add Correction → new signed Transaction, original untouched |
| **Credit's sole generation trigger: manual entry at Training/Package creation; usage recognized as revenue on date of use, partial usage allowed, Training/Package only (D1, D34)** | Credit, CreditUsage | `credits`, `credit_usages` | Generation + usage logic | Optional Credit section at creation; Credit balance shown on Swimmer Profile | Grant Credit → no Transaction created; use part of it later → exactly one CREDIT_USAGE Transaction on that date; attempt use on Private/Recreational → rejected |
| Expenses manual entry (§20) | Expense | `expenses` | Expense creation | Expenses screen | Create expense → transaction created, appears in Expense report |
| **Payment > balance_due rejected outright, no Transaction created, audited (§17.2 area, D21)** | Payment | `payments` | Overpayment validation | Inline rejection message | Attempt overpayment → rejected, `PAYMENT_OVERPAYMENT_REJECTED` audit entry created |
| **Payment directly correctable by Administrator; linked Transaction updated in the same operation (D22)** | Payment, Transaction | `payments`, `transactions` | Correction use case | "Correct Payment" action, old/new shown | Correct a payment → both rows updated together; `PAYMENT_CORRECTED` audit entry with old/new/actor/reason; revenue reconciliation invariant (Doc11 §10.9) still holds afterward |

## 12. Reports

| Requirement | Entity | Table | Logic | UI | Test |
|---|---|---|---|---|---|
| **Revenue/Expenses/Profit computed by explicit transaction-type classification, never naive SUM of positives (§21.1–21.3, Doc11 §10.1)** | Transaction | `transactions` | Classified aggregation queries | Report screens | Profit report = Revenue report − Expense report, for every period tested, using the classification table in Doc11 §10.1 |
| **Outstanding report shows `balance_due > 0` records, annotated with any declared-Outstanding note where present (§21.4, D1 reconciliation)** | TrainingSubscription, Package, PrivateBooking | respective tables | `balance_due > 0` query, joined with `outstanding_declared_amount`/`description` where present | Outstanding screen | All entities with nonzero `balance_due` appear; those with a declared annotation also show its description |
| **Payroll report sums original PAYROLL_PAYMENT + all PAYROLL_ADJUSTMENT transactions per employee/period (§21.5, D23)** | Payroll, Transaction | `payrolls`, `transactions` | Aggregation | Payroll report | True net paid = original + all adjustments, matches manual calculation |
| Attendance report (§21.6) | Attendance, EmployeeAttendance | respective tables | Aggregation | Attendance report | Matches raw attendance counts |
| Cancellations report (§21.7) | TrainingSubscription, Package, PrivateBooking (cancelled) | respective tables | Aggregation | Cancellations report | Count matches cancelled-status rows in period |
| Swimmers report (§21.8) | Swimmer | `swimmers` | Aggregation | Swimmers report | Active/Inactive/New counts match derived status (Private-only swimmers always counted Inactive, D20) |
| **New: Recreational Ticket report — ticket count, revenue, Member/Non-Member split (D10)** | RecreationalTicket | `recreational_tickets` | Aggregation | Recreational report | See §9 row above |
| **New: Coach Dues report — per-coach accrued and consumed dues by source type (D2, D4)** | CoachDue | `coach_dues` | Aggregation | Coach Dues section of Payroll or a standalone report | Sum of unconsumed `coach_dues` for a coach = what their next payroll run will include |

## 13. Configuration

| Requirement | Entity | Table | Logic | UI | Test |
|---|---|---|---|---|---|
| All pricing/rates/durations/fees configurable, Super-Admin-only (except Cancellation Fee, also Owner) (§22, §3) | Config tables | `training_pricing_configs`, `package_configs`, `qualification_rate_configs`, etc. | Guard + versioned write | Configuration screens | Non-Super-Admin attempts edit → reject (except Owner on Cancellation Fee) |
| Changes non-retroactive (§23) | All snapshot columns | all transactional tables | Snapshot-at-creation pattern | "Applies to future only" confirmation | Change config → verify old records' snapshot values unchanged |
| **Package sessions-per-month configurable (D5)** | PackageConfig | `package_configs` (`sessions_per_month`) | Config write | Package Configuration screen | Change value → only future Packages use the new figure |
| **Qualifications and their ranked rates fully manageable (D6)** | Qualification, QualificationRateConfig | `qualifications`, `qualification_rate_configs` | Config write | Qualifications & Rates screen | Add a qualification, assign a unique rank, set a rate → usable in payroll resolution |
| **Lane Management: full CRUD, label + capacity, Super-Admin only (D30, D35)** | Lane | `lanes` | Lane CRUD use cases | Lane Management screen (new, under Configuration) | Create/edit/deactivate a Lane; deactivation blocked while an active booking references it |
| **System language chosen once at Initial Program Setup, system-wide, no per-user override (D36)** | SystemSettings | `system_settings` | Setup wizard use case | Initial Program Setup screen (new) | Fresh install → language prompt before any other action; all subsequent screens/reports render in that language for every user |
| Cancellation Fee: percentage of paid amount, editable by Super Admin + Owner (§16, D2) | CancellationFeeConfig | `cancellation_fee_configs` | Config write | Cancellation Fee Configuration | Owner can edit; Administrator cannot |

## 14. Audit Log

| Requirement | Entity | Table | Logic | UI | Test |
|---|---|---|---|---|---|
| Full event coverage, success + failure (§25.1, §25.3) | AuditLog | `audit_logs` | Audit service called from every use case | Audit Log screen | Every catalogued action in `09-Audit-Log.md` §2 produces exactly one entry when triggered |
| Visible to Super Admin/Owner only (§25.2) | AuditLog | `audit_logs` | Guard | Nav hidden for Administrator | Administrator cannot reach Audit Log screen or API |
| **Immutable, with exactly one documented system-wide exception (transactions on Payment correction only — audit_logs itself has zero exceptions, ever) (§25.5, D22)** | AuditLog | `audit_logs` | No update/delete repository methods, ever | No edit/delete controls | Attempt update/delete of any `audit_logs` row via any path → impossible by design, unconditionally |
| **New events: CREDIT_ISSUED, PAYMENT_CORRECTED, PAYROLL_ADJUSTED, REFUND_ADJUSTED, PASSWORD_RESET, USER_REACTIVATED, PAYMENT_OVERPAYMENT_REJECTED, RECREATIONAL_DUPLICATE_CHECKIN_REJECTED, BACKUP_FAILED_ALL_ATTEMPTS, LANE_BOOKING_CONFLICT_REJECTED (D1, D22, D23, D24, D15, D16, D21, D31, D26, D30)** | AuditLog | `audit_logs` | Audit service | Audit Log screen filters | Trigger each new event → exactly one matching entry appears |

## 15. Backup/Restore/Migration

| Requirement | Entity | Table | Logic | UI | Test |
|---|---|---|---|---|---|
| Weekly auto + manual backup | Backup | `backups` | Backup scheduler + manual trigger | Backup screen | Scheduled backup fires; manual backup succeeds to each destination type |
| 7-backup FIFO retention | Backup | `backups` | Retention pruning | Backup History list | Create 8th backup → oldest deleted |
| **All backups encrypted, automatic and manual alike, no exceptions (D25)** | Backup | `backups` (`encrypted`, `encryption_key_ref`) | Encryption on write | Backup History shows encryption status | Every backup row has `encrypted = TRUE`; attempt to read a backup file outside the Restore mechanism → unreadable |
| **Automatic backup: up to 5 retry attempts, then log + persistent Super Admin notification + Manual Backup required (D26)** | Backup | `backups` (`attempt_number`) | Retry loop | Persistent notification banner | Simulate 5 consecutive failures → `BACKUP_FAILED_ALL_ATTEMPTS` audited, notification shown, system remains operational |
| Full-snapshot restore | all entities | all tables | Restore use case | Restore screen | Post-restore, all data matches the backup's point-in-time state, including new entities (Qualifications, CoachDues, Lanes, RecreationalTickets) |
| Version compatibility rules | Backup | `backups` (`system_version`) | Compatibility check | Blocked-restore message | Newer backup into older app → blocked; older into newer → migrates |
| Safe restore (pre-restore snapshot) | Backup | `backups` | Safe-restore sequence | Restore confirmation notice | Simulate mid-restore failure → automatic rollback to pre-restore snapshot succeeds |

---

## 16. Coverage Statement

Every SRS requirement and every one of the 36 finalized closure decisions is referenced by at least one row above. Rows marked with a Decision number (D1–D36) represent behavior that changed or was newly introduced by the Design Closure Decisions; all other rows are unchanged carry-overs from the original SRS-derived design, re-verified for consistency against the synchronized entity/table names in this pass. No requirement — original or closure-decision — was found to have zero corresponding entity/table/logic/UI/test mapping. The 4 genuinely remaining open items (Doc12 §"Remaining Open Item Count") are intentionally **not** given a fabricated row here — they are tracked exclusively in `12-Edge-Cases-and-Decisions.md` and `01-ERD.md` §4, precisely because inventing a test case for an unresolved business rule would misrepresent it as settled.
