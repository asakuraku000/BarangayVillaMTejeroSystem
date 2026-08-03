using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BarangayVillaMTejeroSystem.Models;

namespace BarangayVillaMTejeroSystem.Services
{
    /// <summary>
    /// Fills the Barangay's official Word (.docx) certificate templates by
    /// replacing placeholder tokens (e.g. [NAME], [DATE], [PURPOSE]) inside the
    /// document's XML. Works even when a token is split across multiple &lt;w:t&gt;
    /// runs (Word frequently breaks text runs mid-word), by reconstructing the
    /// concatenated text, mapping each character back to its source run, and
    /// rewriting only the run(s) the token spans.
    /// </summary>
    public static class DocxTemplateFiller
    {
        // Word sometimes writes an *empty* text run as a self-closing tag,
        // e.g. <w:t xml:space="preserve"/> (no separate closing tag) — most
        // often right next to the "_GoBack" bookmark it drops at the last
        // cursor position when a document is saved. The old pattern
        // "<w:t...>(.*?)</w:t>" doesn't have an alternative for that
        // self-closing form, so its lazy ".*?" would keep matching forward
        // past the "/>" and swallow everything up to the *next* real
        // </w:t> in the document — silently absorbing paragraph/run XML as
        // literal text, which got re-escaped into the output and corrupted
        // the .docx. The self-closing alternative below is matched first so
        // an empty run is captured on its own instead of bleeding into
        // whatever follows it.
        private static readonly Regex TextRunRegex = new Regex(
            @"<w:t(?<selfClose>\s+[^>]*)?/>|<w:t(?<attrs>\s+[^>]*)?>(?<text>.*?)</w:t>",
            RegexOptions.Singleline);

        // ----- Public API -----

        /// <summary>
        /// Copies <paramref name="templatePath"/> to a temp .docx, replaces every
        /// token from <paramref name="values"/> inside the document/header/footer
        /// XML, and returns the path to the filled temp file. The caller is
        /// responsible for opening/deleting that temp file.
        /// </summary>
        public static string FillTemplate(string templatePath, IReadOnlyDictionary<string, string> values)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"BVMT_{Guid.NewGuid():N}.docx");
            File.Copy(templatePath, tempPath, overwrite: true);

            using var zip = ZipFile.Open(tempPath, ZipArchiveMode.Update);
            foreach (var entry in zip.Entries.ToList())
            {
                if (!entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) continue;
                if (!entry.FullName.StartsWith("word/", StringComparison.OrdinalIgnoreCase)) continue;

                string name = entry.Name.ToLowerInvariant();
                if (name != "document.xml" && !name.StartsWith("header") && !name.StartsWith("footer"))
                    continue;

                string xml = ReadEntryText(entry);
                string filled = ReplaceTokens(xml, values);
                if (filled == xml) continue;

                entry.Delete();
                var newEntry = zip.CreateEntry(entry.FullName);
                using var ws = newEntry.Open();
                using var sw = new StreamWriter(ws, new UTF8Encoding(false));
                sw.Write(filled);
            }

            return tempPath;
        }

        /// <summary>
        /// Replaces every occurrence of each token (key, case-insensitive) with
        /// its value. Tokens may be split across multiple &lt;w:t&gt; runs.
        /// </summary>
        public static string ReplaceTokens(string xml, IReadOnlyDictionary<string, string> values)
        {
            if (values == null || values.Count == 0) return xml;

            var matches = TextRunRegex.Matches(xml).Cast<Match>().ToList();
            if (matches.Count == 0) return xml;

            // Work with the *unescaped* text of each run so tokens and values
            // compare against real characters (and so we don't double-escape).
            var texts = matches.Select(m => UnescapeXml(m.Groups["text"].Value)).ToList();

            foreach (var kvp in values)
            {
                string token = kvp.Key;
                string val = kvp.Value ?? "";
                if (string.IsNullOrEmpty(token)) continue;

                // A token can appear multiple times; keep scanning until none left.
                while (true)
                {
                    var map = new List<(int node, int local)>();
                    var sb = new StringBuilder();
                    for (int i = 0; i < texts.Count; i++)
                    {
                        string t = texts[i];
                        for (int j = 0; j < t.Length; j++)
                            map.Add((i, j));
                        sb.Append(t);
                    }

                    string full = sb.ToString();
                    int idx = full.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                    if (idx < 0) break;

                    int end = idx + token.Length;
                    int firstNode = map[idx].node;
                    int firstLocal = map[idx].local;
                    int lastNode = map[end - 1].node;
                    int lastLocal = map[end - 1].local;

                    if (firstNode == lastNode)
                    {
                        string t = texts[firstNode];
                        texts[firstNode] = t.Substring(0, firstLocal) + val + t.Substring(lastLocal + 1);
                    }
                    else
                    {
                        string ft = texts[firstNode];
                        string lt = texts[lastNode];
                        texts[firstNode] = ft.Substring(0, firstLocal) + val + lt.Substring(lastLocal + 1);
                        texts[lastNode] = "";
                        for (int n = firstNode + 1; n < lastNode; n++) texts[n] = "";
                    }
                }
            }

            // Rebuild the XML, substituting each run's (re-escaped) text. Some
            // tokens (e.g. a Fee/BusinessType/BusinessTax list with more than one
            // business, one per line) can now contain embedded newlines and tabs.
            // Word does not treat a literal "\n" or "\t" character inside <w:t> as
            // a line break or a tab stop, so each line becomes its own <w:t> with
            // a real <w:br/> between them, and each tab-separated column within a
            // line becomes its own <w:t> with a real <w:tab/> between them — all
            // still inside the same <w:r> run, so it keeps that run's original
            // formatting (font, size, bold, etc.). This is what lets a token's
            // value carry its own per-row "label <TAB> amount" column instead of
            // depending on a fixed number of literal <w:tab/> elements baked into
            // the template, which only ever lined up for exactly one row.
            var result = new StringBuilder();
            int cursor = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                Match m = matches[i];
                result.Append(xml, cursor, m.Index - cursor);
                bool wasSelfClosing = m.Groups["selfClose"].Success;
                string attrs = wasSelfClosing ? m.Groups["selfClose"].Value : m.Groups["attrs"].Value; // e.g. " xml:space=\"preserve\""
                string text = texts[i];

                if (text.Length == 0 && wasSelfClosing)
                {
                    // Untouched empty run — keep its original self-closing form.
                    result.Append($"<w:t{attrs}/>");
                }
                else if (text.IndexOf('\n') >= 0 || text.IndexOf('\t') >= 0)
                {
                    string[] lines = text.Replace("\r\n", "\n").Split('\n');
                    for (int li = 0; li < lines.Length; li++)
                    {
                        if (li > 0) result.Append("<w:br/>");
                        string[] cells = lines[li].Split('\t');
                        for (int ci = 0; ci < cells.Length; ci++)
                        {
                            if (ci > 0) result.Append("<w:tab/>");
                            result.Append($"<w:t{attrs}>{EscapeXml(cells[ci])}</w:t>");
                        }
                    }
                }
                else
                {
                    result.Append($"<w:t{attrs}>{EscapeXml(text)}</w:t>");
                }

                cursor = m.Index + m.Length;
            }
            result.Append(xml, cursor, xml.Length - cursor);
            return result.ToString();
        }

        // ----- Template (re)generation helpers -----

        /// <summary>
        /// Builds the placeholder body (paragraphs only) for a document type.
        /// Used by the one-time template generator to turn the static sample
        /// .docx files into mail-merge templates containing [TOKENS].
        /// </summary>
        public static string BuildPlaceholderBody(DocumentType type)
        {
            var lines = new List<(string text, bool bold, string align, int size, bool italic, bool rule)>();

            void Add(string text, bool bold = false, string align = null, int size = 22, bool italic = false, bool rule = false)
                => lines.Add((text, bold, align, size, italic, rule));

            // ----- Branding header -----
            Add("Republic of the Philippines", align: "center", size: 18);
            Add("PROVINCE OF ZAMBOANGA DEL NORTE", bold: true, align: "center", size: 18);
            Add("Municipality of Liloy", align: "center", size: 18);
            Add("BARANGAY VILLA M. TEJERO", bold: true, align: "center", size: 22);
            Add("OFFICE OF THE PUNONG BARANGAY", align: "center", size: 18);
            Add("", rule: true);

            // ----- Certificate title -----
            Add(type.CertificateTitle(), bold: true, align: "center", size: 30);

            // ----- Body -----
            Add("TO WHOM IT MAY CONCERN:", bold: true);

            switch (type)
            {
                case DocumentType.CertificateOfResidency:
                    Add("This is to certify that [NAME], [PERSONAL], is a BONA FIDE RESIDENT of [ADDRESS].");
                    Add("He/She has been residing in this barangay and is known to be a person of good moral character and a law-abiding citizen.");
                    Add("This certification is issued upon the request of the above-named person for [PURPOSE].");
                    break;
                case DocumentType.CertificateOfIndigency:
                    Add("This is to certify that [NAME], [PERSONAL], is a resident of [ADDRESS].");
                    Add("He/She belongs to a LOW-INCOME / INDIGENT family based on their social and economic standing in the community.");
                    Add("This certification is issued upon the request of the above-named person for [PURPOSE].");
                    break;
                case DocumentType.BarangayClearanceEmployment:
                    Add("This is to certify that [NAME], [PERSONAL], is a resident of [ADDRESS].");
                    Add("He/She is hereby CLEARED for EMPLOYMENT purposes, having no derogatory record or pending case filed against him/her in this barangay.");
                    Add("This clearance is issued upon the request of the above-named person for [PURPOSE].");
                    break;
                case DocumentType.BarangayClearanceBusiness:
                    Add("This is to certify that [NAME], [PERSONAL], is a resident of [ADDRESS].");
                    Add("He/She is hereby CLEARED to operate / engage in the stated business, having complied with the barangay's requirements and having no pending case or complaint filed against him/her in this barangay.");
                    Add("This clearance is issued upon the request of the above-named person for [PURPOSE].");
                    break;
                case DocumentType.CertificateOfOneness:
                    Add("This is to certify that [NAME], [PERSONAL], a bona fide resident of [ADDRESS], and [ALIAS], refer to one and the same person.", italic: true);
                    Add("This certification is being issued upon the request of the above-named person for [PURPOSE].");
                    break;
            }

            Add("Documentary requirements verified: [REQUIREMENTS]");
            Add("Given this [DATE] at the Office of the Punong Barangay, Villa M. Tejero, Liloy, Zamboanga del Norte, Philippines.", italic: true);

            // ----- Footer / details -----
            Add("Control No.: [CONTROLNO]      O.R. No.: [ORNO]      Fee: [FEE]");
            Add("Issued by: [ISSUEDBY]");
            Add("");
            Add("[CAPTAIN]", bold: true, align: "center");
            Add("Punong Barangay", align: "center");

            var body = new StringBuilder();
            foreach (var l in lines)
                body.Append(Paragraph(l.text, l.bold, l.align, l.size, l.italic, l.rule));
            return body.ToString();
        }

        /// <summary>
        /// Replaces the entire body of a document.xml with <paramref name="newBody"/>
        /// while preserving the &lt;w:document&gt; root and the trailing
        /// &lt;w:sectPr&gt; (page setup). Used by the template generator.
        /// </summary>
        public static string RebuildDocumentXml(string documentXml, string newBody)
        {
            int bodyOpen = documentXml.IndexOf("<w:body>", StringComparison.OrdinalIgnoreCase);
            if (bodyOpen < 0) return documentXml;

            string rootOpen = documentXml.Substring(0, bodyOpen); // includes <?xml?> and <w:document ...>
            int sectStart = documentXml.IndexOf("<w:sectPr", bodyOpen, StringComparison.OrdinalIgnoreCase);
            string sectPr = "";
            if (sectStart >= 0)
            {
                int sectEnd = documentXml.IndexOf("</w:sectPr>", sectStart, StringComparison.OrdinalIgnoreCase);
                if (sectEnd >= 0)
                    sectPr = documentXml.Substring(sectStart, sectEnd + "</w:sectPr>".Length - sectStart);
            }

            return $"{rootOpen}<w:body>{newBody}{sectPr}</w:body></w:document>";
        }

        // ----- Low-level XML helpers -----

        private static string Paragraph(string text, bool bold, string align, int size, bool italic, bool rule)
        {
            var pPr = new StringBuilder();
            if (align != null) pPr.Append($"<w:jc w:val=\"{align}\"/>");
            if (rule)
                pPr.Append("<w:pBdr><w:bottom w:val=\"single\" w:sz=\"6\" w:space=\"1\" w:color=\"C81D25\"/></w:pBdr>");
            if (pPr.Length > 0) pPr.Insert(0, "<w:pPr>").Append("</w:pPr>");

            var rPr = new StringBuilder();
            if (bold) rPr.Append("<w:b/>");
            if (italic) rPr.Append("<w:i/>");
            rPr.Append($"<w:sz w:val=\"{size}\"/><w:szCs w:val=\"{size}\"/>");
            rPr.Insert(0, "<w:rPr>").Append("</w:rPr>");

            return $"<w:p>{pPr}<w:r>{rPr}<w:t xml:space=\"preserve\">{EscapeXml(text)}</w:t></w:r></w:p>";
        }

        private static string ReadEntryText(ZipArchiveEntry entry)
        {
            using var rs = entry.Open();
            using var sr = new StreamReader(rs, Encoding.UTF8);
            return sr.ReadToEnd();
        }

        private static string EscapeXml(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                    .Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        private static string UnescapeXml(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("&lt;", "<").Replace("&gt;", ">")
                    .Replace("&quot;", "\"").Replace("&apos;", "'")
                    .Replace("&amp;", "&");
        }
    }
}
