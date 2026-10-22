using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Services;
namespace Staybnb.Controllers;
[Authorize]
public class MessagesController(ApplicationDbContext db, INotifier notifier) : Controller
{
    string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier);
    public async Task<IActionResult> Index()
    {
        var msgs = await db.Messages.Include(m => m.Sender).Include(m => m.Receiver).Where(m => m.SenderId == Uid || m.ReceiverId == Uid).OrderByDescending(m => m.Timestamp).ToListAsync();
        var convos = msgs.GroupBy(m => m.SenderId == Uid ? m.ReceiverId : m.SenderId).Select(g => g.First()).ToList();
        return View(convos);
    }
    public async Task<IActionResult> Thread(string userId)
    {
        var other = await db.Users.FindAsync(userId);
        if (other == null || userId == Uid) return NotFound();
        var unread = await db.Messages.Where(m => m.SenderId == userId && m.ReceiverId == Uid && !m.IsRead).ToListAsync();
        unread.ForEach(m => m.IsRead = true); // read status persisted
        await db.SaveChangesAsync();
        ViewBag.Other = other;
        return View(await db.Messages.Where(m => (m.SenderId == Uid && m.ReceiverId == userId) || (m.SenderId == userId && m.ReceiverId == Uid)).OrderBy(m => m.Timestamp).ToListAsync());
    }
    [HttpPost]
    public async Task<IActionResult> Send(string receiverId, string content)
    {
        if (!string.IsNullOrWhiteSpace(content) && await db.Users.AnyAsync(u => u.Id == receiverId))
        {
            db.Messages.Add(new Message { SenderId = Uid, ReceiverId = receiverId, Content = content.Trim() });
            await db.SaveChangesAsync();
            await notifier.NotifyAsync(receiverId, "New Message", "You have a new message.", NotificationType.Message, $"/Messages/Thread?userId={Uid}");
        }
        return RedirectToAction(nameof(Thread), new { userId = receiverId });
    }
}
