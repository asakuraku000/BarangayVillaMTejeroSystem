using System;
using System.Collections.Generic;
using System.Linq;

namespace BarangayVillaMTejeroSystem.Models
{
    public enum Gender
    {
        Male,
        Female
    }

    public enum CivilStatus
    {
        Single,
        Married,
        Widowed,
        Separated
    }

    /// <summary>
    /// Represents a single resident's profile record, matching the Resident
    /// Records module fields called out in the project's feature list and
    /// Class Diagram: residentID, firstName, middleName, lastName, address,
    /// birthDate, civilStatus, gender, dateRegistered — plus occupation,
    /// contact details, and household composition per Chapter 4.2.3.
    /// </summary>
    public class Resident
    {
        public int ResidentId { get; set; }

        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty; // Jr., Sr., III, etc. — optional, part of full name

        /// <summary>
        /// Optional "also known as" / maiden / previous name on record. Used by
        /// the Certificate of Oneness, which certifies that this alias and the
        /// resident's current legal name (FullName) refer to one and the same person.
        /// </summary>
        public string AliasName { get; set; } = string.Empty;

        public DateTime BirthDate { get; set; }

        /// <summary>Place of birth, as printed on the BIRTHPLACE line of the
        /// Barangay Clearance templates (e.g. "Iligan City").</summary>
        public string Birthplace { get; set; } = string.Empty;

        public Gender Gender { get; set; }
        public CivilStatus CivilStatus { get; set; }

        public string Purok { get; set; } = string.Empty;   // e.g. "Purok 3" — the resident's address
        public string ContactNo { get; set; } = string.Empty;
        public string Occupation { get; set; } = string.Empty;

        /// <summary>
        /// Household composition — names of the other people in this resident's
        /// household, per the required field in Chapter 4.2.3. One name per entry.
        /// </summary>
        public List<string> HouseholdMembers { get; set; } = new();

        /// <summary>Active = currently residing in the barangay. Inactive = moved out / deceased (kept for historical records instead of hard-deleted).</summary>
        public bool IsActive { get; set; } = true;
        public string Remarks { get; set; } = string.Empty;

        public DateTime DateRegistered { get; set; } = DateTime.Now;

        public string FullName
        {
            get
            {
                string middleInitial = string.IsNullOrWhiteSpace(MiddleName) ? "" : $" {MiddleName.Trim()[0]}.";
                string suffixPart = string.IsNullOrWhiteSpace(Suffix) ? "" : $" {Suffix.Trim()}";
                return $"{FirstName.Trim()}{middleInitial} {LastName.Trim()}{suffixPart}".Replace("  ", " ").Trim();
            }
        }

        public int Age
        {
            get
            {
                if (BirthDate == default) return 0;
                var today = DateTime.Today;
                int age = today.Year - BirthDate.Year;
                if (BirthDate.Date > today.AddYears(-age)) age--;
                return Math.Max(age, 0);
            }
        }

        public bool IsSeniorCitizen => Age >= 60;

        public string GenderLabel => Gender == Gender.Male ? "Male" : "Female";

        // ----- Pronoun helpers, used to fill the [HE/SHE] / [HIM/HER] / [HIS/HER]
        // tokens in the certificate templates so the correct pronoun is printed
        // automatically instead of leaving a manual "he/she" for staff to circle. -----
        public string PronounSubject => Gender == Gender.Male ? "he" : "she";
        public string PronounObject => Gender == Gender.Male ? "him" : "her";
        public string PronounPossessive => Gender == Gender.Male ? "his" : "her";

        public string CivilStatusLabel => CivilStatus.ToString();

        public string Initial => string.IsNullOrWhiteSpace(FirstName) ? "?" : FirstName.Trim()[0].ToString().ToUpper();

        public int HouseholdSize => 1 + HouseholdMembers.Count(m => !string.IsNullOrWhiteSpace(m));

        public string HouseholdMembersDisplay =>
            HouseholdMembers.Count(m => !string.IsNullOrWhiteSpace(m)) == 0
                ? "None on record"
                : string.Join(", ", HouseholdMembers.Where(m => !string.IsNullOrWhiteSpace(m)));
    }
}
