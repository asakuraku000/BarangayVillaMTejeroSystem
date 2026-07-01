using System.Collections.Generic;
using System.Linq;
using BarangayVillaMTejeroSystem.Models;

namespace BarangayVillaMTejeroSystem.Services
{
    /// <summary>
    /// Handles user accounts for the system. For this phase (Login + Dashboard
    /// scaffold), accounts are seeded in-memory so the login flow and role-based
    /// sidebar can be demonstrated immediately. This will later be replaced by
    /// the Microsoft Access-backed User Management module described in the
    /// project's Class Diagram (userID, username, password, userType, fullName,
    /// contactNo).
    /// </summary>
    public static class UserService
    {
        private static readonly List<UserAccount> _accounts = new()
        {
            new UserAccount
            {
                UserId = 1,
                Username = "admin",
                Password = "Admin@123",
                FullName = "Hon. Juan Dela Cruz",
                Position = "Barangay Captain / System Administrator",
                ContactNo = "0917-000-0001",
                Role = UserRole.Administrator
            },
            new UserAccount
            {
                UserId = 2,
                Username = "staff1",
                Password = "Staff@123",
                FullName = "Maria Santos",
                Position = "Barangay Secretary",
                ContactNo = "0917-000-0002",
                Role = UserRole.Staff
            },
            new UserAccount
            {
                UserId = 3,
                Username = "staff2",
                Password = "Staff@123",
                FullName = "Pedro Reyes",
                Position = "Barangay Records Officer",
                ContactNo = "0917-000-0003",
                Role = UserRole.Staff
            },
        };

        /// <summary>
        /// Validates credentials against the seeded accounts.
        /// Returns the matching account, or null if invalid (per spec:
        /// invalid credentials return the user to the login form).
        /// Deactivated accounts cannot log in.
        /// </summary>
        public static UserAccount Authenticate(string username, string password)
        {
            return _accounts.FirstOrDefault(a =>
                a.IsActive &&
                a.Username.Equals(username?.Trim(), System.StringComparison.OrdinalIgnoreCase) &&
                a.Password == password);
        }

        public static IReadOnlyList<UserAccount> GetAllAccounts() =>
            _accounts.OrderBy(a => a.UserId).ToList().AsReadOnly();

        public static UserAccount GetById(int userId) =>
            _accounts.FirstOrDefault(a => a.UserId == userId);

        /// <summary>
        /// Case-insensitive username uniqueness check. Pass excludeUserId when
        /// editing an existing account so it doesn't collide with itself.
        /// </summary>
        public static bool UsernameExists(string username, int excludeUserId = 0)
        {
            if (string.IsNullOrWhiteSpace(username)) return false;
            return _accounts.Any(a =>
                a.UserId != excludeUserId &&
                a.Username.Equals(username.Trim(), System.StringComparison.OrdinalIgnoreCase));
        }

        public static UserAccount AddAccount(UserAccount account)
        {
            account.UserId = _accounts.Count == 0 ? 1 : _accounts.Max(a => a.UserId) + 1;
            account.Username = account.Username.Trim();
            account.IsActive = true;
            _accounts.Add(account);
            return account;
        }

        /// <summary>
        /// Updates an existing account in place, matched by UserId.
        /// Password is only overwritten when a non-empty value is supplied.
        /// </summary>
        public static bool UpdateAccount(UserAccount updated)
        {
            var existing = GetById(updated.UserId);
            if (existing == null) return false;

            existing.FullName = updated.FullName;
            existing.Username = updated.Username.Trim();
            existing.Position = updated.Position;
            existing.ContactNo = updated.ContactNo;
            existing.Role = updated.Role;
            if (!string.IsNullOrWhiteSpace(updated.Password))
                existing.Password = updated.Password;

            return true;
        }

        public static bool SetActive(int userId, bool isActive)
        {
            var existing = GetById(userId);
            if (existing == null) return false;
            existing.IsActive = isActive;
            return true;
        }

        public static bool DeleteAccount(int userId)
        {
            var existing = GetById(userId);
            if (existing == null) return false;
            return _accounts.Remove(existing);
        }
    }
}
