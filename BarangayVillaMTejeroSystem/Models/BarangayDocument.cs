using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BarangayVillaMTejeroSystem.Models
{
    /// <summary>
    /// The kinds of documents the Barangay Documents & Clearance Issuance
    /// module can produce, per the project's required document list.
    /// </summary>
    public enum DocumentType
    {
        CertificateOfResidency,
        CertificateOfIndigency,
        BarangayClearanceEmployment,
        BarangayClearanceBusiness,
        SchoolRequirement
    }

    /// <summary>
    /// Lifecycle status of a document request, as required by the module spec
    /// (Approved / Pending / Rejected) with a remarks/reason field.
    /// </summary>
    public enum DocumentStatus
    {
        Pending,
        Approved,
        Rejected
    }

    /// <summary>
    /// A single issued (or requested) barangay document record. Stored in the
    /// IssuedDocuments table created by DatabaseHelper. Resident details are
    /// never re-encoded — they are always pulled from the resident record by
    /// ResidentId at generate/print time.
    /// </summary>
    public class BarangayDocument
    {
        public int DocumentId { get; set; }
        public string ControlNo { get; set; } = string.Empty;

        public int ResidentId { get; set; }
        public DocumentType DocumentType { get; set; }

        public string Purpose { get; set; } = string.Empty;

        /// <summary>Residency verification gate — must be true before a request can be Approved.</summary>
        public bool ResidencyVerified { get; set; }

        /// <summary>Documentary requirements that were checked/verified for this request.</summary>
        public List<string> Requirements { get; set; } = new();

        public string OrNumber { get; set; } = string.Empty;
        public decimal Fee { get; set; }

        public DocumentStatus Status { get; set; }
        public string Remarks { get; set; } = string.Empty;

        /// <summary>UserAccount.UserId of the staff who processed the request.</summary>
        public int RequestedBy { get; set; }

        public DateTime DateRequested { get; set; } = DateTime.Now;
        public DateTime? DateProcessed { get; set; }

        public string RequirementsDisplay =>
            Requirements.Count == 0 ? "None checked" : string.Join(", ", Requirements);
    }

    /// <summary>
    /// Display labels, certificate titles, and the documentary requirements
    /// suggested for each DocumentType. Kept as extension methods so the enum
    /// and its metadata live together and the UI never hard-codes strings.
    /// </summary>
    public static class DocumentTypeMeta
    {
        public static string Label(this DocumentType type) => type switch
        {
            DocumentType.CertificateOfResidency => "Certificate of Residency",
            DocumentType.CertificateOfIndigency => "Certificate of Indigency",
            DocumentType.BarangayClearanceEmployment => "Barangay Clearance — Employment",
            DocumentType.BarangayClearanceBusiness => "Barangay Clearance — Business",
            DocumentType.SchoolRequirement => "Clearance / Certificate — School Requirement",
            _ => type.ToString()
        };

        public static string CertificateTitle(this DocumentType type) => type switch
        {
            DocumentType.CertificateOfResidency => "CERTIFICATE OF RESIDENCY",
            DocumentType.CertificateOfIndigency => "CERTIFICATE OF INDIGENCY",
            DocumentType.BarangayClearanceEmployment => "BARANGAY CLEARANCE",
            DocumentType.BarangayClearanceBusiness => "BARANGAY CLEARANCE",
            DocumentType.SchoolRequirement => "CERTIFICATE / CLEARANCE",
            _ => "BARANGAY DOCUMENT"
        };

        /// <summary>Documentary requirements automatically suggested when a type is selected.</summary>
        public static string[] RequiredDocuments(this DocumentType type) => type switch
        {
            DocumentType.CertificateOfResidency => new[]
            {
                "Valid ID",
                "Proof of Residency (utility bill / barangay ID)"
            },
            DocumentType.CertificateOfIndigency => new[]
            {
                "Valid ID",
                "Proof of Income / Indigency (cert from employer or unemployment)",
                "Barangay Residency (if available)"
            },
            DocumentType.BarangayClearanceEmployment => new[]
            {
                "Valid ID",
                "2 pcs passport-size photo",
                "Barangay Residency Certificate"
            },
            DocumentType.BarangayClearanceBusiness => new[]
            {
                "Valid ID",
                "DTI/Permit (if registered)",
                "Sketch of business location",
                "Proof of Residency"
            },
            DocumentType.SchoolRequirement => new[]
            {
                "Valid ID",
                "School ID / Enrollment form",
                "Proof of Residency"
            },
            _ => new[] { "Valid ID" }
        };
    }

    public static class DocumentStatusMeta
    {
        public static string Label(this DocumentStatus status) => status switch
        {
            DocumentStatus.Pending => "Pending",
            DocumentStatus.Approved => "Approved",
            DocumentStatus.Rejected => "Rejected",
            _ => status.ToString()
        };

        public static Color StatusColor(this DocumentStatus status) => status switch
        {
            DocumentStatus.Approved => Color.FromArgb(60, 130, 90),
            DocumentStatus.Pending => Color.FromArgb(214, 158, 46),
            DocumentStatus.Rejected => Color.FromArgb(200, 29, 37),
            _ => Color.FromArgb(140, 148, 160)
        };

        public static Color StatusBackColor(this DocumentStatus status) => status switch
        {
            DocumentStatus.Approved => Color.FromArgb(232, 244, 236),
            DocumentStatus.Pending => Color.FromArgb(252, 245, 229),
            DocumentStatus.Rejected => Color.FromArgb(252, 235, 236),
            _ => Color.FromArgb(240, 242, 245)
        };
    }
}
