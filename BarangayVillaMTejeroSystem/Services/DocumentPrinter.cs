using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BarangayVillaMTejeroSystem.Models;

namespace BarangayVillaMTejeroSystem.Services
{
    /// <summary>
    /// Produces a printable BarangayDocument by filling the matching official
    /// Word (.docx) template with the resident's details and opening the
    /// resulting file (so staff can print / save it from Word or LibreOffice).
    /// Resident details are pulled live from the Resident object — nothing is
    /// re-encoded. Placeholder tokens in the template are replaced via
    /// DocxTemplateFiller.
    /// </summary>
    public static class DocumentPrinter
    {
        public static void Print(BarangayDocument doc, Resident resident, string issuedByName = "", string captainName = "")
        {
            if (resident == null)
            {
                MessageBox.Show("No resident selected for this document.", "Cannot Print",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string template = ResolveTemplatePath(doc.DocumentType);
            if (template == null)
            {
                MessageBox.Show("No Word template is configured for this document type.", "Cannot Print",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var values = BuildTokenValues(doc, resident, issuedByName, captainName);

            string filledPath;
            try
            {
                filledPath = DocxTemplateFiller.FillTemplate(template, values);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Couldn't generate the document:\n{ex.Message}", "Cannot Print",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(filledPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The document was generated but couldn't be opened:\n{ex.Message}\n\nIt was saved at:\n{filledPath}",
                    "Cannot Open", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static string ResolveTemplatePath(DocumentType type)
        {
            string fileName = type.TemplateFileName();
            if (fileName == null) return null;
            string path = Path.Combine(AppContext.BaseDirectory, "Templates", fileName);
            return File.Exists(path) ? path : null;
        }

        /// <summary>
        /// Maps a document + resident onto the placeholder tokens the templates
        /// use. Keys are matched case-insensitively by DocxTemplateFiller.
        /// </summary>
        private static IReadOnlyDictionary<string, string> BuildTokenValues(
            BarangayDocument doc, Resident r, string issuedByName, string captainName)
        {
            DateTime issuedDate = doc.DateProcessed ?? DateTime.Now;

            // Staff sometimes type just the number ("2") and sometimes type the
            // whole phrase ("Purok 2") into the resident's Purok field. Strip a
            // leading "Purok" word (if present) before we prepend our own, so the
            // printed address is always "Purok 2, Barangay ..." either way,
            // instead of doubling up into "Purok Purok 2, Barangay ...".
            string purokValue = (r.Purok ?? "").Trim();
            if (purokValue.StartsWith("purok", StringComparison.OrdinalIgnoreCase))
                purokValue = purokValue.Substring(5).TrimStart('.', '-', ':', ' ');

            string address = string.IsNullOrWhiteSpace(purokValue)
                ? "Barangay Villa M. Tejero, Liloy, Zamboanga del Norte"
                : $"Purok {purokValue}, Barangay Villa M. Tejero, Liloy, Zamboanga del Norte";

            string personal = $"{r.Age} years old, {r.GenderLabel}, {r.CivilStatusLabel}";

            string requirements = doc.Requirements.Count == 0
                ? "None checked"
                : string.Join(" • ", doc.Requirements);

            string alias = string.IsNullOrWhiteSpace(r.AliasName)
                ? "(no alias / also-known-as name on file)"
                : r.AliasName.Trim();

            // The two Barangay Clearance templates lay everything out like an ID
            // card (NAME / SEX / CIVIL STATUS / ADDRESS all as caps values next to
            // their labels) — that was the original sample documents' convention.
            // The prose certificates (Residency/Indigency/Oneness) only ever put
            // the resident's own NAME in caps, and read the civil status/address
            // as normal sentence case in a sentence. Match whichever convention
            // this particular document type used originally.
            bool idCardStyle = doc.DocumentType == DocumentType.BarangayClearanceEmployment
                             || doc.DocumentType == DocumentType.BarangayClearanceBusiness;

            string civilStatusValue = idCardStyle ? r.CivilStatusLabel.ToUpperInvariant() : r.CivilStatusLabel;
            string addressValue = idCardStyle ? address.ToUpperInvariant() : address;
            string purokDisplay = string.IsNullOrWhiteSpace(purokValue)
                ? "(not on file)"
                : (idCardStyle ? purokValue.ToUpperInvariant() : purokValue);

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // A resident's or official's own name is always printed in caps —
                // every original sample document did this consistently, whether
                // the name was stored in the database as "Rosa V. Fernandez" or
                // "ROSA V. FERNANDEZ".
                ["[NAME]"] = r.FullName.ToUpperInvariant(),
                ["[PERSONAL]"] = personal,
                ["[ADDRESS]"] = addressValue,
                ["[PUROK]"] = purokDisplay,
                // Only ever printed on the ID-card-style clearance templates, so
                // it's always caps there, matching "MALE" / "FEMALE" originally.
                ["[SEX]"] = r.GenderLabel.ToUpperInvariant(),
                // Age is never typed in by staff — it's always derived from the
                // resident's birth date (Resident.Age), so it can't drift out of
                // sync with what's on file the way a manually-entered age could.
                ["[AGE]"] = r.Age.ToString(),
                ["[BIRTHDATE]"] = r.BirthDate == default ? "(not on file)" : r.BirthDate.ToString("MMMM d, yyyy").ToUpperInvariant(),
                ["[BIRTHPLACE]"] = string.IsNullOrWhiteSpace(r.Birthplace) ? "(not on file)" : r.Birthplace.ToUpperInvariant(),
                ["[CIVILSTATUS]"] = civilStatusValue,
                ["[PURPOSE]"] = string.IsNullOrWhiteSpace(doc.Purpose) ? "(not specified)" : doc.Purpose,
                ["[DATE]"] = FormatGivenThisDate(issuedDate),
                // Numeric "Issued On:" date (e.g. "07-26-2026"), as opposed to
                // [DATE] above which is spelled out in words — the two are printed
                // in different spots on the same certificate.
                ["[ISSUEDON]"] = issuedDate.ToString("MM-dd-yyyy"),
                ["[CONTROLNO]"] = doc.ControlNo,
                ["[ORNO]"] = string.IsNullOrWhiteSpace(doc.OrNumber) ? "(none)" : doc.OrNumber,
                // Community Tax Certificate number, a.k.a. "Res. Cert. No." on
                // the two Barangay Clearance templates (Employment and
                // Business) — typed in by staff exactly as it appears on the
                // resident's Cedula, never auto-generated with a "BVMT-"
                // prefix the way ControlNo is.
                ["[CTCNO]"] = string.IsNullOrWhiteSpace(doc.CtcNo) ? "(none)" : doc.CtcNo,
                // Spelled out as "Php ###.00" explicitly (not culture-dependent
                // ToString("C2")) so it always matches the original templates'
                // wording regardless of what locale the machine running the app
                // is set to. Fee/BusinessTax can now have more than one line (one
                // amount per business, e.g. a clearance covering both a sari-sari
                // store and fermented liquor) — each line is formatted separately
                // and printed as a real line break in the Word document.
                ["[FEE]"] = FormatAmountLines(doc.Fee, "Php "),
                ["[BUSINESSTYPE]"] = string.IsNullOrWhiteSpace(doc.BusinessType) ? "(not specified)" : doc.BusinessType,
                ["[BUSINESSTAX]"] = FormatAmountLines(doc.BusinessTax),
                // Combined, row-per-line tokens for the Business Clearance
                // template: each pairs a business type with its matching amount
                // (by position — line 1 with line 1, line 2 with line 2, ...),
                // separated by a literal tab. The template gives that tab a
                // single right-aligned tab stop, so no matter how many
                // businesses are listed, or how long a business's name is, its
                // own amount lands at the right margin on its own line instead
                // of drifting down onto a line by itself the way a shared, fixed
                // number of tabs did.
                ["[BUSINESSFEEROWS]"] = BuildBusinessRows(doc.BusinessType, doc.Fee, "Php "),
                ["[BUSINESSTAXROWS]"] = BuildBusinessRows(doc.BusinessType, doc.BusinessTax, "Php "),
                ["[ISSUEDBY]"] = string.IsNullOrWhiteSpace(issuedByName) ? "(Barangay Staff)" : issuedByName,
                ["[CAPTAIN]"] = string.IsNullOrWhiteSpace(captainName) ? "BARANGAY CAPTAIN" : captainName.ToUpperInvariant(),
                ["[REQUIREMENTS]"] = requirements,
                ["[ALIAS]"] = alias.StartsWith("(") ? alias : alias.ToUpperInvariant(),
                ["[STATUS]"] = doc.Status.Label(),

                // Pronoun tokens — the template sentence itself never hard-codes
                // "he/she"; whichever pronoun is correct for the resident's
                // gender on file is substituted in automatically at print time.
                ["[HE/SHE]"] = r.PronounSubject,
                ["[HIM/HER]"] = r.PronounObject,
                ["[HIS/HER]"] = r.PronounPossessive
            };
        }

        /// <summary>
        /// Formats the "Given/Issued this ___" date the way every original
        /// sample certificate wrote it — an ordinal day number spelled out
        /// against the month and year (e.g. "7th day of July, 2026"), instead
        /// of the plain "July 7, 2026" a straight MMMM-d-yyyy format gives.
        /// Only feeds [DATE] (the sentence inside the certificate body); the
        /// numeric "Issued On: [ISSUEDON]" stamp elsewhere on the same
        /// certificate is unrelated and keeps its own MM-dd-yyyy format.
        /// </summary>
        private static string FormatGivenThisDate(DateTime date)
            => $"{date.Day}{OrdinalSuffix(date.Day)} day of {date:MMMM}, {date:yyyy}";

        /// <summary>
        /// "st"/"nd"/"rd"/"th" for a day-of-month number. The 11th–13th are
        /// always "th" (never "11st", "12nd", "13rd"); every other day goes
        /// by its last digit.
        /// </summary>
        private static string OrdinalSuffix(int day)
        {
            if (day % 100 is >= 11 and <= 13) return "th";
            return (day % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            };
        }

        /// <summary>
        /// Splits a Fee/BusinessTax value into individual lines (staff enter one
        /// amount per line — one per business — pressing Enter between each) and
        /// formats every numeric-looking line as currency (e.g. "Php 100.00"),
        /// leaving any non-numeric line printed exactly as typed. A single-line
        /// value (the common case) is simply formatted on its own.
        /// </summary>
        private static string FormatAmountLines(string raw, string prefix = "")
        {
            if (string.IsNullOrWhiteSpace(raw)) return $"{prefix}0.00";

            var lines = raw.Replace("\r\n", "\n").Split('\n');
            var formatted = lines.Select(line =>
            {
                string t = line.Trim();
                if (t.Length == 0) return t;
                return decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount)
                    ? $"{prefix}{amount:N2}"
                    : t;
            });
            return string.Join("\n", formatted);
        }

        /// <summary>
        /// Pairs each "Type of Business" line with its matching amount line by
        /// position (line 1 with line 1, line 2 with line 2, ...), numbers every
        /// row ("1.", "2.", ...), and joins the type and its amount with a
        /// literal tab character. <see cref="DocxTemplateFiller"/> turns that
        /// tab into a real Word tab-stop jump, so — paired with a single
        /// right-aligned tab stop set on the template's paragraph — every row's
        /// amount lands at the right margin on its own line, regardless of how
        /// many rows there are or how long any one business name is.
        ///
        /// If the two lists are different lengths (e.g. staff typed a business
        /// but forgot its amount, or vice versa), the missing side of that row
        /// is left blank rather than defaulting to "0.00" — a blank amount is
        /// obviously incomplete; a "0.00" would silently read as free.
        /// </summary>
        private static string BuildBusinessRows(string businessTypeRaw, string amountRaw, string prefix)
        {
            string[] types = SplitNonEmptyLines(businessTypeRaw);
            if (types.Length == 0) types = new[] { "(not specified)" };

            // FormatAmountLines forces a default "0.00" for a wholly blank input,
            // which is right for a single amount but wrong here — a business
            // with no amount typed at all should stay blank, not "Php 0.00".
            string[] amounts = string.IsNullOrWhiteSpace(amountRaw)
                ? Array.Empty<string>()
                : FormatAmountLines(amountRaw, prefix).Replace("\r\n", "\n").Split('\n');

            int rows = Math.Max(types.Length, amounts.Length);
            var sb = new StringBuilder();
            for (int i = 0; i < rows; i++)
            {
                string type = i < types.Length ? types[i] : "";
                string amount = i < amounts.Length ? amounts[i] : "";
                if (i > 0) sb.Append('\n');
                sb.Append(rows > 1 ? $"{i + 1}. {type}" : type);
                sb.Append('\t');
                sb.Append(amount);
            }
            return sb.ToString();
        }

        /// <summary>Splits a multi-line, one-per-line field into trimmed, non-blank lines.</summary>
        private static string[] SplitNonEmptyLines(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
            return raw.Replace("\r\n", "\n").Split('\n')
                       .Select(l => l.Trim())
                       .Where(l => l.Length > 0)
                       .ToArray();
        }
    }
}
