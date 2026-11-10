using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NMB_HLabSys.Data;
using NMB_HLabSys.Services;
using NMB_HLabSys.Models.ViewModels;
using System.Text;

namespace NMB_HLabSys.Controllers;

public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AuthService _auth;

    public NotificationsController(ApplicationDbContext context, AuthService auth)
    {
        _context = context;
        _auth = auth;
    }

    private IActionResult? RequireLogin()
    {
        if (!_auth.IsLoggedIn())
            return RedirectToAction("Login", "Account");

        return null;
    }

    private int GetUserId() => _auth.GetCurrentUserId() ?? 0;

    // =============================================================
    // LIST
    // =============================================================

    [HttpGet]
    public IActionResult Index(string? filter)
    {
        var guard = RequireLogin();
        if (guard != null) return guard;

        var userId = GetUserId();
        if (userId == 0) return Forbid();

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        var vm = new NotificationListViewModel
        {
            Filter = filter,
            TotalCount = query.Count(),
            UnreadCount = query.Count(n => !n.IsRead)
        };

        query = filter switch
        {
            "Unread" => query.Where(n => !n.IsRead),
            "Overdue" => query.Where(n => n.Category == "Overdue"),
            "NearDue" => query.Where(n => n.Category == "NearDue"),
            "Stock" => query.Where(n => n.Category == "Stock"),
            "STAT" => query.Where(n => n.Category == "STAT"),
            _ => query
        };

        vm.Notifications = query
            .OrderByDescending(n => n.CreatedAt)
            .Take(200)
            .Select(n => new NotificationRowViewModel
            {
                NotificationId = n.NotificationId,
                Category = n.Category,
                Title = n.Title,
                Message = n.Message,
                LinkUrl = n.LinkUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                HasCalendar = n.CalendarStart.HasValue
            })
            .ToList();

        return View(vm);
    }

    // =============================================================
    // MARK AS READ
    // =============================================================

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult MarkAsRead(int id, string? returnFilter)
    {
        var guard = RequireLogin();
        if (guard != null) return guard;

        var userId = GetUserId();
        if (userId == 0) return Forbid();

        var n = _context.Notifications
            .FirstOrDefault(x => x.NotificationId == id && x.UserId == userId);

        if (n == null) return NotFound();

        n.IsRead = true;
        _context.SaveChanges();

        // If there's a link, go there; otherwise return to list
        if (!string.IsNullOrWhiteSpace(n.LinkUrl))
            return Redirect(n.LinkUrl);

        return RedirectToAction(nameof(Index), new { filter = returnFilter });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult MarkAllAsRead(string? returnFilter)
    {
        var guard = RequireLogin();
        if (guard != null) return guard;

        var userId = GetUserId();
        if (userId == 0) return Forbid();

        var unread = _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToList();

        foreach (var n in unread) n.IsRead = true;
        _context.SaveChanges();

        TempData["Success"] = $"{unread.Count} notification(s) marked as read.";
        return RedirectToAction(nameof(Index), new { filter = returnFilter });
    }

    // =============================================================
    // DISMISS (delete)
    // =============================================================

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Dismiss(int id, string? returnFilter)
    {
        var guard = RequireLogin();
        if (guard != null) return guard;

        var userId = GetUserId();
        if (userId == 0) return Forbid();

        var n = _context.Notifications
            .FirstOrDefault(x => x.NotificationId == id && x.UserId == userId);

        if (n == null) return NotFound();

        _context.Notifications.Remove(n);
        _context.SaveChanges();

        return RedirectToAction(nameof(Index), new { filter = returnFilter });
    }

    // =============================================================
    // UNREAD COUNT (AJAX for bell)
    // =============================================================

    [HttpGet]
    public IActionResult GetUnreadCount()
    {
        if (!_auth.IsLoggedIn())
            return Json(new { count = 0 });

        var userId = GetUserId();
        if (userId == 0) return Json(new { count = 0 });

        var count = _context.Notifications
            .AsNoTracking()
            .Count(n => n.UserId == userId && !n.IsRead);

        // Also return the 5 most recent unread for the dropdown
        var latest = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .Select(n => new
            {
                id = n.NotificationId,
                category = n.Category,
                title = n.Title,
                createdAt = n.CreatedAt.ToString("dd MMM HH:mm")
            })
            .ToList();

        return Json(new { count, latest });
    }

    // =============================================================
    // CALENDAR (.ics download)
    // =============================================================

    [HttpGet]
    public IActionResult CalendarIcs(int id)
    {
        var guard = RequireLogin();
        if (guard != null) return guard;

        var userId = GetUserId();
        if (userId == 0) return Forbid();

        var n = _context.Notifications
            .AsNoTracking()
            .FirstOrDefault(x => x.NotificationId == id && x.UserId == userId);

        if (n == null) return NotFound();
        if (!n.CalendarStart.HasValue) return BadRequest("This notification has no calendar entry.");

        var start = n.CalendarStart.Value;
        var end = n.CalendarEnd ?? start.AddMinutes(30);

        var ics = BuildIcs(n.Title, n.Message, start, end, n.NotificationId);

        var bytes = Encoding.UTF8.GetBytes(ics);
        var filename = $"nmb-hlabsys-{n.NotificationId}.ics";

        return File(bytes, "text/calendar", filename);
    }

    private static string BuildIcs(string title, string description, DateTime start, DateTime end, int uid)
    {
        // RFC 5545 basic VEVENT — works with Outlook, Google Calendar, Apple Calendar
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//NMB-HLabSys//Notification//EN");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
        sb.AppendLine("BEGIN:VEVENT");
        sb.AppendLine($"UID:{uid}@nmb-hlabsys.local");
        sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
        sb.AppendLine($"DTSTART:{start.ToUniversalTime():yyyyMMddTHHmmssZ}");
        sb.AppendLine($"DTEND:{end.ToUniversalTime():yyyyMMddTHHmmssZ}");
        sb.AppendLine($"SUMMARY:{Escape(title)}");
        sb.AppendLine($"DESCRIPTION:{Escape(description)}");
        sb.AppendLine("BEGIN:VALARM");
        sb.AppendLine("TRIGGER:-PT15M");
        sb.AppendLine("ACTION:DISPLAY");
        sb.AppendLine("DESCRIPTION:Reminder");
        sb.AppendLine("END:VALARM");
        sb.AppendLine("END:VEVENT");
        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("\\", "\\\\")
                .Replace(";", "\\;")
                .Replace(",", "\\,")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n");
    }
}