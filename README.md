# Barangay Villa M. Tejero — Integrated Management System

Desktop application scaffold (C# WinForms, .NET 8) — Login + Dashboard + Resident Records phase.

## What's included in this phase

- **Login form** — branded with your barangay logo, validates against seeded accounts, shows an error and clears the password field on invalid credentials.
- **Dashboard form** — role-based sidebar that is fully wired up and navigable:
  - **Administrator** sees: Dashboard, Resident Records, Barangay Documents, Transaction Logs, User Management, Backup & Restore, Logout.
  - **Staff** sees: Dashboard, Resident Records, Barangay Documents, Transaction Logs (view-only note), Logout.
  - Dashboard itself shows stat cards (Total Residents, Documents Issued, Pending Requests, Registered Users) and a "Recent Activity" card. **Total Residents** and **Registered Users** are now wired to real (in-memory) data; Documents Issued and Pending Requests remain zeroed until the Documents module is built.
  - The remaining not-yet-built modules (Barangay Documents, Transaction Logs, Backup & Restore) show a clean "Coming Soon" placeholder card so the navigation is demonstrably complete even though that module logic isn't built yet.
- **Resident Records module** — fully functional, in-memory for now, aligned to the feature list's field set (residentID, firstName, middleName, lastName, address, birthDate, civilStatus, gender, dateRegistered, occupation, contact details, household composition):
  - Seeded with 10 sample resident profiles across 7 puroks.
  - Searchable/filterable grid (by name, purok, contact number, purok dropdown, and Active/Inactive status).
  - **Add / Edit** — full profile form: name (last/first/middle/suffix), birth date, gender, civil status, purok/address, contact number, occupation, and household composition (household members, one per line).
  - **View** — dedicated read-only profile dialog with all fields plus a **Document History** card. The history list is a "Coming Soon" placeholder for now (shows "No documents issued yet") — it's the hook point for wiring in real per-resident issued-document history once the Barangay Documents module is built.
  - **Deactivate / Reactivate** — soft-deletes a resident (e.g. moved out or deceased) with an optional reason, keeping their record instead of hard-deleting it.
  - **Delete** — permanent removal, restricted to Administrator accounts (Staff can add/edit/view but not delete).
- Logout button asks for confirmation before closing, per the spec.
- Application + window icon use your barangay seal.

## Seeded accounts (in-memory, for this phase only)

| Username | Password   | Role          |
|----------|-----------|---------------|
| admin    | Admin@123 | Administrator |
| staff1   | Staff@123 | Staff         |
| staff2   | Staff@123 | Staff         |

These are defined in `Services/UserService.cs`. Once you build out the User Management + database module, swap this for real Microsoft Access-backed lookups (the project spec calls for an offline MS Access database — this in-memory seed is a stand-in just for this login/navigation phase).

## How to open

1. Unzip this folder.
2. Double-click `BarangayVillaMTejeroSystem.sln` to open it in Visual Studio.
3. Press **F5** (or Ctrl+F5) to run. Visual Studio will restore the project automatically on first build — make sure you have the **.NET desktop development** workload installed (Visual Studio Installer → Modify → check that workload) and the **.NET 8.0 SDK**, both of which the workload installs by default in current Visual Studio 2022 releases.
4. Log in with one of the seeded accounts above.

## Project structure

```
BarangayVillaMTejeroSystem.sln
BarangayVillaMTejeroSystem/
 ├─ BarangayVillaMTejeroSystem.csproj
 ├─ Program.cs                  (entry point)
 ├─ Controls/
 │   ├─ UserManagementControl.cs
 │   └─ ResidentManagementControl.cs
 ├─ Forms/
 │   ├─ LoginForm.cs
 │   ├─ DashboardForm.cs
 │   ├─ UserFormDialog.cs
 │   ├─ ResidentFormDialog.cs
 │   └─ ResidentProfileDialog.cs
 ├─ Models/
 │   ├─ UserAccount.cs
 │   ├─ UserRole.cs
 │   └─ Resident.cs             (also defines the Gender and CivilStatus enums)
 ├─ Services/
 │   ├─ UserService.cs          (seeded accounts + Authenticate())
 │   └─ ResidentService.cs      (seeded residents + CRUD + dashboard stats)
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

- The project's feature list calls for a Microsoft Access database, OleDb connection, and the full Document Issuance / Transaction Log modules described in your documentation. This scaffold intentionally keeps those as "Coming Soon" placeholders — User Management and Resident Records are now fully built out, so those are the two modules to prioritize when moving to real persistence.
- `UserService.cs` and `ResidentService.cs` are the two files you'll replace first when moving to real persistence — swap each in-memory list for OleDb queries against your `.accdb` file's `Users` and `Residents` tables, keeping the same method signatures (`Authenticate()`, `GetAllResidents()`, `AddResident()`, etc.) so the rest of the app (forms, controls, dashboard stats) doesn't need to change.
