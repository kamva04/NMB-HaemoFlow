using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models.Domain;
using NMB_HLabSys.Models.ViewModels;
using NMB_HLabSys.Services;
using NMB_HLabSys.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;


namespace NMB_HLabSys.Controllers
{
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _auth;
        private readonly EmailService _email;

        public PatientController(ApplicationDbContext context, AuthService auth, EmailService email)
        {
            _context = context;
            _auth = auth;
            _email = email;
        }

        private IActionResult RequirePatient()
        {
            if (!_auth.IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!_auth.IsInRole("Patient")) return RedirectToAction("AccessDenied", "Account");
            return null!;
        }

        private int GetPatientId()
        {
            var uid = _auth.GetCurrentUserId()!.Value;
            return _context.Patients
                .Where(p => p.UserId == uid)
                .Select(p => p.PatientId)
                .FirstOrDefault();
        }
        [HttpGet]
        public IActionResult MedicalHistory()
        {
            var guard = RequirePatient();
            if (guard != null)
                return guard;

            var patientId = GetPatientId();

            var model = new PatientMedicalHistoryViewModel
            {
                Conditions = _context.PatientConditions
                    .Where(pc => pc.PatientId == patientId)
                    .Include(pc => pc.Condition)
                    .ThenInclude(c => c.Category)
                    .ToList(),
                Allergies = _context.PatientAllergies
                    .Where(pa => pa.PatientId == patientId)
                    .Include(pa => pa.Allergy)
                    .ThenInclude(a => a.Category)
                    .ToList(),
                Medications = _context.PatientMedications
                    .Where(pm => pm.PatientId == patientId)
                    .Include(pm => pm.Medication)
                    .ThenInclude(m => m.Category)
                    .ToList()
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult EditMedicalHistory(string type, int? id)
        {
            var guard = RequirePatient();
            if (guard != null) return guard;

            var entryType = NormalizeMedicalHistoryType(type);
            if (entryType == null) return BadRequest();

            var patientId = GetPatientId();
            var model = new PatientMedicalHistoryEditViewModel { EntryType = entryType };
            if (id.HasValue)
            {
                var existing = GetPatientMedicalHistoryName(patientId, entryType, id.Value);
                if (existing == null) return NotFound();
                model.CurrentItemId = id.Value;
                model.SelectedItemId = id.Value;
                model.CurrentName = existing;
            }

            PopulateMedicalHistoryOptions(entryType, model.CurrentItemId);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditMedicalHistory(PatientMedicalHistoryEditViewModel model)
        {
            var guard = RequirePatient();
            if (guard != null) return guard;

            var entryType = NormalizeMedicalHistoryType(model.EntryType);
            if (entryType == null) return BadRequest();
            model.EntryType = entryType;

            var patientId = GetPatientId();
            string? existingName = null;
            if (model.CurrentItemId.HasValue)
            {
                existingName = GetPatientMedicalHistoryName(patientId, entryType, model.CurrentItemId.Value);
                if (existingName == null)
                    return NotFound();
                model.CurrentName = existingName;
            }

            var selectedItemIsActive = entryType switch
            {
                "condition" => _context.MedicalConditions.Any(x => x.ConditionId == model.SelectedItemId && x.IsActive && x.Category!.IsActive),
                "allergy" => _context.Allergies.Any(x => x.AllergyId == model.SelectedItemId && x.IsActive && x.Category!.IsActive),
                _ => _context.Medications.Any(x => x.MedicationId == model.SelectedItemId && x.IsActive && x.Category!.IsActive)
            };
            if (model.SelectedItemId <= 0
                || (!selectedItemIsActive && !(model.CurrentItemId == model.SelectedItemId && existingName != null)))
                ModelState.AddModelError(nameof(model.SelectedItemId), "Select an active medical history item.");

            if (!ModelState.IsValid)
            {
                PopulateMedicalHistoryOptions(entryType, model.CurrentItemId);
                return View(model);
            }

            switch (entryType)
            {
                case "condition":
                    SavePatientCondition(patientId, model);
                    break;
                case "allergy":
                    SavePatientAllergy(patientId, model);
                    break;
                default:
                    SavePatientMedication(patientId, model);
                    break;
            }

            if (!ModelState.IsValid)
            {
                PopulateMedicalHistoryOptions(entryType, model.CurrentItemId);
                return View(model);
            }

            _context.SaveChanges();
            TempData["Success"] = model.CurrentItemId.HasValue
                ? "Medical history updated."
                : "Medical history added.";
            return RedirectToAction(nameof(MedicalHistory));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ArchiveMedicalHistory(string type, int id)
        {
            var guard = RequirePatient();
            if (guard != null) return guard;

            var entryType = NormalizeMedicalHistoryType(type);
            if (entryType == null) return BadRequest();

            var patientId = GetPatientId();
            switch (entryType)
            {
                case "condition":
                    var condition = _context.PatientConditions.SingleOrDefault(x => x.PatientId == patientId && x.ConditionId == id);
                    if (condition == null) return NotFound();
                    condition.IsActive = false;
                    break;
                case "allergy":
                    var allergy = _context.PatientAllergies.SingleOrDefault(x => x.PatientId == patientId && x.AllergyId == id);
                    if (allergy == null) return NotFound();
                    allergy.IsActive = false;
                    break;
                default:
                    var medication = _context.PatientMedications.SingleOrDefault(x => x.PatientId == patientId && x.MedicationId == id);
                    if (medication == null) return NotFound();
                    medication.IsActive = false;
                    break;
            }

            _context.SaveChanges();
            TempData["Success"] = "Medical history archived.";
            return RedirectToAction(nameof(MedicalHistory));
        }

        private static string? NormalizeMedicalHistoryType(string? type)
        {
            var normalized = type?.Trim().ToLowerInvariant();
            return normalized is "condition" or "allergy" or "medication" ? normalized : null;
        }

        private string? GetPatientMedicalHistoryName(int patientId, string type, int itemId)
        {
            return type switch
            {
                "condition" => _context.PatientConditions
                    .Where(x => x.PatientId == patientId && x.ConditionId == itemId && x.IsActive)
                    .Select(x => x.Condition!.ConditionName)
                    .FirstOrDefault(),
                "allergy" => _context.PatientAllergies
                    .Where(x => x.PatientId == patientId && x.AllergyId == itemId && x.IsActive)
                    .Select(x => x.Allergy!.AllergyName)
                    .FirstOrDefault(),
                _ => _context.PatientMedications
                    .Where(x => x.PatientId == patientId && x.MedicationId == itemId && x.IsActive)
                    .Select(x => x.Medication!.MedicationName)
                    .FirstOrDefault()
            };
        }

        private void PopulateMedicalHistoryOptions(string type, int? includeItemId = null)
        {
            ViewBag.Options = type switch
            {
                "condition" => _context.MedicalConditions
                    .Where(x => (x.IsActive && x.Category!.IsActive) || x.ConditionId == includeItemId)
                    .OrderBy(x => x.ConditionName)
                    .Select(x => new SelectListItem(x.ConditionName, x.ConditionId.ToString()))
                    .ToList(),
                "allergy" => _context.Allergies
                    .Where(x => (x.IsActive && x.Category!.IsActive) || x.AllergyId == includeItemId)
                    .OrderBy(x => x.AllergyName)
                    .Select(x => new SelectListItem(x.AllergyName, x.AllergyId.ToString()))
                    .ToList(),
                _ => _context.Medications
                    .Where(x => (x.IsActive && x.Category!.IsActive) || x.MedicationId == includeItemId)
                    .OrderBy(x => x.MedicationName)
                    .Select(x => new SelectListItem(x.MedicationName, x.MedicationId.ToString()))
                    .ToList()
            };
        }

        private void SavePatientCondition(int patientId, PatientMedicalHistoryEditViewModel model)
        {
            var current = model.CurrentItemId.HasValue
                ? _context.PatientConditions.Single(x => x.PatientId == patientId && x.ConditionId == model.CurrentItemId.Value && x.IsActive)
                : null;
            var target = _context.PatientConditions.SingleOrDefault(x => x.PatientId == patientId && x.ConditionId == model.SelectedItemId);
            SavePatientHistoryEntry(
                current, target, model.SelectedItemId,
                x => x.IsActive,
                (x, isActive) => x.IsActive = isActive,
                id => new PatientCondition { PatientId = patientId, ConditionId = id });
        }

        private void SavePatientAllergy(int patientId, PatientMedicalHistoryEditViewModel model)
        {
            var current = model.CurrentItemId.HasValue
                ? _context.PatientAllergies.Single(x => x.PatientId == patientId && x.AllergyId == model.CurrentItemId.Value && x.IsActive)
                : null;
            var target = _context.PatientAllergies.SingleOrDefault(x => x.PatientId == patientId && x.AllergyId == model.SelectedItemId);
            SavePatientHistoryEntry(
                current, target, model.SelectedItemId,
                x => x.IsActive,
                (x, isActive) => x.IsActive = isActive,
                id => new PatientAllergy { PatientId = patientId, AllergyId = id });
        }

        private void SavePatientMedication(int patientId, PatientMedicalHistoryEditViewModel model)
        {
            var current = model.CurrentItemId.HasValue
                ? _context.PatientMedications.Single(x => x.PatientId == patientId && x.MedicationId == model.CurrentItemId.Value && x.IsActive)
                : null;
            var target = _context.PatientMedications.SingleOrDefault(x => x.PatientId == patientId && x.MedicationId == model.SelectedItemId);
            SavePatientHistoryEntry(
                current, target, model.SelectedItemId,
                x => x.IsActive,
                (x, isActive) => x.IsActive = isActive,
                id => new PatientMedication { PatientId = patientId, MedicationId = id });
        }

        private void SavePatientHistoryEntry<T>(
            T? current,
            T? target,
            int selectedItemId,
            Func<T, bool> isActive,
            Action<T, bool> setActive,
            Func<int, T> createEntry) where T : class
        {
            if (current != null && target == current)
                return;

            if (target != null && isActive(target))
            {
                ModelState.AddModelError(nameof(PatientMedicalHistoryEditViewModel.SelectedItemId), "This item is already in your medical history.");
                return;
            }

            if (current != null) setActive(current, false);

            if (target != null)
            {
                setActive(target, true);
            }
            else
            {
                _context.Add(createEntry(selectedItemId));
            }
        }
        [HttpGet]
        public IActionResult Register()
            => View(new PatientRegisterViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Register(PatientRegisterViewModel model)
        {
            model.SAIDNumber = model.SAIDNumber?.Trim() ?? string.Empty;
            model.CellNumber = model.CellNumber?.Trim();

            if (SouthAfricanIdNumber.TryGetDateOfBirth(model.SAIDNumber, out var dateOfBirth))
            {
                model.DateOfBirth = dateOfBirth;
                ModelState.Remove(nameof(model.DateOfBirth));
            }
            else
            {
                ModelState.AddModelError(nameof(model.SAIDNumber), "Enter a valid 13-digit South African ID number.");
            }

            if (_context.Patients.Any(p => p.SAIDNumber.Trim() == model.SAIDNumber))
                ModelState.AddModelError(nameof(model.SAIDNumber), "An account with this SA ID number already exists.");

            if (!ModelState.IsValid) return View(model);

            if (_context.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already registered.");
                return View(model);
            }

            var pwHash = _auth.HashPassword(model.Password);
            var user = new User
            {
                Email = model.Email,
                PasswordHash = pwHash,
                Role = "Patient",
                IsFirstLogin = false
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            _context.Patients.Add(new Patient
            {
                UserId = user.UserId,
                Name = model.Name,
                Surname = model.Surname,
                SAIDNumber = model.SAIDNumber,
                DateOfBirth = model.DateOfBirth,
                CellNumber = model.CellNumber,
                Gender = model.Gender,
                Street = model.Street,
                Street2 = model.Street2,
                Suburb = model.Suburb,
                Town = model.Town,
                Province = model.Province,
                PostalCode = model.PostalCode
            });
            _context.SaveChanges();

            TempData["Success"] = "Registration successful. Please log in.";
            return RedirectToAction("Login", "Account");
        }     
         public IActionResult Dashboard()
        {
            var guard = RequirePatient(); if (guard != null) return guard;
            var patientId = GetPatientId();
            var patient = GetPatient(patientId);

            var recentRequests = _context.TestRequests
                .AsNoTracking()
                .Where(tr => tr.PatientId == patientId)
                .OrderByDescending(tr => tr.RequestDate)
                .Select(tr => new TestRequest
                {
                    RequestId = tr.RequestId,
                    RequestNumber = tr.RequestNumber,
                    RequestDate = tr.RequestDate,
                    Urgency = tr.Urgency,
                    Status = tr.Status
                })
                .Take(5)
                .ToList();

            var allStatuses = _context.TestRequests
                .AsNoTracking()
                .Where(tr => tr.PatientId == patientId)
                .Select(tr => tr.Status)
                .ToList();

            ViewBag.TotalRequests = allStatuses.Count;
            ViewBag.ReleasedCount = allStatuses.Count(s => s == "ReleasedByDoctor");
            ViewBag.PendingCount = allStatuses.Count(s => s != "ReleasedByDoctor" && s != "Cancelled");
            ViewBag.CompletedCount = allStatuses.Count(s => s == "Completed" || s == "ReleasedByDoctor");

            return View("~/Views/Patient/Dashboard.cshtml",
                new PatientDashboardViewModel { Patient = patient!, Requests = recentRequests });
        }

        // ── MY REQUESTS (full list, its own page) ───────────────────
        public IActionResult MyRequests()
        {
            var guard = RequirePatient(); if (guard != null) return guard;
            var patientId = GetPatientId();
            var patient = GetPatient(patientId);

            var requests = _context.TestRequests
                .AsNoTracking()
                .Where(tr => tr.PatientId == patientId)
                .OrderByDescending(tr => tr.RequestDate)
                .Select(tr => new TestRequest
                {
                    RequestId = tr.RequestId,
                    RequestNumber = tr.RequestNumber,
                    RequestDate = tr.RequestDate,
                    Urgency = tr.Urgency,
                    Status = tr.Status
                })
                .ToList();

            return View("~/Views/Patient/MyRequests.cshtml",
                new PatientDashboardViewModel { Patient = patient!, Requests = requests });
        }
         
        public IActionResult ViewResults(int requestId)
        {
            var guard = RequirePatient(); if (guard != null) return guard;
            var patientId = GetPatientId();

            var request = _context.TestRequests
                .AsNoTracking()
                .Where(tr => tr.RequestId == requestId && tr.PatientId == patientId && tr.Status == "ReleasedByDoctor")
                .Select(tr => new TestRequest
                {
                    RequestId = tr.RequestId,
                    RequestNumber = tr.RequestNumber,
                    Status = tr.Status,
                    RequestDate = tr.RequestDate,
                    Doctor = tr.Doctor == null ? null : new Doctor
                    {
                        Name = tr.Doctor.Name,
                        Surname = tr.Doctor.Surname
                    }
                })
                .FirstOrDefault();

            if (request == null) { TempData["Error"] = "Results not yet released by your doctor."; return RedirectToAction(nameof(Dashboard)); }

            request.RequestTestTypes = _context.RequestTestTypes
                .AsNoTracking()
                .Where(rtt => rtt.RequestId == requestId
                    && (rtt.Status == "Completed" || rtt.Status == "Verified"))
                .Include(rtt => rtt.TestType)
                .Include(rtt => rtt.Technician)
                .OrderBy(rtt => rtt.TestType!.TestName)
                .ToList();

            return View(request);
        }

        public IActionResult Consent()
        {
            var guard = RequirePatient();

            if (guard != null)
                return guard;

            var patientId = GetPatientId();

            // Existing consent records
            var consents = _context.DoctorPatientConsents
                .AsNoTracking()
                .Where(c => c.PatientId == patientId)
                .Include(c => c.Doctor)
                .Include(c => c.Request)
                .Include(c => c.RequestTest)
                    .ThenInclude(rt => rt!.TestType)
                .Include(c => c.Sample)
                    .ThenInclude(s => s!.SampleType)
                .OrderBy(c => c.Doctor!.Surname)
                .ThenBy(c => c.Doctor!.Name)
                .ThenByDescending(c => c.GrantedAt)
                .ToList();

            var allDoctors = _context.Doctors
                .AsNoTracking()
                .Where(d => !d.IsDeleted)
                .OrderBy(d => d.Surname)
                .ThenBy(d => d.Name)
                .Select(d => new Doctor
                {
                    DoctorId = d.DoctorId,
                    Name = d.Name,
                    Surname = d.Surname
                })
                .ToList();

            // Patient's test requests
            var testRequests = _context.TestRequests
                .AsNoTracking()
                .Where(r => r.PatientId == patientId)
                .OrderBy(r => r.RequestNumber)
                .Select(r => new TestRequest
                {
                    RequestId = r.RequestId,
                    RequestNumber = r.RequestNumber,
                    RequestDate = r.RequestDate,
                    Status = r.Status,
                    Urgency = r.Urgency
                })
                .ToList();

            var testTypes = _context.RequestTestTypes
                .AsNoTracking()
                .Where(rt => rt.Request!.PatientId == patientId)
                .Include(rt => rt.Request!)
                    .ThenInclude(r => r.Doctor)
                .Include(rt => rt.TestType)
                .OrderBy(rt => rt.TestType!.TestName)
                .ThenByDescending(rt => rt.CompletedAt ?? rt.Request!.RequestDate)
                .ToList();

            var samples = _context.RequestSamples
                .AsNoTracking()
                .Where(s => s.Request!.PatientId == patientId)
                .Include(s => s.Request!)
                    .ThenInclude(r => r.Doctor)
                .Include(s => s.SampleType)
                .OrderBy(s => s.SampleType!.TypeName)
                .ThenBy(s => s.Barcode)
                .ToList();

            var model = new ConsentManagementViewModel
            {
                PatientId = patientId,
                ActiveConsents = consents,
                AllDoctors = allDoctors,
                TestRequests = testRequests,
                TestTypes = testTypes,
                Samples = samples
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GrantConsent(int doctorId, int[]? requestTestIds, int? sampleId)
        {
            var guard = RequirePatient();

            if (guard != null)
                return guard;

            var patientId = GetPatientId();
            var selectedTestIds = (requestTestIds ?? Array.Empty<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if ((selectedTestIds.Count > 0 ? 1 : 0) + (sampleId.HasValue ? 1 : 0) != 1)
            {
                TempData["Error"] = "Select one or more tests, or choose one sample to share.";
                return RedirectToAction(nameof(Consent));
            }

            var selectedTests = _context.RequestTestTypes
                .Where(rt => selectedTestIds.Contains(rt.RequestTestId) && rt.Request!.PatientId == patientId)
                .Include(rt => rt.Request)
                .Include(rt => rt.TestType)
                .ToList();
            var selectedSample = sampleId.HasValue
                ? _context.RequestSamples
                    .Include(s => s.Request)
                    .Include(s => s.SampleType)
                    .FirstOrDefault(s => s.SampleId == sampleId.Value && s.Request!.PatientId == patientId)
                : null;

            if ((selectedTestIds.Count > 0 && selectedTests.Count != selectedTestIds.Count) ||
                (sampleId.HasValue && selectedSample == null))
            {
                TempData["Error"] = "One or more selected tests or samples are invalid.";
                return RedirectToAction(nameof(Consent));
            }

            var doctor = _context.Doctors
                .Include(d => d.User)
                .FirstOrDefault(d => d.DoctorId == doctorId && !d.IsDeleted);

            if (doctor == null)
            {
                TempData["Error"] = "Doctor not found.";
                return RedirectToAction(nameof(Consent));
            }

            var sharedItems = new List<string>();
            foreach (var test in selectedTests)
            {
                var consent = _context.DoctorPatientConsents
                    .FirstOrDefault(c =>
                        c.PatientId == patientId &&
                        c.DoctorId == doctorId &&
                        c.RequestId == test.RequestId &&
                        c.RequestTestId == test.RequestTestId &&
                        c.SampleId == null);

                if (consent == null)
                {
                    consent = new DoctorPatientConsent
                    {
                        PatientId = patientId,
                        DoctorId = doctorId,
                        RequestId = test.RequestId,
                        RequestTestId = test.RequestTestId
                    };
                    _context.DoctorPatientConsents.Add(consent);
                }

                consent.IsActive = true;
                consent.GrantedAt = DateTime.Now;
                sharedItems.Add(test.TestType?.TestName ?? "selected test");
            }

            if (selectedSample != null)
            {
                var consent = _context.DoctorPatientConsents
                    .FirstOrDefault(c =>
                        c.PatientId == patientId &&
                        c.DoctorId == doctorId &&
                        c.RequestId == selectedSample.RequestId &&
                        c.RequestTestId == null &&
                        c.SampleId == selectedSample.SampleId);

                if (consent == null)
                {
                    consent = new DoctorPatientConsent
                    {
                        PatientId = patientId,
                        DoctorId = doctorId,
                        RequestId = selectedSample.RequestId,
                        SampleId = selectedSample.SampleId
                    };
                    _context.DoctorPatientConsents.Add(consent);
                }

                consent.IsActive = true;
                consent.GrantedAt = DateTime.Now;
                sharedItems.Add($"Sample {selectedSample.Barcode}");
            }

            _context.SaveChanges();
            var scope = string.Join(", ", sharedItems);

            if (doctor.User != null &&
                !string.IsNullOrEmpty(doctor.User.Email))
            {
                _email.Send(
                    doctor.User.Email,
                    "Patient Consent Granted – NMB-HLabSys",
                    $"""
            <p>
                A patient has granted you access to selected test data.
            </p>
            <p>
                Shared items: <strong>{scope}</strong>.
            </p>
            """);
            }

            TempData["Success"] = $"Access granted to Dr {doctor.Name} {doctor.Surname} for {scope}.";

            return RedirectToAction(nameof(Consent));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RevokeConsent(int consentId)
        {
            var guard = RequirePatient();

            if (guard != null)
                return guard;

            var patientId = GetPatientId();

            var consent = _context.DoctorPatientConsents
                .Include(c => c.Request)
                .FirstOrDefault(c =>
                    c.ConsentId == consentId &&
                    c.PatientId == patientId);

            if (consent == null)
            {
                TempData["Error"] = "Consent record not found.";
                return RedirectToAction(nameof(Consent));
            }

            consent.IsActive = false;

            _context.SaveChanges();

            TempData["Success"] =
                $"Access revoked for request {consent.Request?.RequestNumber}.";

            return RedirectToAction(nameof(Consent));
        }
       
        [HttpGet]
        public IActionResult ViewDetails(int requestId)
        {
            var guard = RequirePatient();

            if (guard != null)
                return guard;

            var patientId = GetPatientId();

            var request = _context.TestRequests
                .AsNoTracking()
                .Where(r =>
                    r.RequestId == requestId &&
                    r.PatientId == patientId)
                .Include(r => r.Doctor)
                .Include(r => r.RequestTestTypes)
                    .ThenInclude(rtt => rtt.TestType)
                        .ThenInclude(tt => tt.Category)
                .FirstOrDefault();

            if (request == null)
                return NotFound("Test request was not found.");

            return View("~/Views/Patient/ViewDetails.cshtml", request);
        }
        
        [HttpGet]
        public IActionResult Reports()
        {
            if (!_auth.IsInRole("Patient"))
                return Forbid();

            var model = new ReportFilterViewModel
            {
                FromDate = DateTime.Today.AddMonths(-1),
                ToDate = DateTime.Today
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Appointments()
        {
            var guard = RequirePatient(); if (guard != null) return guard;
            var patientId = GetPatientId();
            var appointments = _context.PatientAppointments
                .AsNoTracking()
                .Where(a => a.PatientId == patientId)
                .Include(a => a.Doctor)
                .Include(a => a.Request)
                .OrderByDescending(a => a.ProposedAt)
                .ToList();
            return View(appointments);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult AppointmentResponse(int appointmentId, bool accept)
        {
            var guard = RequirePatient(); if (guard != null) return guard;
            var patientId = GetPatientId();
            var appointment = _context.PatientAppointments.FirstOrDefault(a =>
                a.AppointmentId == appointmentId && a.PatientId == patientId);
            if (appointment == null) return NotFound();
            if (appointment.Status != "Pending")
            {
                TempData["Error"] = "This appointment proposal has already been answered.";
                return RedirectToAction(nameof(Appointments));
            }
            if (accept && appointment.ProposedAt <= DateTime.Now)
            {
                TempData["Error"] = "This proposed appointment time has passed. Please contact your doctor.";
                return RedirectToAction(nameof(Appointments));
            }

            appointment.Status = accept ? "Confirmed" : "Declined";
            _context.SaveChanges();

            var doctor = _context.Doctors
                .Include(d => d.User)
                .FirstOrDefault(d => d.DoctorId == appointment.DoctorId);
            var emailSent = false;
            if (!string.IsNullOrWhiteSpace(doctor?.User?.Email))
            {
                var response = accept ? "confirmed" : "declined";
                try
                {
                    _email.Send(doctor.User.Email, "Patient appointment response – NMB-HLabSys",
                        $"<p>The patient has {response} the proposed appointment for {appointment.ProposedAt:dd MMM yyyy HH:mm}.</p>");
                    emailSent = true;
                }
                catch (InvalidOperationException)
                {
                    emailSent = false;
                }
                catch (System.Net.Mail.SmtpException)
                {
                    emailSent = false;
                }
            }

            var responseMessage = accept ? "Appointment confirmed." : "Appointment declined.";
            TempData[emailSent ? "Success" : "Warning"] = emailSent
                ? responseMessage
                : $"{responseMessage} The doctor could not be emailed, but the response is saved in your portal.";
            return RedirectToAction(nameof(Appointments));
        }
       
        private Patient? GetPatient(int patientId)
        {
            var p = _context.Patients
                .AsNoTracking()
                .Where(pat => pat.PatientId == patientId)
                .Select(pat => new Patient { PatientId = pat.PatientId, Name = pat.Name, Surname = pat.Surname })
                .FirstOrDefault();
            if (p == null) return null;

            p.Conditions = _context.PatientConditions
                .AsNoTracking()
                .Where(pc => pc.PatientId == patientId && pc.IsActive)
                .Include(pc => pc.Condition)
                .Select(pc => new MedicalCondition { ConditionId = pc.Condition!.ConditionId, ConditionName = pc.Condition.ConditionName })
                .ToList();
            p.Allergies = _context.PatientAllergies
                .AsNoTracking()
                .Where(pa => pa.PatientId == patientId && pa.IsActive)
                .Include(pa => pa.Allergy)
                .Select(pa => new Allergy { AllergyId = pa.Allergy!.AllergyId, AllergyName = pa.Allergy.AllergyName })
                .ToList();
            p.Medications = _context.PatientMedications
                .AsNoTracking()
                .Where(pm => pm.PatientId == patientId && pm.IsActive)
                .Include(pm => pm.Medication)
                .Select(pm => new Medication { MedicationId = pm.Medication!.MedicationId, MedicationName = pm.Medication.MedicationName })
                .ToList();
            return p;
        }
        public IActionResult TrackResults()
        {
            var guard = RequirePatient(); if (guard != null) return guard;
            var patientId = GetPatientId();

            // Release is recorded on the request; use the captured numeric result for trend eligibility.
            var testTypes = _context.RequestTestTypes
                .AsNoTracking()
                .Where(rtt => rtt.Request!.PatientId == patientId
                    && rtt.Request.Status == "ReleasedByDoctor"
                    && rtt.ResultValue.HasValue)
                .Select(rtt => rtt.TestType!)
                .Distinct()
                .OrderBy(tt => tt.TestName)
                .ToList();

            return View(testTypes);
        }

        // ── AJAX: historical values for a single test type ────────
        [HttpGet]
        public IActionResult TrackResultsJson(int testTypeId, DateTime? fromDate, DateTime? toDate)
        {
            var guard = RequirePatient(); if (guard != null) return Json(Array.Empty<object>());
            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
                return BadRequest("Start date must be on or before the end date.");

            var patientId = GetPatientId();

            var rows = _context.RequestTestTypes
                .AsNoTracking()
                .Where(rtt => rtt.Request!.PatientId == patientId
                              && rtt.TestTypeId == testTypeId
                              && rtt.Request.Status == "ReleasedByDoctor"
                              && rtt.ResultValue.HasValue
                              && (!fromDate.HasValue
                                  || (rtt.CompletedAt ?? rtt.Request.RequestDate) >= fromDate.Value.Date)
                              && (!toDate.HasValue
                                  || (rtt.CompletedAt ?? rtt.Request.RequestDate) < toDate.Value.Date.AddDays(1)))
                .OrderBy(rtt => rtt.CompletedAt ?? rtt.Request!.RequestDate)
                .Select(rtt => new
                {
                    date = rtt.CompletedAt ?? rtt.Request!.RequestDate,
                    value = rtt.ResultValue,
                    units = rtt.TestType!.Units,
                    normalMin = rtt.TestType.NormalRangeMin,
                    normalMax = rtt.TestType.NormalRangeMax,
                    isAbnormal = rtt.IsAbnormal
                })
                .ToList();

            return Json(rows);
        }

        [HttpGet]
        public IActionResult EditProfile()
        {
            var guard = RequirePatient();

            if (guard != null)
                return guard;

            var userId = _auth.GetCurrentUserId();

            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var patient = _context.Patients
                .AsNoTracking()
                .FirstOrDefault(p => p.UserId == userId.Value);

            if (patient == null)
                return NotFound("Patient profile was not found.");

            var model = new PatientEditProfileViewModel
            {
                PatientId = patient.PatientId,
                Name = patient.Name,
                Surname = patient.Surname,
                SAIDNumber = patient.SAIDNumber,
                DateOfBirth = patient.DateOfBirth,
                CellNumber = patient.CellNumber,
                Gender = patient.Gender ?? string.Empty,
                Street = patient.Street ?? string.Empty,
                Street2 = patient.Street2 ?? string.Empty,
                Suburb = patient.Suburb ?? string.Empty,
                Town = patient.Town ?? string.Empty,
                Province = patient.Province ?? string.Empty,
                PostalCode = patient.PostalCode ?? string.Empty
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(PatientEditProfileViewModel model)
        {
            var guard = RequirePatient();

            if (guard != null)
                return guard;

            model.SAIDNumber = model.SAIDNumber?.Trim() ?? string.Empty;
            model.CellNumber = model.CellNumber?.Trim();

            if (SouthAfricanIdNumber.TryGetDateOfBirth(model.SAIDNumber, out var dateOfBirth))
            {
                model.DateOfBirth = dateOfBirth;
                ModelState.Remove(nameof(model.DateOfBirth));
            }
            else
            {
                ModelState.AddModelError(nameof(model.SAIDNumber), "Enter a valid 13-digit South African ID number.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var userId = _auth.GetCurrentUserId();

            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var patient = _context.Patients
                .FirstOrDefault(p =>
                    p.PatientId == model.PatientId &&
                    p.UserId == userId.Value);

            if (patient == null)
                return NotFound("Patient profile was not found.");

            if (_context.Patients.Any(p => p.PatientId != patient.PatientId && p.SAIDNumber.Trim() == model.SAIDNumber))
            {
                ModelState.AddModelError(nameof(model.SAIDNumber), "Another patient already uses this SA ID number.");
                return View(model);
            }

            patient.Name = model.Name.Trim();
            patient.Surname = model.Surname.Trim();
            patient.SAIDNumber = model.SAIDNumber.Trim();
            patient.DateOfBirth = model.DateOfBirth;
            patient.CellNumber = string.IsNullOrWhiteSpace(model.CellNumber)
                ? null
                : model.CellNumber.Trim();
            patient.Gender = model.Gender.Trim();
            patient.Street = model.Street.Trim();
            patient.Street2 = string.IsNullOrWhiteSpace(model.Street2) ? null : model.Street2.Trim();
            patient.Suburb = model.Suburb.Trim();
            patient.Town = model.Town.Trim();
            patient.Province = model.Province.Trim();
            patient.PostalCode = model.PostalCode.Trim();

            _context.SaveChanges();

            TempData["Success"] = "Your profile has been updated successfully.";

            return RedirectToAction(nameof(EditProfile));
        }
    }
}
