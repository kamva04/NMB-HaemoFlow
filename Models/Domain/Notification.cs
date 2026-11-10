using System.ComponentModel.DataAnnotations;

namespace NMB_HLabSys.Models.Domain;

public class Notification
{
    [Key]
    public int NotificationId { get; set; }

    // Who receives this notification
    public int UserId { get; set; }

    // Category: Info | Overdue | NearDue | Stock | STAT
    [Required, StringLength(20)]
    public string Category { get; set; } = "Info";

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    // Where the bell click goes
    [StringLength(500)]
    public string? LinkUrl { get; set; }

    // For .ics calendar download
    public DateTime? CalendarStart { get; set; }
    public DateTime? CalendarEnd { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public User? User { get; set; }
}