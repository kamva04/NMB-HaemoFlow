 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _auth;
        private readonly EmailService _email;
        private readonly PdfService _pdf;

        public DoctorController(ApplicationDbContext context, AuthService auth, EmailService email, PdfService pdf)
        {
            _context = context;
            _auth = auth;
            _email = email;
            _pdf = pdf;

        }

        private IActionResult RequireDoctor()
        {
            if (!_auth.IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!_auth.IsInRole("Doctor")) return RedirectToAction("AccessDenied", "Account");
            return null!;
        }
        // ═══════════════════════════════════════════════════════════
        // PROFILE
        // ═══════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult Profile()
        {
            var guard = RequireDoctor(); if (guard != null) return guard;

            var doctorId = GetDoctorId();
            if (doctorId == 0) return NotFound();

            var doctor = _context.Doctors
                .AsNoTracking()
                .Include(d => d.User)
                .FirstOrDefault(d => d.DoctorId == doctorId);

            if (doctor == null) return NotFound();

            var vm = new DoctorProfileViewModel
            {
                DoctorId = doctor.DoctorId,
                Name = doctor.Name,
                Surname = doctor.Surname,
                HPCSANumber = doctor.HPCSANumber,
                Email = doctor.User?.Email ?? string.Empty,
                ContactNumber = doctor.ContactNumber
            };

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditProfile(DoctorProfileViewModel model)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;

            var doctorId = GetDoctorId();
            if (doctorId == 0) return NotFound();

            // These fields are display-only on this form; they're owned by the
            // Laboratory Manager subsystem, so don't let a stale/tampered post
            // fail validation or silently overwrite them here.
            ModelState.Remove(nameof(model.Name));
            ModelState.Remove(nameof(model.Surname));
            ModelState.Remove(nameof(model.HPCSANumber));
            ModelState.Remove(nameof(model.Email));

            if (!ModelState.IsValid)
                return View(nameof(Profile), ReloadDoctorProfileVm(model));

            var doctor = _context.Doctors.FirstOrDefault(d => d.DoctorId == doctorId);
            if (doctor == null) return NotFound();

            doctor.ContactNumber = string.IsNullOrWhiteSpace(model.ContactNumber)
                ? null
                : model.ContactNumber.Trim();

            _context.SaveChanges();

            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            return View(new DoctorChangePasswordViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ChangePassword(DoctorChangePasswordViewModel model)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;

            if (!ModelState.IsValid)
                return View(model);

            var userId = _auth.GetCurrentUserId();
            if (!userId.HasValue) return NotFound();

            var (ok, err) = _auth.ChangePassword(userId.Value, model.CurrentPassword, model.NewPassword);

            if (!ok)
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), err ?? "Could not change password.");
                return View(model);
            }

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(Profile));
        }

        private DoctorProfileViewModel ReloadDoctorProfileVm(DoctorProfileViewModel incoming)
        {
            var doctor = _context.Doctors
                .AsNoTracking()
                .Include(d => d.User)
                .FirstOrDefault(d => d.DoctorId == incoming.DoctorId);

            if (doctor == null) return incoming;

            incoming.Name = doctor.Name;
            incoming.Surname = doctor.Surname;
            incoming.HPCSANumber = doctor.HPCSANumber;
            incoming.Email = doctor.User?.Email ?? string.Empty;
            return incoming;
        }
        private int GetDoctorId()
        {
            var uid = _auth.GetCurrentUserId()!.Value;
            return _context.Doctors
                .Where(d => d.UserId == uid)
                .Select(d => d.DoctorId)
                .FirstOrDefault();
        }

        // ── DASHBOARD ─────────────────────────────────────────────
        public IActionResult Index() => RedirectToAction(nameof(Dashboard));
        public IActionResult Dashboard()
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            var doctorId = GetDoctorId();
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var prevMonthStart = monthStart.AddMonths(-1);
            var alertSince = DateTime.Today.AddDays(-5);

            var doctor = _context.Doctors
                .AsNoTracking()
                .Where(d => d.DoctorId == doctorId)
                .Select(d => new { d.Name, d.Surname })
                .FirstOrDefault();

            var patientQuery = _context.Patients
                .AsNoTracking()
                .Where(p =>
                    p.DoctorPatientConsents.Any(c => c.DoctorId == doctorId && c.IsActive) ||
                    p.TestRequests.Any(tr => tr.DoctorId == doctorId));

            var requestQuery = _context.TestRequests
                .AsNoTracking()
                .Where(tr => tr.DoctorId == doctorId);

            var patientCount = patientQuery.Count();
            var testRequestCount = requestQuery.Count();
            var resultsCount = requestQuery.Count(tr =>
                tr.Status == "Completed" || tr.Status == "ReleasedByDoctor");

            var criticalAlertsCount = _context.RequestTestTypes
                .AsNoTracking()
                .Count(rtt =>
                    rtt.Request.DoctorId == doctorId &&
                    rtt.IsAbnormal &&
                    rtt.CompletedAt.HasValue &&
                    rtt.CompletedAt.Value >= alertSince);

            var patientsThisMonth = _context.DoctorPatientConsents
                .AsNoTracking()
                .Count(c => c.DoctorId == doctorId && c.GrantedAt >= monthStart);
            var patientsLastMonth = _context.DoctorPatientConsents
                .AsNoTracking()
                .Count(c => c.DoctorId == doctorId &&
                    c.GrantedAt >= prevMonthStart && c.GrantedAt < monthStart);

            var requestsThisMonth = requestQuery.Count(tr => tr.RequestDate >= monthStart);
            var requestsLastMonth = requestQuery.Count(tr =>
                tr.RequestDate >= prevMonthStart && tr.RequestDate < monthStart);

            var resultsThisMonth = requestQuery.Count(tr =>
                (tr.Status == "Completed" || tr.Status == "ReleasedByDoctor") &&
                tr.RequestDate >= monthStart);
            var resultsLastMonth = requestQuery.Count(tr =>
                (tr.Status == "Completed" || tr.Status == "ReleasedByDoctor") &&
                tr.RequestDate >= prevMonthStart && tr.RequestDate < monthStart);

            var alertsThisMonth = _context.RequestTestTypes
                .AsNoTracking()
                .Count(rtt =>
                    rtt.Request.DoctorId == doctorId &&
                    rtt.IsAbnormal &&
                    rtt.CompletedAt.HasValue &&
                    rtt.CompletedAt.Value >= monthStart);
            var alertsLastMonth = _context.RequestTestTypes
                .AsNoTracking()
                .Count(rtt =>
                    rtt.Request.DoctorId == doctorId &&
                    rtt.IsAbnormal &&
                    rtt.CompletedAt.HasValue &&
                    rtt.CompletedAt.Value >= prevMonthStart &&
                    rtt.CompletedAt.Value < monthStart);

            var activePatients = patientQuery.Count(p =>
                p.TestRequests.Any(tr =>
                    tr.DoctorId == doctorId &&
                    tr.RequestDate >= monthStart.AddMonths(-3)));

            var pendingResults = requestQuery.Count(tr =>
                tr.Status == "Submitted" ||
                tr.Status == "SamplesReceived" ||
                tr.Status == "InProgress" ||
                tr.Status == "ToBeReviewed" ||
                tr.Status == "Verified");

            var recentRequestRows = _context.TestRequests
                .AsNoTracking()
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestTestTypes).ThenInclude(rtt => rtt.TestType)
                .Where(tr => tr.DoctorId == doctorId)
                .OrderByDescending(tr => tr.RequestDate)
                .Take(8)
                .ToList();

            var recentRequests = recentRequestRows.Select(tr => new DoctorDashboardRequestRow
            {
                RequestId = tr.RequestId,
                PatientName = tr.Patient.Name + " " + tr.Patient.Surname,
                TestTypesSummary = string.Join(", ", tr.RequestTestTypes
                    .Select(rtt => rtt.TestType.TestName)
                    .Take(3)),
                Status = tr.Status,
                RequestDate = tr.RequestDate
            }).ToList();

            var criticalAlerts = _context.RequestTestTypes
                .AsNoTracking()
                .Include(rtt => rtt.Request).ThenInclude(tr => tr.Patient)
                .Include(rtt => rtt.TestType)
                .Where(rtt =>
                    rtt.Request.DoctorId == doctorId &&
                    rtt.IsAbnormal &&
                    rtt.CompletedAt.HasValue &&
                    rtt.CompletedAt.Value >= alertSince)
                .OrderByDescending(rtt => rtt.CompletedAt)
                .Take(5)
                .Select(rtt => new DoctorDashboardAlertRow
                {
                    RequestId = rtt.RequestId,
                    RequestTestId = rtt.RequestTestId,
                    PatientName = rtt.Request.Patient.Name + " " + rtt.Request.Patient.Surname,
                    TestName = rtt.TestType.TestName,
                    ResultValue = rtt.ResultValue,
                    Units = rtt.TestType.Units,
                    CompletedAt = rtt.CompletedAt
                })
                .ToList();

            var vm = new DoctorDashboardViewModel
            {
                DoctorDisplayName = doctor != null
                    ? $"Dr {doctor.Name} {doctor.Surname}"
                    : "Dr",
                PatientCount = patientCount,
                TestRequestCount = testRequestCount,
                ResultsCount = resultsCount,
                CriticalAlertsCount = criticalAlertsCount,
                PatientTrend = patientsThisMonth - patientsLastMonth,
                TestRequestTrend = requestsThisMonth - requestsLastMonth,
                ResultsTrend = resultsThisMonth - resultsLastMonth,
                AlertsTrend = alertsThisMonth - alertsLastMonth,
                ActivePatients = activePatients,
                PendingResults = pendingResults,
                RecentRequests = recentRequests,
                CriticalAlerts = criticalAlerts
            };

            return View(vm);
        }
        [HttpGet]
        public IActionResult Reports()
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            return View(new ReportFilterViewModel());
        }



        private static List<string> ParseList(string? value, char separator = ',')
        {
            if (string.IsNullOrWhiteSpace(value)) return new List<string>();
            return value.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        private void PopulateReleaseResultContext(ReleaseResultViewModel model)
        {
            var request = _context.TestRequests
                .AsNoTracking()
                .Include(tr => tr.Patient).ThenInclude(p => p.User)
                .FirstOrDefault(tr => tr.RequestId == model.RequestId);

            if (request == null) return;

            model.RequestNumber = request.RequestNumber;
            model.PatientName = $"{request.Patient.Name} {request.Patient.Surname}";
            model.PatientEmail = request.Patient.User?.Email;
            model.AbnormalTestNames = _context.RequestTestTypes
                .AsNoTracking()
                .Where(rtt => rtt.RequestId == model.RequestId && rtt.IsAbnormal)
                .Include(rtt => rtt.TestType)
                .Select(rtt => rtt.TestType!.TestName)
                .ToList();
            model.AbnormalCount = model.AbnormalTestNames.Count;
        }

        private bool CanAccessPatient(int doctorId, int patientId)
        {
            return _context.DoctorPatientConsents
                .Any(c => c.PatientId == patientId && c.DoctorId == doctorId && c.IsActive)
                || _context.TestRequests.Any(tr => tr.PatientId == patientId && tr.DoctorId == doctorId);
        }

        private bool TrySendEmail(string to, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(to)) return false;
            try
            {
                _email.Send(to, subject, htmlBody);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TrySendEmailWithAttachment(string to, string subject, string htmlBody, byte[] pdfBytes, string fileName)
        {
            if (string.IsNullOrWhiteSpace(to)) return false;
            try
            {
                _email.SendWithAttachment(to, subject, htmlBody, pdfBytes, fileName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string BuildReleaseResultsEmailHtml(
            Patient patient,
            TestRequest request,
            ReleaseResultViewModel model,
            IReadOnlyList<string> abnormalTestNames)
        {
            var patientName = $"{patient.Name} {patient.Surname}".Trim();
            var noteBlock = string.IsNullOrWhiteSpace(model.DoctorNote)
                ? string.Empty
                : $"<p><strong>Message from your doctor:</strong><br/>{System.Net.WebUtility.HtmlEncode(model.DoctorNote)}</p>";

            string abnormalBlock;
            if (abnormalTestNames.Count == 0)
            {
                abnormalBlock = "<p>All reported results are within the normal reference range.</p>";
            }
            else
            {
                var items = string.Join("", abnormalTestNames.Select(t =>
                    $"<li>{System.Net.WebUtility.HtmlEncode(t)}</li>"));
                abnormalBlock =
                    $"<p><strong>Abnormal results ({abnormalTestNames.Count}):</strong></p><ul>{items}</ul>" +
                    "<p>Please discuss these findings with your doctor.</p>";
            }

            if (model.NotifyType == "Appointment")
            {
                return $"""
					<p>Hello {System.Net.WebUtility.HtmlEncode(patientName)},</p>
					<p>Your lab results for request <strong>{System.Net.WebUtility.HtmlEncode(request.RequestNumber)}</strong>
					(requested {request.RequestDate:dd MMM yyyy}) are ready.</p>
					<p>Your doctor has asked you to <strong>schedule an appointment</strong> to discuss your results in person.</p>
					{noteBlock}
					{abnormalBlock}
					<p>You can also sign in to the NMB-HLabSys patient portal to review your results online.</p>
					<p style="color:#666;font-size:12px;">Nelson Mandela Bay Haematology Diagnostic Laboratories</p>
					""";
            }

            return $"""
				<p>Hello {System.Net.WebUtility.HtmlEncode(patientName)},</p>
				<p>Your lab results for request <strong>{System.Net.WebUtility.HtmlEncode(request.RequestNumber)}</strong>
				(requested {request.RequestDate:dd MMM yyyy}) have been released by your doctor.</p>
				<p>A PDF copy of your full laboratory report is attached to this email.</p>
				{noteBlock}
				{abnormalBlock}
				<p>You can also sign in to the NMB-HLabSys patient portal at any time to view these results.</p>
				<p style="color:#666;font-size:12px;">Nelson Mandela Bay Haematology Diagnostic Laboratories</p>
				""";
        }

        //ALERTS

        [HttpGet]
        public IActionResult ActivityTrendJson()
        {
            var guard = RequireDoctor(); if (guard != null) return Json(Array.Empty<object>());
            var doctorId = GetDoctorId();

            var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);

            var counts = _context.TestRequests
                .AsNoTracking()
                .Where(tr => tr.DoctorId == doctorId && tr.RequestDate >= start)
                .GroupBy(tr => new { tr.RequestDate.Year, tr.RequestDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToList()
                .ToDictionary(x => (x.Year, x.Month), x => x.Count);

            var rows = new List<object>();
            for (var i = 0; i < 6; i++)
            {
                var month = start.AddMonths(i);
                var label = month.ToString("MMM yyyy");
                counts.TryGetValue((month.Year, month.Month), out var count);
                rows.Add(new { label, count });
            }

            return Json(rows);
        }
        // ═══════════════════════════════════════════════════════════
        // PATIENTS
        // ═══════════════════════════════════════════════════════════

        public IActionResult Patients()
        {
            var guard = RequireDoctor();
            if (guard != null) return guard;

            var doctorId = GetDoctorId();

            var list = _context.Patients
                .AsNoTracking()
                .Include(p => p.User)
                .Where(p =>
                    p.DoctorPatientConsents.Any(c =>
                        c.DoctorId == doctorId &&
                        c.IsActive)
                    ||
                    p.TestRequests.Any(tr =>
                        tr.DoctorId == doctorId)
                )
                .OrderBy(p => p.Surname)
                .ToList();

            return View(list);
        }
        [HttpGet]
        public IActionResult CreatePatient()
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            return View(new PatientCreateViewModel
            {
                AvailableConditions = GetConditions(),
                AvailableAllergies = GetAllergies(),
                AvailableMedications = GetMedications()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CreatePatient(PatientCreateViewModel model)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            model.SAIDNumber = model.SAIDNumber?.Trim() ?? string.Empty;
            model.CellNumber = model.CellNumber?.Trim();
            model.AvailableConditions = GetConditions();
            model.AvailableAllergies = GetAllergies();
            model.AvailableMedications = GetMedications();

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
                ModelState.AddModelError(nameof(model.SAIDNumber), "A patient with this SA ID number already exists.");

            if (_context.Users.Any(u => u.Email == model.Email))
                ModelState.AddModelError(nameof(model.Email), "A user with this e-mail address already exists.");

            if (!ModelState.IsValid) return View(model);

            var tempPw = _auth.GenerateTemporaryPassword();
            var pwHash = _auth.HashPassword(tempPw);

            var user = new User
            {
                Email = model.Email,
                PasswordHash = pwHash,
                Role = "Patient",
                IsFirstLogin = true
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            var patient = new Patient
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
            };
            _context.Patients.Add(patient);
            _context.SaveChanges();

            foreach (var cId in model.SelectedConditionIds)
            {
                _context.PatientConditions.Add(new PatientCondition
                {
                    PatientId = patient.PatientId,
                    ConditionId = cId,
                    IsDoctorAdded = true
                });
            }
            foreach (var aId in model.SelectedAllergyIds)
            {
                _context.PatientAllergies.Add(new PatientAllergy
                {
                    PatientId = patient.PatientId,
                    AllergyId = aId,
                    IsDoctorAdded = true
                });
            }
            foreach (var mId in model.SelectedMedicationIds)
            {
                _context.PatientMedications.Add(new PatientMedication
                {
                    PatientId = patient.PatientId,
                    MedicationId = mId,
                    IsDoctorAdded = true
                });
            }
            _context.SaveChanges();

            _email.Send(model.Email, "NMB-HLabSys – Your Patient Account",
                $"<p>Hello {model.Name}, your temporary password is: <strong>{tempPw}</strong>. Please change it on first login.</p>");

            TempData["Success"] = "Patient created. You can now submit a test request for them.";
            return RedirectToAction(nameof(CreateRequest), new { patientId = patient.PatientId });
        }

        public IActionResult PatientDetails(int id)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            var doctorId = GetDoctorId();

            // Only allow access if this doctor has an active consent for the patient
            // OR has previously submitted a request for them (same rule as the Patients list).
            var canAccess = _context.Patients.Any(p =>
                p.PatientId == id &&
                (p.DoctorPatientConsents.Any(c => c.DoctorId == doctorId && c.IsActive) ||
                 p.TestRequests.Any(tr => tr.DoctorId == doctorId)));

            if (!canAccess)
            {
                TempData["Error"] = "You do not have access to this patient's record.";
                return RedirectToAction(nameof(Patients));
            }

            var patient = _context.Patients
                .Include(p => p.User)
                .Include(p => p.PatientConditions).ThenInclude(pc => pc.Condition)
                .Include(p => p.PatientAllergies).ThenInclude(pa => pa.Allergy)
                .Include(p => p.PatientMedications).ThenInclude(pm => pm.Medication)
                .FirstOrDefault(p => p.PatientId == id);

            if (patient == null) return NotFound();

            var result = new Patient
            {
                PatientId = patient.PatientId,
                Name = patient.Name,
                Surname = patient.Surname,
                SAIDNumber = patient.SAIDNumber,
                DateOfBirth = patient.DateOfBirth,
                CellNumber = patient.CellNumber,
                User = new User { Email = patient.User.Email },
                Conditions = patient.PatientConditions.Where(pc => pc.IsActive && pc.Condition != null).Select(pc => new MedicalCondition
                {
                    ConditionId = pc.Condition.ConditionId,
                    ConditionName = pc.Condition.ConditionName
                }).ToList(),
                Allergies = patient.PatientAllergies.Where(pa => pa.IsActive && pa.Allergy != null).Select(pa => new Allergy
                {
                    AllergyId = pa.Allergy.AllergyId,
                    AllergyName = pa.Allergy.AllergyName
                }).ToList(),
                Medications = patient.PatientMedications.Where(pm => pm.IsActive && pm.Medication != null).Select(pm => new Medication
                {
                    MedicationId = pm.Medication.MedicationId,
                    MedicationName = pm.Medication.MedicationName
                }).ToList()
            };

            return View(result);
        }
        //TRACK REQUEST 
        public IActionResult TrackRequests(string? status = null)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            var doctorId = GetDoctorId();

            var query = _context.RequestTestTypes
                .Include(rtt => rtt.Request).ThenInclude(tr => tr.Patient)
                .Include(rtt => rtt.TestType)
                .Where(rtt => rtt.Request.DoctorId == doctorId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(rtt => rtt.Request.Status == status);

            var rows = query
                .OrderByDescending(rtt => rtt.Request.RequestDate)
                .ThenBy(rtt => rtt.TestType.TestName)
                .Select(rtt => new TrackRequestRowViewModel
                {
                    RequestId = rtt.RequestId,
                    PatientId = rtt.Request.PatientId,
                    RequestNumber = rtt.Request.RequestNumber,
                    PatientName = rtt.Request.Patient.Name,
                    PatientSurname = rtt.Request.Patient.Surname,
                    PatientDateOfBirth = rtt.Request.Patient.DateOfBirth,
                    TestName = rtt.TestType.TestName,
                    RequestDate = rtt.Request.RequestDate,
                    Urgency = rtt.Request.Urgency,
                    RequestStatus = rtt.Request.Status,
                    TestStatus = rtt.Status
                })
                .ToList();

            ViewBag.StatusFilter = status;
            return View("TrackRequests", rows);
        }
        [HttpGet]
        public IActionResult SelectPatient()
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            var doctorId = GetDoctorId();

            var patients = _context.Patients
                .AsNoTracking()
                .Where(p =>
                    p.DoctorPatientConsents.Any(c => c.DoctorId == doctorId && c.IsActive) ||
                    p.TestRequests.Any(tr => tr.DoctorId == doctorId))
                .OrderBy(p => p.Surname)
                .ThenBy(p => p.Name)
                .Select(p => new Patient
                {
                    PatientId = p.PatientId,
                    Name = p.Name,
                    Surname = p.Surname,
                    DateOfBirth = p.DateOfBirth,
                    User = new User { Email = p.User != null ? p.User.Email : string.Empty }
                })
                .ToList();

            return View(patients);
        }
        // ═══════════════════════════════════════════════════════════
        // TEST REQUESTS
        // ═══════════════════════════════════════════════════════════


        [HttpGet]
        public IActionResult FindPatientForRequest(string? q)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;

            List<Patient> results = new();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                if (term.Length == 13 && term.All(char.IsDigit))
                {
                    results = _context.Patients
                        .AsNoTracking()
                        .Include(p => p.User)
                        .Where(p => p.SAIDNumber == term)
                        .ToList();
                }
                else
                {
                    ViewBag.SearchError = "Enter the patient's complete 13-digit South African ID number.";
                }
            }

            ViewBag.Query = q;
            return View(results);
        }

        [HttpGet]
        public IActionResult CreateRequest(int patientId)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;

            var patient = _context.Patients
                .Where(p => p.PatientId == patientId)
                .Select(p => new Patient { PatientId = p.PatientId, Name = p.Name, Surname = p.Surname })
                .FirstOrDefault();

            if (patient == null) return NotFound();

            var vm = new CreateTestRequestViewModel
            {
                PatientId = patientId,
                PatientName = $"{patient.Name} {patient.Surname}",
                AvailableTestTypes = GetTestTypes()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CreateRequest(CreateTestRequestViewModel model)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            model.AvailableTestTypes = GetTestTypes();
            model.SelectedTestTypeIds = (model.SelectedTestTypeIds ?? new List<int>()).Distinct().ToList();
            var selectedTypes = model.AvailableTestTypes
                .Where(t => model.SelectedTestTypeIds.Contains(t.TestTypeId))
                .ToList();
            if (!_context.Patients.Any(p => p.PatientId == model.PatientId))
                ModelState.AddModelError(nameof(model.PatientId), "Select a registered patient.");
            if (selectedTypes.Count != model.SelectedTestTypeIds.Count)
                ModelState.AddModelError(nameof(model.SelectedTestTypeIds), "One or more selected test types are unavailable.");
            if (!selectedTypes.Any())
                ModelState.AddModelError("SelectedTestTypeIds", "At least one test type is required.");

            if (!ModelState.IsValid) return View(model);

            var doctorId = GetDoctorId();
            var reqNumber = $"REQ-{DateTime.Now:yyyyMMddHHmmss}";

            var request = new TestRequest
            {
                RequestNumber = reqNumber,
                PatientId = model.PatientId,
                DoctorId = doctorId,
                RequestDate = DateTime.Now,
                Urgency = model.Urgency,
                ClinicalNotes = model.ClinicalNotes,
                Status = "Submitted"
            };
            _context.TestRequests.Add(request);
            _context.SaveChanges();

            var requestTests = new List<RequestTestType>();
            foreach (var testType in selectedTypes)
            {
                var requestTest = new RequestTestType
                {
                    RequestId = request.RequestId,
                    TestTypeId = testType.TestTypeId,
                    Status = "Submitted"
                };
                requestTests.Add(requestTest);
                _context.RequestTestTypes.Add(requestTest);
            }
            _context.SaveChanges();

            foreach (var requestTest in requestTests)
            {
                _context.RequestSamples.Add(new RequestSample
                {
                    RequestId = request.RequestId,
                    RequestTestId = requestTest.RequestTestId,
                    Barcode = $"NMB-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}".ToUpperInvariant(),
                    SampleTypeId = selectedTypes.Single(t => t.TestTypeId == requestTest.TestTypeId).SampleTypeId
                });
            }
            _context.SaveChanges();

            // Get patient email – using projection avoids needing Include
            var patientEmail = _context.Patients
                .Where(p => p.PatientId == model.PatientId)
                .Select(p => p.User.Email)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(patientEmail))
                _email.Send(patientEmail, "Test Request Submitted – NMB-HLabSys",
                    $"<p>A lab test request ({reqNumber}) has been submitted for you. You can track progress in the portal.</p>");

            TempData["Success"] = $"Request {reqNumber} submitted.";
            return RedirectToAction(nameof(RequestDetails), new { id = request.RequestId });
        }

        public IActionResult RequestDetails(int id)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            var doctorId = GetDoctorId();

            var request = _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestTestTypes).ThenInclude(rtt => rtt.TestType)
                .Include(tr => tr.RequestSamples).ThenInclude(s => s.SampleType)
                .Include(tr => tr.RequestSamples).ThenInclude(s => s.RequestTest).ThenInclude(rt => rt!.TestType)
                .FirstOrDefault(tr => tr.RequestId == id && tr.DoctorId == doctorId);

            if (request == null) return NotFound();

            var cancellationReason = _context.RequestCancellations
                .AsNoTracking()
                .Where(c => c.RequestId == id)
                .OrderByDescending(c => c.CancelledAt)
                .Select(c => c.Reason)
                .FirstOrDefault();
            var release = _context.ResultReleases
                .AsNoTracking()
                .Where(r => r.RequestId == id)
                .OrderByDescending(r => r.ReleasedAt)
                .FirstOrDefault();

            var testResults = request.RequestTestTypes.Select(rtt => new RequestTestType
            {
                RequestTestId = rtt.RequestTestId,
                TestType = new TestType
                {
                    TestName = rtt.TestType.TestName,
                    Units = rtt.TestType.Units,
                    NormalRangeMin = rtt.TestType.NormalRangeMin,
                    NormalRangeMax = rtt.TestType.NormalRangeMax
                },
                Status = rtt.Status,
                ResultValue = rtt.ResultValue,
                ResultNotes = rtt.ResultNotes,
                IsAbnormal = rtt.IsAbnormal
            }).ToList();

            var viewModel = new TestRequestDetailsViewModel
            {
                Request = new TestRequest
                {
                    RequestId = request.RequestId,
                    RequestNumber = request.RequestNumber,
                    PatientId = request.PatientId,
                    Patient = new Patient { Name = request.Patient.Name, Surname = request.Patient.Surname },
                    RequestDate = request.RequestDate,
                    Urgency = request.Urgency,
                    Status = request.Status,
                    ClinicalNotes = request.ClinicalNotes,
                    RequestSamples = request.RequestSamples
                },
                TestResults = testResults,
                CancellationReason = cancellationReason,
                ReleasedAt = release?.ReleasedAt,
                ReleaseDoctorNote = release?.DoctorNote,
                ReleaseNotifyType = release?.NotifyType
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult ProposeAppointment(int patientId, int? requestId)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            var doctorId = GetDoctorId();
            var patient = _context.Patients
                .AsNoTracking()
                .Where(p => p.PatientId == patientId &&
                    (p.TestRequests.Any(r => r.DoctorId == doctorId) ||
                     p.DoctorPatientConsents.Any(c => c.DoctorId == doctorId && c.IsActive)))
                .Select(p => new { p.PatientId, p.Name, p.Surname })
                .FirstOrDefault();
            if (patient == null) return NotFound();

            string? requestNumber = null;
            if (requestId.HasValue)
            {
                requestNumber = _context.TestRequests
                    .Where(r => r.RequestId == requestId && r.PatientId == patientId && r.DoctorId == doctorId)
                    .Select(r => r.RequestNumber)
                    .FirstOrDefault();
                if (requestNumber == null) return NotFound();
            }

            return View(new ProposeAppointmentViewModel
            {
                PatientId = patient.PatientId,
                PatientName = $"{patient.Name} {patient.Surname}",
                RequestId = requestId,
                RequestNumber = requestNumber,
                ProposedAt = DateTime.Now.AddDays(1).Date.AddHours(9)
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ProposeAppointment(ProposeAppointmentViewModel model)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            var doctorId = GetDoctorId();
            var patient = _context.Patients
                .Include(p => p.User)
                .Where(p => p.PatientId == model.PatientId &&
                    (p.TestRequests.Any(r => r.DoctorId == doctorId) ||
                     p.DoctorPatientConsents.Any(c => c.DoctorId == doctorId && c.IsActive)))
                .FirstOrDefault();
            if (patient == null) return NotFound();

            TestRequest? request = null;
            if (model.RequestId.HasValue)
            {
                request = _context.TestRequests.FirstOrDefault(r =>
                    r.RequestId == model.RequestId && r.PatientId == model.PatientId && r.DoctorId == doctorId);
                if (request == null) return NotFound();
            }

            model.PatientName = $"{patient.Name} {patient.Surname}";
            model.RequestNumber = request?.RequestNumber;
            if (model.ProposedAt <= DateTime.Now)
                ModelState.AddModelError(nameof(model.ProposedAt), "Propose a date and time in the future.");
            if (!ModelState.IsValid) return View(model);

            var appointment = new PatientAppointment
            {
                PatientId = patient.PatientId,
                DoctorId = doctorId,
                RequestId = request?.RequestId,
                ProposedAt = model.ProposedAt,
                Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim()
            };
            _context.PatientAppointments.Add(appointment);
            _context.SaveChanges();

            var emailSent = false;
            if (!string.IsNullOrWhiteSpace(patient.User?.Email))
            {
                var date = appointment.ProposedAt.ToString("dddd, dd MMMM yyyy 'at' HH:mm");
                var doctorName = _context.Doctors
                    .Where(d => d.DoctorId == doctorId)
                    .Select(d => d.Name + " " + d.Surname)
                    .FirstOrDefault() ?? "your doctor";
                var note = string.IsNullOrWhiteSpace(appointment.Notes)
                    ? string.Empty
                    : $"<p>{System.Net.WebUtility.HtmlEncode(appointment.Notes)}</p>";
                var link = Url.Action("Appointments", "Patient", null, Request.Scheme);
                emailSent = TrySendEmail(patient.User.Email, "Appointment proposed – NMB-HLabSys",
                    $"<p>Dr {System.Net.WebUtility.HtmlEncode(doctorName)} has proposed an appointment for {date}.</p>{note}<p>Sign in to your patient portal to confirm or decline: <a href=\"{link}\">Review appointment</a>.</p>");
            }

            TempData[emailSent ? "Success" : "Warning"] = emailSent
                ? "Appointment proposed and sent to the patient. They can confirm or decline it from their portal."
                : "Appointment proposal saved, but the patient could not be emailed. They can still respond from their portal.";
            return request == null
                ? RedirectToAction(nameof(PatientDetails), new { id = patient.PatientId })
                : RedirectToAction(nameof(RequestDetails), new { id = request.RequestId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult CancelRequest(int requestId, string reason)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            var doctorId = GetDoctorId();
            var userId = _auth.GetCurrentUserId()!.Value;

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "A reason is required to cancel a request.";
                return RedirectToAction(nameof(RequestDetails), new { id = requestId });
            }

            var request = _context.TestRequests
                .Include(tr => tr.RequestTestTypes)
                .FirstOrDefault(tr => tr.RequestId == requestId && tr.DoctorId == doctorId);

            if (request == null || (request.Status != "Submitted" && request.Status != "SamplesReceived"))
            {
                TempData["Error"] = "This request cannot be cancelled at its current status.";
                return RedirectToAction(nameof(RequestDetails), new { id = requestId });
            }

            request.Status = "Cancelled";

            // Cascade: cancel any test type on this request that hasn't already
            // finished, so it stops appearing as available/in-progress lab work.
            foreach (var rtt in request.RequestTestTypes.Where(t => t.Status != "Completed" && t.Status != "Verified"))
            {
                rtt.Status = "Cancelled";
                rtt.CancelReason = reason.Trim();
            }

            _context.RequestCancellations.Add(new RequestCancellation
            {
                RequestId = requestId,
                CancelledBy = userId,
                Reason = reason.Trim()
            });
            _context.SaveChanges();

            TempData["Success"] = "Request cancelled.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ReleaseResults(int requestId)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            var doctorId = GetDoctorId();

            var request = _context.TestRequests
                .Include(tr => tr.Patient).ThenInclude(p => p.User)
                .FirstOrDefault(tr => tr.RequestId == requestId && tr.DoctorId == doctorId);

            if (request == null) return NotFound();
            if (request.Status == "ReleasedByDoctor")
                return RedirectToAction(nameof(RequestDetails), new { id = requestId });
            if (request.Status != "Completed")
            {
                TempData["Error"] = "Results can only be released when the request status is Completed.";
                return RedirectToAction(nameof(RequestDetails), new { id = requestId });
            }

            var abnormalTests = _context.RequestTestTypes
                .AsNoTracking()
                .Where(rtt => rtt.RequestId == requestId && rtt.IsAbnormal)
                .Include(rtt => rtt.TestType)
                .Select(rtt => rtt.TestType!.TestName)
                .ToList();

            return View(new ReleaseResultViewModel
            {
                RequestId = requestId,
                RequestNumber = request.RequestNumber,
                PatientName = $"{request.Patient.Name} {request.Patient.Surname}",
                PatientEmail = request.Patient.User?.Email,
                AbnormalCount = abnormalTests.Count,
                AbnormalTestNames = abnormalTests
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ReleaseResults(ReleaseResultViewModel model)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            model.NotifyType = "Email";
            if (!ModelState.IsValid)
            {
                PopulateReleaseResultContext(model);
                return View(model);
            }

            var doctorId = GetDoctorId();

            var request = _context.TestRequests
                .FirstOrDefault(tr => tr.RequestId == model.RequestId && tr.DoctorId == doctorId);
            if (request == null) return NotFound();
            if (request.Status == "ReleasedByDoctor")
            {
                TempData["Error"] = "These results have already been released.";
                return RedirectToAction(nameof(RequestDetails), new { id = model.RequestId });
            }
            if (request.Status != "Completed")
            {
                TempData["Error"] = "Results can only be released when the request status is Completed.";
                return RedirectToAction(nameof(RequestDetails), new { id = model.RequestId });
            }

            request.Status = "ReleasedByDoctor";
            _context.ResultReleases.Add(new ResultRelease
            {
                RequestId = model.RequestId,
                DoctorId = doctorId,
                ReleasedAt = DateTime.Now,
                DoctorNote = model.DoctorNote,
                NotifyType = model.NotifyType
            });
            _context.SaveChanges();

            var patient = _context.Patients
                .Include(p => p.User)
                .FirstOrDefault(p => p.PatientId == request.PatientId);

            if (patient?.User == null || string.IsNullOrWhiteSpace(patient.User.Email))
            {
                TempData["Warning"] = "Results released successfully, but the patient has no email address on file. They can view results in the patient portal.";
                return RedirectToAction(nameof(RequestDetails), new { id = model.RequestId });
            }

            var abnormalTestNames = _context.RequestTestTypes
                .AsNoTracking()
                .Where(rtt => rtt.RequestId == model.RequestId && rtt.IsAbnormal)
                .Include(rtt => rtt.TestType)
                .Select(rtt => rtt.TestType!.TestName)
                .ToList();

            var emailBody = BuildReleaseResultsEmailHtml(patient, request, model, abnormalTestNames);
            const string subject = "Lab Results Available – NMB-HLabSys";
            var pdfFileName = $"NMB_Results_{request.RequestNumber}.pdf";
            bool emailSent;

            try
            {
                var pdfBytes = _pdf.GenerateRequestResultsPdf(model.RequestId);
                emailSent = TrySendEmailWithAttachment(patient.User.Email, subject, emailBody, pdfBytes, pdfFileName);
            }
            catch (InvalidOperationException)
            {
                emailSent = TrySendEmail(patient.User.Email, subject, emailBody);
            }

            TempData[emailSent ? "Success" : "Warning"] = emailSent
                ? $"Results released and emailed to {patient.User.Email} with PDF attachment."
                : "Results released successfully, but the email could not be sent (SMTP may be unavailable). The patient can still view results in the portal.";

            return RedirectToAction(nameof(RequestDetails), new { id = model.RequestId });
        }
        //ALERTS IAction
        public IActionResult Alerts(DateTime? from = null)
        {
            var guard = RequireDoctor();

            if (guard != null)
                return guard;

            var doctorId = GetDoctorId();
            var since = from ?? DateTime.Today.AddDays(-5);

            var alerts = _context.RequestTestTypes
                .AsNoTracking()
                .Include(rtt => rtt.Request)
                    .ThenInclude(tr => tr.Patient)
                .Include(rtt => rtt.TestType)
                .Where(rtt =>
                    rtt.Request != null &&
                    rtt.TestType != null &&
                    rtt.Request.DoctorId == doctorId &&
                    rtt.IsAbnormal &&
                    rtt.CompletedAt.HasValue &&
                    rtt.CompletedAt.Value >= since)
                .OrderByDescending(rtt => rtt.CompletedAt)
                .Select(rtt => new DoctorAlertViewModel
                {
                    RequestId = rtt.RequestId,
                    RequestTestId = rtt.RequestTestId,
                    RequestNumber = rtt.Request!.RequestNumber,
                    PatientName = rtt.Request.Patient!.Name + " " +
                                  rtt.Request.Patient.Surname,
                    TestName = rtt.TestType!.TestName,
                    ResultValue = rtt.ResultValue,
                    Units = rtt.TestType.Units,
                    NormalRangeMin = rtt.TestType.NormalRangeMin,
                    NormalRangeMax = rtt.TestType.NormalRangeMax,
                    CompletedAt = rtt.CompletedAt
                })
                .ToList();

            ViewBag.Since = since;

            return View(alerts);
        }
        [HttpGet]
        public IActionResult AlertsJson(DateTime? from = null)
        {
            var guard = RequireDoctor();

            if (guard != null)
                return Json(Array.Empty<DoctorAlertViewModel>());

            var doctorId = GetDoctorId();
            var since = from ?? DateTime.Today.AddDays(-5);

            var alerts = _context.RequestTestTypes
                .AsNoTracking()
                .Include(rtt => rtt.Request)
                    .ThenInclude(tr => tr.Patient)
                .Include(rtt => rtt.TestType)
                .Where(rtt =>
                    rtt.Request != null &&
                    rtt.TestType != null &&
                    rtt.Request.DoctorId == doctorId &&
                    rtt.IsAbnormal &&
                    rtt.CompletedAt.HasValue &&
                    rtt.CompletedAt.Value >= since)
                .OrderByDescending(rtt => rtt.CompletedAt)
                .Select(rtt => new DoctorAlertViewModel
                {
                    RequestId = rtt.RequestId,
                    RequestTestId = rtt.RequestTestId,
                    RequestNumber = rtt.Request!.RequestNumber,
                    PatientName = rtt.Request.Patient!.Name + " " +
                                  rtt.Request.Patient.Surname,
                    TestName = rtt.TestType!.TestName,
                    ResultValue = rtt.ResultValue,
                    Units = rtt.TestType.Units,
                    NormalRangeMin = rtt.TestType.NormalRangeMin,
                    NormalRangeMax = rtt.TestType.NormalRangeMax,
                    CompletedAt = rtt.CompletedAt
                })
                .ToList();

            return Json(alerts);
        }
        // ── AJAX: recent requests for PatientDetails page ─────────
        [HttpGet]
        public IActionResult PatientRequestsJson(int patientId)
        {
            var guard = RequireDoctor(); if (guard != null) return Json(Array.Empty<object>());
            var doctorId = GetDoctorId();

            var rows = _context.TestRequests
                .Where(tr => tr.PatientId == patientId && tr.DoctorId == doctorId)
                .OrderByDescending(tr => tr.RequestDate)
                .Select(tr => new
                {
                    requestId = tr.RequestId,
                    requestNumber = tr.RequestNumber,
                    requestDate = tr.RequestDate,
                    urgency = tr.Urgency,
                    status = tr.Status,
                    doctorName = tr.Doctor == null ? "—" : $"Dr {tr.Doctor.Name} {tr.Doctor.Surname}"
                })
                .ToList();

            return Json(rows);
        }

        [HttpGet]
        public IActionResult PatientHistoryJson(int patientId)
        {
            var guard = RequireDoctor(); if (guard != null) return Json(Array.Empty<object>());
            var doctorId = GetDoctorId();
            if (!CanAccessPatient(doctorId, patientId)) return Forbid();

            var history = _context.RequestTestTypes
                .AsNoTracking()
                .Where(test => test.Request!.PatientId == patientId &&
                    (test.Request.DoctorId == doctorId ||
                     _context.DoctorPatientConsents.Any(c =>
                         c.RequestTestId == test.RequestTestId && c.DoctorId == doctorId && c.IsActive)) &&
                    test.Request.Status == "ReleasedByDoctor" &&
                    test.ResultValue.HasValue)
                .OrderByDescending(test => test.CompletedAt ?? test.Request!.RequestDate)
                .Select(test => new
                {
                    requestNumber = test.Request!.RequestNumber,
                    requestDate = test.Request.RequestDate,
                    completedAt = test.CompletedAt,
                    category = test.TestType!.Category!.CategoryName,
                    testName = test.TestType.TestName,
                    result = test.ResultValue,
                    units = test.TestType.Units,
                    minimum = test.TestType.NormalRangeMin,
                    maximum = test.TestType.NormalRangeMax,
                    abnormal = test.IsAbnormal,
                    doctor = test.Request.Doctor == null ? "—" : $"Dr {test.Request.Doctor.Name} {test.Request.Doctor.Surname}"
                })
                .ToList();
            return Json(history);
        }

        // ── AJAX: results for PatientResults page ─────────────────
        [HttpGet]
        public IActionResult RequestResultsJson(int requestId)
        {
            var guard = RequireDoctor(); if (guard != null) return Json(Array.Empty<object>());
            var doctorId = GetDoctorId();



            // I just Added this and commentet out the above
            var canAccess = _context.TestRequests
                .Any(tr => tr.RequestId == requestId &&
                    (tr.DoctorId == doctorId ||
                     _context.DoctorPatientConsents.Any(c =>
                         c.RequestId == tr.RequestId &&
                         c.RequestTestId != null &&
                         c.DoctorId == doctorId && c.IsActive)));



            if (!canAccess) return Json(Array.Empty<object>());

            var results = _context.RequestTestTypes
                .Include(rtt => rtt.TestType)
                .Where(rtt => rtt.RequestId == requestId &&
                              (rtt.Status == "Verified" || rtt.Status == "Completed") &&
                              (rtt.Request!.DoctorId == doctorId ||
                               _context.DoctorPatientConsents.Any(c =>
                                   c.RequestTestId == rtt.RequestTestId &&
                                   c.DoctorId == doctorId &&
                                   c.IsActive)))
                .Select(rtt => new
                {
                    requestId = rtt.RequestId,
                    testName = rtt.TestType.TestName,
                    resultValue = rtt.ResultValue,
                    units = rtt.TestType.Units,
                    normalRangeMin = rtt.TestType.NormalRangeMin,
                    normalRangeMax = rtt.TestType.NormalRangeMax,
                    isAbnormal = rtt.IsAbnormal,
                    resultNotes = rtt.ResultNotes
                })
                .ToList();

            return Json(results);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EmailResults(int requestId, string? message)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            var doctorId = GetDoctorId();

            var request = _context.TestRequests
                .Include(tr => tr.Patient).ThenInclude(p => p.User)
                .FirstOrDefault(tr => tr.RequestId == requestId &&
                    tr.DoctorId == doctorId &&
                    (tr.Status == "Completed" || tr.Status == "ReleasedByDoctor"));

            if (request == null)
            {
                TempData["Error"] = "Only your completed or released requests can be emailed.";
                return RedirectToAction(nameof(TrackRequests));
            }

            var email = request.Patient?.User?.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "This patient does not have an email address on file.";
                return RedirectToAction(nameof(PatientResults), new { patientId = request.PatientId });
            }

            var abnormalTestNames = _context.RequestTestTypes
                .AsNoTracking()
                .Where(rtt => rtt.RequestId == requestId &&
                    rtt.Status == "Verified" && rtt.IsAbnormal)
                .Include(rtt => rtt.TestType)
                .Select(rtt => rtt.TestType!.TestName)
                .ToList();

            var safeMessage = string.IsNullOrWhiteSpace(message)
                ? "Please find your laboratory results attached. Contact your doctor if you have any questions."
                : System.Net.WebUtility.HtmlEncode(message.Trim());
            var patientName = System.Net.WebUtility.HtmlEncode(
                $"{request.Patient?.Name} {request.Patient?.Surname}".Trim());
            var abnormalText = abnormalTestNames.Count == 0
                ? "<p>All reported results are within the normal reference range.</p>"
                : $"<p><strong>Abnormal results:</strong> {string.Join(", ", abnormalTestNames.Select(System.Net.WebUtility.HtmlEncode))}</p>";
            var body = $"""
                <p>Hello {patientName},</p>
                <p>{safeMessage}</p>
                {abnormalText}
                <p>Please contact your doctor if you need help interpreting these results.</p>
                """;

            try
            {
                var pdf = _pdf.GenerateRequestResultsPdf(requestId);
                _email.SendWithAttachment(email, $"Laboratory Results – {request.RequestNumber}",
                    body, pdf, $"NMB_Results_{request.RequestNumber}.pdf");
                TempData["Success"] = $"Results emailed to {email}.";
            }
            catch (InvalidOperationException)
            {
                TempData["Error"] = "The results could not be emailed. Check the email configuration and try again.";
            }
            catch (System.Net.Mail.SmtpException)
            {
                TempData["Error"] = "The results could not be emailed. Check the email configuration and try again.";
            }

            return RedirectToAction(nameof(PatientResults), new { patientId = request.PatientId });
        }

        // ── PATIENT RESULTS (own requests + per-request consent) ──
        public IActionResult PatientResults(int patientId)
        {
            var guard = RequireDoctor(); if (guard != null) return guard;
            var doctorId = GetDoctorId();

            var hasAccess = _context.DoctorPatientConsents
                .Any(c => c.PatientId == patientId && c.DoctorId == doctorId &&
                    c.RequestTestId != null && c.IsActive)
                || _context.TestRequests
                    .Any(tr => tr.PatientId == patientId && tr.DoctorId == doctorId);

            if (!hasAccess)
                return RedirectToAction(nameof(Patients));

            var patient = _context.Patients
                .Where(p => p.PatientId == patientId)
                .Select(p => new Patient { PatientId = p.PatientId, Name = p.Name, Surname = p.Surname })
                .FirstOrDefault();

            var requests = _context.TestRequests
                .Where(tr => tr.PatientId == patientId &&
                    (tr.Status == "Completed" || tr.Status == "ReleasedByDoctor") &&
                    (tr.DoctorId == doctorId ||
                     _context.DoctorPatientConsents.Any(c =>
                         c.RequestId == tr.RequestId &&
                         c.RequestTestId != null &&
                         c.DoctorId == doctorId && c.IsActive)))


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

            ViewBag.Patient = patient;
            ViewBag.Requests = requests;
            return View();
        }


        // ── AJAX: requests preview for Reports page ───────────────
        [HttpGet]
        public IActionResult RequestsPreviewJson(DateTime from, DateTime to)
        {
            var guard = RequireDoctor(); if (guard != null) return Json(Array.Empty<object>());
            var doctorId = GetDoctorId();

            var rows = _context.TestRequests
                .Include(tr => tr.Patient)
                .Include(tr => tr.RequestTestTypes)
                .Where(tr => tr.DoctorId == doctorId &&
                             tr.RequestDate >= from &&
                             tr.RequestDate <= to)
                .GroupBy(tr => new
                {
                    tr.RequestNumber,
                    PatientName = tr.Patient.Name + " " + tr.Patient.Surname,
                    tr.RequestDate,
                    tr.Urgency,
                    tr.Status
                })
                .Select(g => new
                {
                    requestNumber = g.Key.RequestNumber,
                    patient = g.Key.PatientName,
                    requestDate = g.Key.RequestDate,
                    urgency = g.Key.Urgency,
                    status = g.Key.Status,
                    testCount = g.SelectMany(tr => tr.RequestTestTypes).Count()
                })
                .OrderByDescending(r => r.requestDate)
                .ToList();

            return Json(rows);
        }
 

        // ── AJAX: status breakdown doughnut chart ─────────────────
        [HttpGet]
        public IActionResult StatusSummaryJson()
        {
            var guard = RequireDoctor(); if (guard != null) return Json(Array.Empty<object>());
            var doctorId = GetDoctorId();

            var rows = _context.TestRequests
                .Where(tr => tr.DoctorId == doctorId)
                .GroupBy(tr => tr.Status)
                .Select(g => new
                {
                    status = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(g => g.count)
                .ToList();

            return Json(rows);
        }

        // ── AJAX: patient list for patient-PDF dropdown ───────────
        [HttpGet]
        public IActionResult MyPatientsJson()
        {
            var guard = RequireDoctor(); if (guard != null) return Json(Array.Empty<object>());
            var doctorId = GetDoctorId();

            var rows = _context.Patients
                .AsNoTracking()
                .Where(p => p.TestRequests.Any(tr => tr.DoctorId == doctorId))
                .OrderBy(p => p.Surname)
                .ThenBy(p => p.Name)
                .Select(p => new
                {
                    patientId = p.PatientId,
                    name = p.Name,
                    surname = p.Surname,
                    dateOfBirth = p.DateOfBirth
                })
                .ToList();

            return Json(rows);
        }
        // ── PRIVATE HELPERS ───────────────────────────────────────

        private List<TestType> GetTestTypes()
        {
            return _context.TestTypes
                .Where(tt => tt.IsActive)
                .Include(tt => tt.Category)
                .Include(tt => tt.SampleType)
                .OrderBy(tt => tt.Category!.CategoryName)
                .ThenBy(tt => tt.TestName)
                .Select(tt => new TestType
                {
                    TestTypeId = tt.TestTypeId,
                    TestName = tt.TestName,
                    SampleTypeId = tt.SampleTypeId,
                    Category = new TestCategory { CategoryName = tt.Category.CategoryName },
                    SampleType = new SampleType { TypeName = tt.SampleType.TypeName }
                })
                .ToList();
        }

        private List<MedicalCondition> GetConditions()
        {
            return _context.MedicalConditions
                .Where(mc => mc.IsActive && mc.Category!.IsActive)
                .Include(mc => mc.Category)
                .OrderBy(mc => mc.Category!.CategoryName)
                .ThenBy(mc => mc.ConditionName)
                .Select(mc => new MedicalCondition
                {
                    ConditionId = mc.ConditionId,
                    ConditionName = mc.ConditionName,
                    Category = new MedicalConditionCategory { CategoryName = mc.Category!.CategoryName }
                })
                .ToList();
        }

        private List<Allergy> GetAllergies()
        {
            return _context.Allergies
                .Where(a => a.IsActive && a.Category!.IsActive)
                .Include(a => a.Category)
                .OrderBy(a => a.Category!.CategoryName)
                .ThenBy(a => a.AllergyName)
                .Select(a => new Allergy
                {
                    AllergyId = a.AllergyId,
                    AllergyName = a.AllergyName,
                    Category = new AllergyCategory { CategoryName = a.Category!.CategoryName }
                })
                .ToList();
        }

        private List<Medication> GetMedications()
        {
            return _context.Medications
                .Where(m => m.IsActive && m.Category!.IsActive)
                .Include(m => m.Category)
                .OrderBy(m => m.Category!.CategoryName)
                .ThenBy(m => m.MedicationName)
                .Select(m => new Medication
                {
                    MedicationId = m.MedicationId,
                    MedicationName = m.MedicationName,
                    Category = new MedicationCategory { CategoryName = m.Category!.CategoryName }
                })
                .ToList();
        }

        private static int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}