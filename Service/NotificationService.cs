using NMB_HLabSys.Data;
using NMB_HLabSys.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace NMB_HLabSys.Services;

/// <summary>
/// Interface so the controller can depend on an abstraction (registered in Program.cs).
/// </summary>
public interface INotificationService
{
    Notification? Notify(
        int userId,
        string category,
        string title,
        string message,
        string? linkUrl = null,
        DateTime? calendarStart = null,
        DateTime? calendarEnd = null);

    Notification? NotifyOnce(
        int userId,
        string category,
        string title,
        string message,
        string? linkUrl = null,
        DateTime? calendarStart = null,
        DateTime? calendarEnd = null,
        int dedupWindowHours = 6);

    List<Notification> NotifyRole(
        string role,
        string category,
        string title,
        string message,
        string? linkUrl = null,
        DateTime? calendarStart = null,
        DateTime? calendarEnd = null);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Create a notification for a specific user.
    /// Returns null (never throws) if the user doesn't exist or the insert fails.
    /// </summary>
    public Notification? Notify(
        int userId,
        string category,
        string title,
        string message,
        string? linkUrl = null,
        DateTime? calendarStart = null,
        DateTime? calendarEnd = null)
    {
        if (userId <= 0)
        {
            Console.WriteLine($"[NotificationService] SKIPPED: userId={userId} (<=0). Title='{title}'");
            return null;
        }

        var userExists = _context.Users.Any(u => u.UserId == userId);
        if (!userExists)
        {
            Console.WriteLine($"[NotificationService] SKIPPED: no User with UserId={userId}. Title='{title}'");
            return null;
        }

        if (!string.IsNullOrEmpty(category) && category.Length > 20)
            category = category.Substring(0, 20);

        if (!string.IsNullOrEmpty(title) && title.Length > 200)
            title = title.Substring(0, 200);

        if (!string.IsNullOrEmpty(message) && message.Length > 1000)
            message = message.Substring(0, 1000);

        if (!string.IsNullOrEmpty(linkUrl) && linkUrl.Length > 500)
            linkUrl = linkUrl.Substring(0, 500);

        var n = new Notification
        {
            UserId = userId,
            Category = string.IsNullOrWhiteSpace(category) ? "Info" : category,
            Title = string.IsNullOrWhiteSpace(title) ? "(no title)" : title,
            Message = message ?? string.Empty,
            LinkUrl = linkUrl,
            CalendarStart = calendarStart,
            CalendarEnd = calendarEnd,
            IsRead = false,
            CreatedAt = DateTime.Now
        };

        try
        {
            _context.Notifications.Add(n);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine("=========================================");
            Console.WriteLine("[NotificationService] SAVE FAILED");
            Console.WriteLine("  Category : " + category);
            Console.WriteLine("  Title    : " + title);
            Console.WriteLine("  UserId   : " + userId);
            Console.WriteLine("  Outer    : " + ex.Message);
            Console.WriteLine("  Inner    : " + ex.InnerException?.Message);
            Console.WriteLine("  SQL      : " + ex.InnerException?.InnerException?.Message);
            Console.WriteLine("=========================================");

            try { _context.Entry(n).State = EntityState.Detached; } catch { }

            return null;
        }

        return n;
    }

    /// <summary>
    /// Create a notification only if one doesn't already exist for the same
    /// user, category and title in the last X hours. Prevents spam.
    /// Never throws.
    /// </summary>
    public Notification? NotifyOnce(
        int userId,
        string category,
        string title,
        string message,
        string? linkUrl = null,
        DateTime? calendarStart = null,
        DateTime? calendarEnd = null,
        int dedupWindowHours = 6)
    {
        if (userId <= 0) return null;

        try
        {
            var cutoff = DateTime.Now.AddHours(-dedupWindowHours);

            var existing = _context.Notifications
                .Where(n => n.UserId == userId
                    && n.Category == category
                    && n.Title == title
                    && n.CreatedAt >= cutoff)
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefault();

            if (existing != null) return existing;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationService] Dedup check failed: {ex.Message}");
        }

        return Notify(userId, category, title, message, linkUrl, calendarStart, calendarEnd);
    }

    /// <summary>
    /// Notify every user in the given role (e.g. "Manager").
    /// Never throws.
    /// </summary>
    public List<Notification> NotifyRole(
        string role,
        string category,
        string title,
        string message,
        string? linkUrl = null,
        DateTime? calendarStart = null,
        DateTime? calendarEnd = null)
    {
        var created = new List<Notification>();

        List<int> userIds;
        try
        {
            userIds = _context.Users
                .Where(u => u.Role == role)
                .Select(u => u.UserId)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationService] Failed to look up role '{role}': {ex.Message}");
            return created;
        }

        foreach (var id in userIds)
        {
            var n = Notify(id, category, title, message, linkUrl, calendarStart, calendarEnd);
            if (n != null) created.Add(n);
        }

        return created;
    }
}