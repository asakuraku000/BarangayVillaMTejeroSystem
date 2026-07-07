using System;
using System.Collections.Generic;
using BarangayVillaMTejeroSystem.Data;
using BarangayVillaMTejeroSystem.Models;
using Microsoft.Data.Sqlite;

namespace BarangayVillaMTejeroSystem.Services
{
    /// <summary>
    /// Handles user accounts for the system. Backed by the SQLite database
    /// created by DatabaseHelper (Data\barangay.db). Public method
    /// signatures are unchanged from the earlier in-memory version, so
    /// LoginForm, UserFormDialog, UserManagementControl, and DashboardForm
    /// needed no changes.
    /// </summary>
    public static class UserService
    {
        /// <summary>
        /// Validates credentials against the accounts table.
        /// Returns the matching account, or null if invalid (per spec:
        /// invalid credentials return the user to the login form).
        /// Deactivated accounts cannot log in.
        /// </summary>
        public static UserAccount Authenticate(string username, string password)
        {
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT * FROM UserAccounts
                WHERE IsActive = 1
                  AND Username = $username COLLATE NOCASE
                  AND Password = $password;";
            cmd.Parameters.AddWithValue("$username", username?.Trim() ?? "");
            cmd.Parameters.AddWithValue("$password", password ?? "");

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? ReadAccount(reader) : null;
        }

        public static IReadOnlyList<UserAccount> GetAllAccounts()
        {
            var accounts = new List<UserAccount>();

            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM UserAccounts ORDER BY UserId;";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                accounts.Add(ReadAccount(reader));

            return accounts.AsReadOnly();
        }

        public static UserAccount GetById(int userId)
        {
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM UserAccounts WHERE UserId = $id;";
            cmd.Parameters.AddWithValue("$id", userId);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? ReadAccount(reader) : null;
        }

        /// <summary>
        /// Case-insensitive username uniqueness check. Pass excludeUserId when
        /// editing an existing account so it doesn't collide with itself.
        /// </summary>
        public static bool UsernameExists(string username, int excludeUserId = 0)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;

            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*) FROM UserAccounts
                WHERE UserId <> $excludeId
                  AND Username = $username COLLATE NOCASE;";
            cmd.Parameters.AddWithValue("$excludeId", excludeUserId);
            cmd.Parameters.AddWithValue("$username", username.Trim());

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public static UserAccount AddAccount(UserAccount account)
        {
            account.Username = account.Username.Trim();
            account.IsActive = true;

            using var connection = DatabaseHelper.CreateOpenConnection();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO UserAccounts (Username, Password, FullName, Position, ContactNo, Role, IsActive)
                    VALUES ($username, $password, $fullName, $position, $contactNo, $role, 1);";
                cmd.Parameters.AddWithValue("$username", account.Username);
                cmd.Parameters.AddWithValue("$password", account.Password ?? "");
                cmd.Parameters.AddWithValue("$fullName", account.FullName ?? "");
                cmd.Parameters.AddWithValue("$position", account.Position ?? "");
                cmd.Parameters.AddWithValue("$contactNo", account.ContactNo ?? "");
                cmd.Parameters.AddWithValue("$role", (int)account.Role);
                cmd.ExecuteNonQuery();
            }

            using (var idCmd = connection.CreateCommand())
            {
                idCmd.CommandText = "SELECT last_insert_rowid();";
                account.UserId = (int)(long)idCmd.ExecuteScalar();
            }

            return account;
        }

        /// <summary>
        /// Updates an existing account in place, matched by UserId.
        /// Password is only overwritten when a non-empty value is supplied.
        /// </summary>
        public static bool UpdateAccount(UserAccount updated)
        {
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();

            bool changePassword = !string.IsNullOrWhiteSpace(updated.Password);
            cmd.CommandText = changePassword
                ? @"UPDATE UserAccounts SET
                        FullName = $fullName, Username = $username, Position = $position,
                        ContactNo = $contactNo, Role = $role, Password = $password
                    WHERE UserId = $id;"
                : @"UPDATE UserAccounts SET
                        FullName = $fullName, Username = $username, Position = $position,
                        ContactNo = $contactNo, Role = $role
                    WHERE UserId = $id;";

            cmd.Parameters.AddWithValue("$fullName", updated.FullName ?? "");
            cmd.Parameters.AddWithValue("$username", updated.Username.Trim());
            cmd.Parameters.AddWithValue("$position", updated.Position ?? "");
            cmd.Parameters.AddWithValue("$contactNo", updated.ContactNo ?? "");
            cmd.Parameters.AddWithValue("$role", (int)updated.Role);
            cmd.Parameters.AddWithValue("$id", updated.UserId);
            if (changePassword)
                cmd.Parameters.AddWithValue("$password", updated.Password);

            return cmd.ExecuteNonQuery() > 0;
        }

        public static bool SetActive(int userId, bool isActive)
        {
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE UserAccounts SET IsActive = $active WHERE UserId = $id;";
            cmd.Parameters.AddWithValue("$active", isActive ? 1 : 0);
            cmd.Parameters.AddWithValue("$id", userId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public static bool DeleteAccount(int userId)
        {
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM UserAccounts WHERE UserId = $id;";
            cmd.Parameters.AddWithValue("$id", userId);
            return cmd.ExecuteNonQuery() > 0;
        }

        private static UserAccount ReadAccount(SqliteDataReader reader)
        {
            return new UserAccount
            {
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                Password = reader.GetString(reader.GetOrdinal("Password")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Position = reader.GetString(reader.GetOrdinal("Position")),
                ContactNo = reader.GetString(reader.GetOrdinal("ContactNo")),
                Role = (UserRole)reader.GetInt32(reader.GetOrdinal("Role")),
                IsActive = reader.GetInt32(reader.GetOrdinal("IsActive")) == 1
            };
        }
    }
}
