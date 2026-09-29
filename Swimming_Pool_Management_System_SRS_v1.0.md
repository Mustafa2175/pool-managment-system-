# Swimming Pool Management System
## Software Requirements Specification (SRS) — v1.0

**Status:** Final Business Requirements Baseline  
**Deployment:** Local / Offline  
**Scope:** One club per system instance

---

## 1. System Overview

The Swimming Pool Management System is a desktop/local application for managing the operations, subscriptions, attendance, employees, payroll, finance, reporting, configuration, backup, and audit history of one swimming club.

### General Rules

- The application works fully offline.
- No internet or cloud dependency is required.
- Each installed system instance belongs to one club only.
- The same software can be deployed to different clubs using separate databases/system instances.
- Super Admin can configure club branding and basic club information.

---

## 2. User Roles

The system has three roles:

### 2.1 Super Admin

Full system access, including system configuration, users, backup/restore, audit log, financial operations, operational modules, and reporting.

### 2.2 Owner

Administrative and financial access. The Owner does not perform normal daily operational tasks.

### 2.3 Administrator

Responsible for daily operational activities such as swimmers, subscriptions, attendance, employees, payments, expenses, and operational management.

---

## 3. Permission Matrix

| Function | Super Admin | Owner | Administrator |
|---|:---:|:---:|:---:|
| Dashboard | Yes | Yes | Yes |
| Reports | Yes | Yes | Yes |
| Swimmers | Yes | No | Yes |
| Training | Yes | No | Yes |
| Packages | Yes | No | Yes |
| Private | Yes | No | Yes |
| Recreational | Yes | No | Yes |
| Attendance | Yes | No | Yes |
| Employees | Yes | View | Yes |
| Payments | Yes | Yes | Yes |
| Expenses | Yes | Yes | Yes |
| Payroll | Yes | Yes | Yes |
| Pricing / Rates | Yes | No | No |
| Cancellation Fee | Yes | Yes | No |
| General Configuration | Yes | No | No |
| Users & Roles | Yes | Administrator Users Only | No |
| Backup / Restore | Yes | No | No |
| Audit Log | Yes | Yes | No |

---

# 4. Authentication and User Management

## 4.1 User Accounts

- An Administrator User must be linked to an Employee of type Administrator.
- Coaches and Lifeguards may exist as Employees without user accounts.
- Initial username = Employee Name.
- Initial password = National ID.
- Password change is optional.
- National ID is stored as employee information.

## 4.2 User Management Rules

### Super Admin

- Can create, edit, deactivate, and manage all users.
- Can manage roles.

### Owner

- Can create and manage Administrator Users only.
- Cannot create Owner or Super Admin accounts.

### Administrator

- Cannot create or manage users.

## 4.3 User Deactivation

Users with history should be deactivated rather than hard-deleted.

Deactivated users:

- Cannot log in.
- Remain associated with historical operations.

---

# 5. Swimmers

## 5.1 Swimmer Data

Each swimmer contains:

- Swimmer ID
- Name
- Date of Birth
- Gender
- Parent Name
- Phone Number
- Member / Non-Member
- QR Code / Token
- Status

## 5.2 Swimmer ID

The system automatically generates a unique swimmer ID.

Example:

`SW-000125`

## 5.3 Global Search

Search supports:

- Swimmer Name
- Swimmer ID
- Phone Number
- Parent Name

Phone number is not unique. A parent may have multiple children.

## 5.4 Swimmer Status

A swimmer is **Active** when at least one active subscription exists.

When all subscriptions become inactive/completed/cancelled, the swimmer becomes **Inactive**.

If the swimmer subscribes again, the swimmer becomes **Active**.

Inactive swimmers remain available for:

- History
- Reports
- Search
- Renewal/new subscription

## 5.5 Member / Non-Member

Member / Non-Member is a swimmer attribute.

Administrators can change it.

The current value affects pricing of new Training and Package subscriptions only.

Existing subscriptions keep their historical price.

Private pricing ignores Member / Non-Member status.

## 5.6 Swimmer Deletion

- A swimmer with an Active Subscription cannot be deleted.
- A swimmer without an Active Subscription can be deleted.
- Deletion is implemented as Soft Delete.
- Historical records remain preserved.

## 5.7 Swimmer Profile

### Overview

- Basic information
- Status
- Member / Non-Member
- QR

### Subscriptions

- Current subscriptions
- Previous subscriptions

### Payments

- Payments
- Outstanding
- Financial history

### Attendance

- Attendance history

### Activity

- Important swimmer-related activities

---

# 6. Programs and Periods

## 6.1 Programs

Training programs include:

- Regular
- Star
- Team

Recreational / Free periods are also supported.

## 6.2 Training Period

Each Training Period belongs to exactly one Program.

A Period contains:

- Program
- Day(s)
- Start Time
- End Time
- Capacity
- Assigned Coaches
- Assigned Lifeguards
- Active / Inactive status

## 6.3 Period Creation

Super Admin creates Training and Recreational Periods.

Super Admin defines:

- Days
- Start Time
- End Time
- Capacity
- Other period configuration

Administrator can:

- Edit Periods
- Assign Coaches
- Assign Lifeguards
- Operate the Period

Owner can view Periods only.

## 6.4 Period Status

### Active

- Available for new subscriptions.
- Available for normal operations.

### Inactive

- Not available for new subscriptions.
- Historical subscriptions remain linked to it.

## 6.5 Period Schedule Changes

If a Period's day/time changes while subscriptions exist:

- Past sessions remain unchanged.
- Future sessions follow the new schedule.
- The change is recorded in Audit Log.

---

# 7. Training Subscriptions

## 7.1 Creation

Administrator selects:

- Swimmer
- Program
- One Period
- Start Date
- Paid Amount

The system calculates/stores:

- Total Price
- Configured Session Count
- Outstanding
- Sessions
- End Date
- Subscription state

## 7.2 Training Pricing

Training price is determined by:

`Program + Member/Non-Member`

Super Admin controls the prices.

Administrator cannot override configured pricing when creating a subscription.

## 7.3 Start Date

Training Start Date:

- Must be today or a future date.
- Cannot be in the past.
- Must match one of the selected Period's days.

## 7.4 Capacity

A new Training Subscription cannot be created when the selected Period has no available capacity.

## 7.5 Schedule Conflicts

An active subscription cannot overlap another active subscription.

This applies to:

- Training + Training
- Training + Package
- Package + Package
- Training + Private
- Package + Private
- Other conflicting active subscriptions/bookings

Multiple active subscriptions are allowed when their schedules do not overlap.

## 7.6 Session Generation

Sessions are generated automatically using:

- Period schedule
- Start Date
- Globally configured Training Session Count

Session count is configurable and is not hardcoded.

## 7.7 Training Attendance

Administrator can record swimmer attendance manually.

QR flow:

1. Open the Period.
2. Display swimmers/subscriptions.
3. Scan swimmer QR.
4. Identify the swimmer/subscription/session.
5. Validate the active subscription and correct Period/session.
6. Mark attendance.

QR is an identification mechanism and does not independently grant access.

## 7.8 Training Pause

Pause:

- Does not consume sessions.
- Keeps future sessions stored as paused/inactive.
- On Resume, remaining sessions are scheduled from the first available occurrence of the same Period after Resume.
- Administrator may manually adjust generated dates.
- Subscription end date is extended by the exact pause duration.

## 7.9 Training Renewal

If the old subscription has remaining sessions:

`New Start = after the last session of the old subscription`

If the old subscription is fully completed:

`New Start = first available Period occurrence from the renewal date`

The new subscription is independent and receives its own sessions, price, and history.

## 7.10 Training Cancellation

### Before First Session

- Full refund.
- No credit.
- No cancellation fee.

### After First Session

- No refund.
- No credit.
- Future sessions are cancelled.
- Paid amount remains club revenue.
- Subscription becomes Cancelled.
- Financial transactions and Audit Log records are created automatically.

---

# 8. Coach and Lifeguard Assignment

## 8.1 Base Assignment

- Coaches are assigned to Periods.
- Lifeguards are assigned to Periods.
- Multiple coaches can be assigned to one Period.
- Multiple lifeguards can be assigned to one Period.
- Lifeguards are optional.
- An employee cannot be assigned to overlapping Periods.

## 8.2 Attendance vs Assignment

Assignment and attendance are separate concepts.

- Assignment = who is expected to attend.
- Attendance = who actually attended.

## 8.3 Replacement

A replacement is valid for one Training Session only.

If the assigned employee is absent:

- Assigned employee earns 0 for that Session.
- If a replacement attends, the replacement earns the configured rate for that Session.
- Replacement does not change the base Period assignment.

---

# 9. Employee Attendance

## 9.1 Coach / Lifeguard

Attendance can be recorded as applicable, including:

- Present
- Absent
- Excused

Administrator can enter employee attendance until one hour after the Period ends.

Example:

Period = 5:00 PM–6:00 PM  
Attendance entry deadline = 7:00 PM

After the deadline, changes are administrative edits and must be recorded in Audit Log.

## 9.2 Administrator Attendance

Administrator payroll is based on absence days.

The system records Administrator attendance/absence so payroll can calculate deductions.

---

# 10. Employees

## 10.1 Employee Types

- Administrator
- Coach
- Lifeguard

## 10.2 Employee Status

- Active
- Inactive

Employees are not hard-deleted when historical data exists.

---

# 11. Payroll

## 11.1 Coach / Lifeguard Payroll

Payroll is calculated from actual attendance.

- Present → configured Session Rate.
- Absent → 0.
- Replacement present → replacement's configured Rate.
- Original assigned employee remains at 0 if absent.

Rates are configured by Super Admin and may depend on qualification/certificate.

## 11.2 Administrator Payroll

Administrator has a fixed Monthly Salary.

Daily value:

`Monthly Salary ÷ Number of Days in Month`

Absence deduction:

`Absent Days × Daily Value`

Net salary:

`Monthly Salary − Absence Deduction`

## 11.3 Payroll Payment

Status:

- Not Paid
- Paid

Super Admin, Owner, and Administrator can mark payroll as Paid/Not Paid.

When payroll is marked Paid:

- Payment Date is recorded.
- Payroll Transaction is created automatically.

Salary/rate configuration cannot be changed by Administrator.

---

# 12. Packages

## 12.1 Package Types

There are two Package types:

### Training Package

- Regular Training only.
- No Star.
- No Team.
- One Training Period.

### Recreational Package

- One Recreational Period.

A Package cannot combine Training and Recreational.

## 12.2 Package Configuration

Package duration is globally configured.

Example:

`3 Months`

Packages are duration-based, not session-count based.

## 12.3 Package Pricing

### Training Package

- Member Price
- Non-Member Price

### Recreational Package

- Member Price
- Non-Member Price

Package pricing is fixed by package type and does not depend on the selected Period.

## 12.4 Package Creation

Administrator selects:

### Training Package

- Swimmer
- Package Type
- Regular Program
- Training Period
- Start Date
- Paid Amount

### Recreational Package

- Swimmer
- Package Type
- Recreational Period
- Start Date
- Paid Amount

System calculates:

- Duration
- End Date
- Total Price
- Outstanding
- QR

## 12.5 Package Validation

- Start Date must be today or future.
- Period must be Active.
- Capacity must be available.
- Schedule conflict is prohibited.
- Duplicate active package on the same Period is prohibited.

## 12.6 Package Renewal

If the old Package is Active:

`New Start = after Old End Date`

If the old Package is Expired:

- Administrator chooses a new Start Date.
- Start Date must be today or future.

Renewal may switch:

- Training → Recreational
- Recreational → Training

A new Period can also be selected.

The old Package remains in history.

## 12.7 Package Period Change

Allowed while the Package is Active.

Rules:

- Old Period applies until the day before the change.
- New Period starts on the Change Date.
- Package Start/End Dates remain unchanged.
- New Period must be Active.
- New Period must have capacity.
- Schedule conflict is prohibited.
- Change is recorded in Audit Log.

## 12.8 Package Pause

Package Pause is not allowed.

Package duration continues regardless of usage.

No extension is granted for non-use.

## 12.9 Package QR

- A Package receives a QR.
- Creating a new QR invalidates the old QR.
- Expired Package QR is invalid.
- Cancelled Package QR is invalid.
- Valid check-in requires an Active, non-expired Package and correct Period.

## 12.10 Package Cancellation

### Before Package Starts

- Full refund.
- No credit.

### During First Month

Do not divide Package price by months.

Use the configured normal Training price for the same Regular Program.

`Refund = Paid Amount − Normal Training Price`

No credit is generated.

The applicable Training price is the historical price associated with the Package context at creation.

### After First Month

- No refund.
- No credit.
- Full paid amount remains with the club.

First month is based on elapsed time from Start Date.

Example:

Start = September 10  
First month = September 10 through October 9  
October 10 = after first month

---

# 13. Recreational

## 13.1 Recreational Period

A Recreational Period contains:

- Day(s)
- Start Time
- End Time
- Capacity
- Active / Inactive

Super Admin creates the Period.

Administrator can edit and operate it.

## 13.2 Recreational Single Entry

Normal Recreational Single Entry does not use QR.

Administrator:

1. Opens the Recreational Period.
2. Checks the person's club membership card manually.
3. Records Check-in.
4. Records the amount paid.
5. System creates a Transaction automatically.

No outstanding is created for a fully paid Single Entry.

## 13.3 Check-in Window

Check-in is allowed:

`30 minutes before Period start → 30 minutes after Period start`

Example:

Period starts at 5:00 PM.

Allowed window:

`4:30 PM → 5:30 PM`

No new check-in is allowed after the window closes.

## 13.4 Checkout

There is no checkout.

If someone leaves and returns, no second check-in is recorded.

## 13.5 Capacity

Capacity is based on actual check-ins.

Package holders have priority, but their place is not permanently reserved.

If a Package holder does not arrive during the allowed check-in time:

- Their place becomes available.

If capacity is full:

- No additional person can enter.
- A place does not become available when someone leaves because the system has no checkout tracking.

## 13.6 Recreational Package Check-in

Recreational Package holders use QR.

The system validates:

- QR
- Active Package
- Correct Recreational Period
- Expiration
- Capacity

Then it records the check-in.

---

# 14. Private Management

Private has three business types:

1. Lane Rental
2. Coach-Brought Private
3. Club-Brought Private

Private ignores Member / Non-Member status.

## 14.1 Lane Rental

The club rents a lane for a configured private duration.

Example:

`Club Fee = 1,500 EGP`

The club receives the configured lane fee regardless of whether one or the maximum allowed number of swimmers uses the lane.

The coach's price to swimmers is outside the club's financial calculation.

## 14.2 Coach-Brought Private

The coach brings the private.

Club revenue:

`Number of Swimmers × Fixed Fee Per Swimmer`

Example:

`10 swimmers × 500 EGP = 5,000 EGP`

The fee is for the complete configured Private duration, not per session.

The coach may charge swimmers any amount. The club does not calculate or control the coach's customer pricing.

## 14.3 Club-Brought Private

The club brings the customer.

Customer pays a total amount to the club.

System splits the amount according to configured percentages:

`Club % + Coach % = 100%`

The system calculates both shares automatically.

## 14.4 Private Coach

Each Private booking has exactly one coach.

No:

- Multiple coaches
- Replacement coach

## 14.5 Private Scheduling

Private cannot overlap another Private booking for the same coach.

Private also cannot overlap the same coach's Training Period assignment.

## 14.6 Private Sessions

Private session count is globally configured.

Sessions are generated from the Private Start Date and configured schedule.

## 14.7 Private Attendance

Private swimmer attendance is not entered through swimmer QR.

The coach reports attendance to the Administrator.

Administrator records attendance based on the coach's report.

## 14.8 Lane Rules

Lanes are independent resources.

Training Periods are not tied to lanes.

The same lane cannot be used by two overlapping Private bookings.

---

# 15. Private Cancellation

## 15.1 Before First Session

For Private:

- Full refund.
- No credit.
- No cancellation fee.

## 15.2 Lane Rental and Coach-Brought

If cancellation occurs after Private starts:

### Before Completing Two Sessions

- Deduct 50% of the applicable club value.
- Refund the remainder.

### After Completing Two Sessions

- Club keeps the full applicable club value.
- No customer refund.

## 15.3 Club-Brought

If cancelled after the first Session:

- Apply configured Cancellation Fee.
- Club receives its configured percentage.
- Coach receives the Cancellation Fee.
- Remaining amount is refunded to customer.
- No credit.

---

# 16. Cancellation Fee

Cancellation Fee supports:

- Percentage
- Fixed Amount

Super Admin and Owner can edit the Cancellation Fee.

Administrator cannot edit it.

Configuration changes apply to future cancellations only.

Cancellation rules themselves are fixed system logic.

---

# 17. Payments and Finance

## 17.1 Payment Principles

Payments are created as part of business operations.

Administrator enters the amount paid.

System calculates:

- Total
- Paid
- Outstanding

## 17.2 Partial Payments

Partial payments are supported for applicable Training and Package operations.

Example:

`Total = 2,000`  
`Paid = 1,200`  
`Outstanding = 800`

When the remaining 800 is paid:

`Outstanding = 0`

A new payment Transaction is automatically created.

## 17.3 Payment Description

There is no separate payment-method configuration.

Payment details/method can be written in Description.

Examples:

- Cash
- Visa
- Instapay
- Bank Transfer

## 17.4 Manual Transactions

Administrators cannot create arbitrary manual Transactions.

Transactions are generated automatically by business operations.

## 17.5 Transaction Sources

Possible sources include:

- Training Subscription Payment
- Package Payment
- Private Payment
- Recreational Single Entry
- Outstanding Payment
- Expense
- Payroll Payment
- Refund
- Credit Usage

---

# 18. Credit

Credit can be used only for:

- Training
- Package

Credit cannot be used for:

- Private
- Recreational Single Entry

Credit is not manually created as a standalone transaction.

It is generated by qualifying business operations and automatically consumed.

## 18.1 Credit Revenue

Credit is not counted as revenue when it is generated.

When Credit is used in a Training or Package transaction:

- Its used value is counted as Revenue on the usage date.
- It is not counted twice.

---

# 19. Refunds

Refund amount is calculated automatically from the applicable cancellation rules.

Administrator cannot manually change the refund amount.

Administrator confirms the calculated refund.

After confirmation:

- Refund Transaction is created automatically.
- Audit Log is updated.
- Financial history is preserved.

---

# 20. Expenses

Administrator can create an Expense with:

- Amount
- Description

The system:

- Creates the Expense.
- Creates the related Transaction automatically.
- Includes it in financial reporting.

---

# 21. Reports

Reports support:

- Daily
- Monthly
- Yearly
- Custom From Date → To Date

Available reports:

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

## 21.1 Revenue

Revenue is based on financial Transactions.

Includes applicable:

- Training
- Packages
- Private
- Recreational Single Entry
- Used Credit

Refunds reduce Revenue.

## 21.2 Expenses

Includes recorded Expenses and Payroll payments.

## 21.3 Profit

`Profit = Revenue − Expenses`

## 21.4 Outstanding

Shows amounts currently still due.

Outstanding is calculated automatically.

## 21.5 Payroll Report

Includes:

- Employee
- Calculated payroll
- Paid / Not Paid
- Payment information

## 21.6 Attendance Report

Includes applicable:

- Swimmer Training Attendance
- Coach Attendance
- Lifeguard Attendance
- Recreational Check-ins

## 21.7 Cancellation Report

Includes:

- Cancelled operation
- Type
- Date
- Paid Amount
- Refund
- Cancellation Fee
- Related financial information

## 21.8 Swimmer Report

Includes useful swimmer statistics such as:

- Total swimmers
- Active / Inactive
- Member / Non-Member

---

# 22. Configuration

Super Admin controls system configuration.

## 22.1 Club Information

- Club Name
- Logo
- Basic Club Information

## 22.2 Training Configuration

- Training Session Count

## 22.3 Private Configuration

- Private Session Count
- Lane Rental Fee
- Coach-Brought Fixed Fee Per Swimmer
- Club-Brought Club Percentage
- Club-Brought Coach Percentage

## 22.4 Package Configuration

- Package Duration
- Training Package Member Price
- Training Package Non-Member Price
- Recreational Package Member Price
- Recreational Package Non-Member Price

## 22.5 Schedule Configuration

- Training Days
- Training Start/End Times
- Recreational Days
- Recreational Start/End Times

## 22.6 Training Pricing

Prices are configured by:

`Program + Member/Non-Member`

Programs include:

- Regular
- Star
- Team

## 22.7 Employee Rates

- Coach rates
- Lifeguard rates
- Qualification/Certificate-based rates
- Administrator Monthly Salary

## 22.8 Cancellation Configuration

- Cancellation Fee Type
- Cancellation Fee Value

---

# 23. Configuration Rules

Historical transactions/subscriptions retain their historical financial values.

Changing a current price does not modify old subscriptions.

Club-Brought Private percentage validation:

`Club % + Coach % = 100%`

The system must reject invalid configurations where the total is not exactly 100%.

---

# 24. Dashboard

All roles use the same general Dashboard design.

Visible data and actions depend on the logged-in user's permissions.

Examples:

- Administrator sees operational information.
- Owner sees management/financial information.
- Super Admin sees all permitted system information.

---

# 25. Audit Log

## 25.1 Purpose

Audit Log records important system activity and security events.

It records both successful and unsuccessful actions.

## 25.2 Visibility

- Super Admin: Yes
- Owner: Yes
- Administrator: No

## 25.3 Events to Record

### Authentication

- Login
- Logout
- Failed Login
- Other important authentication events

### Users / Employees

- User creation
- User modification
- User deactivation
- Permission/role changes
- Employee changes

### Swimmers

- Creation
- Modification
- Soft Delete
- Member / Non-Member changes

### Subscriptions

- Creation
- Renewal
- Pause
- Resume
- Cancellation
- Period change
- Important modifications

### Attendance

- Attendance changes
- Late administrative attendance edits

### Finance

- Payments
- Refunds
- Credit generation/use
- Expenses
- Payroll payments
- Cancellation financial operations

### Configuration

- Pricing changes
- Rate changes
- Session/duration changes
- Schedule changes
- Cancellation Fee changes
- Club information changes

### System

- Backup
- Restore
- User/Role operations
- Important system configuration

### Unauthorized / Failed Actions

Unauthorized access attempts and failed important operations are also logged.

## 25.4 Audit Entry

Each entry contains:

- User or SYSTEM
- Action
- Success / Failed
- Date and Time
- Entity
- Details
- Reason when applicable

Automatic system operations use:

`SYSTEM`

## 25.5 Audit Security

- Audit records cannot be edited manually.
- Audit records cannot be deleted manually.

---

# 26. Backup and Restore

The application is designed for local/offline operation.

## 26.1 Automatic Backup

- Runs once per week.
- Stored locally.
- No cloud dependency.

## 26.2 Manual Backup

Super Admin can create a backup at any time.

Backup location may be:

- Local folder
- USB
- External drive
- Network folder

## 26.3 Backup Retention

Maximum number of backups:

`7 total backups`

Automatic and manual backups share the same limit.

If an eighth backup is created:

- The oldest backup is automatically deleted.

## 26.4 Backup Metadata

Each backup stores:

- Date
- Time
- System Version

---

# 27. Restore

Restore is available to Super Admin only.

Restore returns the system to a complete snapshot state.

It includes:

- Database
- Users
- Settings
- Configuration
- Transactions
- Attendance
- Payroll
- Audit Log
- Swimmers
- Employees
- Subscriptions
- Other system state

---

# 28. Version Migration

Older backups can be restored into a newer compatible system version through database migrations.

Example:

`Backup v1.0 → Migration → Current v1.3`

Migration transforms the old database schema into the current schema while preserving existing data.

## 28.1 Newer Backup Than Current Software

Example:

`Backup v1.3 → Current v1.1`

The system must not restore the newer backup into the older application version.

The application must be upgraded first.

## 28.2 Safe Restore

Before migration/restore, the system should create a backup of the current state.

If migration or restore fails, the current state can be recovered.

---

# 29. Global Business Rules

## 29.1 No Schedule Overlap

Active subscriptions and bookings must not create conflicting schedules for the same swimmer/coach/resource as applicable.

## 29.2 No Past Start Dates

Training, Package, and applicable Private Start Dates cannot be created in the past.

## 29.3 Historical Financial Values

Historical subscriptions and transactions retain the price/rate that applied to them.

Current configuration changes do not retroactively modify historical financial data.

## 29.4 Automatic Financial Effects

Business operations drive their financial consequences automatically.

Examples:

`Create Subscription → Calculate Price → Record Payment → Calculate Outstanding → Create Transaction`

`Cancellation → Calculate Refund → Admin Confirmation → Refund Transaction → Audit Log`

`Payroll → Calculate → Mark Paid → Payroll Transaction → Audit Log`

`Recreational Single Entry → Check-in → Payment → Transaction`

---

# 30. System Modules

The final system consists of:

1. Dashboard
2. Swimmers
3. Training
4. Packages
5. Private
6. Recreational
7. Attendance
8. Employees
9. Payments
10. Expenses
11. Payroll
12. Reports
13. Configuration
14. Users & Roles
15. Backup & Restore
16. Audit Log

---

# 31. Recommended Screen Structure

## Dashboard

- Main Dashboard

## Swimmers

- Swimmers List
- Add Swimmer
- Swimmer Profile
- Edit Swimmer

## Training

- Training Periods
- Period Details
- Training Subscriptions
- Add Subscription
- Subscription Details
- Sessions / Attendance

## Packages

- Packages
- Add Package
- Package Details
- Package Period Change
- Package Cancellation
- Package Renewal

## Private

- Private Management
- Add Private
- Private Details
- Private Sessions
- Private Cancellation

## Recreational

- Recreational Periods
- Period Details
- Recreational Check-in
- Recreational Packages

## Attendance

- Training Attendance
- Coach Attendance
- Lifeguard Attendance
- Administrator Attendance
- Recreational Check-in

## Employees

- Employees List
- Add Employee
- Employee Profile
- Edit Employee
- Employee Attendance

## Finance

- Payments
- Outstanding
- Expenses
- Transactions

## Payroll

- Payroll
- Payroll Details
- Payroll Payment Status

## Reports

- Reports Dashboard
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

## Configuration

- Club Information
- Training Configuration
- Private Configuration
- Package Configuration
- Schedule Configuration
- Pricing
- Coach/Lifeguard Rates
- Cancellation Configuration
- System Settings

## Users & Roles

- Users
- Add User
- User Details
- Roles / Permissions

## Backup & Restore

- Backup
- Backup History
- Restore

## Audit Log

- Audit Log

---

# 32. Subscription Lifecycle Summary

## Training

`NEW → ACTIVE → PAUSED → RESUMED → ACTIVE → COMPLETED → RENEWED`

Alternative terminal state:

`ACTIVE → CANCELLED`

## Package

`NEW → ACTIVE → EXPIRED`

Alternative terminal state:

`ACTIVE → CANCELLED`

## Private

`NEW → ACTIVE → COMPLETED`

Alternative terminal state:

`ACTIVE → CANCELLED`

---

# 33. Final Business Requirements Status

The Business Requirements are considered **closed for implementation**.

Implementation should not invent new business rules without explicitly resolving the new case first.

Recommended next engineering stages:

1. Requirements Baseline
2. ERD / Domain Model
3. Database Schema
4. System Architecture
5. UI/UX Design
6. Services / Business Logic
7. Implementation
8. Testing
9. Deployment
10. Backup/Restore Validation
