namespace BarangayVillaMTejeroSystem.Models
{
    /// <summary>
    /// Represents a registered system account (Administrator or Staff/Officer).
    /// Mirrors the "User" entity fields from the project's Class Diagram:
    /// userID, username, password, userType, fullName, contactNo.
    /// </summary>
    public class UserAccount
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string ContactNo { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;

        public string RoleLabel => Role == UserRole.Administrator ? "System Administrator" : "Barangay Staff";

        public string Initial => string.IsNullOrWhiteSpace(FullName) ? "?" : FullName.Trim()[0].ToString().ToUpper();
    }
}
