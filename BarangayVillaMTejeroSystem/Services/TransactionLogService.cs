using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BarangayVillaMTejeroSystem.Data;
using BarangayVillaMTejeroSystem.Models;
using Microsoft.Data.Sqlite;

namespace BarangayVillaMTejeroSystem.Services
{
    /// <summary>
    /// Audit-trail store for the Transaction Logs module. Every meaningful
    /// system action (logins, resident/document changes, user management) is
    /// recorded here via Log(). The dashboard "Recent Activity" panel and the
    /// Transaction Logs page both read from this service.
    /// </summary>
    public static class TransactionLogService
    {
        public static void Log(LogType type, string action, string actor, int userId = 0, string details = "")
        {
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO TransactionLogs (Timestamp, UserId, Actor, Type, Action, Details)
                VALUES ($ts, $userId, $actor, $type, $action, $details);";
            cmd.Parameters.AddWithValue("$ts", DateTime.Now.ToString("O"));
            cmd.Parameters.AddWithValue("$userId", userId);
            cmd.Parameters.AddWithValue("$actor", actor ?? "");
            cmd.Parameters.AddWithValue("$type", (int)type);
            cmd.Parameters.AddWithValue("$action", action ?? "");
            cmd.Parameters.AddWithValue("$details", details ?? "");
            cmd.ExecuteNonQuery();
        }

        public static IReadOnlyList<TransactionLog> GetAll()
            => Query("SELECT * FROM TransactionLogs ORDER BY Timestamp DESC;");

        public static IReadOnlyList<TransactionLog> GetRecent(int count)
        {
            var list = new List<TransactionLog>();
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM TransactionLogs ORDER BY Timestamp DESC LIMIT $limit;";
            cmd.Parameters.AddWithValue("$limit", count);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(ReadLog(reader));
            return list.AsReadOnly();
        }

        /// <summary>Filtered history: free-text search plus optional type and date-range filters.</summary>
        public static IReadOnlyList<TransactionLog> Search(
            string search = null,
            LogType? type = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            var clauses = new List<string>();
            if (type.HasValue) clauses.Add("Type = $type");
            if (!string.IsNullOrWhiteSpace(search))
                clauses.Add("(Action LIKE $search OR Details LIKE $search OR Actor LIKE $search)");
            if (from.HasValue) clauses.Add("Timestamp >= $from");
            if (to.HasValue) clauses.Add("Timestamp <= $to");

            string where = clauses.Count == 0 ? "" : "WHERE " + string.Join(" AND ", clauses);
            string sql = $"SELECT * FROM TransactionLogs {where} ORDER BY Timestamp DESC;";

            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            if (type.HasValue) cmd.Parameters.AddWithValue("$type", (int)type.Value);
            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("$search", $"%{search.Trim()}%");
            if (from.HasValue) cmd.Parameters.AddWithValue("$from", from.Value.ToString("O"));
            if (to.HasValue) cmd.Parameters.AddWithValue("$to", to.Value.AddDays(1).AddTicks(-1).ToString("O"));

            var results = new List<TransactionLog>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                results.Add(ReadLog(reader));
            return results.AsReadOnly();
        }

        public static int Total => CountWhere("1 = 1");
        public static int CountByType(LogType type) => CountWhere("Type = $t", ("$t", (int)type));

        // ----- CSV export -----

        public static void ExportCsv(IReadOnlyList<TransactionLog> logs, string filePath)
        {
            var lines = new List<string>
            {
                "Timestamp,User ID,Actor,Type,Action,Details"
            };

            foreach (var l in logs)
            {
                lines.Add(string.Join(",",
                    CsvField(l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                    CsvField(l.UserId.ToString()),
                    CsvField(l.Actor),
                    CsvField(l.Type.Label()),
                    CsvField(l.Action),
                    CsvField(l.Details)));
            }

            File.WriteAllText(filePath, string.Join(Environment.NewLine, lines));
        }

        private static string CsvField(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            bool needsQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n");
            var escaped = value.Replace("\"", "\"\"");
            return needsQuote ? $"\"{escaped}\"" : escaped;
        }

        // ----- Internal helpers -----

        private static IReadOnlyList<TransactionLog> Query(string sql)
        {
            var list = new List<TransactionLog>();
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(ReadLog(reader));
            return list.AsReadOnly();
        }

        private static int CountWhere(string condition, params (string, object)[] parameters)
        {
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM TransactionLogs WHERE {condition};";
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static TransactionLog ReadLog(SqliteDataReader reader)
        {
            return new TransactionLog
            {
                LogId = reader.GetInt32(reader.GetOrdinal("LogId")),
                Timestamp = ParseDate(reader.GetString(reader.GetOrdinal("Timestamp"))),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                Actor = reader.GetString(reader.GetOrdinal("Actor")),
                Type = (LogType)reader.GetInt32(reader.GetOrdinal("Type")),
                Action = reader.GetString(reader.GetOrdinal("Action")),
                Details = reader.GetString(reader.GetOrdinal("Details"))
            };
        }

        private static DateTime ParseDate(string value) =>
            DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }
}
