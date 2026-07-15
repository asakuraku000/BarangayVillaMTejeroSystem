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
| `[DATE]` | Date the document was processed/issued | |
| `[CONTROLNO]` | Auto-generated control number (BVMT-YYYY-NNNN) | |
| `[ORNO]` | O.R. number entered for the request | |
| `[FEE]` | Fee entered for the request | |
| `[BUSINESSTYPE]` | "Type of Business" field on the issuance form | only shown/used for Barangay Clearance – Business |
| `[BUSINESSTAX]` | "Business Tax" field on the issuance form | separate from `[FEE]`; only shown/used for Barangay Clearance – Business |
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

## Note

`DocumentType.SchoolRequirement` still points at `SchoolRequirement.docx`,
which isn't in this Templates folder — printing that one document type will
show "No Word template is configured" until that file is added. None of the
other 6 document types are affected.
