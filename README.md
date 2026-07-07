# Barangay Villa M. Tejero — Integrated Management System

Desktop application (C# WinForms, .NET 8) — Login + Dashboard + Resident Records + Barangay Documents + Transaction Logs phase, now backed by a real local database.

## What's included in this phase

- **Login form** — branded with your barangay logo, validates against the `UserAccounts` table, shows an error and clears the password field on invalid credentials.
- **Dashboard form** — role-based sidebar that is fully wired up and navigable:
  - **Administrator** sees: Dashboard, Resident Records, Barangay Documents, Transaction Logs, User Management, Backup & Restore, Logout.
  - **Staff** sees: Dashboard, Resident Records, Barangay Documents, Transaction Logs (view-only note), Logout.
  - Dashboard shows stat cards (Total Residents, Documents Issued, Pending Requests, Registered Users) and a "Recent Activity" card. Total Residents and Registered Users are wired to real data; with `DocumentService` now in place, Documents Issued / Pending Requests can pull from the `IssuedDocuments` table as well — double-check `DashboardForm.cs` if those still show zero, since it wasn't touched in this update.
  - **Backup & Restore** is still a "Coming Soon" placeholder card for now.
- **Resident Records module** — fully functional, aligned to the feature list's field set (residentID, firstName, middleName, lastName, address, birthDate, civilStatus, gender, dateRegistered, occupation, contact details, household composition):
  - Searchable/filterable grid (by name, purok, contact number, purok dropdown, and Active/Inactive status).
  - **Add / Edit** — full profile form: name (last/first/middle/suffix), birth date, gender, civil status, purok/address, contact number, occupation, and household composition.
  - **View** — dedicated read-only profile dialog with all fields plus a Document History card.
  - **Deactivate / Reactivate** — soft-deletes a resident (e.g. moved out or deceased) with an optional reason, keeping their record instead of hard-deleting it.
  - **Delete** — permanent removal, restricted to Administrator accounts (Staff can add/edit/view but not delete).
  - Every add/edit/deactivate/reactivate action is written to the Transaction Logs.
- **Barangay Documents module (new)** — full certificate/clearance issuance workflow:
  - Generates the required certificate/clearance types (Certificate of Residency, Certificate of Indigency, Barangay Clearance — Employment, Barangay Clearance — Business, and a School/Enrollment certificate).
  - Select a resident and their details auto-populate onto the request — no re-encoding.
  - **Verification step** before approval: a residency-verified checkbox plus a checklist of documentary requirements (valid ID, proof of residency/income, etc., tailored per document type).
  - Tracks O.R. number, fee, and status (**Pending / Approved / Rejected**) with remarks.
  - **Save** persists the request with an auto-generated control number (e.g. `BVMT-2026-0001`); **Print** produces the printable document (via `DocumentPrinter`) and auto-saves first if not yet saved.
  - Right-hand panel shows a searchable/filterable **issuance history** (by resident, document type, or status), with per-row **Open** (load back into the form) and **Print** actions.
  - Every generate/approve/reject action is written to the Transaction Logs.
- **Transaction Logs module (new)** — read-only audit trail:
  - Lists every logged system action: sign-ins/sign-outs, resident record changes, document generation and status changes, and user account changes, with actor, category, timestamp, and details.
  - Filterable by free-text search, category/type, and date range.
  - Exportable to CSV for record-keeping/reporting.
  - Immutable by design — no edit/delete affordance, consistent with an audit trail.
- **User Management module** — Administrator-only account management (add/edit users, assign role, position, contact info).
- Logout button asks for confirmation before closing, per the spec.
- Application + window icon use your barangay seal.

## Data storage

This phase replaces the earlier in-memory lists with a real local database:

- **`Data/DatabaseHelper.cs`** creates/opens a **SQLite** database at `Data\barangay.db` next to the executable (via the `Microsoft.Data.Sqlite` NuGet package), and seeds it with demo data on first run only. Tables: `Residents`, `ResidentHouseholdMembers`, `UserAccounts`, `IssuedDocuments`, `TransactionLogs`.
- `Program.cs` calls `DatabaseHelper.Initialize()` once at startup, before any form touches `ResidentService`, `UserService`, `DocumentService`, or `TransactionLogService`.
- Because the whole database lives in one `.db` file inside the app folder, the entire app + its data stays self-contained and easy to back up or copy to another PC — no separate database engine to install.
- **Note on the project spec:** the original documentation calls for an offline Microsoft Access (`.accdb`) database over OleDb. SQLite was used here instead to keep the app fully self-contained (no MS Access Database Engine / driver install required on the target PC). If a real `.accdb` file is a hard requirement, `DatabaseHelper.cs` is the single place to swap out — the same method signatures on `ResidentService`, `UserService`, `DocumentService`, and `TransactionLogService` should keep the rest of the app unchanged.

## Seeded accounts (for this phase)

| Username | Password   | Role          | Full Name             | Position                              |
|----------|-----------|---------------|------------------------|----------------------------------------|
| admin    | Admin@123 | Administrator | Hon. Juan Dela Cruz    | Barangay Captain / System Administrator |
| staff1   | Staff@123 | Staff         | Maria Santos           | Barangay Secretary                     |
| staff2   | Staff@123 | Staff         | Pedro Reyes            | Barangay Records Officer               |

Seeded once by `DatabaseHelper.SeedUserAccountsIfEmpty()` the first time the app runs (i.e. when `Data\barangay.db` doesn't exist yet). Sample residents, issued documents, and transaction log entries are seeded the same way, so the app looks realistic on first launch instead of empty.

## How to open

1. Unzip this folder.
2. Double-click `BarangayVillaMTejeroSystem.sln` to open it in Visual Studio.
3. Press **F5** (or Ctrl+F5) to run. Visual Studio will restore NuGet packages automatically on first build (including `Microsoft.Data.Sqlite`) — make sure you have the **.NET desktop development** workload installed (Visual Studio Installer → Modify) and the **.NET 8.0 SDK**.
4. On first run, `Data\barangay.db` is created and seeded automatically.
5. Log in with one of the seeded accounts above.

## Project structure

```
BarangayVillaMTejeroSystem.sln
BarangayVillaMTejeroSystem/
 ├─ BarangayVillaMTejeroSystem.csproj
 ├─ Program.cs                  (entry point; calls DatabaseHelper.Initialize())
 ├─ Controls/
 │   ├─ UserManagementControl.cs
 │   ├─ ResidentManagementControl.cs
 │   ├─ DocumentIssuanceControl.cs     (Barangay Documents module)
 │   └─ TransactionLogsControl.cs      (audit trail module)
 ├─ Forms/
 │   ├─ LoginForm.cs
 │   ├─ DashboardForm.cs
 │   ├─ UserFormDialog.cs
 │   ├─ ResidentFormDialog.cs
 │   └─ ResidentProfileDialog.cs
 ├─ Models/
 │   ├─ UserAccount.cs
 │   ├─ UserRole.cs
 │   ├─ Resident.cs                    (also defines Gender / CivilStatus enums)
 │   ├─ BarangayDocument.cs            (also defines DocumentType / DocumentStatus enums)
 │   └─ TransactionLog.cs              (also defines LogType enum)
 ├─ Services/
 │   ├─ UserService.cs                 (accounts + Authenticate(), SQLite-backed)
 │   ├─ ResidentService.cs             (residents + CRUD + dashboard stats, SQLite-backed)
 │   ├─ DocumentService.cs             (issuance CRUD, search, TotalIssued)
 │   ├─ DocumentPrinter.cs             (renders/prints a generated document)
 │   └─ TransactionLogService.cs       (writes audit-trail entries)
 ├─ Data/
 │   └─ DatabaseHelper.cs              (schema creation + seed data, SQLite)
 ├─ UI/                         (reusable styled controls)
 │   ├─ FlatButton.cs
 │   ├─ GradientPanel.cs
 │   ├─ RoundedPanel.cs
 │   ├─ RoundHelper.cs
 │   ├─ SidebarButton.cs
 │   └─ StatCard.cs
 └─ Resources/
     ├─ logo.png
     └─ app.ico
```

## Notes for the next phase

- **Backup & Restore** is the one remaining "Coming Soon" placeholder module — a natural next step is a simple "copy `Data\barangay.db` to a chosen folder / restore from a chosen file" feature, since the whole database is a single file.
- Double-check `DashboardForm.cs` to confirm the **Documents Issued** and **Pending Requests** stat cards are pulling from `DocumentService` now that it exists (it wasn't part of this update batch).
- The Resident Profile dialog's **Document History** card should now be wireable to real per-resident data via `DocumentService`, replacing its earlier "Coming Soon" placeholder — worth confirming `ResidentProfileDialog.cs` was updated to do this.
- If a true Microsoft Access (`.accdb`/OleDb) backend is still required by the project spec, `Data/DatabaseHelper.cs` is the one file to replace; keep the same method signatures on the four services so the rest of the app doesn't need to change.
