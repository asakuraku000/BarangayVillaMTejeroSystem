using System;
using System.Drawing;

namespace BarangayVillaMTejeroSystem.Models
{
    /// <summary>
    /// Categories of activity captured by the Transaction Logs module.
    /// </summary>
    public enum LogType
    {
        Authentication,
        Resident,
        Document,
        User,
        System
    }

    /// <summary>
    /// A single system audit-trail entry. Stored in the TransactionLogs table
    /// created by DatabaseHelper. The actor's display name is denormalized so
    /// the log survives even if the originating user account is later deleted.
    /// </summary>
    public class TransactionLog
    {
        public int LogId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int UserId { get; set; }              // 0 = system / not attributable
        public string Actor { get; set; } = "";      // display name of who acted
        public LogType Type { get; set; }
        public string Action { get; set; } = "";     // short description
        public string Details { get; set; } = "";

        public string TimestampLabel => Timestamp.ToString("MMM d, yyyy  h:mm tt");
    }

    /// <summary>
    /// Display labels and accent colors per LogType, kept with the enum so the
    /// UI never hard-codes category strings or colors.
    /// </summary>
    public static class LogTypeMeta
    {
        public static string Label(this LogType type) => type switch
        {
            LogType.Authentication => "Authentication",
            LogType.Resident => "Resident",
            LogType.Document => "Document",
            LogType.User => "User",
            LogType.System => "System",
            _ => type.ToString()
        };

        public static Color Color(this LogType type) => type switch
        {
            LogType.Authentication => System.Drawing.Color.FromArgb(27, 90, 130),
            LogType.Resident => System.Drawing.Color.FromArgb(16, 37, 66),
            LogType.Document => System.Drawing.Color.FromArgb(200, 29, 37),
            LogType.User => System.Drawing.Color.FromArgb(60, 130, 90),
            LogType.System => System.Drawing.Color.FromArgb(140, 148, 160),
            _ => System.Drawing.Color.FromArgb(140, 148, 160)
        };

        public static Color BackColor(this LogType type) => type switch
        {
            LogType.Authentication => System.Drawing.Color.FromArgb(232, 240, 245),
            LogType.Resident => System.Drawing.Color.FromArgb(235, 238, 243),
            LogType.Document => System.Drawing.Color.FromArgb(252, 235, 236),
            LogType.User => System.Drawing.Color.FromArgb(232, 244, 236),
            LogType.System => System.Drawing.Color.FromArgb(240, 242, 245),
            _ => System.Drawing.Color.FromArgb(240, 242, 245)
        };
    }
}
