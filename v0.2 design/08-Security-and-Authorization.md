# 08 — Security & Authorization Design (SYNCHRONIZED — supersedes all prior versions of this document)
## Swimming Pool Management System

**Depends on:** `02-Database-Schema.md`, `03-System-Architecture.md` (both synchronized).

---

## 1. Authentication

### 1.1 Credentials (§4.1)
- **Username:** initial value = Employee Name. **Collision handling is now resolved (Decision 13):** a required Nickname is appended, forming `"{name} {nickname}"` (e.g., `"Ahmed Mohamed"` + nickname `"Hamo"` → `"Ahmed Mohamed Hamo"`). Nickname never alters the Employee's official `name` field.
- **Initial password:** = National ID. This value is used **once**, at account-creation time, to seed a hash — it is never stored in retrievable form and never re-displayed after the creation confirmation screen (Doc 04 §3.13).
- **Password change:** optional — the system must not force a password change on first login, but the UI should still *offer* it prominently at first login as a security nudge (a UX recommendation, not a requirement).

### 1.2 Password Storage
- Passwords are **never** stored in plaintext or reversibly encrypted. Use a modern salted hash (bcrypt or argon2, per Doc 03 §5).
- **Password reset is now fully specified (Decision 16), replacing the earlier open question:**
  - **Owner/Super Admin resets an Administrator's password:** requires entering the target Administrator's National ID, validated against their Employee record; mismatch rejects the reset. Both success and failure are audited.
  - **Administrator self-reset:** requires own Username + own National ID, both matched against their record.
  - **Resulting value (technical decision, not reopened as a business question):** the reset reseeds the password hash to the employee's National ID — identical to the original account-creation convention. Passwords remain never-displayed regardless of outcome.

### 1.3 Session Management
- Since this is a local desktop app with no network exposure, a session is effectively "logged in until explicit logout or app close." Idle-timeout auto-logout for shared front-desk terminals remains a genuinely open, low-risk technical decision (not addressed by any closure decision) — a good-practice recommendation, not a hard requirement.

---

## 2. Authorization (Role-Based Access Control)

### 2.1 Enforcement Point
The **Authorization Guard** in the Application layer (Doc 03 §8) is the single source of truth for permission checks, driven directly by the fixed matrix in SRS §3. It is invoked at the start of every use case, before any repository read/write, with signature roughly: `authorize(current_user, action, [optional resource context])`.

### 2.2 Matrix-to-Guard Mapping
Every module/action pair in §3's matrix becomes one authorization rule. Representative examples (full mapping is the literal transcription of §3, not reproduced narrative-style here to avoid duplicating Doc 04 §2's table):

| Action | Allowed roles |
|---|---|
| `CREATE_SWIMMER`, `EDIT_SWIMMER`, `DELETE_SWIMMER` | SUPER_ADMIN, ADMINISTRATOR |
| `VIEW_SWIMMER` | SUPER_ADMIN, ADMINISTRATOR (Owner: no access at all, §3) |
| `CREATE_TRAINING_PERIOD` | SUPER_ADMIN only |
| `EDIT_TRAINING_PERIOD`, `ASSIGN_COACH`, `ASSIGN_LIFEGUARD` | SUPER_ADMIN, ADMINISTRATOR |
| `VIEW_EMPLOYEE` | SUPER_ADMIN, OWNER (view only), ADMINISTRATOR |
| `EDIT_EMPLOYEE` | SUPER_ADMIN, ADMINISTRATOR (not Owner — view only per §3) |
| `RECORD_PAYMENT`, `RECORD_EXPENSE`, `MARK_PAYROLL_PAID` | SUPER_ADMIN, OWNER, ADMINISTRATOR |
| `CORRECT_PAYMENT` | SUPER_ADMIN, ADMINISTRATOR (Decision 22 — new) |
| `ADJUST_PAYROLL`, `ADJUST_REFUND` | SUPER_ADMIN, OWNER, ADMINISTRATOR (Decisions 23 & 24 — new; mirrors the mark-paid/confirm-refund authority) |
| `RESET_PASSWORD` (Owner/Super Admin → Administrator) | SUPER_ADMIN, OWNER (Decision 16 — new) |
| `REACTIVATE_USER` | Same authority as deactivation: SUPER_ADMIN (any), OWNER (Administrator-role only) (Decision 15 — new) |
| `MANAGE_LANES` | SUPER_ADMIN only (Decision 35 — new) |
| `MANAGE_QUALIFICATIONS_AND_RATES` | SUPER_ADMIN only (Decision 6 — new, replaces the earlier per-employee rate ambiguity) |
| `EDIT_PRICING_RATES` | SUPER_ADMIN only |
| `EDIT_CANCELLATION_FEE` | SUPER_ADMIN, OWNER |
| `EDIT_GENERAL_CONFIGURATION` | SUPER_ADMIN only |
| `MANAGE_USERS` | SUPER_ADMIN (any role), OWNER (Administrator-role users only) |
| `BACKUP_RESTORE` | SUPER_ADMIN only |
| `VIEW_AUDIT_LOG` | SUPER_ADMIN, OWNER |

### 2.3 Resource-Level Nuance
Some permissions are **role + context** rather than pure role: e.g., Owner's "Users & Roles" access is restricted not just by module but by **which role of user** they're managing (§4.2 — Owner can manage Administrator users only, never Owner/Super Admin accounts). The guard must accept the *target role* as context for `MANAGE_USERS` actions, not just check "is this an Owner."

### 2.4 Failure Behavior
- An unauthorized attempt is rejected before any data is touched, with a generic "You do not have permission" message to the user (avoid leaking *why* in detail, to reduce information disclosure to a potentially malicious actor probing the UI).
- Every unauthorized attempt is written to the Audit Log (§25.3 "Unauthorized access attempts... are also logged") with `success = FALSE` and the attempted action/entity recorded — see Doc 09.

---

## 3. Sensitive Data Handling

### 3.1 National ID
- Stored once on the `employees` table, now also used as the identity-verification input for Password Reset (Decision 16, §1.2 above).
- **Never** displayed in full in list views; masked by default in Employee Profile (e.g., `•••••1234`).
- Whether a Super-Admin "unmask" action is needed at all remains an open, low-priority UX question (not addressed by any closure decision) — recommend not exposing one, since National ID's only functional uses (initial password seed, password-reset verification) are both backend-only operations that never require displaying the value on screen.

### 3.2 Financial Data
- Visible per the §3 matrix (Payments/Expenses/Payroll: all three roles; Pricing/Rates: Super Admin only). An Administrator can *operate* payments but must not see the underlying rate/pricing configuration values beyond what's needed to see the computed subscription price (i.e., they see "Total: 1200 EGP" on a subscription, but cannot navigate to Configuration → Pricing to see the master rate table).

### 3.3 Audit Log Content
- Audit entries may contain sensitive before/after values (e.g., a price change, a Member/Non-Member flip). Since Audit Log itself is restricted to Super Admin/Owner (§25.2), this is an acceptable data flow — Administrators never see Audit Log contents regardless of what's in them.

---

## 4. Backup Protection

- **Backup encryption is now mandatory and confirmed (Decision 25) — resolves the earlier open question.** Every backup, automatic and manual, on every destination type (local, USB, external drive, network folder), is always encrypted before being written. Backups are never restorable/readable outside the system's own controlled Restore mechanism.
- Concrete key-management approach (passphrase-derived, OS keychain, or key file) is a technical implementation choice — see `10-Backup-Restore-and-Migration.md` §5 for the adopted approach; it does not change any business-visible behavior.
- Restore remains Super-Admin-only — no other role can even view the Restore screen.

---

## 5. Unauthorized / Failed Operation Handling

- Every use case in the Application layer distinguishes three outcomes: (a) success, (b) ordinary business-rule rejection (e.g., capacity full), (c) authorization/security or financially-consequential rejection. The closure decisions establish that several specific business-validation failures — Overpayment (Decision 21), Recreational duplicate check-in (Decision 31), and failed Password Reset attempts (Decision 16) — are explicitly audited even though they are not authorization failures. This narrows (without fully closing) the original open question about audit scope: **audit financial and identity-security-adjacent validation failures explicitly; routine capacity/scheduling rejections remain UI-only unless similarly named.**
- Failed **login attempts** are explicitly named in §25.3 ("Failed Login") and must be logged with the attempted username (not the password) and timestamp, regardless of whether the username exists in the system (to avoid both under-logging attacks and leaking which usernames are valid via error-message differences — use a generic "Invalid username or password" message for both "user doesn't exist" and "wrong password" cases).

---

## 6. Summary Table: Security Controls vs. Requirement

| Control | Basis | Status |
|---|---|---|
| Hashed password storage | Standard security baseline | GREEN |
| Role-based access control matching the fixed matrix exactly | Decision 27 (roles/matrix permanently fixed) | GREEN |
| Deactivation instead of deletion (Users, Employees, Swimmers) | Confirmed throughout | GREEN |
| National ID masking | Explicit instruction | GREEN |
| Audit logging of unauthorized attempts | Confirmed | GREEN |
| Password reset flow (forgotten password) | Decision 16 | **GREEN — fully resolved** |
| User reactivation | Decision 15 | **GREEN — fully resolved** |
| Backup encryption at rest | Decision 25 | **GREEN — fully resolved** |
| Session idle timeout | Not addressed by any closure decision | YELLOW — technical decision, low risk |
| Key-management mechanism for backup encryption | Not addressed by any closure decision (the *requirement* to encrypt is resolved; the *mechanism* is not) | YELLOW — technical decision, see Doc10 §5 |
| National ID unmask control necessity | Not addressed by any closure decision | YELLOW — low-priority UX question |
