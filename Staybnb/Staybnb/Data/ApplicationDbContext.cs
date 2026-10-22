using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Staybnb.Models;
namespace Staybnb.Data;
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> o) : base(o) { }
    public DbSet<HostProperty> Properties => Set<HostProperty>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<HostApplication> HostApplications => Set<HostApplication>();
    public DbSet<CheckInProcess> CheckInProcesses => Set<CheckInProcess>();
    public DbSet<GuestCheckIn> GuestCheckIns => Set<GuestCheckIn>();
    public DbSet<GuestDocument> GuestDocuments => Set<GuestDocument>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
        mb.Entity<Booking>().HasOne(b => b.Payment).WithOne(p => p.Booking).HasForeignKey<Payment>(p => p.BookingId);
        mb.Entity<Booking>().HasOne(b => b.GuestCheckIn).WithOne(g => g.Booking).HasForeignKey<GuestCheckIn>(g => g.BookingId);
        mb.Entity<HostProperty>().HasOne(p => p.CheckInProcess).WithOne(c => c.Property).HasForeignKey<CheckInProcess>(c => c.PropertyId);
        mb.Entity<HostProperty>().HasOne(p => p.Host).WithMany().HasForeignKey(p => p.HostId);
        mb.Entity<Message>().HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId);
        mb.Entity<Message>().HasOne(m => m.Receiver).WithMany().HasForeignKey(m => m.ReceiverId);
        mb.Entity<HostApplication>().HasOne(a => a.ApplicationUser).WithMany().HasForeignKey(a => a.ApplicationUserId);
        // avoid multiple cascade paths in SQL Server
        foreach (var fk in mb.Model.GetEntityTypes().Where(t => t.ClrType.Namespace == "Staybnb.Models").SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
