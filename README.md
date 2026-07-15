# Barangay Villa M. Tejero — Integrated Management System

Desktop application (C# WinForms, .NET 8) for barangay records, document issuance, and audit logging. Backed by a local SQLite database, so the whole app folder is self-contained and portable — no server or internet connection required.

## What's included

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

| Username | Password   | Role          |
|----------|-----------|---------------|
| admin    | Admin@123 | Administrator |
| staff1   | Staff@123 | Staff         |
| staff2   | Staff@123 | Staff         |

Seeded once into the `UserAccounts` table by `DatabaseHelper` the first time the app runs. Change these from the User Management module once you're ready to hand the system off.

## How to open

1. Unzip this folder.
2. Open it in Visual Studio (File → Open → Folder, or double-click `BarangayVillaMTejeroSystem.csproj`). Make sure you have the **.NET desktop development** workload and the **.NET 8.0 SDK** installed (Visual Studio Installer → Modify).
3. Press **F5** (or Ctrl+F5) — NuGet restores `Microsoft.Data.Sqlite` automatically on first build, and the database + seed data are created automatically on first run.
4. Log in with one of the seeded accounts above.

## Project structure

```
BarangayVillaMTejeroSystem/
 ├─ BarangayVillaMTejeroSystem.csproj
 ├─ Program.cs                     (entry point — initializes the DB, opens LoginForm)
 ├─ Controls/
 │   ├─ ResidentManagementControl.cs
 │   ├─ DocumentIssuanceControl.cs
 │   ├─ TransactionLogsControl.cs
 │   ├─ UserManagementControl.cs
 │   └─ BackupRestoreControl.cs
 ├─ Forms/
 │   ├─ LoginForm.cs
 │   ├─ DashboardForm.cs
 │   ├─ ResidentFormDialog.cs
 │   ├─ ResidentProfileDialog.cs
 │   └─ UserFormDialog.cs
 ├─ Models/
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

- **Missing template:** add `Templates/SchoolRequirement.docx` (see the Barangay Documents note above) so all 6 `DocumentType` values are actually generatable.
- The Backup & Restore module is manual/on-demand only — there's no scheduled/automatic backup yet if that's something you want to add later.
- No .accdb/OleDb references remain anywhere in the project — the earlier plan to use Microsoft Access was superseded by the SQLite implementation, so no migration is needed there.
