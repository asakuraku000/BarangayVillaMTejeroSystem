using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BarangayVillaMTejeroSystem.Data;
using BarangayVillaMTejeroSystem.Models;
using Microsoft.Data.Sqlite;

namespace BarangayVillaMTejeroSystem.Services
{
    /// <summary>
    /// Handles resident profile records for the barangay. Backed by the
    /// SQLite database created by DatabaseHelper (Data\barangay.db).
    /// Public method signatures are unchanged from the earlier in-memory
    /// version, so ResidentManagementControl, ResidentFormDialog,
    /// ResidentProfileDialog, and DashboardForm needed no changes.
    /// </summary>
    public static class ResidentService
    {
        public static IReadOnlyList<Resident> GetAllResidents()
        {
            var residents = new List<Resident>();

            using var connection = DatabaseHelper.CreateOpenConnection();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Residents ORDER BY LastName, FirstName;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    residents.Add(ReadResident(reader));
            }

            AttachHouseholdMembers(connection, residents);
            return residents.AsReadOnly();
        }

        public static Resident GetById(int residentId)
        {
            using var connection = DatabaseHelper.CreateOpenConnection();
            Resident resident = null;

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Residents WHERE ResidentId = $id;";
                cmd.Parameters.AddWithValue("$id", residentId);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                    resident = ReadResident(reader);
            }

            if (resident != null)
                AttachHouseholdMembers(connection, new List<Resident> { resident });

            return resident;
        }

        public static IReadOnlyList<string> GetPuroks()
        {
            var puroks = new List<string>();

            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT DISTINCT Purok FROM Residents
                WHERE Purok IS NOT NULL AND TRIM(Purok) <> ''
                ORDER BY Purok;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                puroks.Add(reader.GetString(0));

            return puroks.AsReadOnly();
        }

        public static Resident AddResident(Resident resident)
        {
            resident.IsActive = true;
            resident.DateRegistered = DateTime.Now;

            using var connection = DatabaseHelper.CreateOpenConnection();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO Residents
                        (LastName, FirstName, MiddleName, Suffix, BirthDate, Gender, CivilStatus, Purok, ContactNo, Occupation, IsActive, Remarks, DateRegistered)
                    VALUES
                        ($lastName, $firstName, $middleName, $suffix, $birthDate, $gender, $civilStatus, $purok, $contactNo, $occupation, 1, $remarks, $registered);";
                BindResidentFields(cmd, resident);
                cmd.Parameters.AddWithValue("$registered", resident.DateRegistered.ToString("O"));
                cmd.ExecuteNonQuery();
            }

            using (var idCmd = connection.CreateCommand())
            {
                idCmd.CommandText = "SELECT last_insert_rowid();";
                resident.ResidentId = (int)(long)idCmd.ExecuteScalar();
            }

            ReplaceHouseholdMembers(connection, resident.ResidentId, resident.HouseholdMembers);
            return resident;
        }

        public static bool UpdateResident(Resident updated)
        {
            using var connection = DatabaseHelper.CreateOpenConnection();

            int rows;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE Residents SET
                        LastName = $lastName,
                        FirstName = $firstName,
                        MiddleName = $middleName,
                        Suffix = $suffix,
                        BirthDate = $birthDate,
                        Gender = $gender,
                        CivilStatus = $civilStatus,
                        Purok = $purok,
                        ContactNo = $contactNo,
                        Occupation = $occupation,
                        Remarks = $remarks
                    WHERE ResidentId = $id;";
                BindResidentFields(cmd, updated);
                cmd.Parameters.AddWithValue("$id", updated.ResidentId);
                rows = cmd.ExecuteNonQuery();
            }

            if (rows == 0) return false;

            ReplaceHouseholdMembers(connection, updated.ResidentId, updated.HouseholdMembers);
            return true;
        }

        public static bool SetActive(int residentId, bool isActive, string remarks = null)
        {
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();

            if (remarks != null)
            {
                cmd.CommandText = "UPDATE Residents SET IsActive = $active, Remarks = $remarks WHERE ResidentId = $id;";
                cmd.Parameters.AddWithValue("$remarks", remarks);
            }
            else
            {
                cmd.CommandText = "UPDATE Residents SET IsActive = $active WHERE ResidentId = $id;";
            }
            cmd.Parameters.AddWithValue("$active", isActive ? 1 : 0);
            cmd.Parameters.AddWithValue("$id", residentId);

            return cmd.ExecuteNonQuery() > 0;
        }

        public static bool DeleteResident(int residentId)
        {
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Residents WHERE ResidentId = $id;";
            cmd.Parameters.AddWithValue("$id", residentId);
            return cmd.ExecuteNonQuery() > 0;
        }

        // ----- Stats, for the Dashboard's stat cards -----

        public static int TotalActiveResidents
        {
            get
            {
                using var connection = DatabaseHelper.CreateOpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM Residents WHERE IsActive = 1;";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static int TotalSeniorCitizens
        {
            get
            {
                // Age is computed in C# (Resident.Age), so pull active
                // residents' birth dates and count client-side instead of
                // duplicating the age math as SQL date arithmetic.
                using var connection = DatabaseHelper.CreateOpenConnection();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT BirthDate FROM Residents WHERE IsActive = 1;";
                using var reader = cmd.ExecuteReader();

                int count = 0;
                var today = DateTime.Today;
                while (reader.Read())
                {
                    var birthDate = ParseDate(reader.GetString(0));
                    int age = today.Year - birthDate.Year;
                    if (birthDate.Date > today.AddYears(-age)) age--;
                    if (age >= 60) count++;
                }
                return count;
            }
        }

        // ----- Internal helpers -----

        private static Resident ReadResident(SqliteDataReader reader)
        {
            return new Resident
            {
                ResidentId = reader.GetInt32(reader.GetOrdinal("ResidentId")),
                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                MiddleName = reader.GetString(reader.GetOrdinal("MiddleName")),
                Suffix = reader.GetString(reader.GetOrdinal("Suffix")),
                BirthDate = ParseDate(reader.GetString(reader.GetOrdinal("BirthDate"))),
                Gender = (Gender)reader.GetInt32(reader.GetOrdinal("Gender")),
                CivilStatus = (CivilStatus)reader.GetInt32(reader.GetOrdinal("CivilStatus")),
                Purok = reader.GetString(reader.GetOrdinal("Purok")),
                ContactNo = reader.GetString(reader.GetOrdinal("ContactNo")),
                Occupation = reader.GetString(reader.GetOrdinal("Occupation")),
                IsActive = reader.GetInt32(reader.GetOrdinal("IsActive")) == 1,
                Remarks = reader.GetString(reader.GetOrdinal("Remarks")),
                DateRegistered = ParseDate(reader.GetString(reader.GetOrdinal("DateRegistered"))),
                HouseholdMembers = new List<string>()
            };
        }

        private static void AttachHouseholdMembers(SqliteConnection connection, List<Resident> residents)
        {
            if (residents.Count == 0) return;

            var byId = residents.ToDictionary(r => r.ResidentId);

            using var cmd = connection.CreateCommand();
            if (residents.Count == 1)
            {
                cmd.CommandText = "SELECT ResidentId, MemberName FROM ResidentHouseholdMembers WHERE ResidentId = $id ORDER BY Id;";
                cmd.Parameters.AddWithValue("$id", residents[0].ResidentId);
            }
            else
            {
                cmd.CommandText = "SELECT ResidentId, MemberName FROM ResidentHouseholdMembers ORDER BY ResidentId, Id;";
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int residentId = reader.GetInt32(0);
                if (byId.TryGetValue(residentId, out var resident))
                    resident.HouseholdMembers.Add(reader.GetString(1));
            }
        }

        private static void ReplaceHouseholdMembers(SqliteConnection connection, int residentId, List<string> members)
        {
            using (var del = connection.CreateCommand())
            {
                del.CommandText = "DELETE FROM ResidentHouseholdMembers WHERE ResidentId = $id;";
                del.Parameters.AddWithValue("$id", residentId);
                del.ExecuteNonQuery();
            }

            foreach (var member in members.Where(m => !string.IsNullOrWhiteSpace(m)))
            {
                using var insert = connection.CreateCommand();
                insert.CommandText = "INSERT INTO ResidentHouseholdMembers (ResidentId, MemberName) VALUES ($id, $name);";
                insert.Parameters.AddWithValue("$id", residentId);
                insert.Parameters.AddWithValue("$name", member);
                insert.ExecuteNonQuery();
            }
        }

        private static void BindResidentFields(SqliteCommand cmd, Resident resident)
        {
            cmd.Parameters.AddWithValue("$lastName", resident.LastName ?? "");
            cmd.Parameters.AddWithValue("$firstName", resident.FirstName ?? "");
            cmd.Parameters.AddWithValue("$middleName", resident.MiddleName ?? "");
            cmd.Parameters.AddWithValue("$suffix", resident.Suffix ?? "");
            cmd.Parameters.AddWithValue("$birthDate", resident.BirthDate.ToString("O"));
            cmd.Parameters.AddWithValue("$gender", (int)resident.Gender);
            cmd.Parameters.AddWithValue("$civilStatus", (int)resident.CivilStatus);
            cmd.Parameters.AddWithValue("$purok", resident.Purok ?? "");
            cmd.Parameters.AddWithValue("$contactNo", resident.ContactNo ?? "");
            cmd.Parameters.AddWithValue("$occupation", resident.Occupation ?? "");
            cmd.Parameters.AddWithValue("$remarks", resident.Remarks ?? "");
        }

        private static DateTime ParseDate(string value) =>
            DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }
}
