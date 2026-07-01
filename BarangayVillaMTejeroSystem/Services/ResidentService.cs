using System;
using System.Collections.Generic;
using System.Linq;
using BarangayVillaMTejeroSystem.Models;

namespace BarangayVillaMTejeroSystem.Services
{
    /// <summary>
    /// Handles resident profile records for the barangay. As with
    /// UserService, this phase keeps everything in-memory (seeded with
    /// sample residents) so the module is fully demonstrable before the
    /// Microsoft Access-backed persistence layer is wired in. Swap the
    /// in-memory list for OleDb queries against the .accdb "Residents"
    /// table when that phase begins, keeping the same method signatures.
    /// </summary>
    public static class ResidentService
    {
        private static readonly List<Resident> _residents = new()
        {
            new Resident
            {
                ResidentId = 1, LastName = "Dela Cruz", FirstName = "Juan", MiddleName = "Santos",
                BirthDate = new DateTime(1978, 3, 14), Gender = Gender.Male, CivilStatus = CivilStatus.Married,
                Purok = "Purok 1", ContactNo = "0917-100-0001", Occupation = "Barangay Captain",
                HouseholdMembers = new List<string> { "Maria Dela Cruz (spouse)", "Jenny Dela Cruz (daughter)" },
                DateRegistered = new DateTime(2019, 1, 15)
            },
            new Resident
            {
                ResidentId = 2, LastName = "Santos", FirstName = "Maria", MiddleName = "Reyes",
                BirthDate = new DateTime(1985, 7, 22), Gender = Gender.Female, CivilStatus = CivilStatus.Married,
                Purok = "Purok 2", ContactNo = "0917-100-0002", Occupation = "Teacher",
                HouseholdMembers = new List<string> { "Carlos Santos (spouse)" },
                DateRegistered = new DateTime(2019, 2, 3)
            },
            new Resident
            {
                ResidentId = 3, LastName = "Reyes", FirstName = "Pedro", MiddleName = "Garcia",
                BirthDate = new DateTime(1960, 11, 5), Gender = Gender.Male, CivilStatus = CivilStatus.Widowed,
                Purok = "Purok 3", ContactNo = "0917-100-0003", Occupation = "Retired",
                DateRegistered = new DateTime(2019, 2, 20)
            },
            new Resident
            {
                ResidentId = 4, LastName = "Garcia", FirstName = "Ana", MiddleName = "Lopez",
                BirthDate = new DateTime(2001, 4, 9), Gender = Gender.Female, CivilStatus = CivilStatus.Single,
                Purok = "Purok 1", ContactNo = "0917-100-0004", Occupation = "Student",
                HouseholdMembers = new List<string> { "Juan Dela Cruz (father)", "Maria Dela Cruz (mother)" },
                DateRegistered = new DateTime(2020, 6, 11)
            },
            new Resident
            {
                ResidentId = 5, LastName = "Lopez", FirstName = "Jose", MiddleName = "Fernandez", Suffix = "Jr.",
                BirthDate = new DateTime(1995, 9, 30), Gender = Gender.Male, CivilStatus = CivilStatus.Single,
                Purok = "Purok 4", ContactNo = "0917-100-0005", Occupation = "Driver",
                DateRegistered = new DateTime(2020, 9, 2)
            },
            new Resident
            {
                ResidentId = 6, LastName = "Fernandez", FirstName = "Rosa", MiddleName = "Villanueva",
                BirthDate = new DateTime(1954, 1, 18), Gender = Gender.Female, CivilStatus = CivilStatus.Widowed,
                Purok = "Purok 2", ContactNo = "0917-100-0006", Occupation = "None",
                DateRegistered = new DateTime(2019, 4, 8)
            },
            new Resident
            {
                ResidentId = 7, LastName = "Villanueva", FirstName = "Mark", MiddleName = "Torres",
                BirthDate = new DateTime(1990, 12, 2), Gender = Gender.Male, CivilStatus = CivilStatus.Married,
                Purok = "Purok 5", ContactNo = "0917-100-0007", Occupation = "Vendor",
                HouseholdMembers = new List<string> { "Liza Villanueva (spouse)", "Miguel Villanueva (son)" },
                DateRegistered = new DateTime(2021, 1, 25)
            },
            new Resident
            {
                ResidentId = 8, LastName = "Torres", FirstName = "Liza", MiddleName = "Bautista",
                BirthDate = new DateTime(1988, 5, 16), Gender = Gender.Female, CivilStatus = CivilStatus.Separated,
                Purok = "Purok 3", ContactNo = "0917-100-0008", Occupation = "Sari-sari Store Owner",
                HouseholdMembers = new List<string> { "Ramon Torres (son)" },
                DateRegistered = new DateTime(2021, 3, 30)
            },
            new Resident
            {
                ResidentId = 9, LastName = "Bautista", FirstName = "Ramon", MiddleName = "Cruz",
                BirthDate = new DateTime(2015, 8, 21), Gender = Gender.Male, CivilStatus = CivilStatus.Single,
                Purok = "Purok 6", ContactNo = "0917-100-0009", Occupation = "None",
                DateRegistered = new DateTime(2022, 2, 14)
            },
            new Resident
            {
                ResidentId = 10, LastName = "Cruz", FirstName = "Ella", MiddleName = "Marquez",
                BirthDate = new DateTime(1999, 10, 27), Gender = Gender.Female, CivilStatus = CivilStatus.Single,
                Purok = "Purok 7", ContactNo = "0917-100-0010", Occupation = "Call Center Agent",
                DateRegistered = new DateTime(2022, 7, 19)
            },
        };

        public static IReadOnlyList<Resident> GetAllResidents() =>
            _residents.OrderBy(r => r.LastName).ThenBy(r => r.FirstName).ToList().AsReadOnly();

        public static Resident GetById(int residentId) =>
            _residents.FirstOrDefault(r => r.ResidentId == residentId);

        public static IReadOnlyList<string> GetPuroks() =>
            _residents.Select(r => r.Purok)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .OrderBy(p => p)
                .ToList()
                .AsReadOnly();

        public static Resident AddResident(Resident resident)
        {
            resident.ResidentId = _residents.Count == 0 ? 1 : _residents.Max(r => r.ResidentId) + 1;
            resident.IsActive = true;
            resident.DateRegistered = DateTime.Now;
            _residents.Add(resident);
            return resident;
        }

        public static bool UpdateResident(Resident updated)
        {
            var existing = GetById(updated.ResidentId);
            if (existing == null) return false;

            existing.LastName = updated.LastName;
            existing.FirstName = updated.FirstName;
            existing.MiddleName = updated.MiddleName;
            existing.Suffix = updated.Suffix;
            existing.BirthDate = updated.BirthDate;
            existing.Gender = updated.Gender;
            existing.CivilStatus = updated.CivilStatus;
            existing.Purok = updated.Purok;
            existing.ContactNo = updated.ContactNo;
            existing.Occupation = updated.Occupation;
            existing.HouseholdMembers = updated.HouseholdMembers;
            existing.Remarks = updated.Remarks;

            return true;
        }

        public static bool SetActive(int residentId, bool isActive, string remarks = null)
        {
            var existing = GetById(residentId);
            if (existing == null) return false;
            existing.IsActive = isActive;
            if (remarks != null) existing.Remarks = remarks;
            return true;
        }

        public static bool DeleteResident(int residentId)
        {
            var existing = GetById(residentId);
            if (existing == null) return false;
            return _residents.Remove(existing);
        }

        // ----- Stats, for the Dashboard's stat cards -----

        public static int TotalActiveResidents => _residents.Count(r => r.IsActive);

        public static int TotalSeniorCitizens => _residents.Count(r => r.IsActive && r.IsSeniorCitizen);
    }
}
