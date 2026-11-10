
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models.ViewModels;
using NMB_HLabSys.Services;
using System.Linq;

namespace NMB_HLabSys.Controllers
{
    /// <summary>
    /// Handles all PDF download actions for every role.
    /// </summary>
    public class ReportController : Controller
    {
        private readonly PdfService _pdf;
        private readonly AuthService _auth;
        private readonly ApplicationDbContext _context;

        public ReportController(PdfService pdf, AuthService auth, ApplicationDbContext context)
        {
            _pdf = pdf;
            _auth = auth;
            _context = context;
        }

        private int GetProfileId(string role)
        {
            var uid = _auth.GetCurrentUserId()!.Value;
            return role switch
            {
                "Doctor" => _context.Doctors.Where(d => d.UserId == uid).Select(d => d.DoctorId).FirstOrDefault(),
                "Technician" => _context.Technicians.Where(t => t.UserId == uid).Select(t => t.TechnicianId).FirstOrDefault(),
                "Patient" => _context.Patients.Where(p => p.UserId == uid).Select(p => p.PatientId).FirstOrDefault(),
                _ => 0
            };
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ManagerTestsByCategory(ReportFilterViewModel filter)
        {
            if (!_auth.IsInRole("Manager")) return Forbid();
            if (!ModelState.IsValid || filter.FromDate.Date > filter.ToDate.Date)
            {
                TempData["Error"] = "Choose a valid date range.";
                return RedirectToAction("Reports", "Manager");
            }

            var bytes = _pdf.GenerateTestsByCategoryReport(filter.FromDate, filter.ToDate);
            return File(bytes, "application/pdf",
                $"NMB_TestsByCategory_{filter.FromDate:yyyyMMdd}_{filter.ToDate:yyyyMMdd}.pdf");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DoctorRequestsReport(ReportFilterViewModel filter)
        {
            if (!_auth.IsInRole("Doctor")) return Forbid();
            if (!ModelState.IsValid || filter.FromDate.Date > filter.ToDate.Date)
            {
                TempData["Error"] = "Choose a valid date range.";
                return RedirectToAction("Reports", "Doctor");
            }

            var doctorId = GetProfileId("Doctor");
            var bytes = _pdf.GenerateDoctorRequestsReport(doctorId, filter.FromDate, filter.ToDate);
            return File(bytes, "application/pdf",
                $"NMB_DoctorRequests_{filter.FromDate:yyyyMMdd}_{filter.ToDate:yyyyMMdd}.pdf");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult DoctorPatientReport(int patientId, DateTime fromDate, DateTime toDate)
        {
            if (!_auth.IsInRole("Doctor")) return Forbid();
            if (patientId <= 0 || fromDate.Date > toDate.Date)
            {
                TempData["Error"] = "Choose a patient and a valid date range.";
                return RedirectToAction("Reports", "Doctor");
            }

            var doctorId = GetProfileId("Doctor");
            var access = _context.TestRequests
                .Any(tr => tr.PatientId == patientId && tr.DoctorId == doctorId)
                || _context.Patients
                .Any(p => p.PatientId == patientId
                    && p.DoctorPatientConsents.Any(c => c.DoctorId == doctorId && c.IsActive));

            if (!access) return Forbid();

            var bytes = _pdf.GeneratePatientResultsReport(patientId, fromDate, toDate);
            return File(bytes, "application/pdf",
                $"NMB_PatientResults_{patientId}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.pdf");
        }

        [HttpGet]
        public IActionResult RequestResultPdf(int requestId)
        {
            if (!_auth.IsInRole("Doctor")) return Forbid();

            var doctorId = GetProfileId("Doctor");
            var allowed = _context.TestRequests.Any(tr =>
                tr.RequestId == requestId &&
                (tr.Status == "Completed" || tr.Status == "ReleasedByDoctor") &&
                (tr.DoctorId == doctorId ||
                 _context.DoctorPatientConsents.Any(c =>
                     c.RequestId == requestId &&
                     c.DoctorId == doctorId &&
                     c.IsActive)));
            if (!allowed) return Forbid();

            var bytes = _pdf.GenerateRequestResultsPdf(requestId);
            return File(bytes, "application/pdf", $"NMB_Results_{requestId}.pdf");
        }

        [HttpGet]
        public IActionResult PatientRequestPdf(int requestId)
        {
            if (!_auth.IsInRole("Patient")) return Forbid();

            var patientId = GetProfileId("Patient");
            var owned = _context.TestRequests
                .Any(tr => tr.RequestId == requestId
                           && tr.PatientId == patientId
                           && tr.Status == "ReleasedByDoctor");

            if (!owned) return Forbid();

            var bytes = _pdf.GenerateRequestResultsPdf(requestId);
            return File(bytes, "application/pdf", $"NMB_Results_{requestId}.pdf");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult TechnicianReport(ReportFilterViewModel filter)
        {
            if (!_auth.IsInRole("Technician")) return Forbid();
            if (!ModelState.IsValid || filter.FromDate.Date > filter.ToDate.Date)
            {
                TempData["Error"] = "Choose a valid date range.";
                return RedirectToAction("Reports", "Technician");
            }

            var techId = GetProfileId("Technician");
            var bytes = _pdf.GenerateTechnicianReport(techId, filter.FromDate, filter.ToDate);
            return File(bytes, "application/pdf",
                $"NMB_TechReport_{filter.FromDate:yyyyMMdd}_{filter.ToDate:yyyyMMdd}.pdf");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult PatientResultsReport(ReportFilterViewModel filter, string reportType = "results")
        {
            if (!_auth.IsInRole("Patient")) return Forbid();
            if (!ModelState.IsValid || filter.FromDate.Date > filter.ToDate.Date)
            {
                TempData["Error"] = "Choose a valid date range.";
                return RedirectToAction("Reports", "Patient");
            }

            var patientId = GetProfileId("Patient");
            var toDateExclusive = filter.ToDate.Date.AddDays(1).AddTicks(-1);
            if (reportType == "requests")
            {
                var requestBytes = _pdf.GeneratePatientRequestHistoryReport(
                    patientId, filter.FromDate.Date, toDateExclusive);
                return File(requestBytes, "application/pdf",
                    $"NMB_PatientRequests_{filter.FromDate:yyyyMMdd}_{filter.ToDate:yyyyMMdd}.pdf");
            }

            if (reportType != "results") return BadRequest("Unknown report type.");

            var bytes = _pdf.GeneratePatientResultsReport(patientId, filter.FromDate.Date, toDateExclusive);
            return File(bytes, "application/pdf",
                $"NMB_PatientResults_{filter.FromDate:yyyyMMdd}_{filter.ToDate:yyyyMMdd}.pdf");
        }
    }
}



//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using NMB_HLabSys.Data;
//using NMB_HLabSys.Models.ViewModels;
//using NMB_HLabSys.Services;
//using System;
//using System.Linq;

//namespace NMB_HLabSys.Controllers
//{
//    /// <summary>
//    /// Handles PDF download actions for every role.
//    /// </summary>
//    public class ReportController : Controller
//    {
//        private readonly PdfService _pdf;
//        private readonly AuthService _auth;
//        private readonly ApplicationDbContext _context;

//        public ReportController(
//            PdfService pdf,
//            AuthService auth,
//            ApplicationDbContext context)
//        {
//            _pdf = pdf;
//            _auth = auth;
//            _context = context;
//        }

//        private int GetProfileId(string role)
//        {
//            var currentUserId = _auth.GetCurrentUserId();

//            if (!currentUserId.HasValue)
//                return 0;

//            var uid = currentUserId.Value;

//            return role switch
//            {
//                "Doctor" =>
//                    _context.Doctors
//                        .Where(d => d.UserId == uid)
//                        .Select(d => d.DoctorId)
//                        .FirstOrDefault(),

//                "Technician" =>
//                    _context.Technicians
//                        .Where(t => t.UserId == uid)
//                        .Select(t => t.TechnicianId)
//                        .FirstOrDefault(),

//                "Patient" =>
//                    _context.Patients
//                        .Where(p => p.UserId == uid)
//                        .Select(p => p.PatientId)
//                        .FirstOrDefault(),

//                _ => 0
//            };
//        }

//        // =========================================================
//        // MANAGER
//        // =========================================================

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public IActionResult ManagerTestsByCategory(
//            ReportFilterViewModel filter)
//        {
//            if (!_auth.IsInRole("Manager"))
//                return Forbid();

//            if (!ModelState.IsValid)
//                return RedirectToAction("Reports", "Manager");

//            var bytes = _pdf.GenerateTestsByCategoryReport(
//                filter.FromDate,
//                filter.ToDate);

//            return File(
//                bytes,
//                "application/pdf",
//                $"NMB_TestsByCategory_{filter.FromDate:yyyyMMdd}_{filter.ToDate:yyyyMMdd}.pdf");
//        }

//        // =========================================================
//        // DOCTOR
//        // =========================================================

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public IActionResult DoctorRequestsReport(
//            ReportFilterViewModel filter)
//        {
//            if (!_auth.IsInRole("Doctor"))
//                return Forbid();

//            if (!ModelState.IsValid)
//                return RedirectToAction("Reports", "Doctor");

//            var doctorId = GetProfileId("Doctor");

//            if (doctorId == 0)
//                return Forbid();

//            var bytes = _pdf.GenerateDoctorRequestsReport(
//                doctorId,
//                filter.FromDate,
//                filter.ToDate);

//            return File(
//                bytes,
//                "application/pdf",
//                $"NMB_DoctorRequests_{filter.FromDate:yyyyMMdd}_{filter.ToDate:yyyyMMdd}.pdf");
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public IActionResult DoctorPatientReport(
//            int patientId,
//            DateTime fromDate,
//            DateTime toDate)
//        {
//            if (!_auth.IsInRole("Doctor"))
//                return Forbid();

//            if (patientId == 0)
//                return RedirectToAction("Reports", "Doctor");

//            var doctorId = GetProfileId("Doctor");

//            if (doctorId == 0)
//                return Forbid();

//            // Make sure the doctor has access to this patient.
//            var access = _context.TestRequests
//                .Count(tr =>
//                    tr.PatientId == patientId &&
//                    tr.DoctorId == doctorId);

//            if (access == 0)
//                return Forbid();

//            var bytes = _pdf.GeneratePatientResultsReport(
//                patientId,
//                fromDate,
//                toDate);

//            return File(
//                bytes,
//                "application/pdf",
//                $"NMB_PatientResults_{patientId}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.pdf");
//        }

//        [HttpGet]
//        public IActionResult RequestResultPdf(int requestId)
//        {
//            if (!_auth.IsInRole("Doctor"))
//                return Forbid();

//            if (requestId <= 0)
//                return BadRequest();

//            var bytes = _pdf.GenerateRequestResultsPdf(requestId);

//            return File(
//                bytes,
//                "application/pdf",
//                $"NMB_Results_{requestId}.pdf");
//        }

//        // =========================================================
//        // TECHNICIAN
//        // =========================================================

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public IActionResult TechnicianReport(
//            ReportFilterViewModel filter)
//        {
//            if (!_auth.IsInRole("Technician"))
//                return Forbid();

//            if (!ModelState.IsValid)
//                return RedirectToAction("Reports", "Technician");

//            var technicianId = GetProfileId("Technician");

//            if (technicianId == 0)
//                return Forbid();

//            var bytes = _pdf.GenerateTechnicianReport(
//                technicianId,
//                filter.FromDate,
//                filter.ToDate);

//            return File(
//                bytes,
//                "application/pdf",
//                $"NMB_TechReport_{filter.FromDate:yyyyMMdd}_{filter.ToDate:yyyyMMdd}.pdf");
//        }

//        // =========================================================
//        // PATIENT
//        // =========================================================
//        [HttpGet]
//        public IActionResult Reports()
//        {
//            if (!_auth.IsInRole("Patient"))
//                return Forbid();

//            var model = new ReportFilterViewModel
//            {
//                FromDate = DateTime.Today.AddMonths(-1),
//                ToDate = DateTime.Today
//            };

//            return View(model);
//        }
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public IActionResult PatientResultsReport(
//            ReportFilterViewModel filter)
//        {
//            if (!_auth.IsInRole("Patient"))
//                return Forbid();

//            if (!ModelState.IsValid)
//                return RedirectToAction("Reports", "Patient");

//            var patientId = GetProfileId("Patient");

//            if (patientId == 0)
//                return Forbid();

//            var bytes = _pdf.GeneratePatientResultsReport(
//                patientId,
//                filter.FromDate,
//                filter.ToDate);

//            return File(
//                bytes,
//                "application/pdf",
//                $"NMB_PatientResults_{filter.FromDate:yyyyMMdd}_{filter.ToDate:yyyyMMdd}.pdf");
//        }
//    }
//}