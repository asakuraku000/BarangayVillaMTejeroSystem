# Barangay Villa M. Tejero — Integrated Management System

<<<<<<< HEAD
Desktop application (C# WinForms, .NET 8) for barangay records, document issuance, and audit logging. Backed by a local SQLite database, so the whole app folder is self-contained and portable — no server or internet connection required.
=======
Desktop application (C# WinForms, .NET 8) — Login + Dashboard + Resident Records + Barangay Documents + Transaction Logs phase, now backed by a real local database.
>>>>>>> e5b3ac2748cf0fd51813c2ee1a55e411d251b896

## What's included

<<<<<<< HEAD
- **Login** — branded with the barangay logo, validates against the seeded/registered accounts, shows an error and clears the password field on invalid credentials. Password field has a show/hide (eye) toggle.
- **Dashboard** — role-based sidebar, fully wired to live data:
  - **Administrator** sees: Dashboard, Resident Records, Barangay Documents, Transaction Logs, User Management, Backup & Restore, Logout.
  - **Staff** sees: Dashboard, Resident Records, Barangay Documents, Transaction Logs, Logout.
  - Stat cards (Total Residents, Documents Issued, Pending Requests, Registered Users) and the "Recent Activity" feed all read from the database — nothing is a placeholder anymore.
- **Resident Records module** — full CRUD against SQLite:
  - Searchable/filterable grid (name, purok, contact number, Active/Inactive status).
  - **Add / Edit** — name (last/first/middle/suffix/alias), birth date + birthplace, gender, civil status, purok/address, contact number, occupation, household composition.
  - **View** — read-only profile dialog plus a **Document History** card pulled live from that resident's issued documents (`DocumentService.GetByResident`).
  - **Deactivate / Reactivate** — soft-delete with an optional reason, keeps the record instead of hard-deleting it.
  - **Delete** — permanent removal, Administrator-only.
- **Barangay Documents & Clearance Issuance module**:
  - Generates official documents by filling the barangay's real `.docx` templates (`Templates/`) — resident details are pulled live from the resident record, never re-encoded.
  - Enforces a verification step (residency + documentary requirements) before a request can be approved.
  - Request status: Pending / Approved / Rejected, with remarks.
  - Every generated document is saved to the database; supports printing straight from the generated file.
  - Filterable History view (per resident / per document type).
  - ⚠️ **Known gap:** `DocumentType.SchoolRequirement` maps to `Templates/SchoolRequirement.docx` (see `BarangayDocument.cs → TemplateFileName`), but that file doesn't exist in `Templates/` yet — only 5 of the 6 document types have a template. Generating a School Requirement document will fail until that `.docx` is added.
- **Transaction Logs module** — read-only audit trail of every login/logout, resident change, document action, user-account change, and system action (backups/restores). Filterable by free-text search, category, and date range; exportable to CSV.
- **User Management module** (Administrator-only) — searchable list of accounts, Add/Edit with validation, activate/deactivate, permanent delete.
- **Backup & Restore module** (Administrator-only) — new:
  - Shows the live database's path, size, and last-modified time.
  - **Create Backup Now** — snapshots the database (via SQLite's own online-backup API, not a raw file copy) into `Data\Backups\backup_<timestamp>.db`.
  - Lists all backups with file name / date / size; **Restore Selected**, **Export Copy...** (to a USB drive, etc.), and **Delete Selected**.
  - **Import & Restore...** — restore from any `.db` file browsed from outside the app.
  - Every restore automatically snapshots the *current* database first (`pre_restore_<timestamp>.db`) before overwriting it, so a wrong restore can always be undone from the backup list.
  - Prompts to restart the app after a successful restore so every module reloads from the restored data.
  - Every backup/restore/delete action is written to the Transaction Logs audit trail.
- Logout asks for confirmation before closing.
- Application + window icon use the barangay seal.

## Data storage

The app uses a local **SQLite** database (`Microsoft.Data.Sqlite`), not Microsoft Access/OleDb. The database file lives at:

```
<app folder>\Data\barangay.db
```

`Data/DatabaseHelper.cs` creates the schema and seeds demo data automatically on first run (`Initialize()`, called once from `Program.cs`) — it's a no-op on every run after that. Because everything lives under `Data\`, the whole app folder can be copied to another PC and it will carry its data with it — and it's exactly what the new Backup & Restore module snapshots/restores.

## Seeded accounts
=======
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
>>>>>>> e5b3ac2748cf0fd51813c2ee1a55e411d251b896

This phase replaces the earlier in-memory lists with a real local database:

<<<<<<< HEAD
Seeded once into the `UserAccounts` table by `DatabaseHelper` the first time the app runs. Change these from the User Management module once you're ready to hand the system off.
=======
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
>>>>>>> e5b3ac2748cf0fd51813c2ee1a55e411d251b896

## How to open

1. Unzip this folder.
<<<<<<< HEAD
2. Open it in Visual Studio (File → Open → Folder, or double-click `BarangayVillaMTejeroSystem.csproj`). Make sure you have the **.NET desktop development** workload and the **.NET 8.0 SDK** installed (Visual Studio Installer → Modify).
3. Press **F5** (or Ctrl+F5) — NuGet restores `Microsoft.Data.Sqlite` automatically on first build, and the database + seed data are created automatically on first run.
4. Log in with one of the seeded accounts above.
=======
2. Double-click `BarangayVillaMTejeroSystem.sln` to open it in Visual Studio.
3. Press **F5** (or Ctrl+F5) to run. Visual Studio will restore NuGet packages automatically on first build (including `Microsoft.Data.Sqlite`) — make sure you have the **.NET desktop development** workload installed (Visual Studio Installer → Modify) and the **.NET 8.0 SDK**.
4. On first run, `Data\barangay.db` is created and seeded automatically.
5. Log in with one of the seeded accounts above.
>>>>>>> e5b3ac2748cf0fd51813c2ee1a55e411d251b896

## Project structure

```
BarangayVillaMTejeroSystem/
 ├─ BarangayVillaMTejeroSystem.csproj
<<<<<<< HEAD
 ├─ Program.cs                     (entry point — initializes the DB, opens LoginForm)
=======
 ├─ Program.cs                  (entry point; calls DatabaseHelper.Initialize())
>>>>>>> e5b3ac2748cf0fd51813c2ee1a55e411d251b896
 ├─ Controls/
 │   ├─ ResidentManagementControl.cs
 │   ├─ DocumentIssuanceControl.cs
 │   ├─ TransactionLogsControl.cs
 │   ├─ UserManagementControl.cs
<<<<<<< HEAD
 │   └─ BackupRestoreControl.cs
=======
 │   ├─ ResidentManagementControl.cs
 │   ├─ DocumentIssuanceControl.cs     (Barangay Documents module)
 │   └─ TransactionLogsControl.cs      (audit trail module)
>>>>>>> e5b3ac2748cf0fd51813c2ee1a55e411d251b896
 ├─ Forms/
 │   ├─ LoginForm.cs
 │   ├─ DashboardForm.cs
 │   ├─ ResidentFormDialog.cs
 │   ├─ ResidentProfileDialog.cs
 │   └─ UserFormDialog.cs
 ├─ Models/
<<<<<<< HEAD
 │   ├─ UserAccount.cs / UserRole.cs
 │   ├─ Resident.cs                (also defines Gender, CivilStatus)
 │   ├─ BarangayDocument.cs        (also defines DocumentType, DocumentStatus)
 │   └─ TransactionLog.cs          (also defines LogType)
 ├─ Services/
 │   ├─ UserService.cs
 │   ├─ ResidentService.cs
 │   ├─ DocumentService.cs
 │   ├─ DocumentPrinter.cs         (fills + opens a document for printing)
 │   ├─ DocxTemplateFiller.cs      (token replacement inside .docx XML)
 │   ├─ TransactionLogService.cs
 │   └─ BackupService.cs           (SQLite backup/restore)
 ├─ Data/
 │   └─ DatabaseHelper.cs          (SQLite schema + seed data)
 ├─ Templates/
 │   ├─ CertificateOfResidency.docx
 │   ├─ CertificateOfIndigency.docx
 │   ├─ BarangayClearance.docx
 │   ├─ BarangayClearanceForBusiness.docx
 │   ├─ CertificateOfOneness.docx
 │   └─ TOKEN_REFERENCE.md         (placeholder token reference for the templates above)
 ├─ UI/                            (reusable styled controls)
=======
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
>>>>>>> e5b3ac2748cf0fd51813c2ee1a55e411d251b896
 │   ├─ FlatButton.cs
 │   ├─ GradientPanel.cs
 │   ├─ RoundedPanel.cs
 │   ├─ RoundHelper.cs
 │   ├─ SidebarButton.cs
 │   ├─ StatCard.cs
 │   └─ EyeToggleButton.cs         (password show/hide icon)
 └─ Resources/
     ├─ logo.png
     └─ app.ico
```

## Known gaps / next steps

<<<<<<< HEAD
- **Missing template:** add `Templates/SchoolRequirement.docx` (see the Barangay Documents note above) so all 6 `DocumentType` values are actually generatable.
- The Backup & Restore module is manual/on-demand only — there's no scheduled/automatic backup yet if that's something you want to add later.
- No .accdb/OleDb references remain anywhere in the project — the earlier plan to use Microsoft Access was superseded by the SQLite implementation, so no migration is needed there.
=======
- **Backup & Restore** is the one remaining "Coming Soon" placeholder module — a natural next step is a simple "copy `Data\barangay.db` to a chosen folder / restore from a chosen file" feature, since the whole database is a single file.
- Double-check `DashboardForm.cs` to confirm the **Documents Issued** and **Pending Requests** stat cards are pulling from `DocumentService` now that it exists (it wasn't part of this update batch).
- The Resident Profile dialog's **Document History** card should now be wireable to real per-resident data via `DocumentService`, replacing its earlier "Coming Soon" placeholder — worth confirming `ResidentProfileDialog.cs` was updated to do this.
- If a true Microsoft Access (`.accdb`/OleDb) backend is still required by the project spec, `Data/DatabaseHelper.cs` is the one file to replace; keep the same method signatures on the four services so the rest of the app doesn't need to change.
>>>>>>> e5b3ac2748cf0fd51813c2ee1a55e411d251b896
