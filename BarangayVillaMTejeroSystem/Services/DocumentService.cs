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
    /// Handles the Barangay Documents & Clearance Issuance records (the
    /// IssuedDocuments table). Provides save/load, control-number generation,
    /// and the filtered history queries the module's History view and the
    /// dashboard stat cards need.
    /// </summary>
    public static class DocumentService
    {
        public static BarangayDocument Save(BarangayDocument doc)
        {
            if (doc.DocumentId == 0)
            {
                doc.ControlNo = GenerateControlNo();
                doc.DateRequested = DateTime.Now;
                if (doc.Status != DocumentStatus.Pending)
                    doc.DateProcessed = DateTime.Now;
                Insert(doc);
            }
            else
            {
                if (doc.Status != DocumentStatus.Pending && !doc.DateProcessed.HasValue)
                    doc.DateProcessed = DateTime.Now;
                Update(doc);
            }

            return doc;
        }

        public static BarangayDocument GetById(int documentId)
        {
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM IssuedDocuments WHERE DocumentId = $id;";
            cmd.Parameters.AddWithValue("$id", documentId);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? ReadDocument(reader) : null;
        }

        public static IReadOnlyList<BarangayDocument> GetAll()
            => Query("SELECT * FROM IssuedDocuments ORDER BY DateRequested DESC;");

        /// <summary>History filtered by resident, document type, status, and free-text search on control no / purpose.</summary>
        public static IReadOnlyList<BarangayDocument> Search(
            string search = null,
            int? residentId = null,
            DocumentType? type = null,
            DocumentStatus? status = null)
        {
            var clauses = new List<string>();
            var doc = new BarangayDocument();

            if (residentId.HasValue) clauses.Add("ResidentId = $residentId");
            if (type.HasValue) clauses.Add("DocumentType = $type");
            if (status.HasValue) clauses.Add("Status = $status");
            if (!string.IsNullOrWhiteSpace(search))
                clauses.Add("(ControlNo LIKE $search OR Purpose LIKE $search)");

            string where = clauses.Count == 0 ? "" : "WHERE " + string.Join(" AND ", clauses);
            string sql = $"SELECT * FROM IssuedDocuments {where} ORDER BY DateRequested DESC;";

            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            if (residentId.HasValue) cmd.Parameters.AddWithValue("$residentId", residentId.Value);
            if (type.HasValue) cmd.Parameters.AddWithValue("$type", (int)type.Value);
            if (status.HasValue) cmd.Parameters.AddWithValue("$status", (int)status.Value);
            if (!string.IsNullOrWhiteSpace(search))
                cmd.Parameters.AddWithValue("$search", $"%{search.Trim()}%");

            var results = new List<BarangayDocument>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                results.Add(ReadDocument(reader));
            return results.AsReadOnly();
        }

        public static IReadOnlyList<BarangayDocument> GetByResident(int residentId)
            => Search(residentId: residentId);

        // ----- Dashboard stats -----

        public static int TotalIssued => CountWhere("1 = 1");
        public static int TotalApproved => CountWhere("Status = 1");
        public static int PendingRequests => CountWhere("Status = 0");

        // ----- Control number generation: BVMT-YYYY-NNNN -----

        public static string GenerateControlNo()
        {
            int year = DateTime.Now.Year;
            int next;

            using var connection = DatabaseHelper.CreateOpenConnection();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COALESCE(MAX(CAST(SUBSTR(ControlNo, 11) AS INTEGER)), 0)
                    FROM IssuedDocuments
                    WHERE ControlNo LIKE $pattern;";
                cmd.Parameters.AddWithValue("$pattern", $"BVMT-{year}-%");
                next = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
            }

            return $"BVMT-{year}-{next:D4}";
        }

        // ----- Res. Cert. / CTC No. suggestion (typed, not enforced) -----

        /// <summary>
        /// Best-effort convenience default for the Res. Cert. / CTC No. field:
        /// one more than the highest purely-numeric CtcNo already on file, or
        /// empty if there's nothing numeric to go on yet. Unlike ControlNo this
        /// is only a starting point, never a generated ID — the real number
        /// always comes off the resident's physical Cedula stub, so staff can
        /// (and often will) type over it.
        /// </summary>
        public static string SuggestNextCtcNo()
        {
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT MAX(CAST(CtcNo AS INTEGER))
                FROM IssuedDocuments
                WHERE CtcNo GLOB '[0-9]*';";
            object result = cmd.ExecuteScalar();
            if (result == null || result is DBNull) return "";

            long max = Convert.ToInt64(result);
            return max <= 0 ? "" : (max + 1).ToString(CultureInfo.InvariantCulture);
        }

        // ----- Internal helpers -----

        private static IReadOnlyList<BarangayDocument> Query(string sql)
        {
            var list = new List<BarangayDocument>();
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(ReadDocument(reader));
            return list.AsReadOnly();
        }

        private static int CountWhere(string condition)
        {
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM IssuedDocuments WHERE {condition};";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static void Insert(BarangayDocument doc)
        {
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO IssuedDocuments
                    (ControlNo, ResidentId, DocumentType, Purpose, ResidencyVerified, Requirements, OrNumber, CtcNo, Fee, BusinessType, BusinessTax, Status, Remarks, RequestedBy, DateRequested, DateProcessed)
                VALUES
                    ($controlNo, $residentId, $type, $purpose, $residency, $requirements, $orNo, $ctcNo, $fee, $businessType, $businessTax, $status, $remarks, $requestedBy, $dateRequested, $dateProcessed);";
            BindFields(cmd, doc);
            cmd.ExecuteNonQuery();

            using var idCmd = connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            doc.DocumentId = (int)(long)idCmd.ExecuteScalar();
        }

        private static void Update(BarangayDocument doc)
        {
            using var connection = DatabaseHelper.CreateOpenConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE IssuedDocuments SET
                    ControlNo = $controlNo,
                    ResidentId = $residentId,
                    DocumentType = $type,
                    Purpose = $purpose,
                    ResidencyVerified = $residency,
                    Requirements = $requirements,
                    OrNumber = $orNo,
                    CtcNo = $ctcNo,
                    Fee = $fee,
                    BusinessType = $businessType,
                    BusinessTax = $businessTax,
                    Status = $status,
                    Remarks = $remarks,
                    RequestedBy = $requestedBy,
                    DateRequested = $dateRequested,
                    DateProcessed = $dateProcessed
                WHERE DocumentId = $id;";
            BindFields(cmd, doc);
            cmd.Parameters.AddWithValue("$id", doc.DocumentId);
            cmd.ExecuteNonQuery();
        }

        private static void BindFields(SqliteCommand cmd, BarangayDocument doc)
        {
            cmd.Parameters.AddWithValue("$controlNo", doc.ControlNo);
            cmd.Parameters.AddWithValue("$residentId", doc.ResidentId);
            cmd.Parameters.AddWithValue("$type", (int)doc.DocumentType);
            cmd.Parameters.AddWithValue("$purpose", doc.Purpose ?? "");
            cmd.Parameters.AddWithValue("$residency", doc.ResidencyVerified ? 1 : 0);
            cmd.Parameters.AddWithValue("$requirements", string.Join("|", doc.Requirements));
            cmd.Parameters.AddWithValue("$orNo", doc.OrNumber ?? "");
            cmd.Parameters.AddWithValue("$ctcNo", doc.CtcNo ?? "");
            cmd.Parameters.AddWithValue("$fee", doc.Fee ?? "0.00");
            cmd.Parameters.AddWithValue("$businessType", doc.BusinessType ?? "");
            cmd.Parameters.AddWithValue("$businessTax", doc.BusinessTax ?? "0.00");
            cmd.Parameters.AddWithValue("$status", (int)doc.Status);
            cmd.Parameters.AddWithValue("$remarks", doc.Remarks ?? "");
            cmd.Parameters.AddWithValue("$requestedBy", doc.RequestedBy);
            cmd.Parameters.AddWithValue("$dateRequested", doc.DateRequested.ToString("O"));
            cmd.Parameters.AddWithValue("$dateProcessed", doc.DateProcessed.HasValue
                ? doc.DateProcessed.Value.ToString("O")
                : (object)DBNull.Value);
        }

        private static BarangayDocument ReadDocument(SqliteDataReader reader)
        {
            return new BarangayDocument
            {
                DocumentId = reader.GetInt32(reader.GetOrdinal("DocumentId")),
                ControlNo = reader.GetString(reader.GetOrdinal("ControlNo")),
                ResidentId = reader.GetInt32(reader.GetOrdinal("ResidentId")),
                DocumentType = (DocumentType)reader.GetInt32(reader.GetOrdinal("DocumentType")),
                Purpose = reader.GetString(reader.GetOrdinal("Purpose")),
                ResidencyVerified = reader.GetInt32(reader.GetOrdinal("ResidencyVerified")) == 1,
                Requirements = (reader.GetString(reader.GetOrdinal("Requirements")) ?? "")
                    .Split('|', StringSplitOptions.RemoveEmptyEntries).ToList(),
                OrNumber = reader.GetString(reader.GetOrdinal("OrNumber")),
                CtcNo = reader.GetString(reader.GetOrdinal("CtcNo")),
                Fee = ReadAsString(reader, "Fee"),
                BusinessType = reader.GetString(reader.GetOrdinal("BusinessType")),
                BusinessTax = ReadAsString(reader, "BusinessTax"),
                Status = (DocumentStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Remarks = reader.GetString(reader.GetOrdinal("Remarks")),
                RequestedBy = reader.GetInt32(reader.GetOrdinal("RequestedBy")),
                DateRequested = ParseDate(reader.GetString(reader.GetOrdinal("DateRequested"))),
                DateProcessed = reader.IsDBNull(reader.GetOrdinal("DateProcessed"))
                    ? (DateTime?)null
                    : ParseDate(reader.GetString(reader.GetOrdinal("DateProcessed")))
            };
        }

        private static DateTime ParseDate(string value) =>
            DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        /// <summary>
        /// Reads a column as text regardless of whether the underlying SQLite value
        /// is stored as text (new multi-line Fee/BusinessTax entries) or a plain
        /// number (older rows saved back when these columns were REAL) — avoids an
        /// InvalidCastException either way.
        /// </summary>
        private static string ReadAsString(SqliteDataReader reader, string column)
        {
            int ordinal = reader.GetOrdinal(column);
            if (reader.IsDBNull(ordinal)) return "0.00";
            return reader.GetValue(ordinal)?.ToString() ?? "0.00";
        }
    }
}
