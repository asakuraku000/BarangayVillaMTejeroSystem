# Certificate Template Tokens

All 5 Word templates in this folder now use `[BRACKETED]` placeholder tokens
instead of one hard-coded sample person. When staff click **Print** on the
Documents page, `DocumentPrinter.BuildTokenValues()` (in
`Services/DocumentPrinter.cs`) fills every token below from the selected
resident's record and the document request — no template editing needed.

| Token | Filled from | Notes |
|---|---|---|
| `[NAME]` | Resident's full name | |
| `[SEX]` | Resident's gender | "Male" / "Female" |
| `[AGE]` | Resident's birth date | **auto-computed**, never typed by staff |
| `[BIRTHDATE]` | Resident's birth date | e.g. "March 14, 1978" |
| `[BIRTHPLACE]` | Resident's Place of Birth field | new field, see below |
| `[CIVILSTATUS]` | Resident's civil status | Single / Married / Widowed / Separated |
| `[PUROK]` | Resident's Purok | just the purok, e.g. "Purok 3" |
| `[ADDRESS]` | Resident's Purok | full form: "Purok 3, Barangay Villa M. Tejero, Liloy, Zamboanga del Norte" |
| `[PURPOSE]` | The document request's Purpose field | |
| `[DATE]` | Date the document was processed/issued | spelled out, e.g. "July 26, 2026" |
| `[ISSUEDON]` | Date the document was processed/issued | numeric, e.g. "07-26-2026"; used on the "Issued On:" line of the Business Clearance |
| `[CONTROLNO]` | Auto-generated control number (BVMT-YYYY-NNNN) | |
| `[ORNO]` | O.R. number entered for the request | |
| `[CTCNO]` | CTC No. entered for the request | typed by staff exactly as-is, no "BVMT-" prefix; only shown/used for Barangay Clearance – Business |
| `[FEE]` | Fee entered for the request | kept for compatibility; no longer used on the Business Clearance layout — see `[BUSINESSFEEROWS]` below |
| `[BUSINESSTYPE]` | "Type of Business" field on the issuance form | only shown/used for Barangay Clearance – Business |
| `[BUSINESSTAX]` | "Business Tax" field on the issuance form | kept for compatibility; no longer used on the Business Clearance layout — see `[BUSINESSTAXROWS]` below |
| `[BUSINESSFEEROWS]` | `[BUSINESSTYPE]` + `[FEE]`, combined | one numbered row per business, its own Fee amount right-aligned on that same row — see "Fee alignment fix" below |
| `[BUSINESSTAXROWS]` | `[BUSINESSTYPE]` + `[BUSINESSTAX]`, combined | same idea as `[BUSINESSFEEROWS]`, paired with Business Tax instead |
| `[ISSUEDBY]` | Staff member currently logged in | |
| `[CAPTAIN]` | Punong Barangay's name on file | |
| `[REQUIREMENTS]` | Checked documentary requirements | |
| `[ALIAS]` | Resident's "also known as" name | used by Certificate of Oneness |
| `[STATUS]` | Request status | Pending / Approved / Rejected |
| `[HE/SHE]` | Resident's gender | "he" or "she" |
| `[HIM/HER]` | Resident's gender | "him" or "her" |
| `[HIS/HER]` | Resident's gender | "his" or "her" |

Tokens are matched case-insensitively and can appear anywhere, any number of
times, even if Word split them across multiple text runs.

## What changed

- **All 5 templates** (`BarangayClearance.docx`, `BarangayClearanceForBusiness.docx`,
  `CertificateOfIndigency.docx`, `CertificateOfOneness.docx`,
  `CertificateOfResidency.docx`) had their one hard-coded sample person
  swapped out for tokens, with all original letterhead, logos, tables, and
  formatting preserved untouched.
- **`BarangayClearanceForBusiness.docx`** also had an accidentally
  duplicated second "Barangay Clearance" page removed — it was pasted in
  after the actual business clearance content and printed as an extra page
  every time.
- **`Models/Resident.cs`** gained a `Birthplace` field and three pronoun
  helpers (`PronounSubject/Object/Possessive`).
- **`Data/DatabaseHelper.cs`** and **`Services/ResidentService.cs`** were
  updated so `Birthplace` is stored, migrated (existing databases get the
  new column automatically), and read back.
- **`Forms/ResidentFormDialog.cs`** got a "Place of Birth" field, and
  **`Forms/ResidentProfileDialog.cs`** displays it.
- **`Services/DocumentPrinter.cs`** now supplies all the tokens above,
  instead of just the handful it had before.

## Fee alignment fix (Business Clearance)

Previously, the Business Clearance template lined up `[BUSINESSTYPE]` and
`[FEE]` (and separately, `[BUSINESSTAX]`) using a fixed number of literal
`<w:tab/>` characters typed once into the template. That only ever worked
for exactly one business on one line — if staff listed a second business
(or the business name was long enough to wrap), the amount would drift
below the item it belonged to instead of staying lined up with it, and
Word's auto-numbering never advanced past "1." for the extra lines either.

Both rows now use one combined token — `[BUSINESSFEEROWS]` and
`[BUSINESSTAXROWS]` — built by `DocumentPrinter.BuildBusinessRows()`
in `Services/DocumentPrinter.cs`. It numbers each business ("1.", "2.", ...),
pairs it with its own amount by position, and joins them with a real tab
character. `DocxTemplateFiller` turns that into an actual Word `<w:tab/>`,
and the template paragraph now carries one real right-aligned tab stop
(`<w:tabs><w:tab w:val="right" w:pos="9360"/></w:tabs>`) instead of a stack
of default tabs. The result:

- Each business's amount lands at the right margin **on its own row**, no
  matter how many businesses are listed.
- If a business's name is long enough to wrap to a second line, its amount
  still lands at the right margin of whichever line it actually ends up on.
- If an amount is left blank for a business, that row just prints blank
  space in the amount column instead of "0.00" or throwing off the row
  below it.

`[FEE]` and `[BUSINESSTAX]` are still computed and available (in case a
future template wants a single combined total), but the Business Clearance
template itself now uses `[BUSINESSFEEROWS]`/`[BUSINESSTAXROWS]` only.

## Note

The "School Requirement" document type has been removed from the system —
it never had a real `.docx` template on file, so printing it always showed
"No Word template is configured". The 5 document types listed above are the
complete, working set.
