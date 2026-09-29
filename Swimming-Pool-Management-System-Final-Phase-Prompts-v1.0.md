# Swimming Pool Management System
# Final Phase-by-Phase Coding Prompts — v1.0

**Purpose:** Copy-paste one phase prompt at a time into the coding agent (Claude/Cursor/etc.).

**Execution rule:** Never give the agent the next phase until the current phase has been reviewed and accepted.

---

# How to Use This File

For every phase, provide the coding agent with:

1. This prompt file.
2. `Hybrid-Final-Implementation-Plan-v1.0.md`
3. The finalized project documentation.
4. The current repository.

The agent must:

```text
Read source documents
        ↓
Inspect current repository
        ↓
Implement ONLY the requested phase
        ↓
Run tests
        ↓
Verify against source documents
        ↓
Report
        ↓
STOP
```

Do not ask the agent to implement multiple phases in one turn.

---

# GLOBAL INSTRUCTIONS — APPLY TO EVERY PHASE

The following instructions are embedded in every phase prompt.

```text
SOURCE OF TRUTH

Use the following precedence:

1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. Final synchronized SRS/design documents
4. Technical-Architecture-and-Stack-Decision.md
5. Hybrid Final Implementation Plan
6. Current Phase Prompt

The Phase Prompt controls execution scope, not business behavior.

Never invent, reinterpret, simplify, or silently resolve a business rule.

If a finalized document and an older document conflict, the finalized document wins.

Do not use stale entities, fields, workflows, terminology, or architecture from superseded documents.

Before implementing this phase:
- Inspect the repository.
- Inspect the existing implementation.
- Reuse existing correct work.
- Do not rewrite unrelated code.
- Do not create duplicate abstractions.
- Do not introduce technologies that are not approved by the Technical Architecture.

If a genuine contradiction exists between finalized source-of-truth documents:

STOP.

Do not choose a side.
Do not invent a resolution.
Report:
1. The exact contradiction.
2. The source documents involved.
3. The affected feature/code.
4. Why implementation cannot safely continue for that part.

Do not reopen a business decision that has already been finalized.

The four known narrow open business decisions must remain unresolved unless the user explicitly decides them:
1. Training attendance late-edit override after the ±30-minute window.
2. Recreational duplicate check-in enforcement mechanics for anonymous/free-text tickets.
3. Package cancellation first-month boundary interpretation.
4. Private Lane Rental / Coach-Brought cancellation boundary interpretation.
```

---

# UNIVERSAL ENGINEERING RULES

```text
ENGINEERING RULES

1. Keep the architecture layered:
   Domain → Application → Infrastructure → UI.

2. Keep business rules out of the UI where possible.
   UI should call application use cases/services.

3. Do not place database-specific behavior inside Domain entities unless explicitly required.

4. Use dependency injection.

5. Use EF Core for persistence according to the approved architecture.

6. Use SQLite as the application database.

7. Use async APIs where appropriate.

8. Use cancellation tokens where appropriate for I/O and long-running operations.

9. Use transactions for multi-step business operations that must be atomic.

10. Preserve historical truth.
    Configuration changes must not silently rewrite historical records.

11. Do not hard-delete historical business data when the finalized design requires preservation.

12. Validate business rules before persistence.

13. Do not duplicate business calculations in multiple layers.

14. Keep financial operations atomic.

15. Every important security/identity/financial/system operation must follow the finalized Audit Log rules.

16. Do not add convenience features that change business behavior.

17. Do not add custom roles or editable permissions.

18. Do not hardcode configurable values.

19. Do not hardcode session counts, prices, percentages, salaries, rates, or cancellation fees.

20. Use configuration where the finalized design specifies configuration.

21. Use historical snapshot fields where the finalized schema requires them.

22. Do not silently change database schema without migrations.

23. Every schema change must be migration-safe.

24. Keep naming consistent with the finalized ERD/database schema.

25. Do not resurrect removed entities from old documents.

26. Prefer small, testable application use cases.

27. Use parameterized database access.

28. Do not expose passwords, secrets, or sensitive values in logs.

29. Do not commit real credentials, production keys, or user data.

30. Do not automatically continue to the next phase.
```

---

# UNIVERSAL COMPLETION PROTOCOL

Every phase ends with this protocol:

```text
PHASE COMPLETION PROTOCOL

Do NOT start the next phase.

When this phase is complete:

1. Run all relevant tests.
2. Run the full existing test suite when practical.
3. Verify implementation against the finalized design documents.
4. Check for regressions.
5. Review database changes and migrations.
6. Review authorization/security implications.
7. Review audit requirements.
8. Review financial integrity implications if applicable.
9. Report exactly what was implemented.
10. Report all files created or modified.
11. Report all migrations created or changed.
12. Report all tests executed and their results.
13. Report any deviation from the phase plan.
14. Report any unresolved technical issue.
15. Report any unresolved business-rule ambiguity.
16. Confirm that no known open business decision was silently resolved.

Then STOP and wait for my instruction.

Do not continue automatically into the next phase.
```

---

# PHASE 0
# Project Foundation + Stack Setup

## Copy-Paste Prompt

```text
You are implementing Phase 0 of the Swimming Pool Management System.

Read these sources before making changes:

1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. The final synchronized SRS/design documents
4. Technical-Architecture-and-Stack-Decision.md
5. Hybrid-Final-Implementation-Plan-v1.0.md
6. This Phase 0 prompt

Your task is ONLY Phase 0:
PROJECT FOUNDATION + STACK SETUP.

Do not implement business modules yet.

GOAL

Create a clean, buildable .NET 8 WPF solution that follows the approved layered architecture and is ready for Phase 1.

APPROVED STACK

- .NET 8
- C#
- WPF
- MVVM
- CommunityToolkit.Mvvm
- Microsoft.Extensions.DependencyInjection
- SQLite
- EF Core
- BCrypt.Net-Next
- FluentValidation
- Serilog
- xUnit
- Inno Setup for packaging
- Approved UI component/design library selected during this phase
- .resx localization foundation

ARCHITECTURE

Use:

Domain
  ↓
Application
  ↓
Infrastructure
  ↓
UI

Do not create:
- microservices
- client/server architecture
- embedded local API
- unnecessary network layer
- separate backend process

Create the approved project structure:

/src
  /SwimClub.Domain
  /SwimClub.Application
  /SwimClub.Infrastructure
  /SwimClub.UI

/tests
  /SwimClub.Domain.Tests
  /SwimClub.Application.Tests
  /SwimClub.Infrastructure.Tests
  /SwimClub.UI.Tests

/installer
/docs

TASKS

1. Create the solution.
2. Create all required projects.
3. Configure project references according to the layered architecture.
4. Configure nullable reference types.
5. Configure implicit usings appropriately.
6. Configure dependency injection foundation.
7. Create the WPF application shell.
8. Establish MVVM conventions.
9. Add CommunityToolkit.Mvvm.
10. Establish logging foundation with Serilog.
11. Establish localization/resource-file foundation for Arabic and English.
12. Establish RTL/LTR capability without implementing the full application UI.
13. Create the initial application entry point.
14. Create a basic navigation/application shell abstraction if appropriate.
15. Add the test projects.
16. Add a minimal smoke test proving the solution builds and the test infrastructure works.
17. Configure solution-wide coding/build settings where useful.
18. Select the UI component/design library from the approved architecture options and document the choice.
19. Do not implement business entities or database schema yet.
20. Do not implement authentication, payments, training, packages, payroll, etc.

UI FOUNDATION

Create only a minimal professional application shell.

Use the approved aquatic design direction from the Technical Architecture.

Do not spend this phase implementing full screens.

TESTING

The solution must:
- restore/build successfully
- run tests successfully
- launch the WPF shell
- resolve dependency injection
- initialize localization resources without errors

Do not implement Phase 1 work.

PHASE COMPLETION PROTOCOL

Do NOT start Phase 1.
...
```

---

# PHASE 1
# Database + EF Core + Migrations

## Copy-Paste Prompt

```text
You are implementing Phase 1 of the Swimming Pool Management System.

Read:
1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. Final synchronized SRS/design documents
4. 01-ERD.md
5. 02-Database-Schema.md
6. Technical-Architecture-and-Stack-Decision.md
7. Hybrid-Final-Implementation-Plan-v1.0.md
8. This Phase 1 prompt

Your task is ONLY:
DATABASE + EF CORE + MIGRATIONS.

Before coding, inspect the repository and Phase 0 implementation.

IMPORTANT:
01-ERD.md and 02-Database-Schema.md are authoritative for the database structure.

Do not use stale entities from older plans.

GOAL

Create the complete persistence foundation required by later phases.

TASKS

1. Configure SQLite.
2. Configure EF Core.
3. Create the DbContext.
4. Implement the entities defined by the finalized ERD/schema.
5. Implement relationships.
6. Implement foreign keys.
7. Implement indexes.
8. Implement unique constraints.
9. Implement required check constraints where supported and appropriate.
10. Implement nullable/required columns exactly according to the finalized schema.
11. Implement historical snapshot fields.
12. Implement the finalized transaction/payment structures.
13. Implement the finalized CoachDue, Qualification, EmployeeQualification, Lane, PrivateBookingParticipant, RecreationalTicket, and other entities only if present in the final ERD/schema.
14. Do not recreate entities explicitly removed from the final design.
15. Implement `balance_due` according to the finalized schema.
16. Configure SQLite foreign-key enforcement.
17. Configure WAL mode according to the architecture.
18. Configure appropriate SQLite busy timeout/retry behavior where specified.
19. Create the initial EF Core migration.
20. Create database initialization behavior.
21. Add infrastructure integration tests using a real SQLite database file, not only an in-memory substitute.
22. Test creation of the database.
23. Test migration application.
24. Test foreign keys and critical constraints.
25. Test important unique constraints.
26. Test the finalized historical fields.
27. Verify that the schema matches the final ERD and Database Schema documents.

Do not build full UI screens.

Do not implement business workflows that belong to later phases.

Do not seed arbitrary business data.

If seed data is required by the finalized design, implement only that data.

PHASE COMPLETION PROTOCOL

Do NOT start Phase 2.
...
```

---

# PHASE 2
# Core Infrastructure

## Copy-Paste Prompt

```text
You are implementing Phase 2.

Read:
1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. Final synchronized SRS/design documents
4. 03-System-Architecture.md
5. 06-Validation-Rules.md
6. 08-Security-and-Authorization.md
7. 09-Audit-Log.md
8. Technical-Architecture-and-Stack-Decision.md
9. Hybrid-Final-Implementation-Plan-v1.0.md
10. Current repository
11. This prompt

Implement ONLY CORE INFRASTRUCTURE.

GOAL

Implement authentication, authorization, initial setup, configuration foundation, localization, and audit infrastructure.

AUTHENTICATION

Implement:
- Login
- Logout
- Failed login handling
- Password hashing with BCrypt
- Secure password verification
- User deactivation
- User reactivation foundation
- Password reset foundation according to finalized rules

Do not expose passwords.

Do not store plaintext passwords.

AUTHORIZATION

Implement the three fixed roles:
- Super Admin
- Owner
- Administrator

Implement fixed permissions exactly as finalized.

Do not implement:
- custom roles
- custom permissions
- editable permission matrix

Create a reusable authorization guard/service.

INITIAL PROGRAM SETUP

Implement first-run setup foundation for:
- Club name
- Club logo/basic branding
- System language
- Initial configuration

Do not invent configuration fields.

LOCALIZATION

Implement:
- Arabic resources
- English resources
- `.resx`
- System-wide language
- RTL/LTR support
- Language selection during initial setup

Do not implement per-user language.

CONFIGURATION

Create the configuration foundation for finalized configurable values.

Do not hardcode:
- session counts
- prices
- cancellation fees
- percentages
- employee rates
- salaries
- schedules

AUDIT

Implement the finalized insert-only Audit Log infrastructure.

Audit records must not be editable or deletable through normal application code.

Create:
- audit model/repository/service
- event types according to finalized documentation
- actor/user/system support
- success/failure
- entity
- timestamp
- details/reason where required

Implement audit infrastructure, not every business event yet.

SECURITY

Do not log:
- passwords
- password hashes
- secrets
- encryption keys

TESTS

Test:
- login success
- login failure
- password verification
- deactivated user rejection
- fixed-role authorization
- localization loading
- RTL/LTR foundation
- audit insert
- audit immutability behavior

Do not implement employee CRUD or full user management UI yet.

PHASE COMPLETION PROTOCOL

Do NOT start Phase 3.
...
```

---

# PHASE 3
# Financial Foundation

## Copy-Paste Prompt

```text
You are implementing Phase 3.

Read:
1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. Final synchronized SRS/design documents
4. 02-Database-Schema.md
5. 05-Business-Logic.md
6. 06-Validation-Rules.md
7. 07-State-Machines.md
8. 09-Audit-Log.md
9. 11-Cross-Module-Workflows.md
10. Technical-Architecture-and-Stack-Decision.md
11. Hybrid-Final-Implementation-Plan-v1.0.md
12. Current repository
13. This prompt

Implement ONLY THE FINANCIAL FOUNDATION.

GOAL

Create the financial infrastructure that later modules will use.

SCOPE

Implement:
- Transaction infrastructure
- Transaction types
- Payment infrastructure
- Payment correction
- Refund infrastructure
- Credit
- Outstanding
- Expense infrastructure
- Revenue classification
- Financial validation
- Financial Unit of Work
- Atomic database transactions

PAYMENTS

Implement the finalized Payment model and business-service foundation.

Payment amount must respect the finalized balance/payment rules.

Overpayment must be rejected.

A rejected overpayment:
- creates no financial transaction
- is audited according to the finalized audit rules

PAYMENT CORRECTION

Implement the finalized exception to transaction immutability:

Payment correction:
1. Update the Payment.
2. Update its single linked Transaction amount in the same atomic operation.
3. Audit the old value and new value.
4. Preserve historical audit information.

Do not create a separate correction transaction for the normal payment correction unless the finalized documents explicitly require one.

TRANSACTIONS

Transactions must be insert-only/immutable except for the sanctioned payment-correction exception.

Implement transaction classification.

Revenue reporting must later be based on transaction types.

CREDIT

Credit creation is NOT revenue.

Credit usage IS revenue at the usage date.

Credit applies only where the finalized design allows it.

OUTSTANDING

Implement the finalized distinction between:
- calculated `balance_due`
- declared outstanding amount

Do not merge these concepts.

Respect the finalized mutual exclusivity of Credit and declared Outstanding at creation.

REFUNDS

Create refund infrastructure.

Refund amount is calculated by the relevant business workflow later.

Refund confirmation creates a refund transaction.

Confirmed refunds are locked.

Refund corrections later use adjustment transactions.

EXPENSES

Create expense infrastructure:
- amount
- description
- transaction creation

Do not create arbitrary manual transaction types.

ATOMICITY

Financial operations that modify multiple records must be atomic.

Use EF Core transactions/Unit of Work.

TESTS

Create tests for:
- overpayment rejection
- transaction classification
- payment creation
- payment correction
- credit creation
- credit usage
- outstanding handling
- refund transaction creation
- expense transaction creation
- transaction immutability
- atomic rollback
- revenue classification rules

Do not implement Training, Packages, Private, Recreational, or Payroll workflows yet.

PHASE COMPLETION PROTOCOL

Do NOT start Phase 4.
...
```

---

# PHASE 4
# Employees + Qualifications + Attendance Foundation

## Copy-Paste Prompt

```text
You are implementing Phase 4.

Read:
1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. Final synchronized SRS/design documents
4. 01-ERD.md
5. 02-Database-Schema.md
6. 05-Business-Logic.md
7. 06-Validation-Rules.md
8. 08-Security-and-Authorization.md
9. Technical-Architecture-and-Stack-Decision.md
10. Hybrid-Final-Implementation-Plan-v1.0.md
11. Current repository
12. This prompt

Implement ONLY:
EMPLOYEES + QUALIFICATIONS + ATTENDANCE FOUNDATION.

EMPLOYEES

Implement:
- Administrator
- Coach
- Lifeguard
- Employee status
- Employee profile
- Employee attendance foundation
- Salary/rate data according to finalized schema

Important:
Owner and Super Admin are Users, not Employees.

ADMINISTRATOR

Administrator is:
Employee + User

Coach/Lifeguard are:
Employee only
No login required.

USERS

Implement user creation/linking behavior only where finalized.

Username initially follows the finalized employee-name rule.

If collision exists, use the finalized Nickname mechanism.

Do not invent alternative username generation.

QUALIFICATIONS

Implement:
- Qualification
- EmployeeQualification
- Qualification/rate configuration
- Qualification rank order
- Rate resolution foundation
- Historical rate preservation

Super Admin controls qualification ranking/configuration.

ATTENDANCE FOUNDATION

Implement employee attendance records needed by:
- Administrators
- Coaches
- Lifeguards

Do not implement full Training session attendance yet.

ADMINISTRATOR ATTENDANCE

Store daily:
- Administrator
- Date
- Present/Absent

EMPLOYEE HISTORY

Do not hard-delete employees where historical records exist.

Implement Active/Inactive status.

REACTIVATION

Implement reactivation without changing:
- username
- password
- historical records

Audit reactivation.

PASSWORD RESET

Implement the finalized Administrator reset flow:
- validation using National ID
- password reseeding behavior
- success/failure audit

Do not expose passwords.

TESTS

Test:
- employee types
- administrator/user relationship
- coach/lifeguard no-login model
- qualification assignment
- qualification ranking
- rate resolution
- employee deactivation
- employee reactivation
- password reset validation
- employee attendance

Do not implement Training/Payroll calculations yet.

PHASE COMPLETION PROTOCOL

Do NOT start Phase 5.
...
```

---

# PHASE 5
# Swimmers

## Copy-Paste Prompt

```text
You are implementing Phase 5.

Read:
1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. Final synchronized SRS/design documents
4. 01-ERD.md
5. 02-Database-Schema.md
6. 04-UI-UX-Specification.md
7. 05-Business-Logic.md
8. 06-Validation-Rules.md
9. Technical-Architecture-and-Stack-Decision.md
10. Hybrid-Final-Implementation-Plan-v1.0.md
11. Current repository
12. This prompt

Implement ONLY THE SWIMMERS MODULE.

SCOPE

Implement:
- Swimmer entity/application logic
- Create
- Edit
- View
- Soft delete
- Search
- Global search
- Swimmer ID generation
- QR/token generation/linking
- Member/Non-Member
- Active/Inactive status
- Swimmer profile

SWIMMER CREATION

Fields:
- Name
- Date of Birth
- Gender
- Parent Name
- Phone
- Member/Non-Member

System generates:
- Swimmer ID
- QR/token

Do not require fields not specified by the finalized design.

SEARCH

Global search supports:
- Swimmer Name
- Swimmer ID
- Phone Number
- Parent Name

Phone is not unique.

SWIMMER STATUS

Active if:
- at least one active Training Subscription
OR
- at least one active Package

Private activity does NOT make the swimmer Active.

Do not create a manual status rule that contradicts this.

DELETION

If an active subscription exists:
- deletion is rejected.

If no active subscription exists:
- manual delete may be allowed according to the finalized design.

Use soft delete so history remains available.

LIST

Main swimmer list shows:
- Name
- Status
- Current Subscription if Active
- Last Subscription if Inactive
- No Subscription if none

PROFILE TABS

Implement:
- Overview
- Subscriptions
- Payments
- Attendance
- Activity

Only show data available at this phase; do not invent future module behavior.

UI

Implement the Swimmers screens using the design system selected in Phase 0.

Use MVVM and application services.

TESTS

Test:
- ID generation
- QR/token generation
- search
- phone non-uniqueness
- member/non-member
- active/inactive calculation
- soft deletion
- active subscription deletion restriction
- profile loading

Do not implement Training/Package business logic in this phase.

PHASE COMPLETION PROTOCOL

Do NOT start Phase 6.
...
```

---

# PHASE 6
# Training Core

## Copy-Paste Prompt

```text
You are implementing Phase 6.

Read:
1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. Final synchronized SRS/design documents
4. 01-ERD.md
5. 02-Database-Schema.md
6. 04-UI-UX-Specification.md
7. 05-Business-Logic.md
8. 06-Validation-Rules.md
9. 07-State-Machines.md
10. 11-Cross-Module-Workflows.md
11. Technical-Architecture-and-Stack-Decision.md
12. Hybrid-Final-Implementation-Plan-v1.0.md
13. Current repository
14. This prompt

Implement ONLY THE TRAINING CORE.

PROGRAMS

Support:
- Regular
- Star
- Team

Do not add other training programs.

TRAINING PERIOD

Implement:
- Program
- Day(s)
- Start Time
- End Time
- Capacity
- Assigned Coaches
- Assigned Lifeguards
- Active/Inactive

Each Period belongs to one Program.

Period creation authority:
- Super Admin creates Periods.
- Admin can edit the finalized editable fields.
- Owner views only.

SCHEDULE

Periods are persistent ongoing groups.

Default schedules are configuration defaults.

Changing a default must not change existing Periods.

PRICING

Training pricing is based on:
- Program
- Member/Non-Member

Training price is the TOTAL subscription price, not price per session.

Existing subscriptions preserve historical price.

SUBSCRIPTION

Implement:
- Swimmer
- Program
- ONE Period
- Start Date
- Paid Amount
- Calculated total price
- Balance
- Session count
- End date
- Subscription/QR data

VALIDATION

- Start date today or future.
- Start date must match a Period day.
- Period must be Active.
- Capacity must be available.
- No duplicate active subscription on same Period.
- No overlapping active subscription schedules.
- Multiple active subscriptions are allowed when schedules do not overlap.
- Admin cannot override configured price.
- Newly created subscription is Active immediately.
- No Pending/Scheduled state.

SESSION GENERATION

Generate sessions from:
- Start Date
- Period days
- Period time
- Configured session count

Do not hardcode session count.

PERIOD SCHEDULE CHANGES

If schedule changes while subscriptions exist:
- historical/previous sessions remain unchanged
- future sessions follow the new schedule
- audit the change

COACH/LIFEGUARD ASSIGNMENT

Implement base Period assignments.

Prevent overlapping assignments according to finalized rules.

Do not implement replacement attendance behavior until Phase 7.

UI

Implement:
- Training Period list
- Period details
- Subscription creation
- Subscription details
- relevant pricing/configuration screens as applicable

TESTS

Test:
- Period creation
- schedule validation
- capacity
- coach overlap
- lifeguard overlap
- pricing
- member/non-member historical pricing
- subscription start-date rules
- duplicate subscription
- schedule overlap
- session generation
- future schedule changes

Do not implement Pause/Resume/Renewal/Cancellation/attendance yet.

PHASE COMPLETION PROTOCOL

Do NOT start Phase 7.
...
```

---

# PHASE 7
# Training Operations

## Copy-Paste Prompt

```text
You are implementing Phase 7.

Read:
1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. Final synchronized SRS/design documents
4. 04-UI-UX-Specification.md
5. 05-Business-Logic.md
6. 06-Validation-Rules.md
7. 07-State-Machines.md
8. 09-Audit-Log.md
9. 11-Cross-Module-Workflows.md
10. Technical-Architecture-and-Stack-Decision.md
11. Hybrid-Final-Implementation-Plan-v1.0.md
12. Current repository
13. This prompt

Implement ONLY TRAINING OPERATIONS.

ATTENDANCE

Implement:
- Period attendance screen
- swimmer list
- QR scan
- manual attendance
- session validation

QR is an identifier/validation mechanism, not an independent authorization source.

ATTENDANCE WINDOW

Allow swimmer attendance:
30 minutes before session start through 30 minutes after session start.

After the window:
- normal attendance is rejected.

IMPORTANT:
The unresolved late-edit/override business decision remains open.

Do NOT invent:
- admin override
- manager override
- forced attendance
- special bypass

If implementation needs the unresolved behavior, isolate it and report it.

PAUSE

Pause does not consume sessions.

Future sessions remain stored but paused/not active.

RESUME

Resume remaining sessions from the first available occurrence of the same Period after resume.

Admin may manually adjust generated dates according to finalized rules.

Pause extends end date by exact pause duration.

RENEWAL

If old subscription has remaining sessions:
- new subscription starts after old last session
- first available Period occurrence

If fully completed:
- new subscription starts from first available occurrence from renewal date

New subscription is separate.

CANCELLATION

Before first session:
- full refund
- no credit
- no fee

After first session:
- no refund
- no credit
- future sessions cancelled
- paid amount remains club revenue

REFUND

Use the Phase 3 refund infrastructure.

Admin confirms calculated refund; Admin does not arbitrarily edit the calculated amount.

Create transaction and audit.

COACH/LIFEGUARD REPLACEMENT

Implement:
- replacement coach for a specific session
- replacement lifeguard for a specific session
- primary assignment remains unchanged
- actual attending employee controls payroll for that session
- replacement uses own qualification/rate

EMPLOYEE ATTENDANCE

Coach/Lifeguard attendance may be recorded up to one hour after Period end.

After that:
- later administrative edits are audit-logged according to finalized rules.

If assigned employee is absent:
- 0 pay.

If replacement attends:
- replacement receives own rate.

PRIMARY ABSENCE MUST REMAIN RECORDED.

PERIOD CHANGES

Historical sessions remain unchanged.

Future sessions follow the new Period schedule.

Audit important changes.

TESTS

Create detailed tests for:
- QR attendance
- attendance window
- manual attendance
- pause
- resume
- renewal with remaining sessions
- renewal after completion
- cancellation before first session
- cancellation after first session
- replacement coach
- replacement lifeguard
- employee attendance
- payroll-driving attendance state
- historical/future schedule behavior

Do not resolve the known late-edit ambiguity.

PHASE COMPLETION PROTOCOL

Do NOT start Phase 8.
...
```

---

# PHASE 8
# Packages

## Copy-Paste Prompt

```text
You are implementing Phase 8.

Read:
1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. Final synchronized SRS/design documents
4. 01-ERD.md
5. 02-Database-Schema.md
6. 04-UI-UX-Specification.md
7. 05-Business-Logic.md
8. 06-Validation-Rules.md
9. 07-State-Machines.md
10. 11-Cross-Module-Workflows.md
11. Technical-Architecture-and-Stack-Decision.md
12. Hybrid-Final-Implementation-Plan-v1.0.md
13. Current repository
14. This prompt

Implement ONLY PACKAGES.

PACKAGE TYPES

Exactly:
1. Training Package
2. Recreational Package

TRAINING PACKAGE

- Regular training only.
- One Training Period.

RECREATIONAL PACKAGE

- One Recreational Period.

Packages cannot combine Training + Recreational.

PACKAGE MODEL

Package has:
- duration
- session allowance
- start date
- end date
- remaining session balance
- price
- QR
- period
- member/non-member historical pricing

Duration is calendar-based.

Unused sessions do not extend expiry.

PACKAGE PAUSE

Not allowed.

PACKAGE CREATION

Start Date:
- today or future

Period:
- Active
- capacity available
- no schedule conflict

Prevent duplicate active package on same Period.

Allow multiple active packages only where schedule does not conflict.

PRICING

Training Package:
- fixed Member price
- fixed Non-Member price
- Regular only

Recreational Package:
- fixed Member price
- fixed Non-Member price

Existing package preserves historical price.

ATTENDANCE

Each attendance consumes one available session.

QR validation must check:
- package active
- correct period
- not expired
- capacity
- valid QR

RENEWAL

If old package is active:
- new package starts after old End Date.

If expired:
- Admin selects today/future start date.

New package is separate.

Training ↔ Recreational switching is allowed through renewal.

PERIOD CHANGE

Allowed while active.

Old Period applies through day before change.

New Period applies from change date.

Start/End dates remain unchanged.

Remaining session balance carries over.

Audit the change.

CANCELLATION

Before package begins:
- full refund

During first month:
- use normal Training price for the same Regular Program
- normal Training price is the historical price saved at package creation
- refund = paid amount - normal Training price
- if result is negative, refund = 0
- no credit/outstanding is created because of a negative result

After first month:
- no refund
- no credit
- paid amount remains with club

IMPORTANT:
The exact first-month boundary interpretation is an unresolved business decision.

Do not invent an interpretation.

If implementation needs a boundary condition, isolate it and report it.

TESTS

Test:
- package creation
- session balance
- expiry
- attendance consumption
- renewal
- period change
- QR invalidation on new QR
- cancellation before start
- cancellation during first month
- cancellation after first month
- capacity
- schedule conflict
- historical pricing

Do not resolve the open first-month boundary decision.

PHASE COMPLETION PROTOCOL

Do NOT start Phase 9.
...
```

---

# PHASE 9
# Private

## Copy-Paste Prompt

```text
You are implementing Phase 9.

Read:
1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. Final synchronized SRS/design documents
4. 01-ERD.md
5. 02-Database-Schema.md
6. 04-UI-UX-Specification.md
7. 05-Business-Logic.md
8. 06-Validation-Rules.md
9. 07-State-Machines.md
10. 09-Audit-Log.md
11. 11-Cross-Module-Workflows.md
12. Technical-Architecture-and-Stack-Decision.md
13. Hybrid-Final-Implementation-Plan-v1.0.md
14. Current repository
15. This prompt

Implement ONLY PRIVATE.

PRIVATE TYPES

Exactly:
- Lane Rental
- Coach-Brought Private
- Club-Brought Private

PARTICIPANTS

Support:
- Existing Swimmer
- Guest

Guests belong to the Private booking and do not require a Swimmer Profile.

LANE RENTAL

- Specific lane required.
- Club fee is fixed for configured private session count.
- Number of swimmers does not change club fee.
- Capacity applies.
- One specific coach.
- Coach's own charges to swimmers are outside club financial calculations.
- Same lane cannot have overlapping bookings.

COACH-BROUGHT PRIVATE

- One specific coach.
- Club fee is fixed per swimmer for the entire configured private duration.
- Club revenue = number of swimmers × fixed fee per swimmer.
- Coach may charge swimmers independently.
- Club does not track coach's private charge.

CLUB-BROUGHT PRIVATE

- One specific coach.
- Client pays total amount.
- Club % + Coach % = exactly 100%.
- System calculates split automatically.
- Percentages are snapshotted at booking creation.
- Later configuration changes do not affect existing booking.
- Coach share becomes Coach Due.

SCHEDULING

A coach cannot have overlapping:
- Private + Private
- Private + Training Period

No replacement coach for Private.

SESSIONS

Generate private sessions using:
- configured private session count
- selected start date
- applicable schedule

ATTENDANCE

No QR.

Coach tells Admin who attended/was absent.

Admin records private swimmer attendance based on coach report.

CANCELLATION

Before first session:
- full refund
- no credit
- no cancellation fee

Lane Rental + Coach-Brought:
- before completing 2 sessions: 50% of club value is deducted; remainder refunded
- after 2 sessions: club gets full club amount; no refund

Club-Brought:
- before first session: normal/full refund
- after first session:
  - configured cancellation fee applies
  - remaining amount refunded
  - coach receives applicable cancellation fee
  - future sessions cancelled
  - no credit

IMPORTANT:
The exact boundary interpretation for "before completing 2 sessions" is unresolved.

Do not invent whether exactly one completed session belongs to one tier unless the finalized documents explicitly resolve it.

COACH DUES

Ordinary Club-Brought coach revenue share is accrued as one Coach Due entry at booking creation.

Cancellation must offset/reconcile the appropriate due so the coach is not double-paid.

FINANCE

Use Phase 3 financial infrastructure.

Do not create arbitrary manual transactions.

AUDIT

Audit:
- important booking changes
- cancellation
- refund
- financial operations
- configuration-sensitive operations

TESTS

Test:
- lane overlap
- coach overlap
- participant handling
- guest handling
- private session generation
- revenue calculation
- Club/Coach split
- snapshot percentages
- Coach Due
- cancellation
- refund
- no double payment
- attendance

Do not resolve the 2-session boundary ambiguity.

PHASE COMPLETION PROTOCOL

Do NOT start Phase 10.
...
```

---

# PHASE 10
# Recreational

## Copy-Paste Prompt

```text
You are implementing Phase 10.

Read:
1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. Final synchronized SRS/design documents
4. 01-ERD.md
5. 02-Database-Schema.md
6. 04-UI-UX-Specification.md
7. 05-Business-Logic.md
8. 06-Validation-Rules.md
9. 07-State-Machines.md
10. 09-Audit-Log.md
11. 11-Cross-Module-Workflows.md
12. Technical-Architecture-and-Stack-Decision.md
13. Hybrid-Final-Implementation-Plan-v1.0.md
14. Current repository
15. This prompt

Implement ONLY RECREATIONAL.

RECREATIONAL PERIOD

Implement:
- days
- start time
- end time
- capacity
- Active/Inactive

Super Admin creates the Period.

Admin operates/edits the finalized editable fields.

DEFAULTS

Recreational schedule defaults come from configuration.

Changing defaults does not modify existing Periods.

SINGLE ENTRY

Admin manually verifies club membership card.

There is no full club membership database.

Create a Recreational Ticket/entry record containing:
- Name
- Member/Non-Member
- Recreational Period
- Date/time
- Paid amount
- Description/payment details

No receipt.

PAYMENT

- Full payment at entry.
- No partial payment.
- No outstanding.
- No credit.

CHECK-IN WINDOW

Allow entry:
30 minutes before Period start through 30 minutes after Period start.

After the window:
- new check-in rejected.

No checkout.

If a person leaves and returns:
- do not create a second check-in.

CAPACITY

Capacity is based on actual check-ins.

Once capacity is full:
- reject additional entries
- even if someone later leaves, because there is no checkout

PACKAGE HOLDERS

Package holders have priority.

A package does not reserve a spot merely by existing.

If package holder does not arrive during the valid window:
- the spot is considered free.

SINGLE ENTRY

Allowed when capacity is available.

DUPLICATE CHECK-IN

The finalized design says the same person cannot check in twice for the same Recreational Period.

The ticket identity is intentionally anonymous/free-text name based.

IMPORTANT:
The enforcement mechanics/override behavior for duplicate check-in is an unresolved business decision.

Do not invent:
- hidden identity matching
- automatic fuzzy matching
- manual override
- phone-number requirement
- membership database integration

If implementation needs the unresolved behavior, isolate it and report it.

PACKAGE CHECK-IN

Use QR.

Validate:
- package active
- correct Recreational Period
- not expired
- capacity
- QR validity

Attendance consumes one package session.

FINANCE

Create the finalized Recreational Single Entry transaction.

No arbitrary manual transactions.

REPORTING DATA

Persist everything needed for:
- ticket count
- revenue
- Member ticket count
- Non-Member ticket count

TESTS

Test:
- Period creation
- schedule
- capacity
- single entry
- full payment
- check-in window
- no checkout
- package priority
- package check-in
- session consumption
- duplicate attempt handling to the extent explicitly defined
- financial transaction creation

Do not resolve the duplicate identity/enforcement ambiguity.

PHASE COMPLETION PROTOCOL

Do NOT start Phase 11.
...
```

---

# PHASE 11
# Payroll + Coach Dues

## Copy-Paste Prompt

```text
You are implementing Phase 11.

Read:
1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. Final synchronized SRS/design documents
4. 01-ERD.md
5. 02-Database-Schema.md
6. 05-Business-Logic.md
7. 06-Validation-Rules.md
8. 07-State-Machines.md
9. 09-Audit-Log.md
10. 11-Cross-Module-Workflows.md
11. Technical-Architecture-and-Stack-Decision.md
12. Hybrid-Final-Implementation-Plan-v1.0.md
13. Current repository
14. This prompt

Implement ONLY PAYROLL + COACH DUES.

ADMINISTRATOR PAYROLL

Administrator has fixed monthly salary.

Calculate:

Daily Value:
Monthly Salary ÷ number of days in the month

Absence Deduction:
Absent Days × Daily Value

Net Salary:
Monthly Salary - Absence Deduction

If no absence:
- full salary

COACH/LIFEGUARD PAYROLL

Actual attendance determines payroll.

- Present → applicable session rate
- Absent → 0
- Replacement → replacement employee's rate

RATE RESOLUTION

Use the finalized qualification/rate hierarchy.

Rate used for payroll must be historically preserved.

Do not recalculate historical payroll from today's configuration.

COACH DUES

Integrate finalized Coach Due sources.

At minimum, support finalized Private coach revenue-share dues and cancellation-fee-related dues.

Coach dues must be consumed exactly once through payroll.

Do not double-pay dues.

PAYROLL CALCULATION

Implement monthly payroll calculation.

Payroll includes:
- employee
- month
- attendance-derived values
- applicable rates
- dues where applicable
- net amount
- status

PAYMENT STATUS

Support:
- Not Paid
- Paid

Admin, Owner, and Super Admin can mark payroll Paid/Not Paid according to finalized permissions.

Paid records payment date.

MARK PAID

Marking payroll Paid:
- creates Payroll Transaction
- locks the original payroll calculation

ADJUSTMENTS

Once Paid:
- do not edit the original payroll record
- use positive/negative adjustment transaction
- audit the adjustment

FINANCIAL INTEGRITY

Use Phase 3 transaction infrastructure.

Ensure payroll payment is atomic.

AUDIT

Audit:
- payroll calculation where required
- mark paid
- mark unpaid where allowed
- adjustments
- important rate/configuration-sensitive operations

TESTS

Test:
- administrator salary calculation
- month day count
- absence deduction
- no absence
- coach attendance
- lifeguard attendance
- replacement rate
- qualification rate
- historical rate preservation
- Coach Due creation/consumption
- no double payment
- payroll payment transaction
- payroll locking
- adjustment transaction
- audit

PHASE COMPLETION PROTOCOL

Do NOT start Phase 12.
...
```

---

# PHASE 12
# Reports + Dashboard

## Copy-Paste Prompt

```text
You are implementing Phase 12.

Read:
1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. Final synchronized SRS/design documents
4. 02-Database-Schema.md
5. 04-UI-UX-Specification.md
6. 05-Business-Logic.md
7. 09-Audit-Log.md
8. 11-Cross-Module-Workflows.md
9. 13-Requirements-Traceability.md
10. Technical-Architecture-and-Stack-Decision.md
11. Hybrid-Final-Implementation-Plan-v1.0.md
12. Current repository
13. This prompt

Implement ONLY REPORTS + DASHBOARD.

REPORT PERIODS

Support:
- Daily
- Monthly
- Yearly
- Custom From Date → To Date

REPORTS

Implement:
- Revenue
- Expenses
- Profit
- Payments
- Outstanding
- Payroll
- Attendance
- Subscriptions
- Cancellations
- Swimmers
- Coach Dues
- Recreational Tickets
- Qualification/rate information where applicable

REVENUE

Revenue must be classified by finalized transaction type.

Revenue includes the finalized revenue sources:
- Training
- Packages
- Private
- Recreational Single Entry
- Used Credit

Refunds reduce recognized revenue appropriately.

Credit creation is not revenue.

Credit usage is revenue at usage date.

Do NOT calculate revenue as:
SUM(all positive transactions).

OUTSTANDING

Respect the finalized distinction between:
- calculated balance_due
- administrator-declared outstanding amounts

The Outstanding report follows the finalized reporting definition.

PROFIT

Profit:

Revenue - Expenses

Payroll is an expense.

PAYMENTS

Report payment activity with appropriate transaction classifications.

PAYROLL

Show:
- totals
- Paid/Not Paid
- per employee details

ATTENDANCE

Include finalized attendance sources:
- swimmers
- coaches
- lifeguards
- recreational check-ins
- private swimmer attendance

CANCELLATIONS

Include:
- count
- type
- paid amount
- refund
- cancellation fee
- date
- financial information

SWIMMERS

Include:
- total
- Active/Inactive
- Member/Non-Member

COACH DUES

Show:
- due totals
- relevant source
- consumed/paid state according to finalized model

RECREATIONAL TICKETS

Show:
- ticket count
- revenue
- Member/Non-Member counts

DASHBOARD

Create the common dashboard structure for all roles.

Role-specific visibility must follow authorization.

Do not create role-specific business rules that do not exist.

CHARTS

Use the approved charting approach from Technical Architecture.

REPORT QUERIES

Use EF Core LINQ and/or parameterized SQL as appropriate.

Do not introduce a data warehouse or ETL layer.

PERFORMANCE

Use appropriate indexes and query strategies.

Do not load unnecessary full tables into memory.

TESTS

Test:
- revenue classification
- refund effect
- credit creation vs usage
- profit
- payroll expense
- date filtering
- custom ranges
- outstanding definition
- report totals
- role visibility
- dashboard data

Add a financial reconciliation test that verifies the finalized transaction classification produces the expected report totals.

PHASE COMPLETION PROTOCOL

Do NOT start Phase 13.
...
```

---

# PHASE 13
# Backup / Restore / Migration

## Copy-Paste Prompt

```text
You are implementing Phase 13.

Read:
1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. Final synchronized SRS/design documents
4. 07-State-Machines.md
5. 08-Security-and-Authorization.md
6. 09-Audit-Log.md
7. 10-Backup-Restore-and-Migration.md
8. Technical-Architecture-and-Stack-Decision.md
9. Hybrid-Final-Implementation-Plan-v1.0.md
10. Current repository
11. This prompt

Implement ONLY BACKUP / RESTORE / MIGRATION.

BACKUP TYPES

Support:
- automatic weekly backup
- manual backup

Destinations:
- local
- USB/external drive
- network folder

RETENTION

Maximum 7 backups total across automatic and manual backups.

When an 8th backup is successfully created:
- delete the oldest backup.

BACKUP METADATA

Preserve:
- Date
- Time
- System Version

ENCRYPTION

Backups must be encrypted.

Use:
- AES-256-GCM
- Windows DPAPI for key protection

Do not expose encryption keys.

AUTOMATIC BACKUP FAILURE

Attempt up to 5 total times.

If all attempts fail:
- log failure in Audit Log/System Log
- notify Super Admin
- keep system operational
- require manual backup

RESTORE

Restore is a full snapshot.

Restore:
- data
- settings
- users
- audit
- financial history
- system state
- other persisted application state required by the finalized design

Before restore:
- create a safety backup of current state.

MIGRATION

Restore to a newer application version:
- supported through DB migration.

Restore from a newer backup into an older application:
- reject.
- application must be upgraded first.

Use staging-copy migration and atomic replacement/swap behavior where specified by the architecture.

SQLITE

Use the approved SQLite online backup strategy.

Do not simply copy an actively used database file if the architecture requires the online backup API.

TESTS

Implement integration tests for:
- backup creation
- encryption
- metadata
- retention
- oldest-backup deletion
- automatic retry
- failure logging
- restore
- exact-state verification
- restore safety backup
- migration
- newer-backup/older-program rejection
- newer-program restoring older backup
- rollback/failed migration safety

Do not proceed to release hardening.

PHASE COMPLETION PROTOCOL

Do NOT start Phase 14.
...
```

---

# PHASE 14
# Security + Integrity Hardening

## Copy-Paste Prompt

```text
You are implementing Phase 14.

Read:
1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. Final synchronized SRS/design documents
4. 06-Validation-Rules.md
5. 07-State-Machines.md
6. 08-Security-and-Authorization.md
7. 09-Audit-Log.md
8. 10-Backup-Restore-and-Migration.md
9. 11-Cross-Module-Workflows.md
10. 13-Requirements-Traceability.md
11. Technical-Architecture-and-Stack-Decision.md
12. Hybrid-Final-Implementation-Plan-v1.0.md
13. Current repository
14. This prompt

Implement ONLY SECURITY + INTEGRITY HARDENING.

GOAL

Perform a systematic hardening pass across the implemented system without changing finalized business rules.

SECURITY

Verify and harden:
- BCrypt password hashing
- password verification
- authorization
- fixed roles
- user deactivation
- user reactivation
- password reset
- National ID validation
- sensitive data handling
- logging safety
- backup encryption
- DPAPI key protection

Do not expose:
- passwords
- password hashes
- encryption keys
- sensitive credentials

AUTHORIZATION

Test every protected module/action against:
- Super Admin
- Owner
- Administrator

Verify that permissions exactly match the finalized matrix.

AUDIT

Verify that important:
- security operations
- failed attempts
- financial operations
- user operations
- configuration changes
- backup/restore operations
are audited according to finalized requirements.

Audit Log must remain insert-only.

FINANCIAL INTEGRITY

Verify:
- transaction immutability
- sanctioned payment correction exception
- refund adjustment mechanism
- payroll adjustment mechanism
- credit usage
- Coach Due consumption
- no double payment
- revenue classification
- financial atomicity
- no duplicate transactions

DATABASE

Verify:
- foreign keys
- unique constraints
- check constraints
- indexes
- migration correctness
- transaction boundaries
- SQLite busy timeout/retry
- concurrency behavior where applicable

HISTORICAL TRUTH

Verify that:
- old subscription prices remain unchanged
- old package prices remain unchanged
- old private percentages remain unchanged
- old payroll rates remain unchanged
- old financial records remain historically accurate
- configuration changes do not rewrite history

TESTS

Add security and integrity regression tests.

Run the complete existing test suite.

Do not introduce new business behavior.

PHASE COMPLETION PROTOCOL

Do NOT start Phase 15.
...
```

---

# PHASE 15
# Full Integration Testing + UAT + Release

## Copy-Paste Prompt

```text
You are implementing Phase 15, the final implementation and release phase.

Read:
1. Design_Closure_Decisions.md
2. 15-Design-Closure-Amendments.md
3. All final synchronized SRS/design documents
4. 13-Requirements-Traceability.md
5. 14-Implementation-Readiness-Review.md
6. 17-FINAL-DESIGN-SYNCHRONIZATION-REPORT.md
7. Technical-Architecture-and-Stack-Decision.md
8. Hybrid-Final-Implementation-Plan-v1.0.md
9. Current repository
10. This prompt

This phase is:
FULL INTEGRATION TESTING + UAT + RELEASE.

Do not add new business functionality unless required to fix a verified implementation defect against the finalized requirements.

GOAL

Verify the complete system end-to-end and prepare a release build.

TESTING LAYERS

1. Domain unit tests.
2. Application/use-case tests.
3. Infrastructure integration tests using real SQLite.
4. UI smoke tests.
5. End-to-end workflow tests.
6. Security/authorization tests.
7. Backup/restore tests.
8. Financial reconciliation tests.

CRITICAL TRAINING FLOW

Verify:

Create Swimmer
→ Create Training Subscription
→ Payment
→ Session Generation
→ Attendance
→ Employee Attendance
→ Payroll
→ Payroll Payment
→ Revenue
→ Report

CRITICAL PACKAGE FLOW

Verify:

Create Package
→ QR
→ Attendance
→ Session Consumption
→ Expiry
→ Renewal
→ Financial reporting

CRITICAL PRIVATE FLOW

Verify:

Private Booking
→ Participant
→ Sessions
→ Attendance
→ Revenue
→ Coach Due
→ Payroll
→ Cancellation
→ Refund
→ Financial reconciliation

CRITICAL RECREATIONAL FLOW

Verify:

Recreational Period
→ Single Entry
→ Capacity
→ Payment
→ Ticket
→ Revenue
→ Report

Also verify Recreational Package check-in.

CRITICAL BACKUP FLOW

Verify:

Create Backup
→ Change Data
→ Create More Data
→ Restore
→ Verify exact previous state

Also verify:
- encrypted backup
- retention
- migration
- safety backup
- incompatible-version rejection

AUTHORIZATION UAT

Verify every major module and action for:
- Super Admin
- Owner
- Administrator

No unauthorized action may succeed.

AUDIT UAT

Verify:
- successful important actions
- failed security actions
- financial validation failures
- user management events
- configuration events
- backup/restore events
- important corrections

FINANCIAL RECONCILIATION

Run a comprehensive financial reconciliation suite.

Verify that report revenue and expenses are derived from the finalized transaction classification.

Test:
- Training
- Package
- Private
- Recreational
- Credit usage
- Refunds
- Expenses
- Payroll
- Coach dues
- Adjustments

No double counting.

UI

Verify:
- Arabic
- English
- RTL
- LTR
- navigation
- permissions
- forms
- validation messages
- data grids
- reports
- dashboard
- backup/restore UI
- error handling

INSTALLER

Create and test:
- self-contained win-x64 publish
- Inno Setup installer
- fresh installation
- application launch
- database creation
- upgrade path if applicable

DO NOT

- invent new business rules
- silently resolve open business decisions
- add unrequested features
- change finalized calculations merely to make tests pass

OPEN BUSINESS DECISIONS

The following remain explicitly unresolved unless the user has separately finalized them:

1. Training attendance late-edit override after the ±30-minute window.
2. Recreational duplicate check-in enforcement mechanics for anonymous/free-text tickets.
3. Package cancellation first-month boundary interpretation.
4. Private Lane Rental / Coach-Brought cancellation boundary interpretation.

If any release test depends on one of these decisions:

STOP the affected test/feature and report it.

Do not invent a resolution.

RELEASE GATE

The system is release-ready only when:

- All applicable tests pass.
- No critical defects remain.
- Financial reconciliation passes.
- Backup/restore round trip passes.
- Authorization passes.
- Audit requirements pass.
- Localization works.
- RTL/LTR works.
- Requirements traceability is complete.
- Installer works.
- Fresh installation works.
- Migration works.
- No unresolved implementation contradiction remains.

FINAL REPORT

Produce:

1. Implementation summary.
2. Final architecture summary.
3. Files/modules changed.
4. Database/migration summary.
5. Test summary.
6. Known limitations.
7. Open business decisions.
8. Release artifacts.
9. Exact version/build information.
10. Any deviation from the finalized requirements.

Do not claim the system is complete if any release-gate item is failing.

PHASE COMPLETION PROTOCOL

This is the final phase.

Do not start another implementation phase automatically.

After completing the release verification:
- report the final status
- report failures clearly
- report known limitations
- STOP and wait for further instruction.
```

---

# Final Agent Workflow

Use the prompts in this exact order:

```text
Phase 0 Prompt
      ↓
Review / Approve
      ↓
Phase 1 Prompt
      ↓
Review / Approve
      ↓
Phase 2 Prompt
      ↓
Review / Approve
      ↓
Phase 3 Prompt
      ↓
Review / Approve
      ↓
Phase 4 Prompt
      ↓
Review / Approve
      ↓
Phase 5 Prompt
      ↓
Review / Approve
      ↓
Phase 6 Prompt
      ↓
Review / Approve
      ↓
Phase 7 Prompt
      ↓
Review / Approve
      ↓
Phase 8 Prompt
      ↓
Review / Approve
      ↓
Phase 9 Prompt
      ↓
Review / Approve
      ↓
Phase 10 Prompt
      ↓
Review / Approve
      ↓
Phase 11 Prompt
      ↓
Review / Approve
      ↓
Phase 12 Prompt
      ↓
Review / Approve
      ↓
Phase 13 Prompt
      ↓
Review / Approve
      ↓
Phase 14 Prompt
      ↓
Review / Approve
      ↓
Phase 15 Prompt
      ↓
Final Release
```

# Final Rule

**Never tell the coding agent to "build the whole system" in one prompt.**

The agent must implement one phase, test it, report it, and stop.

The user reviews the result before the next phase begins.
