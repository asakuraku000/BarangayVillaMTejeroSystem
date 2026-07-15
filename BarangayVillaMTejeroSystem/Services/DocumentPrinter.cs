using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
                ["[DATE]"] = issuedDate.ToString("MMMM d, yyyy"),
                ["[CONTROLNO]"] = doc.ControlNo,
                ["[ORNO]"] = string.IsNullOrWhiteSpace(doc.OrNumber) ? "(none)" : doc.OrNumber,
                // Spelled out as "Php ###.00" explicitly (not culture-dependent
                // ToString("C2")) so it always matches the original templates'
                // wording regardless of what locale the machine running the app
                // is set to.
                ["[FEE]"] = $"Php {doc.Fee:N2}",
                ["[BUSINESSTYPE]"] = string.IsNullOrWhiteSpace(doc.BusinessType) ? "(not specified)" : doc.BusinessType,
                ["[BUSINESSTAX]"] = doc.BusinessTax.ToString("N2"),
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
    }
}
