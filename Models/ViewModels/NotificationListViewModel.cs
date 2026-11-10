namespace NMB_HLabSys.Models.ViewModels;

public class NotificationListViewModel
{
    public string? Filter { get; set; }
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public List<NotificationRowViewModel> Notifications { get; set; } = new();
}

public class NotificationRowViewModel
{
    public int NotificationId { get; set; }
    public string Category { get; set; } = "Info";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool HasCalendar { get; set; }
}