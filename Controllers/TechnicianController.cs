using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Models.Domain;
using NMB_HLabSys.Models.ViewModels;
using NMB_HLabSys.Services;

namespace NMB_HLabSys.Controllers;

public class TechnicianController : Controller
{
    private static readonly string[] ActiveTestStatuses = { "InProgress", "Completed", "ToBeReviewed" };
    private static readonly string[] TerminalTestStatuses = { "Verified", "Cancelled" };
    private static readonly string[] TechCancellableStatuses = { "Submitted", "SamplesReceived", "InProgress" };

    private readonly ApplicationDbContext _context;
    private readonly AuthService _auth;
    private readonly EmailService _email;
    private readonly PdfService _pdf;
    private readonly NotificationService _notif;
    private readonly IBarcodeService _barcode;

    public TechnicianController(
        ApplicationDbContext context,
        AuthService auth,
        EmailService email,
        PdfService pdf,
        NotificationService notif,
        IBarcodeService barcode)
    {
        _context = context;
        _auth = auth;
        _email = email;
        _pdf = pdf;
        _notif = notif;
        _barcode = barcode;
    }

    private IActionResult? RequireTech()
    {
        if (!_auth.IsLoggedIn())
            return RedirectToAction("Login", "Account");

        if (!_auth.IsInRole("Technician"))
            return RedirectToAction("AccessDenied", "Account");

        return null;
    }

    private int GetTechId()
    {
        var userId = _auth.GetCurrentUserId();
        if (!userId.HasValue) return 0;

        return _context.Technicians
            .Where(t => t.UserId == userId.Value)
            .Select(t => t.TechnicianId)
            .FirstOrDefault();
    }

    private Technician? GetTechnician()
    {
        var techId = GetTechId();
        if (techId == 0) return null;

        return _context.Technicians
            .AsNoTracking()
            .FirstOrDefault(t => t.TechnicianId == techId);
    }

    private bool CanPerformTest(int technicianId, int testTypeId)
    {
        return _context.TechnicianTestTypes.Any(x =>
            x.TechnicianId == technicianId && x.TestTypeId == testTypeId);
    }

    // =============================================================
    // DASHBOARD
    // =============================================================

    [HttpGet]
    public IActionResult Dashboard(
        string? urgency,
        int? categoryId,
        string? dueFilter,
        string? requestNumber,
        string? sortBy)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        var tech = GetTechnician();
        if (techId == 0 || tech == null) return Forbid();

        var now = DateTime.Now;
        var vm = new TechnicianDashboardViewModel
        {
            Technician = tech,
            UrgencyFilter = urgency,
            CategoryId = categoryId,
            DueFilter = dueFilter,
            RequestNumberFilter = requestNumber,
            SortBy = sortBy,
            Categories = _context.TestCategories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToList()
        };

        vm.RecentSamples = _context.RequestSamples
            .AsNoTracking()
            .Where(s => s.ReceivedBy == techId && s.ReceivedAt >= now.AddHours(-24))
            .Include(s => s.SampleType)
            .Include(s => s.Request).ThenInclude(r => r.Patient)
            .OrderByDescending(s => s.ReceivedAt)
            .Select(s => new RecentSampleDto
            {
                SampleId = s.SampleId,
                Barcode = s.Barcode,
                SampleType = s.SampleType!.TypeName,
                RequestNumber = s.Request!.RequestNumber,
                Patient = s.Request.Patient!.Name + " " + s.Request.Patient.Surname,
                ReceivedAt = s.ReceivedAt
            })
            .ToList();

        vm.PendingSamples = _context.TestRequests
            .AsNoTracking()
            .Where(r => r.Status == "Submitted")
            .Include(r => r.Patient)
            .Include(r => r.RequestSamples)
            .OrderByDescending(r => r.Urgency == "Stat")
            .ThenByDescending(r => r.Urgency == "Urgent")
            .ThenBy(r => r.RequestDate)
            .Select(r => new PendingSampleDto
            {
                RequestId = r.RequestId,
                RequestNumber = r.RequestNumber,
                Patient = r.Patient!.Name + " " + r.Patient.Surname,
                RequestDate = r.RequestDate,
                Urgency = r.Urgency,
                SampleCount = r.RequestSamples.Count
            })
            .ToList();

        var inProgressRows = _context.RequestTestTypes
            .AsNoTracking()
            .Where(rtt => rtt.TechnicianId == techId && rtt.Status == "InProgress")
            .Include(rtt => rtt.Request!).ThenInclude(r => r.Patient)
            .Include(rtt => rtt.TestType!).ThenInclude(t => t.Category)
            .ToList();

        vm.InProgress = inProgressRows
            .Select(rtt => new InProgressTestDto
            {
                RequestTestId = rtt.RequestTestId,
                RequestNumber = rtt.Request!.RequestNumber,
                TestName = rtt.TestType!.TestName,
                CategoryName = rtt.TestType.Category?.CategoryName ?? "Uncategorised",
                Urgency = rtt.Request.Urgency,
                StartedAt = rtt.StartedAt,
                DueAt = rtt.StartedAt?.AddMinutes(rtt.TestType.TurnaroundMinutes),
                Patient = rtt.Request.Patient!.Name + " " + rtt.Request.Patient.Surname
            })
            .ToList();

        vm.ReadyToStart = _context.RequestTestTypes
            .AsNoTracking()
            .Where(rtt => rtt.Status == "Submitted"
                && rtt.TechnicianId == null
                && _context.TechnicianTestTypes.Any(tt =>
                    tt.TechnicianId == techId && tt.TestTypeId == rtt.TestTypeId)
                && _context.RequestSamples.Any(rs =>
                    rs.RequestId == rtt.RequestId && rs.ReceivedAt != null))
            .Include(rtt => rtt.Request!).ThenInclude(r => r.Patient)
            .Include(rtt => rtt.TestType!).ThenInclude(t => t.Category)
            .OrderByDescending(rtt => rtt.Request!.Urgency == "Stat")
            .ThenByDescending(rtt => rtt.Request!.Urgency == "Urgent")
            .ThenBy(rtt => rtt.Request!.RequestDate)
            .Select(rtt => new ReadyToStartDto
            {
                RequestTestId = rtt.RequestTestId,
                RequestNumber = rtt.Request!.RequestNumber,
                TestName = rtt.TestType!.TestName,
                CategoryName = rtt.TestType.Category!.CategoryName,
                Urgency = rtt.Request.Urgency,
                Patient = rtt.Request.Patient!.Name + " " + rtt.Request.Patient.Surname,
                RequestDate = rtt.Request.RequestDate
            })
            .ToList();

        vm.AwaitingVerification = _context.RequestTestTypes
            .AsNoTracking()
            .Where(rtt => rtt.Status == "Completed"
                && rtt.TechnicianId != null
                && rtt.TechnicianId != techId)
            .Include(rtt => rtt.Request!).ThenInclude(r => r.Patient)
            .Include(rtt => rtt.TestType)
            .OrderByDescending(rtt => rtt.IsAbnormal)
            .ThenBy(rtt => rtt.CompletedAt)
            .Select(rtt => new AwaitingVerificationDto
            {
                RequestTestId = rtt.RequestTestId,
                RequestNumber = rtt.Request!.RequestNumber,
                TestName = rtt.TestType!.TestName,
                ResultValue = rtt.ResultValue,
                Units = rtt.TestType.Units,
                IsAbnormal = rtt.IsAbnormal,
                Patient = rtt.Request.Patient!.Name + " " + rtt.Request.Patient.Surname
            })
            .ToList();

        vm.ToReview = _context.RequestTestTypes
            .AsNoTracking()
            .Where(rtt => rtt.Status == "ToBeReviewed" && rtt.TechnicianId == techId)
            .Include(rtt => rtt.Request!).ThenInclude(r => r.Patient)
            .Include(rtt => rtt.TestType!).ThenInclude(t => t.Category)
            .OrderByDescending(rtt => rtt.Request!.Urgency == "Stat")
            .ThenBy(rtt => rtt.CompletedAt)
            .Select(rtt => new ToReviewDto
            {
                RequestTestId = rtt.RequestTestId,
                RequestNumber = rtt.Request!.RequestNumber,
                TestName = rtt.TestType!.TestName,
                CategoryName = rtt.TestType.Category!.CategoryName,
                Urgency = rtt.Request.Urgency,
                Patient = rtt.Request.Patient!.Name + " " + rtt.Request.Patient.Surname
            })
            .ToList();

        vm.VerifiedHistory = _context.RequestTestTypes
            .AsNoTracking()
            .Where(rtt => rtt.Status == "Verified"
                && rtt.VerifiedById == techId
                && rtt.VerifiedAt >= now.AddDays(-30))
            .Include(rtt => rtt.Request!).ThenInclude(r => r.Patient)
            .Include(rtt => rtt.TestType)
            .OrderByDescending(rtt => rtt.VerifiedAt)
            .Select(rtt => new VerifiedHistoryDto
            {
                RequestTestId = rtt.RequestTestId,
                RequestNumber = rtt.Request!.RequestNumber,
                TestName = rtt.TestType!.TestName,
                ResultValue = rtt.ResultValue,
                Units = rtt.TestType.Units,
                IsAbnormal = rtt.IsAbnormal,
                VerifiedAt = rtt.VerifiedAt,
                Patient = rtt.Request.Patient!.Name + " " + rtt.Request.Patient.Surname
            })
            .ToList();

        vm.LowStockCount = _context.Consumables
            .AsNoTracking()
            .Count(c => c.IsActive
                && c.QuantityOnHand > 0
                && c.QuantityOnHand <= c.ReorderLevel);

        vm.OutOfStockCount = _context.Consumables
            .AsNoTracking()
            .Count(c => c.IsActive && c.QuantityOnHand <= 0);

        var todayStart = DateTime.Today;
        var tomorrowStart = todayStart.AddDays(1);
        vm.VerifiedTodayCount = _context.RequestTestTypes
            .AsNoTracking()
            .Count(rtt => rtt.Status == "Verified"
                && rtt.VerifiedById == techId
                && rtt.VerifiedAt.HasValue
                && rtt.VerifiedAt.Value >= todayStart
                && rtt.VerifiedAt.Value < tomorrowStart);

        RunNotificationSweep(techId);

        ApplyDashboardFilters(vm);
        return View(vm);
    }

    private void ApplyDashboardFilters(TechnicianDashboardViewModel vm)
    {
        bool HasUrgency(string value) => string.IsNullOrWhiteSpace(vm.UrgencyFilter)
            || string.Equals(value, vm.UrgencyFilter, StringComparison.OrdinalIgnoreCase);

        bool HasRequest(string value) => string.IsNullOrWhiteSpace(vm.RequestNumberFilter)
            || value.Contains(vm.RequestNumberFilter.Trim(), StringComparison.OrdinalIgnoreCase);

        vm.InProgress = vm.InProgress
            .Where(x => HasUrgency(x.Urgency) && HasRequest(x.RequestNumber)
                && (!vm.CategoryId.HasValue || GetCategoryId(x.CategoryName) == vm.CategoryId.Value))
            .Where(x => vm.DueFilter switch
            {
                "Overdue" => x.DueAt.HasValue && x.DueAt.Value < DateTime.Now,
                "Nearing" => x.DueAt.HasValue && x.DueAt.Value >= DateTime.Now && x.DueAt.Value <= DateTime.Now.AddMinutes(60),
                "Critical" => x.DueAt.HasValue && x.DueAt.Value >= DateTime.Now && x.DueAt.Value <= DateTime.Now.AddMinutes(15),
                _ => true
            })
            .ToList();

        vm.InProgress = (vm.SortBy ?? "DueSoonest") switch
        {
            "Urgency" => vm.InProgress
                .OrderByDescending(x => x.Urgency == "Stat")
                .ThenByDescending(x => x.Urgency == "Urgent")
                .ThenBy(x => x.DueAt)
                .ToList(),
            "StartedNewest" => vm.InProgress
                .OrderByDescending(x => x.StartedAt)
                .ToList(),
            _ => vm.InProgress
                .OrderBy(x => x.DueAt ?? DateTime.MaxValue)
                .ToList()
        };

        vm.ReadyToStart = vm.ReadyToStart
            .Where(x => HasUrgency(x.Urgency) && HasRequest(x.RequestNumber)
                && (!vm.CategoryId.HasValue || GetCategoryId(x.CategoryName) == vm.CategoryId.Value))
            .OrderByDescending(x => x.Urgency == "Stat")
            .ThenByDescending(x => x.Urgency == "Urgent")
            .ThenBy(x => x.RequestDate)
            .ToList();

        vm.ToReview = vm.ToReview
            .Where(x => HasUrgency(x.Urgency) && HasRequest(x.RequestNumber)
                && (!vm.CategoryId.HasValue || GetCategoryId(x.CategoryName) == vm.CategoryId.Value))
            .ToList();

        vm.AwaitingVerification = vm.AwaitingVerification
            .Where(x => HasRequest(x.RequestNumber))
            .ToList();

        int? GetCategoryId(string categoryName) =>
            vm.Categories.FirstOrDefault(c => c.CategoryName == categoryName)?.CategoryId;
    }

    // =============================================================
    // WORK BENCH — all tests assigned to me
    // =============================================================

    [HttpGet]
    public IActionResult WorkBench(string? status, string? urgency, string? requestNumber, string? sortBy)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        var rows = _context.RequestTestTypes
            .AsNoTracking()
            .Where(rtt => rtt.TechnicianId == techId
                || (rtt.Status == "Completed"
                    && rtt.TechnicianId.HasValue
                    && rtt.TechnicianId != techId))
            .Include(rtt => rtt.Request!).ThenInclude(r => r.Patient)
            .Include(rtt => rtt.TestType!).ThenInclude(t => t.Category)
            .ToList();

        if (!string.IsNullOrWhiteSpace(status))
            rows = rows.Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(urgency))
            rows = rows.Where(r => string.Equals(r.Request!.Urgency, urgency, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(requestNumber))
            rows = rows.Where(r => r.Request!.RequestNumber.Contains(requestNumber.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        rows = (sortBy ?? "RecentlyStarted") switch
        {
            "Oldest" => rows.OrderBy(r => r.StartedAt).ToList(),
            "Status" => rows.OrderBy(r => r.Status).ThenByDescending(r => r.StartedAt).ToList(),
            "Urgency" => rows.OrderByDescending(r => r.Request!.Urgency == "Stat")
                             .ThenByDescending(r => r.Request!.Urgency == "Urgent")
                             .ThenByDescending(r => r.StartedAt).ToList(),
            _ => rows.OrderByDescending(r => r.StartedAt).ToList()
        };

        var vm = new TechnicianWorkBenchViewModel
        {
            StatusFilter = status,
            UrgencyFilter = urgency,
            RequestNumberFilter = requestNumber,
            SortBy = sortBy,
            Rows = rows.Select(rtt => new WorkBenchRowDto
            {
                RequestTestId = rtt.RequestTestId,
                RequestNumber = rtt.Request!.RequestNumber,
                Patient = rtt.Request.Patient!.Name + " " + rtt.Request.Patient.Surname,
                TestName = rtt.TestType!.TestName,
                Category = rtt.TestType.Category?.CategoryName ?? "Uncategorised",
                Urgency = rtt.Request.Urgency,
                Status = rtt.Status,
                StartedAt = rtt.StartedAt,
                CompletedAt = rtt.CompletedAt,
                DueAt = rtt.StartedAt?.AddMinutes(rtt.TestType.TurnaroundMinutes),
                IsAbnormal = rtt.IsAbnormal,
                ResultValue = rtt.ResultValue,
                Units = rtt.TestType.Units
            }).ToList()
        };

        return View(vm);
    }

    // =============================================================
    // TEST READY TO START
    // =============================================================

    [HttpGet]
    public IActionResult TestReadyToStart(string? urgency, string? requestNumber)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        var rows = _context.RequestTestTypes
            .AsNoTracking()
            .Where(rtt => rtt.Status == "Submitted"
                && rtt.TechnicianId == null
                && _context.TechnicianTestTypes.Any(tt =>
                    tt.TechnicianId == techId && tt.TestTypeId == rtt.TestTypeId)
                && _context.RequestSamples.Any(rs =>
                    rs.RequestId == rtt.RequestId && rs.ReceivedAt != null))
            .Include(rtt => rtt.Request!).ThenInclude(r => r.Patient)
            .Include(rtt => rtt.TestType!).ThenInclude(t => t.Category)
            .Include(rtt => rtt.TestType!).ThenInclude(t => t.SampleType)
            .OrderByDescending(rtt => rtt.Request!.Urgency == "Stat")
            .ThenByDescending(rtt => rtt.Request!.Urgency == "Urgent")
            .ThenBy(rtt => rtt.Request!.RequestDate)
            .ToList();

        if (!string.IsNullOrWhiteSpace(urgency))
            rows = rows.Where(r => string.Equals(r.Request!.Urgency, urgency, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(requestNumber))
            rows = rows.Where(r => r.Request!.RequestNumber.Contains(requestNumber.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        var vm = new TestReadyToStartViewModel
        {
            UrgencyFilter = urgency,
            RequestNumberFilter = requestNumber,
            Rows = rows.Select(rtt => new ReadyToStartRowDto
            {
                RequestTestId = rtt.RequestTestId,
                RequestNumber = rtt.Request!.RequestNumber,
                Patient = rtt.Request.Patient!.Name + " " + rtt.Request.Patient.Surname,
                TestName = rtt.TestType!.TestName,
                Category = rtt.TestType.Category?.CategoryName ?? "Uncategorised",
                SampleType = rtt.TestType.SampleType?.TypeName ?? "Not specified",
                Urgency = rtt.Request.Urgency,
                RequestDate = rtt.Request.RequestDate
            }).ToList()
        };

        return View(vm);
    }

    // =============================================================
    // TEST REQUESTS
    // =============================================================

    [HttpGet]
    public IActionResult TestRequests(string? status, string? urgency, string? requestNumber)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        var rows = _context.RequestTestTypes
            .AsNoTracking()
            .Where(rtt => rtt.TechnicianId == techId)
            .Include(rtt => rtt.Request!).ThenInclude(r => r.Patient)
            .Include(rtt => rtt.TestType!).ThenInclude(t => t.Category)
            .ToList();

        if (!string.IsNullOrWhiteSpace(status))
            rows = rows.Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(urgency))
            rows = rows.Where(r => string.Equals(r.Request!.Urgency, urgency, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(requestNumber))
            rows = rows.Where(r => r.Request!.RequestNumber.Contains(requestNumber.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        rows = rows.OrderByDescending(r => r.Request!.RequestDate).ToList();

        var vm = new TestRequestsListViewModel
        {
            StatusFilter = status,
            UrgencyFilter = urgency,
            RequestNumberFilter = requestNumber,
            Rows = rows.Select(rtt => new TestRequestRowDto
            {
                RequestTestId = rtt.RequestTestId,
                RequestNumber = rtt.Request!.RequestNumber,
                Patient = rtt.Request.Patient!.Name + " " + rtt.Request.Patient.Surname,
                TestName = rtt.TestType!.TestName,
                Category = rtt.TestType.Category?.CategoryName ?? "Uncategorised",
                Urgency = rtt.Request.Urgency,
                Status = rtt.Status,
                RequestDate = rtt.Request.RequestDate,
                StartedAt = rtt.StartedAt,
                CompletedAt = rtt.CompletedAt
            }).ToList()
        };

        return View(vm);
    }

    // =============================================================
    // TEST SENT BACK
    // =============================================================

    [HttpGet]
    public IActionResult TestSentBack(string? requestNumber)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        var rows = _context.RequestTestTypes
            .AsNoTracking()
            .Where(rtt => rtt.Status == "ToBeReviewed" && rtt.TechnicianId == techId)
            .Include(rtt => rtt.Request!).ThenInclude(r => r.Patient)
            .Include(rtt => rtt.TestType!).ThenInclude(t => t.Category)
            .OrderByDescending(rtt => rtt.SentBackAt)
            .ToList();

        if (!string.IsNullOrWhiteSpace(requestNumber))
            rows = rows.Where(r => r.Request!.RequestNumber.Contains(requestNumber.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        var sentBackByIds = rows.Where(r => r.SentBackById.HasValue)
            .Select(r => r.SentBackById!.Value)
            .Distinct()
            .ToList();

        var sentBackByName = _context.Technicians
            .AsNoTracking()
            .Where(t => sentBackByIds.Contains(t.TechnicianId))
            .ToDictionary(t => t.TechnicianId, t => t.Name + " " + t.Surname);

        var vm = new TestSentBackViewModel
        {
            RequestNumberFilter = requestNumber,
            Rows = rows.Select(rtt => new SentBackRowDto
            {
                RequestTestId = rtt.RequestTestId,
                RequestNumber = rtt.Request!.RequestNumber,
                Patient = rtt.Request.Patient!.Name + " " + rtt.Request.Patient.Surname,
                TestName = rtt.TestType!.TestName,
                Category = rtt.TestType.Category?.CategoryName ?? "Uncategorised",
                Urgency = rtt.Request.Urgency,
                ResultValue = rtt.ResultValue,
                Units = rtt.TestType.Units,
                IsAbnormal = rtt.IsAbnormal,
                SentBackAt = rtt.SentBackAt,
                SentBackBy = rtt.SentBackById.HasValue
                    && sentBackByName.ContainsKey(rtt.SentBackById.Value)
                        ? sentBackByName[rtt.SentBackById.Value]
                        : "—",
                SentBackNote = rtt.ReVerificationNote
            }).ToList()
        };

        return View(vm);
    }

    // =============================================================
    // STATES
    // =============================================================

    [HttpGet]
    public IActionResult States()
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        var now = DateTime.Now;
        var todayStart = DateTime.Today;

        var allAssigned = _context.RequestTestTypes
            .AsNoTracking()
            .Where(rtt => rtt.TechnicianId == techId)
            .ToList();

        var vm = new TechnicianStatesViewModel
        {
            TotalAssigned = allAssigned.Count,
            Submitted = allAssigned.Count(r => r.Status == "Submitted"),
            InProgress = allAssigned.Count(r => r.Status == "InProgress"),
            Completed = allAssigned.Count(r => r.Status == "Completed"),
            Verified = allAssigned.Count(r => r.Status == "Verified"),
            ToBeReviewed = allAssigned.Count(r => r.Status == "ToBeReviewed"),
            Cancelled = allAssigned.Count(r => r.Status == "Cancelled"),
            AbnormalCount = allAssigned.Count(r => r.IsAbnormal),
            CompletedToday = allAssigned.Count(r => r.CompletedAt.HasValue && r.CompletedAt.Value >= todayStart),
            VerifiedToday = allAssigned.Count(r => r.VerifiedAt.HasValue && r.VerifiedAt.Value >= todayStart)
        };

        var completedDurations = allAssigned
            .Where(r => r.StartedAt.HasValue && r.CompletedAt.HasValue)
            .Select(r => (r.CompletedAt!.Value - r.StartedAt!.Value).TotalMinutes)
            .ToList();

        vm.AverageTurnaroundMinutes = completedDurations.Any()
            ? Math.Round(completedDurations.Average(), 1)
            : 0;

        var inProgressWithStart = _context.RequestTestTypes
            .AsNoTracking()
            .Where(rtt => rtt.TechnicianId == techId && rtt.Status == "InProgress" && rtt.StartedAt != null)
            .Include(rtt => rtt.TestType)
            .ToList();

        vm.OverdueCount = inProgressWithStart.Count(r =>
            r.StartedAt!.Value.AddMinutes(r.TestType!.TurnaroundMinutes) < now);

        return View(vm);
    }

    // =============================================================
    // SAMPLE RECEPTION
    // =============================================================

    [HttpGet]
    public IActionResult ReceiveSamples()
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var tech = GetTechnician();
        if (tech == null) return Forbid();

        return View(new ReceiveSamplesViewModel
        {
            TechnicianName = tech.Name + " " + tech.Surname
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult LookupRequest(string requestNumber)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        requestNumber = requestNumber?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(requestNumber))
        {
            TempData["Error"] = "Enter a request number.";
            return RedirectToAction(nameof(ReceiveSamples));
        }

        var tech = GetTechnician();
        var req = _context.TestRequests
            .AsNoTracking()
            .Where(tr => tr.RequestNumber == requestNumber && tr.Status == "Submitted")
            .Include(tr => tr.Patient)
            .FirstOrDefault();

        if (req == null)
        {
            TempData["Error"] = "Request not found or it has already been received.";
            return RedirectToAction(nameof(ReceiveSamples));
        }

        var samples = _context.RequestSamples
            .AsNoTracking()
            .Where(rs => rs.RequestId == req.RequestId)
            .Include(rs => rs.SampleType)
            .OrderBy(rs => rs.SampleId)
            .Select(rs => new SampleReceiveLineViewModel
            {
                SampleId = rs.SampleId,
                Barcode = rs.Barcode,
                SampleTypeName = rs.SampleType!.TypeName,
                IsReceived = rs.ReceivedAt != null,
                IsVerified = false
            })
            .ToList();

        var vm = new ReceiveSamplesViewModel
        {
            RequestNumber = requestNumber,
            FoundRequest = req,
            TechnicianName = tech == null ? string.Empty : tech.Name + " " + tech.Surname,
            Samples = samples
        };

        return View(nameof(ReceiveSamples), vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult VerifyBarcode(int requestId, string barcode)
    {
        var guard = RequireTech();
        if (guard != null) return Json(new { success = false, message = "Not authorised." });

        barcode = (barcode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(barcode))
            return Json(new { success = false, message = "Enter a barcode to verify." });

        var sample = _context.RequestSamples
            .AsNoTracking()
            .Where(s => s.RequestId == requestId && s.Barcode == barcode)
            .Select(s => new { s.SampleId, s.Barcode, s.ReceivedAt })
            .FirstOrDefault();

        if (sample == null)
        {
            return Json(new
            {
                success = false,
                status = "notfound",
                message = $"Barcode '{barcode}' does not belong to this request. Check for a typo."
            });
        }

        if (sample.ReceivedAt.HasValue)
        {
            return Json(new
            {
                success = false,
                status = "alreadyreceived",
                sampleId = sample.SampleId,
                message = $"Barcode '{barcode}' has already been received."
            });
        }

        return Json(new
        {
            success = true,
            status = "matched",
            sampleId = sample.SampleId,
            barcode = sample.Barcode,
            message = $"Barcode '{barcode}' matched."
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ConfirmReceive(int requestId)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        var request = _context.TestRequests
            .Include(r => r.RequestSamples)
            .FirstOrDefault(r => r.RequestId == requestId);

        if (request == null) return NotFound();
        if (request.Status != "Submitted")
        {
            TempData["Error"] = "This request is no longer waiting for sample reception.";
            return RedirectToAction(nameof(ReceiveSamples));
        }
        if (!request.RequestSamples.Any())
        {
            TempData["Error"] = "The request has no samples to receive.";
            return RedirectToAction(nameof(ReceiveSamples));
        }

        var receivedAt = DateTime.Now;
        foreach (var sample in request.RequestSamples)
        {
            if (!sample.ReceivedAt.HasValue)
            {
                sample.ReceivedAt = receivedAt;
                sample.ReceivedBy = techId;
            }
        }

        request.Status = "SamplesReceived";
        _context.SaveChanges();

        TempData["Success"] = $"Samples for {request.RequestNumber} were received at {receivedAt:dd MMM yyyy HH:mm}.";
        return RedirectToAction(nameof(Dashboard));
    }

    // =============================================================
    // SAMPLE REJECTION (Phase 5)
    // =============================================================

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RejectSample(int requestId, string reason)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
        {
            TempData["Error"] = "A reason (at least 5 characters) is required to reject a sample.";
            return RedirectToAction(nameof(ReceiveSamples));
        }

        var userId = _auth.GetCurrentUserId();
        var techId = GetTechId();
        if (!userId.HasValue || techId == 0) return Forbid();

        var request = _context.TestRequests
            .Include(r => r.Patient)
            .Include(r => r.Doctor!).ThenInclude(d => d.User)
            .Include(r => r.RequestSamples)
            .Include(r => r.RequestTestTypes)
            .FirstOrDefault(r => r.RequestId == requestId);

        if (request == null) return NotFound();

        if (request.Status != "Submitted")
        {
            TempData["Error"] = "Only requests awaiting sample reception can be rejected.";
            return RedirectToAction(nameof(ReceiveSamples));
        }

        var trimmed = reason.Trim();
        var now = DateTime.Now;

        request.Status = "Rejected";

        foreach (var test in request.RequestTestTypes)
        {
            if (!TerminalTestStatuses.Contains(test.Status))
            {
                test.Status = "Rejected";
                test.CancelReason = trimmed;
            }
        }

        foreach (var sample in request.RequestSamples)
        {
            // Requires AddSampleRejectionFields migration.
            // Delete these 3 lines if you skipped the migration.
            sample.RejectedBy = techId;
            sample.RejectedAt = now;
            sample.RejectionReason = trimmed;
        }

        _context.RequestCancellations.Add(new RequestCancellation
        {
            RequestId = requestId,
            CancelledBy = userId.Value,
            Reason = "Sample rejected: " + trimmed,
            CancelledAt = now
        });

        _context.SaveChanges();

        // Notify the doctor
        var doctorEmail = request.Doctor?.User?.Email;
        if (!string.IsNullOrWhiteSpace(doctorEmail))
        {
            _email.Send(
                doctorEmail,
                $"Sample Rejected – {request.RequestNumber}",
                $"<p>The sample for request <strong>{request.RequestNumber}</strong> " +
                $"({System.Net.WebUtility.HtmlEncode(request.Patient?.Name ?? "")} {System.Net.WebUtility.HtmlEncode(request.Patient?.Surname ?? "")}) " +
                $"was rejected at reception.</p>" +
                $"<p><strong>Reason:</strong> {System.Net.WebUtility.HtmlEncode(trimmed)}</p>" +
                "<p>Please arrange a new sample.</p>");
        }

        // Notify the manager
        _notif.NotifyRole(
            role: "Manager",
            category: "Stock",
            title: "Sample Rejected",
            message: $"Request {request.RequestNumber} was rejected at reception. Reason: {trimmed}",
            linkUrl: "/Manager/Requests");

        TempData["Success"] = $"Sample for {request.RequestNumber} rejected and the doctor was notified.";
        return RedirectToAction(nameof(ReceiveSamples));
    }

    // =============================================================
    // EDIT SAMPLE
    // =============================================================

    [HttpGet]
    public IActionResult EditSample(int sampleId)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var sample = _context.RequestSamples
            .AsNoTracking()
            .Include(s => s.Request!).ThenInclude(r => r.Patient)
            .FirstOrDefault(s => s.SampleId == sampleId);

        if (sample == null) return NotFound();
        if (sample.Request == null) return NotFound();

        if (sample.Request.Status == "Completed" ||
            sample.Request.Status == "Cancelled")
        {
            TempData["Error"] = "This request is no longer editable.";
            return RedirectToAction(nameof(ReceiveSamples));
        }

        var vm = new EditSampleViewModel
        {
            SampleId = sample.SampleId,
            RequestId = sample.RequestId,
            RequestNumber = sample.Request.RequestNumber,
            PatientName = sample.Request.Patient == null
                ? string.Empty
                : sample.Request.Patient.Name + " " + sample.Request.Patient.Surname,
            CurrentBarcode = sample.Barcode,
            CurrentRequestNumber = sample.Request.RequestNumber,
            NewBarcode = sample.Barcode,
            NewRequestNumber = sample.Request.RequestNumber
        };

        if (sample.EditedByTechnicianId.HasValue)
        {
            var editor = _context.Technicians
                .AsNoTracking()
                .Where(t => t.TechnicianId == sample.EditedByTechnicianId.Value)
                .Select(t => t.Name + " " + t.Surname)
                .FirstOrDefault();

            vm.LastEditedBy = editor;
            vm.LastEditedAt = sample.EditedAt;
            vm.LastEditNote = sample.EditNote;
        }

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult EditSample(EditSampleViewModel model)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        if (!ModelState.IsValid)
            return View(model);

        var techId = GetTechId();
        var userId = _auth.GetCurrentUserId();
        if (techId == 0 || !userId.HasValue) return Forbid();

        var sample = _context.RequestSamples
            .Include(s => s.Request!)
            .FirstOrDefault(s => s.SampleId == model.SampleId);

        if (sample == null) return NotFound();
        if (sample.Request == null) return NotFound();

        if (sample.Request.Status == "Completed" ||
            sample.Request.Status == "Cancelled")
        {
            TempData["Error"] = "This request is no longer editable.";
            return RedirectToAction(nameof(ReceiveSamples));
        }

        var barcodeChanged = !string.Equals(sample.Barcode, model.NewBarcode?.Trim(),
            StringComparison.Ordinal);
        var requestNumberChanged = !string.Equals(sample.Request.RequestNumber,
            model.NewRequestNumber?.Trim(), StringComparison.Ordinal);

        if (!barcodeChanged && !requestNumberChanged)
        {
            TempData["Error"] = "Nothing was changed. Edit the barcode or request number first.";
            return View(model);
        }

        var oldBarcode = sample.Barcode;
        var oldRequestNumber = sample.Request.RequestNumber;

        if (barcodeChanged)
            sample.Barcode = model.NewBarcode!.Trim();

        if (requestNumberChanged)
            sample.Request.RequestNumber = model.NewRequestNumber!.Trim();

        sample.EditedByTechnicianId = techId;
        sample.EditNote = model.EditNote.Trim();
        sample.EditedAt = DateTime.Now;

        var testRows = _context.RequestTestTypes
            .Where(r => r.RequestId == sample.RequestId)
            .ToList();

        var historyNote =
            $"Sample barcode correction.\n" +
            (barcodeChanged ? $"Barcode: '{oldBarcode}' → '{sample.Barcode}'\n" : "") +
            (requestNumberChanged ? $"Request number: '{oldRequestNumber}' → '{sample.Request.RequestNumber}'\n" : "") +
            $"Note: {model.EditNote.Trim()}";

        foreach (var r in testRows)
        {
            _context.TestResultHistories.Add(new TestResultHistory
            {
                RequestTestId = r.RequestTestId,
                ActionBy = userId.Value,
                Action = "SampleEdited",
                Notes = historyNote,
                ResultValue = r.ResultValue
            });
        }

        _context.SaveChanges();

        TempData["Success"] = "Sample updated. Note recorded.";
        return RedirectToAction(nameof(ReceiveSamples));
    }

    // =============================================================
    // CONFIRM START
    // =============================================================

    [HttpGet]
    public IActionResult ConfirmStart(int requestTestId)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        var rtt = _context.RequestTestTypes
            .AsNoTracking()
            .Include(r => r.Request!).ThenInclude(r => r.Patient)
            .Include(r => r.TestType!).ThenInclude(t => t.Category)
            .Include(r => r.TestType!).ThenInclude(t => t.SampleType)
            .FirstOrDefault(r => r.RequestTestId == requestTestId);

        if (rtt == null) return NotFound();
        if (rtt.Request == null || rtt.TestType == null) return NotFound();

        if (rtt.Status != "Submitted" || rtt.TechnicianId != null)
        {
            TempData["Error"] = "This test is no longer available for selection.";
            return RedirectToAction(nameof(Dashboard));
        }
        if (rtt.Request.Status == "Cancelled")
        {
            TempData["Error"] = "The test request has been cancelled.";
            return RedirectToAction(nameof(Dashboard));
        }
        var samplesReceived = _context.RequestSamples.Any(s =>
            s.RequestId == rtt.RequestId && s.ReceivedAt != null);
        if (!samplesReceived)
        {
            TempData["Error"] = "The samples for this request must be received before the test can be started.";
            return RedirectToAction(nameof(Dashboard));
        }
        if (!CanPerformTest(techId, rtt.TestTypeId))
        {
            TempData["Error"] = "You are not assigned to perform this test type.";
            return RedirectToAction(nameof(Dashboard));
        }

        var requirements = _context.TestTypeConsumables
            .AsNoTracking()
            .Where(tc => tc.TestTypeId == rtt.TestTypeId)
            .Include(tc => tc.Consumable!).ThenInclude(c => c.Supplier)
            .Select(tc => new ConsumableRequirementDto
            {
                ConsumableId = tc.ConsumableId,
                ConsumableName = tc.Consumable!.ConsumableName,
                RequiredQuantity = tc.QuantityUsed,
                QuantityOnHand = tc.Consumable.QuantityOnHand,
                ReorderLevel = tc.Consumable.ReorderLevel,
                SupplierName = tc.Consumable.Supplier != null ? tc.Consumable.Supplier.SupplierName : null,
                IsActive = tc.Consumable.IsActive
            })
            .OrderBy(c => c.ConsumableName)
            .ToList();

        var vm = new ConfirmStartViewModel
        {
            RequestTestId = rtt.RequestTestId,
            RequestNumber = rtt.Request.RequestNumber,
            TestName = rtt.TestType.TestName,
            CategoryName = rtt.TestType.Category?.CategoryName ?? "Uncategorised",
            PatientName = rtt.Request.Patient!.Name + " " + rtt.Request.Patient.Surname,
            Urgency = rtt.Request.Urgency,
            SampleType = rtt.TestType.SampleType?.TypeName ?? "Not specified",
            Consumables = requirements
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ConfirmStart(int requestTestId, bool confirm)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        var rtt = _context.RequestTestTypes
            .Include(r => r.Request)
            .Include(r => r.TestType)
            .FirstOrDefault(r => r.RequestTestId == requestTestId);

        if (rtt == null) return NotFound();

        if (rtt.Status != "Submitted" || rtt.TechnicianId != null)
        {
            TempData["Error"] = "This test is no longer available for selection.";
            return RedirectToAction(nameof(Dashboard));
        }

        if (!CanPerformTest(techId, rtt.TestTypeId))
        {
            TempData["Error"] = "You are not assigned to perform this test type.";
            return RedirectToAction(nameof(Dashboard));
        }

        var usage = _context.TestTypeConsumables
            .Where(tc => tc.TestTypeId == rtt.TestTypeId)
            .ToList();

        foreach (var u in usage)
        {
            var c = _context.Consumables.FirstOrDefault(x => x.ConsumableId == u.ConsumableId);
            if (c == null || !c.IsActive || c.QuantityOnHand < u.QuantityUsed)
            {
                TempData["Error"] = "Required consumables are short or out of stock. Please request stock before starting.";
                return RedirectToAction(nameof(ConfirmStart), new { requestTestId });
            }
        }

        // ═══ CONCURRENCY GUARD — atomic start ═══
        // The UPDATE only succeeds if the row is still "Submitted" with no technician.
        // A second click / another tab will see 0 rows affected and abort cleanly.
        var now = DateTime.Now;
        var rowsAffected = _context.Database.ExecuteSqlInterpolated($@"
            UPDATE RequestTestTypes
            SET TechnicianId = {techId},
                Status       = 'InProgress',
                StartedAt    = {now}
            WHERE RequestTestId = {requestTestId}
              AND Status        = 'Submitted'
              AND TechnicianId  IS NULL");

        if (rowsAffected == 0)
        {
            TempData["Error"] = "This test was already taken by another user or is no longer available.";
            return RedirectToAction(nameof(Dashboard));
        }

        // Reload tracked entity
        _context.Entry(rtt).Reload();

        if (rtt.Request != null && rtt.Request.Status == "SamplesReceived")
            rtt.Request.Status = "InProgress";

        foreach (var u in usage)
        {
            var c = _context.Consumables.First(x => x.ConsumableId == u.ConsumableId);
            c.QuantityOnHand = Math.Max(0, c.QuantityOnHand - u.QuantityUsed);

            _context.TestConsumableUsages.Add(new TestConsumableUsage
            {
                RequestTestId = rtt.RequestTestId,
                ConsumableId = u.ConsumableId,
                QuantityUsed = u.QuantityUsed,
                UsedByTechnicianId = techId,
                UsedAt = now
            });
        }

        _context.SaveChanges();

        if (rtt.StartedAt.HasValue && rtt.TestType != null)
        {
            var dueAt = rtt.StartedAt.Value.AddMinutes(rtt.TestType.TurnaroundMinutes);
            var linkUrl = Url.Action("TestDetails", "Technician", new { requestTestId = rtt.RequestTestId });
            var currentUserId = _auth.GetCurrentUserId();

            if (currentUserId.HasValue)
            {
                _notif.Notify(
                    currentUserId.Value,
                    "Info",
                    $"Started: {rtt.TestType.TestName}",
                    $"You started {rtt.TestType.TestName} on request {rtt.Request?.RequestNumber}. Due by {dueAt:dd MMM HH:mm}.",
                    linkUrl,
                    calendarStart: dueAt,
                    calendarEnd: dueAt.AddMinutes(30));
            }
        }

        TempData["Success"] = $"{rtt.TestType?.TestName} has been assigned to you and is now In Progress.";
        return RedirectToAction(nameof(TestDetails), new { requestTestId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RequestStock(int consumableId, int quantityNeeded, string? note)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var userId = _auth.GetCurrentUserId();
        if (!userId.HasValue) return Forbid();

        if (quantityNeeded <= 0) quantityNeeded = 1;

        var consumable = _context.Consumables
            .Include(c => c.Supplier)
            .FirstOrDefault(c => c.ConsumableId == consumableId);

        if (consumable == null) return NotFound();

        var managerEmails = _context.Users
            .Where(u => u.Role == "Manager")
            .Select(u => u.Email)
            .ToList();

        var message =
            $"<p><strong>Stock request</strong></p>" +
            $"<p>Consumable: <strong>{System.Net.WebUtility.HtmlEncode(consumable.ConsumableName)}</strong></p>" +
            $"<p>Current quantity on hand: {consumable.QuantityOnHand}</p>" +
            $"<p>Requested additional quantity: <strong>{quantityNeeded}</strong></p>" +
            $"<p>Supplier: {System.Net.WebUtility.HtmlEncode(consumable.Supplier?.SupplierName ?? "—")}</p>" +
            (string.IsNullOrWhiteSpace(note)
                ? ""
                : $"<p>Technician note: {System.Net.WebUtility.HtmlEncode(note)}</p>");

        foreach (var email in managerEmails)
        {
            if (string.IsNullOrWhiteSpace(email)) continue;
            _email.Send(email, $"Stock request – {consumable.ConsumableName}", message);
        }

        var managerUserIds = _context.Users
            .Where(u => u.Role == "Manager")
            .Select(u => u.UserId)
            .ToList();

        foreach (var mgrId in managerUserIds)
        {
            _notif.Notify(
                mgrId,
                "Stock",
                $"Stock request: {consumable.ConsumableName}",
                $"A technician requested {quantityNeeded} extra unit(s) of {consumable.ConsumableName}. Current on hand: {consumable.QuantityOnHand}.",
                Url.Action("Index", "Notifications"));
        }

        TempData["Success"] = $"Stock request for {consumable.ConsumableName} has been sent to the lab manager.";
        return RedirectToAction(nameof(Dashboard));
    }

    // =============================================================
    // TEST DETAILS
    // =============================================================

    [HttpGet]
    public IActionResult TestDetails(int requestTestId)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();
        var rtt = _context.RequestTestTypes
            .AsNoTracking()
            .Include(x => x.Request!).ThenInclude(r => r.Patient!).ThenInclude(p => p.PatientConditions).ThenInclude(pc => pc.Condition)
            .Include(x => x.Request!).ThenInclude(r => r.Patient!).ThenInclude(p => p.PatientAllergies).ThenInclude(pa => pa.Allergy)
            .Include(x => x.Request!).ThenInclude(r => r.Patient!).ThenInclude(p => p.PatientMedications).ThenInclude(pm => pm.Medication)
            .Include(x => x.Request!).ThenInclude(r => r.RequestSamples).ThenInclude(s => s.SampleType)
            .Include(x => x.TestType!).ThenInclude(t => t.Category)
            .Include(x => x.TestType!).ThenInclude(t => t.SampleType)
            .FirstOrDefault(x => x.RequestTestId == requestTestId);

        if (rtt == null) return NotFound();

        bool assignedToThisTech = rtt.TechnicianId == techId;
        bool canVerify = rtt.Status == "Completed"
            && rtt.TechnicianId.HasValue
            && rtt.TechnicianId != techId;

        if (!assignedToThisTech && !canVerify)
            return Forbid();

        var patient = rtt.Request?.Patient;
        if (patient == null || rtt.TestType == null || rtt.Request == null)
            return NotFound();

        var vm = new TechnicianTestDetailsViewModel
        {
            RequestTestId = rtt.RequestTestId,
            RequestId = rtt.RequestId,
            RequestNumber = rtt.Request.RequestNumber,
            PatientId = patient.PatientId,
            DoctorId = rtt.Request.DoctorId,
            TestName = rtt.TestType.TestName,
            CategoryName = rtt.TestType.Category?.CategoryName ?? "Uncategorised",
            SampleType = rtt.TestType.SampleType?.TypeName ?? "Not specified",
            Units = rtt.TestType.Units,
            NormalRangeMin = rtt.TestType.NormalRangeMin,
            NormalRangeMax = rtt.TestType.NormalRangeMax,
            TurnaroundMinutes = rtt.TestType.TurnaroundMinutes,
            Status = rtt.Status,
            CanVerifyResult = canVerify,
            Urgency = rtt.Request.Urgency,
            RequestDate = rtt.Request.RequestDate,
            StartedAt = rtt.StartedAt,
            DueAt = rtt.StartedAt?.AddMinutes(rtt.TestType.TurnaroundMinutes),
            ClinicalNotes = rtt.Request.ClinicalNotes,
            PatientName = patient.Name + " " + patient.Surname,
            PatientDateOfBirth = patient.DateOfBirth,
            PatientAge = CalculateAge(patient.DateOfBirth).ToString(),
            Conditions = patient.PatientConditions
                .Where(x => x.IsActive && x.Condition != null)
                .Select(x => x.Condition!)
                .OrderBy(x => x.ConditionName)
                .ToList(),
            Allergies = patient.PatientAllergies
                .Where(x => x.IsActive && x.Allergy != null)
                .Select(x => x.Allergy!)
                .OrderBy(x => x.AllergyName)
                .ToList(),
            Medications = patient.PatientMedications
                .Where(x => x.IsActive && x.Medication != null)
                .Select(x => x.Medication!)
                .OrderBy(x => x.MedicationName)
                .ToList(),
            Samples = rtt.Request.RequestSamples
                .OrderBy(x => x.SampleId)
                .Select(x => new RequestSampleDetailsViewModel
                {
                    SampleId = x.SampleId,
                    Barcode = x.Barcode,
                    SampleType = x.SampleType?.TypeName ?? "Not specified",
                    ReceivedAt = x.ReceivedAt,
                    ReceivedBy = x.ReceivedBy.HasValue
                        ? _context.Technicians.Where(t => t.TechnicianId == x.ReceivedBy.Value)
                            .Select(t => t.Name + " " + t.Surname).FirstOrDefault()
                        : null,
                    ReceivedById = x.ReceivedBy
                })
                .ToList()
        };

        return View(vm);
    }

    // =============================================================
    // RESULT CAPTURE
    // =============================================================

    [HttpGet]
    public IActionResult CaptureResult(int requestTestId)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        var rtt = _context.RequestTestTypes
            .AsNoTracking()
            .Include(r => r.TestType)
            .Include(r => r.Request!).ThenInclude(r => r.Patient)
            .FirstOrDefault(r => r.RequestTestId == requestTestId && r.TechnicianId == techId);

        if (rtt == null) return Forbid();

        if (rtt.Status != "InProgress" && rtt.Status != "ToBeReviewed")
            return BadRequest("Only tests that are In Progress or To Be Reviewed can have results captured.");

        var vm = new CaptureResultViewModel
        {
            RequestTestId = rtt.RequestTestId,
            TestName = rtt.TestType?.TestName ?? string.Empty,
            Units = rtt.TestType?.Units,
            NormalRangeMin = rtt.TestType?.NormalRangeMin,
            NormalRangeMax = rtt.TestType?.NormalRangeMax,
            ClinicalNotes = rtt.Request?.ClinicalNotes,
            RequestNumber = rtt.Request?.RequestNumber,
            PatientName = rtt.Request?.Patient == null
                ? null
                : rtt.Request.Patient.Name + " " + rtt.Request.Patient.Surname,
            Urgency = rtt.Request?.Urgency ?? string.Empty,
            Status = rtt.Status,
            ResultValue = rtt.ResultValue,
            ResultNotes = rtt.ResultNotes,
            IsResubmission = rtt.Status == "ToBeReviewed"
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult CaptureResult(CaptureResultViewModel model)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        if (!ModelState.IsValid)
            return View(model);

        var techId = GetTechId();
        var userId = _auth.GetCurrentUserId();
        if (techId == 0 || !userId.HasValue) return Forbid();

        var rtt = _context.RequestTestTypes
            .Include(r => r.TestType)
            .Include(r => r.Request)
            .FirstOrDefault(r => r.RequestTestId == model.RequestTestId);

        if (rtt == null) return NotFound();

        if (rtt.TechnicianId != techId)
            return Forbid();

        if (rtt.Status != "InProgress" && rtt.Status != "ToBeReviewed")
            return BadRequest("This test is not available for result capture.");

        var min = rtt.TestType?.NormalRangeMin;
        var max = rtt.TestType?.NormalRangeMax;
        var isAbnormal = (min.HasValue && model.ResultValue < min.Value)
            || (max.HasValue && model.ResultValue > max.Value);

        var wasReturnedForReview = rtt.Status == "ToBeReviewed";

        rtt.ResultValue = model.ResultValue;
        rtt.ResultNotes = model.ResultNotes?.Trim();
        rtt.IsAbnormal = isAbnormal;
        rtt.Status = "Completed";
        rtt.CompletedAt = DateTime.Now;

        rtt.VerifiedById = null;
        rtt.VerifiedAt = null;

        _context.TestResultHistories.Add(new TestResultHistory
        {
            RequestTestId = model.RequestTestId,
            ActionBy = userId.Value,
            Action = wasReturnedForReview ? "Resubmitted" : "Captured",
            Notes = rtt.ResultNotes,
            ResultValue = model.ResultValue
        });

        _context.SaveChanges();
        CheckRequestCompletion(model.RequestTestId);

        if (rtt.TestType != null)
        {
            var otherTechUserIds = _context.TechnicianTestTypes
                .AsNoTracking()
                .Where(tt => tt.TestTypeId == rtt.TestTypeId && tt.TechnicianId != techId)
                .Select(tt => tt.Technician!.UserId)
                .Distinct()
                .ToList();

            var linkUrl = Url.Action("VerifyResult", "Technician",
                new { requestTestId = model.RequestTestId });
            var requestNumber = _context.TestRequests
                .Where(t => t.RequestId == rtt.RequestId)
                .Select(t => t.RequestNumber)
                .FirstOrDefault() ?? "—";

            foreach (var otherUserId in otherTechUserIds)
            {
                _notif.Notify(
                    otherUserId,
                    "Info",
                    $"Awaiting verification: {rtt.TestType.TestName}",
                    $"A result for {rtt.TestType.TestName} on request {requestNumber} is ready for verification.",
                    linkUrl);
            }
        }

        TempData["Success"] = wasReturnedForReview
            ? "The revised result was captured and returned for verification."
            : "Result captured successfully and is awaiting verification.";

        return RedirectToAction(nameof(Dashboard));
    }

    // =============================================================
    // VERIFICATION
    // =============================================================

    [HttpGet]
    public IActionResult VerifyResult(int requestTestId)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();
        var rtt = _context.RequestTestTypes
            .AsNoTracking()
            .Include(r => r.TestType)
            .Include(r => r.Request!).ThenInclude(r => r.Patient)
            .Include(r => r.Technician)
            .FirstOrDefault(r => r.RequestTestId == requestTestId);

        if (rtt == null) return NotFound();
        if (rtt.Status != "Completed") return BadRequest("Only completed results can be verified.");
        if (!rtt.TechnicianId.HasValue || rtt.TechnicianId == techId) return Forbid();

        var vm = new VerifyResultViewModel
        {
            RequestTestId = rtt.RequestTestId,
            RequestNumber = rtt.Request?.RequestNumber,
            PatientName = rtt.Request?.Patient == null
                ? null
                : rtt.Request.Patient.Name + " " + rtt.Request.Patient.Surname,
            TestName = rtt.TestType?.TestName,
            ResultValue = rtt.ResultValue,
            ResultNotes = rtt.ResultNotes,
            IsAbnormal = rtt.IsAbnormal,
            NormalRangeMin = rtt.TestType?.NormalRangeMin,
            NormalRangeMax = rtt.TestType?.NormalRangeMax,
            Units = rtt.TestType?.Units,
            PerformingTechnician = rtt.Technician == null
                ? null
                : rtt.Technician.Name + " " + rtt.Technician.Surname
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult VerifyResult(VerifyResultViewModel model)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        var userId = _auth.GetCurrentUserId();
        if (techId == 0 || !userId.HasValue) return Forbid();

        if (!model.IsApproved.HasValue)
            ModelState.AddModelError(nameof(model.IsApproved), "Please select Approve & Verify or Return for Review.");

        if (model.IsApproved == false && string.IsNullOrWhiteSpace(model.ReviewNote))
            ModelState.AddModelError(nameof(model.ReviewNote), "A note is required when returning a result for review.");

        if (!ModelState.IsValid)
            return View(model);

        var rtt = _context.RequestTestTypes
            .Include(r => r.TestType)
            .Include(r => r.Request!).ThenInclude(r => r.Patient)
            .Include(r => r.Technician)
            .FirstOrDefault(r => r.RequestTestId == model.RequestTestId);

        if (rtt == null) return NotFound();
        if (rtt.Status != "Completed") return BadRequest("This result is no longer awaiting verification.");
        if (!rtt.TechnicianId.HasValue || rtt.TechnicianId == techId) return Forbid();

        var now = DateTime.Now;

        if (model.IsApproved.Value)
        {
            // ═══ CONCURRENCY GUARD — atomic verify ═══
            var rowsAffected = _context.Database.ExecuteSqlInterpolated($@"
                UPDATE RequestTestTypes
                SET Status                 = 'Verified',
                    VerifiedById           = {techId},
                    VerifiedAt             = {now},
                    RequiresReVerification = 0
                WHERE RequestTestId = {model.RequestTestId}
                  AND Status        = 'Completed'
                  AND TechnicianId  <> {techId}");

            if (rowsAffected == 0)
            {
                TempData["Error"] = "This result was already verified or is no longer awaiting verification.";
                return RedirectToAction(nameof(Dashboard));
            }

            _context.Entry(rtt).Reload();

            _context.TestResultHistories.Add(new TestResultHistory
            {
                RequestTestId = model.RequestTestId,
                ActionBy = userId.Value,
                Action = "Verified",
                Notes = string.IsNullOrWhiteSpace(model.ReviewNote) ? null : model.ReviewNote.Trim(),
                ResultValue = rtt.ResultValue
            });

            _context.SaveChanges();
            NotifyDoctorIfAllVerified(model.RequestTestId);

            TempData["Success"] = "Result verified successfully.";
        }
        else
        {
            var trimmed = model.ReviewNote!.Trim();

            // ═══ CONCURRENCY GUARD — atomic send-back ═══
            var rowsAffected = _context.Database.ExecuteSqlInterpolated($@"
                UPDATE RequestTestTypes
                SET Status                 = 'ToBeReviewed',
                    RequiresReVerification = 1,
                    ReVerificationNote     = {trimmed},
                    SentBackById           = {techId},
                    SentBackAt             = {now}
                WHERE RequestTestId = {model.RequestTestId}
                  AND Status        = 'Completed'");

            if (rowsAffected == 0)
            {
                TempData["Error"] = "This result is no longer awaiting verification.";
                return RedirectToAction(nameof(Dashboard));
            }

            _context.Entry(rtt).Reload();

            _context.TestResultHistories.Add(new TestResultHistory
            {
                RequestTestId = model.RequestTestId,
                ActionBy = userId.Value,
                Action = "ReturnedForReview",
                Notes = trimmed,
                ResultValue = rtt.ResultValue
            });

            _context.SaveChanges();

            if (rtt.TechnicianId.HasValue)
            {
                var linkUrl = Url.Action("SentBackDetails", "Technician",
                    new { requestTestId = model.RequestTestId });

                var performingTechUserId = _context.Technicians
                    .AsNoTracking()
                    .Where(t => t.TechnicianId == rtt.TechnicianId.Value)
                    .Select(t => (int?)t.UserId)
                    .FirstOrDefault();

                if (performingTechUserId.HasValue)
                {
                    _notif.Notify(
                        performingTechUserId.Value,
                        "Overdue",
                        $"Result sent back: {rtt.TestType?.TestName}",
                        $"Your result for {rtt.TestType?.TestName} on request {rtt.Request?.RequestNumber} was returned for review. Note: {trimmed}",
                        linkUrl);
                }
            }

            TempData["Success"] = "Result returned to the performing technician for review.";
        }

        return RedirectToAction(nameof(Dashboard));
    }

    // =============================================================
    // SENT-BACK DETAILS
    // =============================================================

    [HttpGet]
    public IActionResult SentBackDetails(int requestTestId)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        var rtt = _context.RequestTestTypes
            .AsNoTracking()
            .Include(r => r.Request!).ThenInclude(r => r.Patient)
            .Include(r => r.TestType!).ThenInclude(t => t.Category)
            .Include(r => r.Technician)
            .FirstOrDefault(r => r.RequestTestId == requestTestId);

        if (rtt == null) return NotFound();
        if (rtt.Request == null || rtt.TestType == null) return NotFound();

        var canView = rtt.TechnicianId == techId
            || (rtt.Status == "Completed"
                && rtt.TechnicianId.HasValue
                && rtt.TechnicianId != techId)
            || CanPerformTest(techId, rtt.TestTypeId);
        if (!canView) return Forbid();

        string? sentBackName = null;
        if (rtt.SentBackById.HasValue)
        {
            sentBackName = _context.Technicians
                .AsNoTracking()
                .Where(t => t.TechnicianId == rtt.SentBackById.Value)
                .Select(t => t.Name + " " + t.Surname)
                .FirstOrDefault();
        }

        var history = _context.TestResultHistories
            .AsNoTracking()
            .Where(h => h.RequestTestId == requestTestId)
            .OrderByDescending(h => h.ActionDate)
            .ToList();

        var historyUserIds = history
            .Where(h => h.ActionBy != 0)
            .Select(h => h.ActionBy)
            .Distinct()
            .ToList();

        var userEmails = _context.Users
            .AsNoTracking()
            .Where(u => historyUserIds.Contains(u.UserId))
            .ToDictionary(u => u.UserId, u => u.Email);

        var vm = new SentBackDetailsViewModel
        {
            RequestTestId = rtt.RequestTestId,
            RequestId = rtt.RequestId,
            PatientId = rtt.Request.PatientId,
            RequestNumber = rtt.Request.RequestNumber,
            TestName = rtt.TestType.TestName,
            CategoryName = rtt.TestType.Category?.CategoryName ?? "Uncategorised",
            Urgency = rtt.Request.Urgency,
            Status = rtt.Status,
            CanVerifyResult = rtt.Status == "Completed"
                && rtt.TechnicianId.HasValue
                && rtt.TechnicianId != techId,
            ResultValue = rtt.ResultValue,
            Units = rtt.TestType.Units,
            NormalRangeMin = rtt.TestType.NormalRangeMin,
            NormalRangeMax = rtt.TestType.NormalRangeMax,
            IsAbnormal = rtt.IsAbnormal,
            ResultNotes = rtt.ResultNotes,
            PatientName = rtt.Request.Patient == null
                ? string.Empty
                : rtt.Request.Patient.Name + " " + rtt.Request.Patient.Surname,
            PatientDateOfBirth = rtt.Request.Patient?.DateOfBirth,
            PatientAge = rtt.Request.Patient != null
                ? CalculateAge(rtt.Request.Patient.DateOfBirth)
                : null,
            PerformingTechnicianName = rtt.Technician == null
                ? null
                : rtt.Technician.Name + " " + rtt.Technician.Surname,
            PerformingTechnicianId = rtt.TechnicianId,
            SentBackByName = sentBackName,
            SentBackById = rtt.SentBackById,
            SentBackAt = rtt.SentBackAt,
            SentBackNote = rtt.ReVerificationNote,
            History = history.Select(h => new TestResultHistoryRow
            {
                HistoryId = h.HistoryId,
                Action = h.Action,
                Notes = h.Notes,
                ResultValue = h.ResultValue,
                ActionDate = h.ActionDate,
                ActionByUserId = h.ActionBy,
                ActionByName = userEmails.TryGetValue(h.ActionBy, out var email) ? email : null
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult AddReviewNote(int requestTestId, string note)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        if (string.IsNullOrWhiteSpace(note))
        {
            TempData["Error"] = "Please type a note before saving.";
            return RedirectToAction(nameof(SentBackDetails), new { requestTestId });
        }

        var userId = _auth.GetCurrentUserId();
        if (!userId.HasValue) return Forbid();

        var rtt = _context.RequestTestTypes
            .FirstOrDefault(r => r.RequestTestId == requestTestId);
        if (rtt == null) return NotFound();

        _context.TestResultHistories.Add(new TestResultHistory
        {
            RequestTestId = requestTestId,
            ActionBy = userId.Value,
            Action = "NoteAdded",
            Notes = note.Trim(),
            ResultValue = rtt.ResultValue
        });

        _context.SaveChanges();

        TempData["Success"] = "Note added to the test history.";
        return RedirectToAction(nameof(SentBackDetails), new { requestTestId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ResendForVerification(int requestTestId)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        var userId = _auth.GetCurrentUserId();
        if (techId == 0 || !userId.HasValue) return Forbid();

        var rtt = _context.RequestTestTypes
            .Include(r => r.Request)
            .FirstOrDefault(r => r.RequestTestId == requestTestId);
        if (rtt == null) return NotFound();

        if (rtt.TechnicianId != techId)
        {
            TempData["Error"] = "Only the performing technician can re-send this test for verification.";
            return RedirectToAction(nameof(SentBackDetails), new { requestTestId });
        }

        if (rtt.Status != "ToBeReviewed")
        {
            TempData["Error"] = "This test is not currently awaiting review.";
            return RedirectToAction(nameof(SentBackDetails), new { requestTestId });
        }

        rtt.Status = "Completed";
        rtt.CompletedAt = DateTime.Now;
        rtt.RequiresReVerification = false;

        _context.TestResultHistories.Add(new TestResultHistory
        {
            RequestTestId = requestTestId,
            ActionBy = userId.Value,
            Action = "ResentForVerification",
            Notes = "Technician re-sent this test for verification.",
            ResultValue = rtt.ResultValue
        });

        _context.SaveChanges();

        TempData["Success"] = "Test re-sent for verification.";
        return RedirectToAction(nameof(Dashboard));
    }

    // =============================================================
    // HISTORY
    // =============================================================

    [HttpGet]
    public IActionResult ResultHistory(int requestTestId)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        var rtt = _context.RequestTestTypes
            .AsNoTracking()
            .FirstOrDefault(r => r.RequestTestId == requestTestId);

        if (rtt == null) return NotFound();

        var canView = rtt.TechnicianId == techId
            || CanPerformTest(techId, rtt.TestTypeId);

        if (!canView) return Forbid();

        var history = _context.TestResultHistories
            .AsNoTracking()
            .Where(h => h.RequestTestId == requestTestId)
            .OrderByDescending(h => h.ActionDate)
            .ToList();

        ViewBag.RequestTestId = requestTestId;
        return View(history);
    }

    // =============================================================
    // FULL DETAILS PAGES
    // =============================================================

    [HttpGet]
    public IActionResult PatientDetails(int patientId)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var p = _context.Patients
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.PatientConditions).ThenInclude(pc => pc.Condition)
            .Include(x => x.PatientAllergies).ThenInclude(pa => pa.Allergy)
            .Include(x => x.PatientMedications).ThenInclude(pm => pm.Medication)
            .FirstOrDefault(x => x.PatientId == patientId);

        if (p == null) return NotFound();

        var vm = new PersonDetailsViewModel
        {
            PersonType = "Patient",
            FullName = p.Name + " " + p.Surname,
            Email = p.User?.Email,
            ContactNumber = p.CellNumber,
            SouthAfricanId = p.SAIDNumber,
            DateOfBirth = p.DateOfBirth,
            Age = CalculateAge(p.DateOfBirth),
            MedicalConditions = p.PatientConditions
                .Where(x => x.IsActive && x.Condition != null)
                .Select(x => x.Condition!.ConditionName)
                .OrderBy(x => x)
                .ToList(),
            Allergies = p.PatientAllergies
                .Where(x => x.IsActive && x.Allergy != null)
                .Select(x => x.Allergy!.AllergyName)
                .OrderBy(x => x)
                .ToList(),
            Medications = p.PatientMedications
                .Where(x => x.IsActive && x.Medication != null)
                .Select(x => x.Medication!.MedicationName)
                .OrderBy(x => x)
                .ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public IActionResult DoctorDetails(int doctorId)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var d = _context.Doctors
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefault(x => x.DoctorId == doctorId);

        if (d == null) return NotFound();

        var vm = new PersonDetailsViewModel
        {
            PersonType = "Doctor",
            FullName = d.Name + " " + d.Surname,
            Email = d.User?.Email,
            ContactNumber = d.ContactNumber,
            HpcsaNumber = d.HPCSANumber
        };

        return View(vm);
    }

    [HttpGet]
    public IActionResult TechnicianDetails(int technicianId)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var t = _context.Technicians
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefault(x => x.TechnicianId == technicianId);

        if (t == null) return NotFound();

        var testTypes = _context.TechnicianTestTypes
            .AsNoTracking()
            .Where(tt => tt.TechnicianId == technicianId)
            .Select(tt => tt.TestType!.TestName)
            .OrderBy(x => x)
            .ToList();

        var vm = new PersonDetailsViewModel
        {
            PersonType = "Technician",
            FullName = t.Name + " " + t.Surname,
            Email = t.User?.Email,
            ContactNumber = t.ContactNumber,
            EmployeeNumber = t.EmployeeNumber,
            SouthAfricanId = t.SAIDNumber,
            AssignedTestTypes = testTypes
        };

        return View(vm);
    }

    // =============================================================
    // CONSUMABLE MANAGEMENT
    // =============================================================

    [HttpGet]
    public IActionResult Consumables(string? search, string? status)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var all = _context.Consumables
            .AsNoTracking()
            .Include(c => c.Supplier)
            .OrderBy(c => c.ConsumableName)
            .ToList();

        var vm = new ConsumableManagementViewModel
        {
            TotalCount = all.Count(c => c.IsActive),
            LowStockCount = all.Count(c => c.IsActive && c.QuantityOnHand > 0 && c.QuantityOnHand <= c.ReorderLevel),
            OutOfStockCount = all.Count(c => c.IsActive && c.QuantityOnHand <= 0),
            RemovedCount = all.Count(c => !c.IsActive),
            SearchTerm = search,
            StatusFilter = status
        };

        var rows = all.Select(c => new ConsumableRowViewModel
        {
            ConsumableId = c.ConsumableId,
            ConsumableName = c.ConsumableName,
            ReorderLevel = c.ReorderLevel,
            QuantityOnHand = c.QuantityOnHand,
            SupplierName = c.Supplier?.SupplierName,
            IsActive = c.IsActive,
            RemovalReason = c.RemovalReason,
            RemovedAt = c.RemovedAt
        });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            rows = rows.Where(r => r.ConsumableName.Contains(s, StringComparison.OrdinalIgnoreCase)
                || (r.SupplierName?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            rows = rows.Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase));
        }

        vm.Consumables = rows.ToList();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RemoveConsumable(int consumableId, string reason)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var userId = _auth.GetCurrentUserId();
        if (!userId.HasValue) return Forbid();

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required when removing a consumable.";
            return RedirectToAction(nameof(Consumables));
        }

        var consumable = _context.Consumables
            .Include(c => c.Supplier)
            .FirstOrDefault(c => c.ConsumableId == consumableId);

        if (consumable == null) return NotFound();

        if (!consumable.IsActive)
        {
            TempData["Error"] = "This consumable has already been removed.";
            return RedirectToAction(nameof(Consumables));
        }

        consumable.IsActive = false;
        consumable.RemovalReason = reason.Trim();
        consumable.RemovedAt = DateTime.Now;

        _context.SaveChanges();

        var managerEmails = _context.Users
            .Where(u => u.Role == "Manager")
            .Select(u => u.Email)
            .ToList();

        var message =
            $"<p><strong>Consumable removed from use</strong></p>" +
            $"<p>Consumable: <strong>{System.Net.WebUtility.HtmlEncode(consumable.ConsumableName)}</strong></p>" +
            $"<p>Removed by a laboratory technician.</p>" +
            $"<p><strong>Reason:</strong> {System.Net.WebUtility.HtmlEncode(reason.Trim())}</p>" +
            $"<p>Supplier: {System.Net.WebUtility.HtmlEncode(consumable.Supplier?.SupplierName ?? "—")}</p>" +
            $"<p>Date/time: {DateTime.Now:dd MMM yyyy HH:mm}</p>";

        foreach (var email in managerEmails)
        {
            if (string.IsNullOrWhiteSpace(email)) continue;
            _email.Send(email, $"Consumable removed – {consumable.ConsumableName}", message);
        }

        var managerUserIds = _context.Users
            .Where(u => u.Role == "Manager")
            .Select(u => u.UserId)
            .ToList();

        foreach (var mgrId in managerUserIds)
        {
            _notif.Notify(
                mgrId,
                "Stock",
                $"Consumable removed: {consumable.ConsumableName}",
                $"A technician removed {consumable.ConsumableName} from use. Reason: {reason.Trim()}",
                Url.Action("Index", "Notifications"));
        }

        TempData["Success"] = $"{consumable.ConsumableName} was removed and the lab manager was notified.";
        return RedirectToAction(nameof(Consumables));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult AdjustConsumableStock(int consumableId, int adjustment, string? note)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var userId = _auth.GetCurrentUserId();
        if (!userId.HasValue) return Forbid();

        if (adjustment == 0)
        {
            TempData["Error"] = "Enter a non-zero adjustment.";
            return RedirectToAction(nameof(Consumables));
        }

        var consumable = _context.Consumables
            .Include(c => c.Supplier)
            .FirstOrDefault(c => c.ConsumableId == consumableId);

        if (consumable == null) return NotFound();

        if (!consumable.IsActive)
        {
            TempData["Error"] = "This consumable is removed and cannot be adjusted.";
            return RedirectToAction(nameof(Consumables));
        }

        var oldQuantity = consumable.QuantityOnHand;
        var newQuantity = Math.Max(0, oldQuantity + adjustment);
        var actualChange = newQuantity - oldQuantity;

        consumable.QuantityOnHand = newQuantity;
        _context.SaveChanges();

        var managerEmails = _context.Users
            .Where(u => u.Role == "Manager")
            .Select(u => u.Email)
            .ToList();

        var changeText = actualChange >= 0 ? $"+{actualChange}" : actualChange.ToString();

        var message =
            $"<p><strong>Consumable stock adjusted</strong></p>" +
            $"<p>Consumable: <strong>{System.Net.WebUtility.HtmlEncode(consumable.ConsumableName)}</strong></p>" +
            $"<p>Previous quantity: {oldQuantity}</p>" +
            $"<p>Adjustment: <strong>{changeText}</strong></p>" +
            $"<p>New quantity on hand: <strong>{newQuantity}</strong></p>" +
            (string.IsNullOrWhiteSpace(note) ? "" : $"<p>Technician note: {System.Net.WebUtility.HtmlEncode(note)}</p>") +
            $"<p>Date/time: {DateTime.Now:dd MMM yyyy HH:mm}</p>";

        foreach (var email in managerEmails)
        {
            if (string.IsNullOrWhiteSpace(email)) continue;
            _email.Send(email, $"Consumable adjusted – {consumable.ConsumableName}", message);
        }

        var managerUserIds = _context.Users
            .Where(u => u.Role == "Manager")
            .Select(u => u.UserId)
            .ToList();

        foreach (var mgrId in managerUserIds)
        {
            _notif.Notify(
                mgrId,
                "Stock",
                $"Stock adjusted: {consumable.ConsumableName}",
                $"{consumable.ConsumableName} adjusted by {changeText}. New quantity on hand: {newQuantity}.",
                Url.Action("Index", "Notifications"));
        }

        TempData["Success"] = $"{consumable.ConsumableName} stock adjusted ({changeText}). New quantity: {newQuantity}.";
        return RedirectToAction(nameof(Consumables));
    }

    // =============================================================
    // CANCEL REQUEST
    // =============================================================

    [HttpGet]
    public IActionResult CancelRequest()
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        return View(new TechnicianCancelRequestViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult LookupRequestForCancel(string requestNumber)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        requestNumber = requestNumber?.Trim() ?? string.Empty;
        var vm = new TechnicianCancelRequestViewModel { RequestNumber = requestNumber };

        if (string.IsNullOrWhiteSpace(requestNumber))
        {
            TempData["Error"] = "Enter a request number.";
            return View(nameof(CancelRequest), vm);
        }

        var req = _context.TestRequests
            .AsNoTracking()
            .Where(tr => tr.RequestNumber == requestNumber)
            .Include(tr => tr.Patient)
            .FirstOrDefault();

        if (req == null)
        {
            TempData["Error"] = "Request not found.";
            return View(nameof(CancelRequest), vm);
        }

        vm.FoundRequest = new TestRequest
        {
            RequestId = req.RequestId,
            RequestNumber = req.RequestNumber,
            Status = req.Status,
            Urgency = req.Urgency,
            RequestDate = req.RequestDate,
            Patient = req.Patient == null ? null : new Patient
            {
                Name = req.Patient.Name,
                Surname = req.Patient.Surname
            }
        };

        vm.CanCancel = TechCancellableStatuses.Contains(req.Status);
        vm.BlockedReason = vm.CanCancel
            ? null
            : $"This request cannot be cancelled because its current status is '{req.Status}'.";

        return View(nameof(CancelRequest), vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult CancelRequest(int requestId, string reason)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var userId = _auth.GetCurrentUserId();
        if (!userId.HasValue) return Forbid();

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "A reason is required to cancel a request.";
            return RedirectToAction(nameof(CancelRequest));
        }

        reason = reason.Trim();

        var request = _context.TestRequests
            .Include(tr => tr.Patient)
            .Include(tr => tr.Doctor!).ThenInclude(d => d.User)
            .Include(tr => tr.RequestTestTypes)
            .FirstOrDefault(tr => tr.RequestId == requestId);

        if (request == null) return NotFound();

        if (!TechCancellableStatuses.Contains(request.Status))
        {
            TempData["Error"] = "This request cannot be cancelled at its current status.";
            return RedirectToAction(nameof(CancelRequest));
        }

        request.Status = "Cancelled";

        foreach (var test in request.RequestTestTypes)
        {
            if (!TerminalTestStatuses.Contains(test.Status) && test.Status != "Completed")
            {
                test.Status = "Cancelled";
                test.CancelReason = reason;
            }
        }

        _context.RequestCancellations.Add(new RequestCancellation
        {
            RequestId = requestId,
            CancelledBy = userId.Value,
            Reason = reason,
            CancelledAt = DateTime.UtcNow
        });

        _context.SaveChanges();

        var doctorEmail = request.Doctor?.User?.Email;
        if (!string.IsNullOrWhiteSpace(doctorEmail))
        {
            _email.Send(
                doctorEmail,
                $"Test Request Cancelled – {request.RequestNumber}",
                $"<p>Request <strong>{request.RequestNumber}</strong> for patient " +
                $"{request.Patient?.Name} {request.Patient?.Surname} has been cancelled by the laboratory.</p>" +
                $"<p><strong>Reason:</strong> {System.Net.WebUtility.HtmlEncode(reason)}</p>");
        }

        TempData["Success"] = $"Request {request.RequestNumber} was cancelled and the doctor was notified.";
        return RedirectToAction(nameof(Dashboard));
    }

    // =============================================================
    // REPORTS
    // =============================================================

    [HttpGet]
    public IActionResult Reports()
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var vm = new TechnicianReportViewModel
        {
            FromDate = DateTime.Today.AddDays(-30),
            ToDate = DateTime.Today
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Reports(TechnicianReportViewModel model)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        if (model.FromDate > model.ToDate)
        {
            ModelState.AddModelError(string.Empty, "From-date must be before to-date.");
            return View(model);
        }

        var pdfBytes = _pdf.GenerateTechnicianReport(techId, model.FromDate, model.ToDate);
        var fileName = $"NMB_TechnicianReport_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult GenerateCompletedTestsPdf(TechnicianReportViewModel model)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        if (model.FromDate > model.ToDate)
        {
            TempData["Error"] = "From-date must be before to-date.";
            return RedirectToAction(nameof(Reports));
        }

        var pdfBytes = _pdf.GenerateCompletedTestsReport(
            techId, model.FromDate, model.ToDate, model.CategoryFilter);

        return File(pdfBytes, "application/pdf",
            $"NMB_CompletedTests_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult GenerateTurnaroundPdf(TechnicianReportViewModel model)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        if (model.FromDate > model.ToDate)
        {
            TempData["Error"] = "From-date must be before to-date.";
            return RedirectToAction(nameof(Reports));
        }

        var pdfBytes = _pdf.GenerateTurnaroundReport(techId, model.FromDate, model.ToDate);
        return File(pdfBytes, "application/pdf",
            $"NMB_Turnaround_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult GenerateUsagePdf(TechnicianReportViewModel model)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        if (model.FromDate > model.ToDate)
        {
            TempData["Error"] = "From-date must be before to-date.";
            return RedirectToAction(nameof(Reports));
        }

        var pdfBytes = _pdf.GenerateConsumableUsageReport(techId, model.FromDate, model.ToDate);
        return File(pdfBytes, "application/pdf",
            $"NMB_ConsumableUsage_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult GenerateSupplyRequestsPdf(TechnicianReportViewModel model)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        if (model.FromDate > model.ToDate)
        {
            TempData["Error"] = "From-date must be before to-date.";
            return RedirectToAction(nameof(Reports));
        }

        var pdfBytes = _pdf.GenerateSupplyRequestsReport(techId, model.FromDate, model.ToDate);
        return File(pdfBytes, "application/pdf",
            $"NMB_SupplyRequests_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    // =============================================================
    // PROFILE
    // =============================================================

    [HttpGet]
    public IActionResult Profile()
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var tech = GetTechnician();
        if (tech == null) return Forbid();

        var email = _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == tech.UserId)
            .Select(u => u.Email)
            .FirstOrDefault() ?? string.Empty;

        var vm = new TechnicianProfileViewModel
        {
            TechnicianId = tech.TechnicianId,
            FullName = tech.Name + " " + tech.Surname,
            EmployeeNumber = tech.EmployeeNumber,
            SouthAfricanId = tech.SAIDNumber,
            Email = email,
            ContactNumber = tech.ContactNumber,
            ProfilePicturePath = tech.ProfilePicturePath
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult EditProfile(TechnicianProfileViewModel model)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        ModelState.Remove(nameof(model.FullName));
        ModelState.Remove(nameof(model.EmployeeNumber));
        ModelState.Remove(nameof(model.SouthAfricanId));
        ModelState.Remove(nameof(model.Email));

        if (!ModelState.IsValid)
        {
            return View(nameof(Profile), ReloadProfileVm(model));
        }

        var tech = _context.Technicians.FirstOrDefault(t => t.TechnicianId == techId);
        if (tech == null) return NotFound();

        tech.ContactNumber = string.IsNullOrWhiteSpace(model.ContactNumber)
            ? null
            : model.ContactNumber.Trim();

        if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
        {
            var uploadError = ValidateImageFile(model.ProfilePictureFile);
            if (uploadError != null)
            {
                TempData["Error"] = uploadError;
                return RedirectToAction(nameof(Profile));
            }

            var uploadsDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "uploads", "technicians");

            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(model.ProfilePictureFile.FileName).ToLowerInvariant();
            var fileName = $"tech-{techId}-{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                model.ProfilePictureFile.CopyTo(stream);
            }

            if (!string.IsNullOrWhiteSpace(tech.ProfilePicturePath))
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(),
                    "wwwroot", tech.ProfilePicturePath.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    try { System.IO.File.Delete(oldPath); } catch { /* ignore */ }
                }
            }

            tech.ProfilePicturePath = $"/uploads/technicians/{fileName}";
        }

        _context.SaveChanges();

        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        return View(new TechnicianChangePasswordViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ChangePassword(TechnicianChangePasswordViewModel model)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        if (!ModelState.IsValid)
            return View(model);

        var userId = _auth.GetCurrentUserId();
        if (!userId.HasValue) return Forbid();

        var (ok, err) = _auth.ChangePassword(userId.Value, model.CurrentPassword, model.NewPassword);

        if (!ok)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), err ?? "Could not change password.");
            return View(model);
        }

        TempData["Success"] = "Password changed successfully.";
        return RedirectToAction(nameof(Profile));
    }

    private TechnicianProfileViewModel ReloadProfileVm(TechnicianProfileViewModel incoming)
    {
        var tech = GetTechnician();
        if (tech == null) return incoming;

        incoming.TechnicianId = tech.TechnicianId;
        incoming.FullName = tech.Name + " " + tech.Surname;
        incoming.EmployeeNumber = tech.EmployeeNumber;
        incoming.SouthAfricanId = tech.SAIDNumber;
        incoming.ContactNumber = tech.ContactNumber;
        incoming.ProfilePicturePath = tech.ProfilePicturePath;
        incoming.Email = _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == tech.UserId)
            .Select(u => u.Email)
            .FirstOrDefault() ?? string.Empty;
        return incoming;
    }

    private static string? ValidateImageFile(IFormFile file)
    {
        const long maxBytes = 2 * 1024 * 1024;
        if (file.Length > maxBytes)
            return "Profile picture must be under 2 MB.";

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (!allowed.Contains(ext))
            return "Only .jpg, .jpeg, .png, .gif, or .webp images are allowed.";

        return null;
    }

    // =============================================================
    // NOTIFICATION SWEEP
    // =============================================================

    private void RunNotificationSweep(int techId)
    {
        var now = DateTime.Now;

        var techUserId = _context.Technicians
            .AsNoTracking()
            .Where(t => t.TechnicianId == techId)
            .Select(t => (int?)t.UserId)
            .FirstOrDefault();

        if (techUserId == null) return;

        var activeTests = _context.RequestTestTypes
            .AsNoTracking()
            .Where(rtt => rtt.TechnicianId == techId && rtt.Status == "InProgress")
            .Include(rtt => rtt.Request!)
            .Include(rtt => rtt.TestType!)
            .ToList();

        foreach (var t in activeTests)
        {
            if (!t.StartedAt.HasValue || t.TestType == null || t.Request == null) continue;

            var dueAt = t.StartedAt.Value.AddMinutes(t.TestType.TurnaroundMinutes);
            var linkUrl = Url.Action("TestDetails", "Technician", new { requestTestId = t.RequestTestId });

            if (dueAt < now)
            {
                _notif.NotifyOnce(
                    techUserId.Value,
                    "Overdue",
                    $"Overdue: {t.TestType.TestName}",
                    $"{t.TestType.TestName} on request {t.Request.RequestNumber} is overdue. Was due {dueAt:dd MMM HH:mm}.",
                    linkUrl,
                    calendarStart: dueAt,
                    calendarEnd: dueAt.AddMinutes(30));
            }
            else if (dueAt <= now.AddMinutes(60))
            {
                _notif.NotifyOnce(
                    techUserId.Value,
                    "NearDue",
                    $"Nearing due: {t.TestType.TestName}",
                    $"{t.TestType.TestName} on request {t.Request.RequestNumber} is due at {dueAt:dd MMM HH:mm} — under an hour left.",
                    linkUrl,
                    calendarStart: dueAt,
                    calendarEnd: dueAt.AddMinutes(30));
            }
        }
    }

    // =============================================================
    // HELPERS
    // =============================================================

    private void CheckRequestCompletion(int requestTestId)
    {
        var requestId = _context.RequestTestTypes
            .Where(r => r.RequestTestId == requestTestId)
            .Select(r => r.RequestId)
            .FirstOrDefault();

        if (requestId == 0) return;

        var pending = _context.RequestTestTypes
            .Count(rtt => rtt.RequestId == requestId
                && !new[] { "Completed", "Verified", "Cancelled" }.Contains(rtt.Status));

        if (pending == 0)
        {
            var request = _context.TestRequests.FirstOrDefault(tr => tr.RequestId == requestId);
            if (request != null && request.Status != "Cancelled")
                request.Status = "Completed";

            _context.SaveChanges();
        }
    }

    private void NotifyDoctorIfAllVerified(int requestTestId)
    {
        var requestId = _context.RequestTestTypes
            .Where(r => r.RequestTestId == requestTestId)
            .Select(r => r.RequestId)
            .FirstOrDefault();

        if (requestId == 0) return;

        var notVerified = _context.RequestTestTypes
            .Count(rtt => rtt.RequestId == requestId
                && !new[] { "Verified", "Cancelled" }.Contains(rtt.Status));

        if (notVerified > 0) return;

        var row = _context.TestRequests
            .AsNoTracking()
            .Where(tr => tr.RequestId == requestId)
            .Include(tr => tr.Doctor!).ThenInclude(d => d.User)
            .Select(tr => new
            {
                Email = tr.Doctor!.User!.Email,
                tr.RequestNumber,
                PatientName = tr.Patient!.Name + " " + tr.Patient.Surname
            })
            .FirstOrDefault();

        if (row == null || string.IsNullOrWhiteSpace(row.Email)) return;

        var pdfBytes = _pdf.GenerateRequestResultsPdf(requestId);

        _email.SendWithAttachment(
            row.Email,
            $"All Results Verified – {row.RequestNumber}",
            $"<p>All test results for request <strong>{row.RequestNumber}</strong> for patient " +
            $"<strong>{System.Net.WebUtility.HtmlEncode(row.PatientName)}</strong> have been verified.</p>" +
            "<p>The official laboratory results report is attached for your review.</p>",
            pdfBytes,
            $"NMB_Results_{row.RequestNumber}.pdf");
    }

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return Math.Max(age, 0);
    }

    // ═════════════════════════════════════════════════════════════
    // SUPPLY REQUESTS
    // ═════════════════════════════════════════════════════════════

    [HttpGet]
    public IActionResult RequestConsumable()
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        return View(new RequestConsumableViewModel
        {
            AvailableConsumables = _context.Consumables
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.ConsumableName)
                .ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RequestConsumable(RequestConsumableViewModel model)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        if (!ModelState.IsValid)
        {
            model.AvailableConsumables = _context.Consumables
                .AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.ConsumableName).ToList();
            return View(model);
        }

        var consumable = _context.Consumables
            .FirstOrDefault(c => c.ConsumableId == model.ConsumableId);

        if (consumable == null || !consumable.IsActive)
        {
            ModelState.AddModelError(nameof(model.ConsumableId), "Selected consumable is not available.");
            model.AvailableConsumables = _context.Consumables
                .AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.ConsumableName).ToList();
            return View(model);
        }

        var requestNumber = GenerateSupplyRequestNumber();

        var entity = new ConsumableRequest
        {
            RequestNumber = requestNumber,
            TechnicianId = techId,
            ConsumableId = model.ConsumableId,
            QuantityRequested = model.QuantityRequested,
            Urgency = model.Urgency,
            Reason = model.Reason.Trim(),
            Status = "Pending",
            RequestedAt = DateTime.Now
        };
        _context.ConsumableRequests.Add(entity);
        _context.SaveChanges();

        var tech = GetTechnician();
        var techName = tech == null ? "A technician" : $"{tech.Name} {tech.Surname}";

        _notif.NotifyRole(
            role: "Manager",
            category: "Supply",
            title: "New Supply Request",
            message: $"{techName} submitted supply request {requestNumber} " +
                     $"({model.QuantityRequested} × {consumable.ConsumableName}, {model.Urgency}).",
            linkUrl: "/Manager/SupplyRequests");

        TempData["Success"] = $"Supply request {requestNumber} submitted to the Lab Manager.";
        return RedirectToAction(nameof(MySupplyRequests));
    }

    [HttpGet]
    public IActionResult MySupplyRequests()
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        var requests = _context.ConsumableRequests
            .AsNoTracking()
            .Include(r => r.Consumable)
            .Where(r => r.TechnicianId == techId)
            .OrderByDescending(r => r.RequestedAt)
            .ToList();

        return View(new MySupplyRequestsViewModel { Requests = requests });
    }

    // ═════════════════════════════════════════════════════════════
    // STOCK PROBLEM REPORTS
    // ═════════════════════════════════════════════════════════════

    [HttpGet]
    public IActionResult ReportStockProblem()
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        return View(new ReportStockProblemViewModel
        {
            AvailableConsumables = _context.Consumables
                .AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.ConsumableName).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ReportStockProblem(ReportStockProblemViewModel model)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        if (!ModelState.IsValid)
        {
            model.AvailableConsumables = _context.Consumables
                .AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.ConsumableName).ToList();
            return View(model);
        }

        var consumable = _context.Consumables
            .FirstOrDefault(c => c.ConsumableId == model.ConsumableId);

        if (consumable == null)
        {
            ModelState.AddModelError(nameof(model.ConsumableId), "Selected consumable was not found.");
            model.AvailableConsumables = _context.Consumables
                .AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.ConsumableName).ToList();
            return View(model);
        }

        var report = new StockProblemReport
        {
            ConsumableId = model.ConsumableId,
            TechnicianId = techId,
            IssueType = model.ProblemType,
            Description = model.Description.Trim(),
            Status = "Open",
            ReportedAt = DateTime.Now
        };
        _context.StockProblemReports.Add(report);
        _context.SaveChanges();

        var tech = GetTechnician();
        var techName = tech == null ? "A technician" : $"{tech.Name} {tech.Surname}";

        _notif.NotifyRole(
            role: "Manager",
            category: "Stock",
            title: $"Stock Problem: {model.ProblemType}",
            message: $"{techName} reported a {model.ProblemType} problem on {consumable.ConsumableName}.",
            linkUrl: "/Manager/StockProblems");

        TempData["Success"] = "Stock problem reported to the Lab Manager.";
        return RedirectToAction(nameof(ReportStockProblem));
    }

    // ═════════════════════════════════════════════════════════════
    // USAGE HISTORY
    // ═════════════════════════════════════════════════════════════

    [HttpGet]
    public IActionResult UsageHistory()
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var techId = GetTechId();
        if (techId == 0) return Forbid();

        var rows = _context.TestConsumableUsages
            .AsNoTracking()
            .Where(u => u.UsedByTechnicianId == techId)
            .OrderByDescending(u => u.UsedAt)
            .Take(500)
            .Select(u => new UsageHistoryRow
            {
                RequestTestId = u.RequestTestId,
                RequestNumber = u.RequestTest != null && u.RequestTest.Request != null
                                    ? u.RequestTest.Request.RequestNumber
                                    : "—",
                TestTypeName = u.RequestTest != null && u.RequestTest.TestType != null
                                    ? u.RequestTest.TestType.TestName
                                    : "—",
                UsedAt = u.UsedAt,
                ConsumableName = u.Consumable != null ? u.Consumable.ConsumableName : "—",
                ConsumableCode = u.Consumable != null ? u.Consumable.ConsumableId.ToString() : "—",
                QuantityUsed = u.QuantityUsed
            })
            .ToList();

        return View(new UsageHistoryViewModel { Rows = rows });
    }

    // ═════════════════════════════════════════════════════════════
    // BARCODE PREVIEW  ← ═══ FIXED ═══ returns PNG bytes, not JSON
    // ═════════════════════════════════════════════════════════════

    [HttpGet]
    public IActionResult BarcodePreview(string value)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        if (string.IsNullOrWhiteSpace(value)) return BadRequest();

        var dataUrl = _barcode.GenerateCode128DataUrl(value, height: 80, showLabel: true);

        // dataUrl is "data:image/png;base64,XXXX" — extract just the base64 part
        var commaIndex = dataUrl.IndexOf(',');
        if (commaIndex < 0 || commaIndex + 1 >= dataUrl.Length) return BadRequest();

        try
        {
            var base64 = dataUrl.Substring(commaIndex + 1);
            var bytes = Convert.FromBase64String(base64);
            return File(bytes, "image/png");
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpGet]
    public IActionResult PrintBarcode(int sampleId)
    {
        var guard = RequireTech();
        if (guard != null) return guard;

        var sample = _context.RequestSamples
            .AsNoTracking()
            .Include(s => s.SampleType)
            .Include(s => s.Request!).ThenInclude(r => r.Patient)
            .FirstOrDefault(s => s.SampleId == sampleId);

        if (sample == null) return NotFound();

        dynamic vm = new System.Dynamic.ExpandoObject();
        vm.Sample = sample;
        vm.BarcodeDataUrl = _barcode.GenerateCode128DataUrl(sample.Barcode ?? "", height: 90, showLabel: true);
        vm.SystemName = "NMB-HLabSys";
        vm.PatientName = sample.Request?.Patient != null
            ? $"{sample.Request.Patient.Name} {sample.Request.Patient.Surname}"
            : "";
        vm.RequestNumber = sample.Request?.RequestNumber ?? "";
        vm.SampleTypeName = sample.SampleType?.TypeName ?? "";

        return View((object)vm);
    }

    // ═════════════════════════════════════════════════════════════
    // SUPPLY REQUEST NUMBER GENERATOR
    // ═════════════════════════════════════════════════════════════

    private string GenerateSupplyRequestNumber()
    {
        var year = DateTime.Now.Year;
        var prefix = $"SUP-{year}-";

        var last = _context.ConsumableRequests
            .Where(r => r.RequestNumber.StartsWith(prefix))
            .OrderByDescending(r => r.RequestNumber)
            .Select(r => r.RequestNumber)
            .FirstOrDefault();

        var next = 1;
        if (!string.IsNullOrEmpty(last))
        {
            var tail = last.Substring(prefix.Length);
            if (int.TryParse(tail, out var n)) next = n + 1;
        }
        return $"{prefix}{next:D4}";
    }
}