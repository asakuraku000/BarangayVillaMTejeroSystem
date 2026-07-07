using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using BarangayVillaMTejeroSystem.Models;

namespace BarangayVillaMTejeroSystem.Services
{
    /// <summary>
    /// Renders a BarangayDocument as a printable certificate and shows a
    /// print-preview dialog. The resident's details are pulled live from the
    /// passed Resident object so nothing is re-encoded here.
    /// </summary>
    public static class DocumentPrinter
    {
        private static readonly string BarangayName = "BARANGAY VILLA M. TEJERO";
        private static readonly string BarangayLocation = "Municipality of Liloy, Province of Zamboanga del Norte";

        public static void Print(BarangayDocument doc, Resident resident, string issuedByName = "", string captainName = "")
        {
            if (resident == null)
            {
                MessageBox.Show("No resident selected for this document.", "Cannot Print",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var printDoc = new PrintDocument();
            printDoc.DocumentName = $"{doc.CertificateTitleText()} - {doc.ControlNo}";
            printDoc.DefaultPageSettings.Margins = new Margins(70, 70, 60, 60);
            printDoc.DefaultPageSettings.PaperSize = new PaperSize("Letter", 850, 1100);

            printDoc.PrintPage += (_, e) =>
                DrawCertificate(e, doc, resident, issuedByName, captainName);

            using var preview = new PrintPreviewDialog
            {
                Document = printDoc,
                WindowState = FormWindowState.Maximized,
                StartPosition = FormStartPosition.CenterScreen
            };
            preview.ShowDialog();
        }

        private static string CertificateTitleText(this BarangayDocument doc)
            => doc.DocumentType.CertificateTitle();

        private static void DrawCertificate(PrintPageEventArgs e, BarangayDocument doc, Resident resident,
            string issuedByName, string captainName)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int pageW = e.MarginBounds.Width;
            int left = e.MarginBounds.Left;
            int top = e.MarginBounds.Top;
            int centerX = left + pageW / 2;

            // ----- Logo -----
            string logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "logo.png");
            if (File.Exists(logoPath))
            {
                try
                {
                    using var img = Image.FromFile(logoPath);
                    int logoSize = 78;
                    g.DrawImage(img, centerX - logoSize / 2, top, logoSize, logoSize);
                }
                catch { /* ignore missing/bad logo */ }
            }

            int y = top + 88;

            // ----- Barangay header -----
            y = DrawCentered(g, BarangayName, left, pageW, y, new Font("Segoe UI", 16, FontStyle.Bold), Color.FromArgb(16, 37, 66));
            y = DrawCentered(g, BarangayLocation, left, pageW, y + 2, new Font("Segoe UI", 9, FontStyle.Regular), Color.FromArgb(90, 100, 115));

            // ----- Rule under header -----
            y += 8;
            using (var pen = new Pen(Color.FromArgb(200, 29, 37), 1.5f))
                g.DrawLine(pen, left, y, left + pageW, y);
            y += 22;

            // ----- Certificate title -----
            y = DrawCentered(g, doc.DocumentType.CertificateTitle(), left, pageW, y,
                new Font("Segoe UI", 18, FontStyle.Bold), Color.FromArgb(16, 37, 66));

            // ----- Body -----
            y += 26;
            var bodyFont = new Font("Segoe UI", 11, FontStyle.Regular);
            var indent = left + 30;

            int bodyW = pageW - 60;
            foreach (var line in BuildBodyLines(doc, resident))
            {
                y = DrawWrapped(g, line, indent, bodyW, y, bodyFont, Color.FromArgb(40, 45, 55),
                    line.StartsWith("  •") ? 14 : 6);
            }

            // ----- Verification note -----
            y += 14;
            var noteFont = new Font("Segoe UI", 9.5f, FontStyle.Italic);
            string verification = doc.ResidencyVerified
                ? "VERIFICATION: Residency and documentary requirements were checked and confirmed by the issuing officer."
                : "VERIFICATION: Residency / documentary requirements NOT yet verified.";
            y = DrawWrapped(g, verification, indent, bodyW, y, noteFont, Color.FromArgb(120, 60, 60));

            // ----- Purpose (if any) -----
            if (!string.IsNullOrWhiteSpace(doc.Purpose))
            {
                y += 10;
                y = DrawWrapped(g, $"Purpose: {doc.Purpose}", indent, bodyW, y, bodyFont, Color.FromArgb(40, 45, 55));
            }

            // ----- Footer: control / OR / date -----
            int footerY = e.MarginBounds.Bottom - 150;
            if (footerY < y + 20) footerY = y + 20;

            var small = new Font("Segoe UI", 9f, FontStyle.Regular);
            g.DrawString($"Control No.: {doc.ControlNo}", small, Brushes.Black, left, footerY);
            if (!string.IsNullOrWhiteSpace(doc.OrNumber))
                g.DrawString($"O.R. No.: {doc.OrNumber}", small, Brushes.Black, left, footerY + 16);
            g.DrawString($"Date Issued: {doc.DateRequested: MMMM d, yyyy}", small, Brushes.Black, left, footerY + 32);
            if (doc.Fee > 0)
                g.DrawString($"Fee: ₱{doc.Fee:N2}", small, Brushes.Black, left, footerY + 48);

            // ----- Signature blocks -----
            int signY = footerY + 70;
            int half = pageW / 2;
            var signFont = new Font("Segoe UI", 10f, FontStyle.Regular);

            g.DrawLine(Pens.Black, left + 10, signY, left + half - 30, signY);
            DrawCentered(g, issuedByName, left, half, signY + 4, new Font("Segoe UI", 9, FontStyle.Bold), Color.Black);
            DrawCentered(g, "Issued by (Barangay Staff)", left, half, signY + 20, new Font("Segoe UI", 8.5f), Color.FromArgb(90, 100, 115));

            g.DrawLine(Pens.Black, left + half + 20, signY, left + pageW - 10, signY);
            DrawCentered(g, captainName, left + half, half, signY + 4, new Font("Segoe UI", 9, FontStyle.Bold), Color.Black);
            DrawCentered(g, "Certified by (Barangay Captain)", left + half, half, signY + 20, new Font("Segoe UI", 8.5f), Color.FromArgb(90, 100, 115));

            // ----- Status stamp -----
            if (doc.Status != DocumentStatus.Approved)
            {
                var stampFont = new Font("Segoe UI", 14, FontStyle.Bold);
                var stampColor = doc.Status == DocumentStatus.Rejected
                    ? Color.FromArgb(200, 29, 37) : Color.FromArgb(214, 158, 46);
                var stampText = doc.Status == DocumentStatus.Rejected ? "REJECTED" : "PENDING";
                var size = g.MeasureString(stampText, stampFont);
                using var stampBrush = new SolidBrush(Color.FromArgb(120, stampColor.R, stampColor.G, stampColor.B));
                g.DrawString(stampText, stampFont, stampBrush,
                    left + pageW - size.Width, top + 20);
            }

            e.HasMorePages = false;
        }

        private static string[] BuildBodyLines(BarangayDocument doc, Resident r)
        {
            string fullName = r.FullName;
            string address = string.IsNullOrWhiteSpace(r.Purok) ? "this Barangay" : $"Purok {r.Purok}, Barangay Villa M. Tejero";
            string ageLine = $"{r.Age} years old, {r.GenderLabel}, {r.CivilStatusLabel}";

            var lines = new System.Collections.Generic.List<string>
            {
                "TO WHOM IT MAY CONCERN:"
            };

            switch (doc.DocumentType)
            {
                case DocumentType.CertificateOfResidency:
                    lines.Add($"This is to certify that {fullName}, {ageLine}, is a BONA FIDE RESIDENT of {address}.");
                    lines.Add($"He/She has been residing in this barangay since {r.DateRegistered:yyyy} and is known to be a person of good moral character and law-abiding citizen.");
                    lines.Add("This certification is issued upon the request of the above-named person for whatever legal purpose it may serve.");
                    break;
                case DocumentType.CertificateOfIndigency:
                    lines.Add($"This is to certify that {fullName}, {ageLine}, is a resident of {address}.");
                    lines.Add($"He/She belongs to a LOW-INCOME / INDIGENT family and is qualified to avail of the assistance / benefits stated in the purpose of this certificate.");
                    lines.Add("This certification is issued upon the request of the above-named person for whatever legal purpose it may serve.");
                    break;
                case DocumentType.BarangayClearanceEmployment:
                    lines.Add($"This is to certify that {fullName}, {ageLine}, is a resident of {address}.");
                    lines.Add("He/She is hereby CLEARED for EMPLOYMENT purposes, having no derogatory record or pending case filed against him/her in this barangay.");
                    lines.Add("This clearance is issued upon the request of the above-named person for whatever legal purpose it may serve.");
                    break;
                case DocumentType.BarangayClearanceBusiness:
                    lines.Add($"This is to certify that {fullName}, {ageLine}, is a resident of {address}.");
                    lines.Add("He/She is hereby CLEARED to operate / engage in the stated business, having complied with the barangay's requirements and having no pending case or complaint filed against him/her in this barangay.");
                    lines.Add("This clearance is issued upon the request of the above-named person for whatever legal purpose it may serve.");
                    break;
                case DocumentType.SchoolRequirement:
                    lines.Add($"This is to certify that {fullName}, {ageLine}, is a resident of {address}.");
                    lines.Add("He/She is a member of this barangay and this certification is issued in support of his/her school / scholarship requirement as stated in the purpose of this document.");
                    lines.Add("This certification is issued upon the request of the above-named person for whatever legal purpose it may serve.");
                    break;
            }

            if (doc.Requirements.Count > 0)
            {
                lines.Add("");
                lines.Add("Documentary requirements verified:");
                foreach (var req in doc.Requirements)
                    lines.Add($"  • {req}");
            }

            return lines.ToArray();
        }

        // ----- low-level text helpers -----

        private static int DrawCentered(Graphics g, string text, int left, int width, int y, Font font, Color color)
        {
            var size = g.MeasureString(text, font);
            float x = left + (width - size.Width) / 2;
            g.DrawString(text, font, new SolidBrush(color), x, y);
            return y + (int)size.Height;
        }

        private static int DrawWrapped(Graphics g, string text, int x, int width, int y, Font font, Color color, int lineGap = 6)
        {
            // DrawString wraps automatically to the given width. The measured
            // height already accounts for the number of wrapped lines.
            var size = g.MeasureString(text, font, width);
            g.DrawString(text, font, new SolidBrush(color), new RectangleF(x, y, width, size.Height));
            return y + (int)Math.Ceiling(size.Height) + lineGap;
        }
    }
}
