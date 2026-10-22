using Staybnb.Data;
using Staybnb.Models;
namespace Staybnb.Services;
public interface IActivityLogger { Task LogAsync(string userId, string action, ActivityType type); }
public class ActivityLogger(ApplicationDbContext db) : IActivityLogger
{
    public async Task LogAsync(string userId, string action, ActivityType type)
    { db.ActivityLogs.Add(new ActivityLog { UserId = userId, Action = action, ActivityType = type }); await db.SaveChangesAsync(); }
}
public interface INotifier { Task NotifyAsync(string userId, string title, string body, NotificationType type, string link = null); }
public class Notifier(ApplicationDbContext db) : INotifier
{
    public async Task NotifyAsync(string userId, string title, string body, NotificationType type, string link = null)
    { db.Notifications.Add(new Notification { UserId = userId, Title = title, Body = body, Type = type, Link = link }); await db.SaveChangesAsync(); }
}
public static class FileHelper
{
    static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
    public static bool IsValid(IFormFile f) => f != null && f.Length > 0 && f.Length < 8_000_000 && Allowed.Contains(Path.GetExtension(f.FileName).ToLower());
    public static async Task<string> SaveAsync(IFormFile f, IWebHostEnvironment env, string folder)
    {
        var name = $"{Guid.NewGuid()}{Path.GetExtension(f.FileName).ToLower()}";
        var dir = Path.Combine(env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(dir);
        using var fs = File.Create(Path.Combine(dir, name));
        await f.CopyToAsync(fs);
        return name;
    }
}
public static class Pricing
{
    public const decimal ServiceFeeRate = 0.12m;
    // TotalPrice = (PricePerNight x Nights) + CleaningFee + ServiceFee
    public static (decimal service, decimal total) Calculate(decimal pricePerNight, int nights, decimal cleaningFee)
    {
        var sub = pricePerNight * nights;
        var service = Math.Round(sub * ServiceFeeRate, 2);
        return (service, sub + cleaningFee + service);
    }
}
