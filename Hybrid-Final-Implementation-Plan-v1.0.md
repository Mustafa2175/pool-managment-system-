# Swimming Pool Management System
## Hybrid Final Implementation Plan — v1.0

**Status:** FINAL — Implementation Plan Locked

---

## 1. Source of Truth

Claude/Coding Agent must follow this precedence:

1. `Design_Closure_Decisions.md`
2. `15-Design-Closure-Amendments.md`
3. Final synchronized SRS / design documents
4. `Technical-Architecture-and-Stack-Decision.md`
5. This Implementation Plan
6. The specific Phase Prompt

> **The Phase Prompt controls execution scope, NOT business behavior.**

Any Business Rule defined in the finalized documents must not be changed, simplified, reinterpreted, or replaced with an invented alternative.

---

# Phase 0 — Project Foundation + Stack Setup

## Goal

Prepare the complete solution and development foundation.

## Scope

- .NET 8
- WPF
- MVVM
- CommunityToolkit.Mvvm
- Dependency Injection
- Logging foundation
- Localization foundation
- UI design system selection/spike
- Project structure
- Test projects
- Git/project conventions
- Initial application shell

## Project Structure

```text
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
```

## Important

The final UI toolkit must be selected here from the options specified in the Technical Architecture document. Do not invent a different UI framework during later phases.

---

# Phase 1 — Database + EF Core + Migrations

## Goal

Build the database foundation according to the finalized ERD and database schema.

## Scope

- SQLite
- EF Core
- DbContext
- Entities
- Relationships
- Constraints
- Indexes
- Foreign Keys
- WAL configuration
- Migration infrastructure
- Initial migration
- Database initialization
- SQLite configuration
- `balance_due` generated/computed behavior according to the finalized schema
- Historical snapshot fields
- Seed data only where explicitly allowed by the design

## Required Source Review

Before implementation, read:

```text
01-ERD.md
02-Database-Schema.md
```

Do not use entities, columns, relationships, or terminology from superseded versions of the design.

---

# Phase 2 — Core Infrastructure

## Goal

Build the infrastructure required by the rest of the system.

## Scope

### Authentication

- Login
- Logout
- Failed login handling
- Password hashing
- BCrypt

### Authorization

- Super Admin
- Owner
- Administrator
- Fixed permissions
- AuthorizationGuard

### Initial Program Setup

- Club information
- Logo/basic branding
- Language
- Initial configuration

### Configuration

- System settings foundation
- Configuration persistence
- Configuration validation

### Localization

- Arabic
- English
- `.resx`
- RTL/LTR
- System-wide language

### Audit Infrastructure

- Audit repository
- Insert-only behavior
- Audit event structure

## Important

- No custom roles.
- No editable permission matrix.

---

# Phase 3 — Financial Foundation

This is a core architectural phase and must be implemented before the main business modules that depend on financial operations.

## Goal

Build the financial engine and contracts used by Training, Packages, Private, Recreational, Payroll, and Reports.

## Scope

- Transactions
- Transaction types
- Payments
- Payment correction
- Refund infrastructure
- Credit
- Outstanding
- Expenses
- Financial validation
- Revenue classification
- Financial Unit of Work
- Database transactions

## Core Financial Relationships

```text
Payment
    ↓
Transaction

Refund
    ↓
Refund Transaction

Expense
    ↓
Expense Transaction

Credit Creation
    ↓
NO Revenue

Credit Usage
    ↓
Revenue

Payroll Payment
    ↓
Payroll Transaction
```

## Critical Rule

Transactions are immutable except for the single finalized Payment Correction exception.

Payment correction:

```text
Payment corrected
      ↓
linked Transaction amount corrected
      ↓
Audit Log
```

Payroll and refund corrections use adjustment transactions.

---

# Phase 4 — Employees + Qualifications + Attendance Foundation

## Goal

Build the employee domain before Training and Payroll.

## Scope

- Employees
- Administrator
- Coach
- Lifeguard
- Employee status
- Employee profile
- Employee attendance foundation
- Qualifications
- EmployeeQualification
- Qualification rate configuration
- Qualification ranking
- Rate resolution
- Historical rate preservation

## Rules

- Owner/Super Admin are not Employees.
- Administrator = Employee + User.
- Coach/Lifeguard = Employee only.
- No hard deletion where historical data exists.
- Qualification ranking is controlled by Super Admin.

---

# Phase 5 — Swimmers

## Scope

- Swimmer CRUD
- Auto-generated Swimmer ID
- QR/token
- Member/Non-Member
- Active/Inactive
- Soft delete
- Global Search
- Swimmer Profile
- Overview
- Subscriptions
- Payments
- Attendance
- Activity

## Active Status

A swimmer is Active when they have:

```text
Active Training Subscription
OR
Active Package
```

Private activity does not make a swimmer Active.

---

# Phase 6 — Training Core

## Goal

Build the Training foundation before advanced Training operations.

## Scope

### Programs

- Regular
- Star
- Team

### Training Periods

- Program
- Days
- Start Time
- End Time
- Capacity
- Coaches
- Lifeguards
- Active/Inactive

### Pricing

- Program
- Member/Non-Member
- Historical price preservation

### Subscriptions

- Swimmer
- Program
- Period
- Start Date
- Paid Amount
- Calculated price
- Balance
- Sessions
- End Date
- QR/subscription data

### Session Generation

Sessions are generated from the Period schedule.

Session count comes from configuration.

## Core Validations

- Start date is today or future.
- Start date matches a Period day.
- Period is Active.
- Capacity is available.
- No duplicate active subscription on the same Period.
- No schedule overlap.
- No price override.
- Newly created Training Subscription is Active immediately.

---

# Phase 7 — Training Operations

## Goal

Implement the operational behavior of Training.

## Scope

- Swimmer attendance
- QR scanning
- Manual attendance
- Attendance window
- Pause
- Resume
- Renewal
- Cancellation
- Replacement Coach
- Replacement Lifeguard
- Employee attendance interaction
- Period schedule changes
- Future session handling

## Attendance Window

```text
Session Start - 30 minutes
        ↓
Attendance allowed
        ↓
Session Start + 30 minutes
        ↓
Attendance closes
```

## Pause

- Does not consume sessions.
- Future sessions become paused/not active.
- Resume schedules remaining sessions.
- Exact pause duration extends the subscription end date.

## Cancellation

Before first session:

```text
Full Refund
No Credit
No Fee
```

After first session:

```text
No Refund
No Credit
Future Sessions Cancelled
Paid Amount remains Revenue
```

## Important

The unresolved Training attendance late-edit decision remains open.

Do not invent an override rule.

---

# Phase 8 — Packages

## Package Types

```text
Training Package
Recreational Package
```

Training Package:

```text
Regular only
One Training Period
```

Recreational Package:

```text
One Recreational Period
```

## Scope

- Package creation
- Session allowance
- Duration
- Expiry
- QR
- Attendance consumption
- Renewal
- Period Change
- Cancellation
- Pricing
- Member/Non-Member
- Capacity
- Schedule conflict

## Package Model

```text
Calendar Duration
+
Session Balance
```

Unused sessions do not extend expiry.

Package Pause is not allowed.

## Period Change

```text
Old Period → day before change
New Period → change date onward

Start Date unchanged
End Date unchanged
Remaining sessions carried over
```

---

# Phase 9 — Private

## Private Types

```text
Lane Rental
Coach-Brought Private
Club-Brought Private
```

## Scope

- Private booking
- Participants
- Swimmer/Guest
- Sessions
- Attendance
- Lane management
- Coach assignment
- Schedule conflict
- Pricing
- Club/Coach revenue split
- Coach dues
- Cancellation
- Refund
- Cancellation fee

## Lane Rental

A specific lane is required.

The same lane cannot have overlapping bookings.

## Coach-Brought

```text
Number of swimmers
×
Fixed club fee per swimmer
```

## Club-Brought

```text
Club %
+
Coach %
=
100%
```

Percentages are snapshotted at booking creation.

## Coach Dues

Coach revenue share is accrued as a single Coach Due entry and consumed through payroll.

## Important

- No replacement coach for Private.
- The unresolved cancellation boundary remains unresolved: “before completing 2 sessions”.
- Do not invent an interpretation.

---

# Phase 10 — Recreational

## Recreational Period

- Days
- Start Time
- End Time
- Capacity
- Active/Inactive

## Single Entry

- Manual membership verification
- Name
- Member/Non-Member
- Recreational Period
- Date/time
- Paid amount
- Description/payment details
- Transaction

## Rules

- No QR.
- No checkout.
- Full payment.
- No partial payment.
- No credit.
- ±30-minute check-in window.
- Capacity based on actual check-ins.
- No second check-in.

## Recreational Package

QR validates:

```text
Package
+
Period
+
Expiry
+
Capacity
```

Attendance consumes one package session.

## Important

The anonymous/free-text duplicate check-in issue remains a business decision.

Do not invent an override mechanism.

---

# Phase 11 — Payroll + Coach Dues

## Administrator Payroll

```text
Daily Value =
Monthly Salary ÷ Days in Month

Absence Deduction =
Absent Days × Daily Value

Net Salary =
Monthly Salary - Absence Deduction
```

## Coach/Lifeguard

Actual attendance determines payment.

```text
Present → Rate
Absent → 0
Replacement → Replacement's Rate
```

Rate is determined by the highest applicable qualification.

## Payroll

- Monthly calculation
- Paid / Not Paid
- Payment date
- Payroll transaction
- Lock after payment
- Adjustment transactions
- Audit

## Coach Dues

Coach dues may include:

- Private Coach revenue share
- Applicable cancellation fee
- Other finalized due sources

Coach dues must not be double-paid when a booking is cancelled.

---

# Phase 12 — Reports + Dashboard

## Reports

- Daily
- Monthly
- Yearly
- Custom From Date → To Date

## Report Types

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

## Revenue

Revenue must be based on classified transaction types.

Do not calculate revenue by simply summing all positive transactions.

## Profit

```text
Revenue - Expenses
```

Payroll is an expense.

## Dashboard

The overall dashboard structure is shared across roles, with data and actions filtered according to permissions.

---

# Phase 13 — Backup / Restore / Migration

## Backup

- Automatic weekly backup
- Manual backup
- Local backup
- USB/external drive
- Network folder

Maximum:

```text
7 backups
```

When the 8th backup is created:

```text
Delete oldest backup
```

## Encryption

- AES-256-GCM
- Key protected using Windows DPAPI

## Automatic Backup Failure

Maximum:

```text
5 attempts
```

If all fail:

```text
Audit/System Log
+
Super Admin notification
+
System remains operational
+
Manual backup required
```

## Restore

Restore is a full snapshot:

```text
Data
Settings
Users
Audit
Transactions
etc.
```

Before restore/migration:

```text
Create safety backup
```

## Version Rules

```text
Older backup → newer application
        ↓
Supported through migration
```

```text
Newer backup → older application
        ↓
Rejected
```

---

# Phase 14 — Security + Integrity Hardening

## Goal

Final hardening before release.

## Security

- Password hashing
- Authorization
- User deactivation
- User reactivation
- Password reset
- National ID validation
- Audit security events
- Backup encryption
- DPAPI
- Sensitive data handling

## Financial Integrity

Verify:

- Transaction immutability
- Payment correction exception
- Refund adjustment
- Payroll adjustment
- Credit usage
- Coach dues consumption
- Revenue reconciliation
- No double payment
- No duplicate financial transactions

## Database Integrity

Verify:

- Foreign key constraints
- Unique constraints
- Check constraints
- Concurrency handling
- SQLite busy timeout/retry
- Transaction boundaries

---

# Phase 15 — Full Integration Testing + UAT + Release

## Testing Layers

### Domain Tests

Business rules and calculations.

### Application Tests

Use cases and workflows.

### Infrastructure Tests

Real SQLite database.

### UI Smoke Tests

Critical user workflows.

## Critical End-to-End Tests

### Training Flow

```text
Create Swimmer
    ↓
Create Subscription
    ↓
Payment
    ↓
Generate Sessions
    ↓
Attendance
    ↓
Payroll
    ↓
Payment
    ↓
Revenue
    ↓
Report
```

### Package Flow

```text
Package
→ Attendance
→ Session Consumption
→ Expiry
→ Renewal
```

### Private Flow

```text
Private
→ Revenue Split
→ Coach Due
→ Cancellation
→ Refund
→ Payroll
```

### Backup Flow

```text
Backup
→ Change Data
→ Restore
→ Verify Exact State
```

## Release Gate

Verify:

- All tests pass.
- No unresolved critical defects.
- Financial reconciliation passes.
- Backup/restore round trip passes.
- Authorization passes.
- Audit requirements pass.
- Localization works.
- RTL/LTR works.
- Final requirements traceability passes.
- Installer works.
- Fresh installation works.
- Upgrade/migration works.

---

# Universal Rules for Every Phase

Every Phase Prompt must include:

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

Do not use stale entities, fields, workflows, or terminology from superseded documents.

If you discover a genuine contradiction between finalized source-of-truth documents:

STOP.

Do not choose a side.
Do not invent a resolution.
Report the exact contradiction and the affected functionality.
```

---

# Universal Phase Completion Protocol

Every Phase Prompt must end with:

```text
PHASE COMPLETION PROTOCOL

Do NOT start the next phase.

When this phase is complete:

1. Run all relevant tests.
2. Verify the implementation against the finalized design documents.
3. Check for regressions.
4. Review database changes.
5. Review security/authorization implications.
6. Review audit requirements.
7. Report exactly what was implemented.
8. Report all files created or modified.
9. Report all tests executed and their results.
10. Report any deviation from this phase plan.
11. Report any unresolved issue.
12. Report any business-rule ambiguity discovered.

Then STOP and wait for my instruction.

Do not continue automatically into the next phase.
```

---

# Recommended Execution Workflow

For every phase, provide the coding agent with:

```text
1. Hybrid Final Implementation Plan
2. Current Phase Prompt
3. Relevant finalized design documents
4. Current repository
```

The agent must:

```text
Read sources
    ↓
Inspect current repository
    ↓
Implement ONLY current phase
    ↓
Run tests
    ↓
Verify against finalized documents
    ↓
Report
    ↓
STOP
```

The next phase begins only after review and explicit instruction.

---

# Implementation Sequence Summary

```text
Phase 0
Project Foundation + Stack Setup
        ↓
Phase 1
Database + EF Core + Migrations
        ↓
Phase 2
Core Infrastructure
        ↓
Phase 3
Financial Foundation
        ↓
Phase 4
Employees + Qualifications + Attendance Foundation
        ↓
Phase 5
Swimmers
        ↓
Phase 6
Training Core
        ↓
Phase 7
Training Operations
        ↓
Phase 8
Packages
        ↓
Phase 9
Private
        ↓
Phase 10
Recreational
        ↓
Phase 11
Payroll + Coach Dues
        ↓
Phase 12
Reports + Dashboard
        ↓
Phase 13
Backup / Restore / Migration
        ↓
Phase 14
Security + Integrity Hardening
        ↓
Phase 15
Full Integration Testing + UAT + Release
```

---

# Final Status

**Hybrid Final Implementation Plan v1.0 is the official implementation roadmap.**

Business requirements remain closed.

The four previously identified narrow business decisions remain open and must not be invented during implementation:

1. Training attendance late-edit override after the ±30-minute window.
2. Recreational duplicate check-in enforcement mechanics for anonymous/free-text tickets.
3. Package cancellation first-month boundary interpretation.
4. Private Lane Rental / Coach-Brought cancellation boundary interpretation.

These do not block starting the project globally. They only block the affected implementation details.

**No phase may silently resolve these decisions.**
