# 04 — UI/UX Specification (SYNCHRONIZED — v2, post Design Closure)
## Swimming Pool Management System

**Depends on:** `01-ERD.md` (v2), `02-Database-Schema.md` (v2), `05-Business-Logic.md` (v2). This document supersedes the original in full for every screen touched by the closure decisions; screens not mentioned below are unchanged from the original `04-UI-UX-Specification.md`.

---

## 1. Language — System-Wide, Not Per-User (finalized decision #36)

**This section replaces the original's per-user toggle design entirely.**

- Language (Arabic or English) is chosen **once**, during a new **Initial Program Setup** wizard that runs before the app is usable on a fresh install (alongside Club Name/Logo). There is **no per-user language preference anywhere** — every screen for every role renders in the one system-wide language.
- RTL layout applies system-wide when Arabic is selected: navigation mirrors, form flows reverse, table columns reverse.
- Number/date formatting follows the selected language; **currency values are never converted or reformatted in magnitude** — only their numeral glyphs and date conventions follow the locale.
- `DECISION REQUIRED (minor, technical, non-business)`: whether Super Admin can change the system language later via Configuration is not addressed by the finalized decision (which covers only the initial setup moment). Recommend allowing a later Super-Admin-only change, consistent with every other Configuration value — pending confirmation.
- Reports render in the same single system-wide language (finalized, explicit).

## 2. Initial Program Setup (new screen, not in the original spec)

A first-run wizard, shown once before login is available:
1. Club Name (EN/AR), Logo upload.
2. System Language selection (Arabic / English) — this becomes the permanent (pending the decision above) system-wide language.
3. Create the first Super Admin account.

## 3. Navigation by Role

Unchanged from the original matrix (Doc04 v1 §2) — the finalized decisions confirm, not alter, the fixed permission structure. One addition: **Configuration** gains a **Lane Management** sub-screen (finalized decision #35), and **Users & Roles** gains the reactivation/password-reset actions described below.

## 4. Screen Updates

### 4.1 Training Subscription Creation — Credit/Outstanding entry (finalized decision #1)

The form always shows a plain, non-editable **"Balance due: [Total − Paid]"** line — this is computed automatically and unconditionally, exactly as before; it is not part of the optional section below and is never absent.

Below it, after Paid Amount is entered, an optional, collapsed **"Credit or Outstanding note (optional)"** section offers two mutually exclusive sub-forms (selecting one disables the other) — this is a separate, additional annotation, not a substitute for the Balance-due line above:
- **Grant Credit:** Amount, Description (both required if this path is chosen). On submit, a Credit ledger entry is created for the swimmer, confirmed with: *"This Credit will be available for future Training/Package use."*
- **Declare Outstanding:** Amount, Description. On submit, this is stored as a formally-flagged, described debt that surfaces on the Outstanding Report/workflow — separately from, and in addition to, the always-visible Balance-due figure above (which continues to be what caps how large a Payment can be, per the overpayment rule).
- If neither section is used: standard behavior — Balance due still shows and still governs payments; there is simply no formal Outstanding annotation on this record.

Identical section appears on **Package Creation** (extended for consistency, per Doc01 §1.13).

### 4.2 Package Creation & Details — Session Balance (finalized decision #5)

- Package Details now shows: **"Sessions: 6 of 16 used"** (a progress indicator, not just a duration/date range) alongside the existing calendar validity dates.
- Check-in attempts at 0 remaining sessions show: *"No sessions remaining on this package. Renew or contact the swimmer."* — the Package still shows as Active if its calendar `end_date` hasn't passed; this is a **quota exhaustion** message, not an expiry message, and the UI must visually distinguish the two ("Sessions: 0/16 remaining" vs. "Expired on [date]").
- Period Change screen now explicitly states: *"Your remaining N sessions carry over to the new period."*
- No Pause action exists anywhere in this module (unchanged, confirmed).

### 4.3 Recreational — Ticket-Based Single Entry (finalized decisions #10, #11)

**Replaces the original Single Entry tab entirely.** Fields: Visitor Name (free text — no Swimmer search/link of any kind), Member/Non-Member toggle, Recreational Period (auto-selected from current time), Amount (defaults to the configured full entry fee, **not editable downward** — partial payment is not offered as an option in this screen at all), Payment Description (method notes). Submitting creates a `RecreationalTicket`, not a Swimmer-linked check-in.

- Duplicate attempt (same visitor name, same Period, same day) shows: *"[Name] already checked in for this session today."* and blocks the second ticket (finalized decision #31).
- Reports (Recreational → Reports) now read: Ticket Count, Total Revenue, Member Ticket Count, Non-Member Ticket Count — grouped by date range, with no Swimmer-table join anywhere in this report.
- The **Package-holder QR check-in** path is a separate, unaffected tab — still Swimmer/Package-linked exactly as originally designed, since D10/D11 apply only to Single Entry.

### 4.4 Private Booking — Participants (finalized decision #9) & Lane Selection (finalized decisions #30, #35)

- Participant entry now offers a toggle per participant row: **"Existing Swimmer"** (searchable, as before) or **"Guest"** (inline Name + Phone fields, no Swimmer record created).
- Lane Rental now requires selecting a real **Lane** from a managed list (Configuration → Lanes), showing that Lane's configured capacity; adding a participant beyond capacity is blocked with: *"This lane's capacity ([N]) has been reached."*
- Club-Brought Cancellation confirmation now shows the **resolved, unambiguous** formula: *"Cancellation Fee (20% of 1,000 EGP paid) = 200 EGP → paid to Coach. Refund to customer = 800 EGP."* — the club/coach ordinary split percentages are never shown on a cancellation screen, only on the ordinary (non-cancelled) revenue breakdown, since the two formulas are now confirmed to be entirely separate (finalized decision #2).

### 4.5 Lane Management (new Configuration screen, finalized decision #35)

Super-Admin-only CRUD: Label, Capacity, Status (Active/Inactive). Attempting to deactivate a Lane with a currently-Active booking against it shows a blocking message rather than silently orphaning the booking.

### 4.6 Employees — Qualifications (finalized decision #6)

Employee Profile gains a **Qualifications** section: a list of held `Qualification`s (multi-select, "+Add Qualification"), with the system automatically indicating which one is currently "Highest" (by `rank_order`, used for payroll rate resolution) — this is computed/read-only, not separately settable per employee.

### 4.7 Users & Roles — Reactivation and Password Reset (finalized decisions #15, #16)

- Deactivated User rows in the Users list now show a **"Reactivate"** action (in addition to the prior deactivate-only state) — confirmation: *"This restores login access. Username and password are unchanged."*
- **Password Reset** — two distinct entry points:
  - From a User's own login screen ("Forgot password?" → self-reset, Administrator role only): prompts for Username + National ID; on mismatch, generic *"These details do not match our records."*
  - From the User Details screen (Owner/Super Admin resetting an Administrator): prompts the actor to re-enter the **target Administrator's** National ID before the reset proceeds; mismatch blocks the reset with the same generic message. Neither flow ever displays the resulting password on screen beyond the standard one-time reveal pattern already used at account creation.

### 4.8 Payroll — Adjustment (finalized decision #23) & Refund — Adjustment (finalized decision #24)

- A **Paid** Payroll row's screen no longer shows an Edit action — only **"Add Adjustment"** (Amount [+/−], Reason), clearly separated visually from the original locked figure: *"Original: 4,500 EGP (Paid on [date]). Adjustments: +200 EGP ([reason], [date]). Net Paid: 4,700 EGP."*
- Confirmed Refunds gain the identical pattern: **"Add Correction"** on a locked Refund, same before/after net display.

### 4.9 Payment Correction (finalized decision #22)

Payment history rows gain an **"Correct Amount"** action (Administrator+): new amount + reason, with a confirmation showing the resulting change to Outstanding: *"Correcting this payment from 500 to 600 EGP reduces Outstanding by 100 EGP."* Every correction is visibly flagged in the Payment history list ("Corrected on [date]") rather than silently overwriting the displayed figure with no trace.

### 4.10 Training Attendance Window (finalized decision #29)

The Attendance screen now shows a live countdown/window indicator per session: *"Attendance open: 4:30 PM – 5:30 PM"* for a 5:00 PM session. Outside that window, the Present/Absent controls are disabled with: *"Attendance window has closed for this session."* **Whether a separate Administrator override/late-edit path exists for swimmer attendance after this window closes — mirroring the employee late-edit pattern — is genuinely unresolved** (`12-Edge-Cases-and-Decisions.md` §3.2, item C5; not addressed by any closure decision). Pending that confirmation, no override control is shown in this version of the screen; the window closing is currently a hard stop.

## 5. Unchanged Screens

Dashboard, Swimmers, Training Periods/Subscriptions (core flows), Payroll (core calculation display), Reports (list unchanged — see Doc11 §8 for the corrected Revenue/Profit computation these reports must now visibly reflect), Configuration (core pricing/rate screens), Audit Log, Backup & Restore (see Doc10 v2 for the encryption/retry messaging additions) are unchanged from the original `04-UI-UX-Specification.md` except as cross-referenced above.
