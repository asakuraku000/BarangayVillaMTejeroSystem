using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace BarangayVillaMTejeroSystem.Data
{
    /// <summary>
    /// Owns the SQLite database file, schema creation, and seed data for the
    /// system. This replaces the in-memory lists that ResidentService and
    /// UserService used to run on for the demo phase.
    ///
    /// The database lives at Data\barangay.db next to the executable, so the
    /// whole app folder stays self-contained and easy to back up/copy to
    /// another PC. Call Initialize() once at startup (see Program.cs) before
    /// any form touches ResidentService or UserService.
    /// </summary>
    public static class DatabaseHelper
    {
        private static readonly string DataDirectory =
            Path.Combine(AppContext.BaseDirectory, "Data");

        public static readonly string DbPath =
            Path.Combine(DataDirectory, "barangay.db");

        private static string ConnectionString => $"Data Source={DbPath}";

        /// <summary>
        /// Opens a new connection with foreign key enforcement turned on.
        /// SQLite requires PRAGMA foreign_keys to be set per-connection
        /// (it is not a persisted database setting), so every connection
        /// used by this app should come through here rather than
        /// `new SqliteConnection(...)` directly.
        /// </summary>
        public static SqliteConnection CreateOpenConnection()
        {
            var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                pragma.ExecuteNonQuery();
            }

            return connection;
        }

        /// <summary>
        /// Creates the database file/tables if they don't exist yet, and
        /// seeds the same demo data the old in-memory lists shipped with so
        /// the app behaves identically the first time it runs against SQL.
        /// Safe to call on every startup - it's a no-op once the schema and
        /// seed rows already exist.
        /// </summary>
        public static void Initialize()
        {
            Directory.CreateDirectory(DataDirectory);

            using var connection = CreateOpenConnection();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Residents (
                        ResidentId      INTEGER PRIMARY KEY AUTOINCREMENT,
                        LastName        TEXT NOT NULL,
                        FirstName       TEXT NOT NULL,
                        MiddleName      TEXT NOT NULL DEFAULT '',
                        Suffix          TEXT NOT NULL DEFAULT '',
                        AliasName       TEXT NOT NULL DEFAULT '',
                        BirthDate       TEXT NOT NULL,
                        Birthplace      TEXT NOT NULL DEFAULT '',
                        Gender          INTEGER NOT NULL,
                        CivilStatus     INTEGER NOT NULL,
                        Purok           TEXT NOT NULL DEFAULT '',
                        ContactNo       TEXT NOT NULL DEFAULT '',
                        Occupation      TEXT NOT NULL DEFAULT '',
                        IsActive        INTEGER NOT NULL DEFAULT 1,
                        Remarks         TEXT NOT NULL DEFAULT '',
                        DateRegistered  TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS ResidentHouseholdMembers (
                        Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                        ResidentId  INTEGER NOT NULL,
                        MemberName  TEXT NOT NULL,
                        FOREIGN KEY (ResidentId) REFERENCES Residents(ResidentId) ON DELETE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS UserAccounts (
                        UserId      INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username    TEXT NOT NULL UNIQUE,
                        Password    TEXT NOT NULL,
                        FullName    TEXT NOT NULL,
                        Position    TEXT NOT NULL DEFAULT '',
                        ContactNo   TEXT NOT NULL DEFAULT '',
                        Role        INTEGER NOT NULL,
                        IsActive    INTEGER NOT NULL DEFAULT 1
                    );

                    CREATE TABLE IF NOT EXISTS IssuedDocuments (
                        DocumentId       INTEGER PRIMARY KEY AUTOINCREMENT,
                        ControlNo        TEXT NOT NULL UNIQUE,
                        ResidentId       INTEGER NOT NULL,
                        DocumentType     INTEGER NOT NULL,
                        Purpose          TEXT NOT NULL DEFAULT '',
                        ResidencyVerified INTEGER NOT NULL DEFAULT 0,
                        Requirements     TEXT NOT NULL DEFAULT '',
                        OrNumber         TEXT NOT NULL DEFAULT '',
                        Fee              REAL NOT NULL DEFAULT 0,
                        BusinessType     TEXT NOT NULL DEFAULT '',
                        BusinessTax      REAL NOT NULL DEFAULT 0,
                        Status           INTEGER NOT NULL DEFAULT 0,
                        Remarks          TEXT NOT NULL DEFAULT '',
                        RequestedBy      INTEGER NOT NULL DEFAULT 0,
                        DateRequested    TEXT NOT NULL,
                        DateProcessed    TEXT,
                        FOREIGN KEY (ResidentId) REFERENCES Residents(ResidentId) ON DELETE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS TransactionLogs (
                        LogId      INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp TEXT NOT NULL,
                        UserId     INTEGER NOT NULL DEFAULT 0,
                        Actor      TEXT NOT NULL DEFAULT '',
                        Type       INTEGER NOT NULL,
                        Action     TEXT NOT NULL DEFAULT '',
                        Details    TEXT NOT NULL DEFAULT ''
                    );
                ";
                cmd.ExecuteNonQuery();
            }

            EnsureColumnExists(connection, "Residents", "AliasName", "TEXT NOT NULL DEFAULT ''");
            EnsureColumnExists(connection, "Residents", "Birthplace", "TEXT NOT NULL DEFAULT ''");
            EnsureColumnExists(connection, "IssuedDocuments", "BusinessType", "TEXT NOT NULL DEFAULT ''");
            EnsureColumnExists(connection, "IssuedDocuments", "BusinessTax", "REAL NOT NULL DEFAULT 0");

            SeedResidentsIfEmpty(connection);
            SeedUserAccountsIfEmpty(connection);
            SeedDocumentsIfEmpty(connection);
            SeedTransactionLogsIfEmpty(connection);
        }

        /// <summary>
        /// Adds a column to an already-existing table if it isn't there yet.
        /// `CREATE TABLE IF NOT EXISTS` only applies the full schema the very
        /// first time a table is created, so databases created by an older
        /// version of this app (before a column was added here) need this to
        /// pick up the new column without losing existing data.
        /// </summary>
        private static void EnsureColumnExists(SqliteConnection connection, string table, string column, string columnDefSql)
        {
            using (var check = connection.CreateCommand())
            {
                check.CommandText = $"PRAGMA table_info({table});";
                using var reader = check.ExecuteReader();
                while (reader.Read())
                {
                    string existingName = reader.GetString(reader.GetOrdinal("name"));
                    if (string.Equals(existingName, column, StringComparison.OrdinalIgnoreCase))
                        return; // already present
                }
            }

            using var alter = connection.CreateCommand();
            alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {columnDefSql};";
            alter.ExecuteNonQuery();
        }

        private static long CountRows(SqliteConnection connection, string table)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {table};";
            return (long)cmd.ExecuteScalar();
        }

        private static void SeedResidentsIfEmpty(SqliteConnection connection)
        {
            if (CountRows(connection, "Residents") > 0) return;

            var seed = new (string Last, string First, string Middle, string Suffix, DateTime Birth, int Gender, int Civil, string Purok, string Contact, string Occupation, string[] Household, DateTime Registered)[]
            {
                ("Dela Cruz", "Juan", "Santos", "", new DateTime(1978, 3, 14), 0, 1, "Purok 1", "0917-100-0001", "Barangay Captain", new[] { "Maria Dela Cruz (spouse)", "Jenny Dela Cruz (daughter)" }, new DateTime(2019, 1, 15)),
                ("Santos", "Maria", "Reyes", "", new DateTime(1985, 7, 22), 1, 1, "Purok 2", "0917-100-0002", "Teacher", new[] { "Carlos Santos (spouse)" }, new DateTime(2019, 2, 3)),
                ("Reyes", "Pedro", "Garcia", "", new DateTime(1960, 11, 5), 0, 2, "Purok 3", "0917-100-0003", "Retired", Array.Empty<string>(), new DateTime(2019, 2, 20)),
                ("Garcia", "Ana", "Lopez", "", new DateTime(2001, 4, 9), 1, 0, "Purok 1", "0917-100-0004", "Student", new[] { "Juan Dela Cruz (father)", "Maria Dela Cruz (mother)" }, new DateTime(2020, 6, 11)),
                ("Lopez", "Jose", "Fernandez", "Jr.", new DateTime(1995, 9, 30), 0, 0, "Purok 4", "0917-100-0005", "Driver", Array.Empty<string>(), new DateTime(2020, 9, 2)),
                ("Fernandez", "Rosa", "Villanueva", "", new DateTime(1954, 1, 18), 1, 2, "Purok 2", "0917-100-0006", "None", Array.Empty<string>(), new DateTime(2019, 4, 8)),
                ("Villanueva", "Mark", "Torres", "", new DateTime(1990, 12, 2), 0, 1, "Purok 5", "0917-100-0007", "Vendor", new[] { "Liza Villanueva (spouse)", "Miguel Villanueva (son)" }, new DateTime(2021, 1, 25)),
                ("Torres", "Liza", "Bautista", "", new DateTime(1988, 5, 16), 1, 3, "Purok 3", "0917-100-0008", "Sari-sari Store Owner", new[] { "Ramon Torres (son)" }, new DateTime(2021, 3, 30)),
                ("Bautista", "Ramon", "Cruz", "", new DateTime(2015, 8, 21), 0, 0, "Purok 6", "0917-100-0009", "None", Array.Empty<string>(), new DateTime(2022, 2, 14)),
                ("Cruz", "Ella", "Marquez", "", new DateTime(1999, 10, 27), 1, 0, "Purok 7", "0917-100-0010", "Call Center Agent", Array.Empty<string>(), new DateTime(2022, 7, 19)),
            };

            foreach (var r in seed)
            {
                long residentId;

                using (var insertResident = connection.CreateCommand())
                {
                    insertResident.CommandText = @"
                        INSERT INTO Residents
                            (LastName, FirstName, MiddleName, Suffix, BirthDate, Gender, CivilStatus, Purok, ContactNo, Occupation, IsActive, Remarks, DateRegistered)
                        VALUES
                            ($lastName, $firstName, $middleName, $suffix, $birthDate, $gender, $civilStatus, $purok, $contactNo, $occupation, 1, '', $registered);";
                    insertResident.Parameters.AddWithValue("$lastName", r.Last);
                    insertResident.Parameters.AddWithValue("$firstName", r.First);
                    insertResident.Parameters.AddWithValue("$middleName", r.Middle);
                    insertResident.Parameters.AddWithValue("$suffix", r.Suffix);
                    insertResident.Parameters.AddWithValue("$birthDate", r.Birth.ToString("O"));
                    insertResident.Parameters.AddWithValue("$gender", r.Gender);
                    insertResident.Parameters.AddWithValue("$civilStatus", r.Civil);
                    insertResident.Parameters.AddWithValue("$purok", r.Purok);
                    insertResident.Parameters.AddWithValue("$contactNo", r.Contact);
                    insertResident.Parameters.AddWithValue("$occupation", r.Occupation);
                    insertResident.Parameters.AddWithValue("$registered", r.Registered.ToString("O"));
                    insertResident.ExecuteNonQuery();
                }

                using (var idCmd = connection.CreateCommand())
                {
                    idCmd.CommandText = "SELECT last_insert_rowid();";
                    residentId = (long)idCmd.ExecuteScalar();
                }

                foreach (var member in r.Household)
                {
                    using var insertMember = connection.CreateCommand();
                    insertMember.CommandText = "INSERT INTO ResidentHouseholdMembers (ResidentId, MemberName) VALUES ($residentId, $memberName);";
                    insertMember.Parameters.AddWithValue("$residentId", residentId);
                    insertMember.Parameters.AddWithValue("$memberName", member);
                    insertMember.ExecuteNonQuery();
                }
            }
        }

        private static void SeedUserAccountsIfEmpty(SqliteConnection connection)
        {
            if (CountRows(connection, "UserAccounts") > 0) return;

            var seed = new (string Username, string Password, string FullName, string Position, string Contact, int Role)[]
            {
                ("admin", "Admin@123", "Hon. Juan Dela Cruz", "Barangay Captain / System Administrator", "0917-000-0001", 0),
                ("staff1", "Staff@123", "Maria Santos", "Barangay Secretary", "0917-000-0002", 1),
                ("staff2", "Staff@123", "Pedro Reyes", "Barangay Records Officer", "0917-000-0003", 1),
            };

            foreach (var u in seed)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO UserAccounts (Username, Password, FullName, Position, ContactNo, Role, IsActive)
                    VALUES ($username, $password, $fullName, $position, $contactNo, $role, 1);";
                cmd.Parameters.AddWithValue("$username", u.Username);
                cmd.Parameters.AddWithValue("$password", u.Password);
                cmd.Parameters.AddWithValue("$fullName", u.FullName);
                cmd.Parameters.AddWithValue("$position", u.Position);
                cmd.Parameters.AddWithValue("$contactNo", u.Contact);
                cmd.Parameters.AddWithValue("$role", u.Role);
                cmd.ExecuteNonQuery();
            }
        }

        private static void SeedDocumentsIfEmpty(SqliteConnection connection)
        {
            if (CountRows(connection, "IssuedDocuments") > 0) return;

            // A handful of sample issued documents so the History view and the
            // dashboard stat cards show realistic data on first run. Resident
            // ids come from SeedResidentsIfEmpty above (1..10).
            var seed = new (int ResidentId, int Type, string Purpose, int Status, string OrNo, decimal Fee, string Req, DateTime Requested, DateTime? Processed, string Remarks)[]
            {
                (1, 0, "Proof of residence for bank loan application", 1, "OR-2026-0142", 30m, "Valid ID|Proof of Residency (utility bill / barangay ID)", new DateTime(2026, 1, 8), new DateTime(2026, 1, 8), ""),
                (2, 1, "Medical assistance / financial aid request", 1, "OR-2026-0151", 0m, "Valid ID|Proof of Income / Indigency (cert from employer or unemployment)|Barangay Residency (if available)", new DateTime(2026, 1, 15), new DateTime(2026, 1, 15), "Indigent family, 4 dependents."),
                (3, 2, "Pre-employment requirement", 1, "OR-2026-0160", 50m, "Valid ID|2 pcs passport-size photo|Barangay Residency Certificate", new DateTime(2026, 2, 2), new DateTime(2026, 2, 2), ""),
                (4, 4, "School enrollment / scholarship requirement", 1, "OR-2026-0168", 30m, "Valid ID|School ID / Enrollment form|Proof of Residency", new DateTime(2026, 2, 19), new DateTime(2026, 2, 19), ""),
                (5, 3, "Sari-sari store permit application", 0, "", 0m, "Valid ID|DTI/Permit (if registered)|Sketch of business location|Proof of Residency", new DateTime(2026, 3, 5), null, "Awaiting business sketch."),
                (6, 0, "Postal ID application", 2, "", 0m, "Valid ID|Proof of Residency (utility bill / barangay ID)", new DateTime(2026, 3, 12), new DateTime(2026, 3, 12), "Applicant could not present valid proof of residency."),
            };

            int counter = 1;
            foreach (var d in seed)
            {
                string controlNo = $"BVMT-2026-{counter:D4}";
                counter++;

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO IssuedDocuments
                        (ControlNo, ResidentId, DocumentType, Purpose, ResidencyVerified, Requirements, OrNumber, Fee, Status, Remarks, RequestedBy, DateRequested, DateProcessed)
                    VALUES
                        ($controlNo, $residentId, $type, $purpose, 1, $req, $orNo, $fee, $status, $remarks, 1, $requested, $processed);";
                cmd.Parameters.AddWithValue("$controlNo", controlNo);
                cmd.Parameters.AddWithValue("$residentId", d.ResidentId);
                cmd.Parameters.AddWithValue("$type", d.Type);
                cmd.Parameters.AddWithValue("$purpose", d.Purpose);
                cmd.Parameters.AddWithValue("$req", d.Req);
                cmd.Parameters.AddWithValue("$orNo", d.OrNo);
                cmd.Parameters.AddWithValue("$fee", d.Fee);
                cmd.Parameters.AddWithValue("$status", d.Status);
                cmd.Parameters.AddWithValue("$remarks", d.Remarks);
                cmd.Parameters.AddWithValue("$requested", d.Requested.ToString("O"));
                cmd.Parameters.AddWithValue("$processed", d.Processed.HasValue ? d.Processed.Value.ToString("O") : (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void SeedTransactionLogsIfEmpty(SqliteConnection connection)
        {
            if (CountRows(connection, "TransactionLogs") > 0) return;

            // A realistic audit trail so the Transaction Logs module and the
            // dashboard's "Recent Activity" panel are populated on first run.
            var seed = new (DateTime When, int UserId, string Actor, int Type, string Action, string Details)[]
            {
                (new DateTime(2026, 1, 8, 9, 5, 0), 1, "Hon. Juan Dela Cruz", 0, "Signed in", "Administrator session started"),
                (new DateTime(2026, 1, 8, 9, 12, 0), 1, "Hon. Juan Dela Cruz", 2, "Generated Certificate of Residency", "Control BVMT-2026-0001 for Juan Dela Cruz — Approved"),
                (new DateTime(2026, 1, 15, 10, 30, 0), 2, "Maria Santos", 0, "Signed in", "Staff session started"),
                (new DateTime(2026, 1, 15, 10, 41, 0), 2, "Maria Santos", 2, "Generated Certificate of Indigency", "Control BVMT-2026-0002 for Maria Santos — Approved"),
                (new DateTime(2026, 2, 2, 14, 3, 0), 1, "Hon. Juan Dela Cruz", 2, "Generated Barangay Clearance — Employment", "Control BVMT-2026-0003 for Pedro Reyes — Approved"),
                (new DateTime(2026, 2, 19, 11, 20, 0), 2, "Maria Santos", 2, "Generated Clearance / Certificate — School Requirement", "Control BVMT-2026-0004 for Ana Garcia — Approved"),
                (new DateTime(2026, 3, 5, 8, 50, 0), 3, "Pedro Reyes", 2, "Generated Barangay Clearance — Business", "Control BVMT-2026-0005 for Jose Lopez — Pending"),
                (new DateTime(2026, 3, 12, 13, 15, 0), 1, "Hon. Juan Dela Cruz", 1, "Registered resident", "Ella Cruz added to Resident Records"),
                (new DateTime(2026, 3, 12, 16, 40, 0), 1, "Hon. Juan Dela Cruz", 2, "Generated Certificate of Residency", "Control BVMT-2026-0006 for Rosa Fernandez — Rejected"),
                (new DateTime(2026, 3, 20, 9, 0, 0), 1, "Hon. Juan Dela Cruz", 0, "Signed out", "Administrator session ended"),
            };

            foreach (var l in seed)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO TransactionLogs (Timestamp, UserId, Actor, Type, Action, Details)
                    VALUES ($ts, $userId, $actor, $type, $action, $details);";
                cmd.Parameters.AddWithValue("$ts", l.When.ToString("O"));
                cmd.Parameters.AddWithValue("$userId", l.UserId);
                cmd.Parameters.AddWithValue("$actor", l.Actor);
                cmd.Parameters.AddWithValue("$type", l.Type);
                cmd.Parameters.AddWithValue("$action", l.Action);
                cmd.Parameters.AddWithValue("$details", l.Details);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
