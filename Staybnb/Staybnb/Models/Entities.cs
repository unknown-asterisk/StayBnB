using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
namespace Staybnb.Models;

public enum BookingStatus { Pending, Approved, Rejected, CheckedIn, Cancelled }
public enum PaymentStatus { Pending, Paid, Refunded }
public enum ApplicationStatus { Pending, Approved, Rejected }
public enum ActivityType { Login, Registration, RoleChange, BookingStatus, Application, Property, CheckIn, System }
public enum NotificationType { Info, Success, Warning, Booking, Message, Application }
public enum CheckInStatus { Pending, DocumentsSubmitted, Verified, Completed }
public enum DocumentStatus { Pending, Approved, Rejected }

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [NotMapped] public string FullName => $"{FirstName} {LastName}".Trim();
}

public class HostProperty
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Title { get; set; }
    [Required] public string Description { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal PricePerNight { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal CleaningFee { get; set; }
    [Required] public string Address { get; set; }
    [Required] public string City { get; set; }
    public string PropertyType { get; set; } = "Apartment";
    public int MaxGuests { get; set; } = 2;
    public bool IsActive { get; set; }
    public string HostId { get; set; }
    public ApplicationUser Host { get; set; }
    public List<PropertyImage> Images { get; set; } = new();
    public List<Amenity> Amenities { get; set; } = new();
    public List<Booking> Bookings { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
    public CheckInProcess CheckInProcess { get; set; }
}

public class PropertyImage { public int Id { get; set; } public int PropertyId { get; set; } public HostProperty Property { get; set; } [Required] public string ImageUrl { get; set; } }
public class Amenity { public int Id { get; set; } [StringLength(50)] public string Name { get; set; } [StringLength(50)] public string IconClass { get; set; } public List<HostProperty> Properties { get; set; } = new(); }

public class Booking
{
    public int Id { get; set; }
    public int PropertyId { get; set; } public HostProperty Property { get; set; }
    public string GuestId { get; set; } public ApplicationUser Guest { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int Guests { get; set; } = 1;
    [Column(TypeName = "decimal(18,2)")] public decimal CleaningFee { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ServiceFee { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public Payment Payment { get; set; }
    public GuestCheckIn GuestCheckIn { get; set; }
    public int Nights => (CheckOutDate.Date - CheckInDate.Date).Days;
}

public class Payment
{
    public int Id { get; set; }
    public int BookingId { get; set; } public Booking Booking { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Review
{
    public int Id { get; set; }
    public int PropertyId { get; set; } public HostProperty Property { get; set; }
    public string ReviewerId { get; set; } public ApplicationUser Reviewer { get; set; }
    [Range(1, 5)] public int Rating { get; set; }
    [StringLength(1000)] public string Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class WishlistItem { public int Id { get; set; } public string UserId { get; set; } public ApplicationUser User { get; set; } public int PropertyId { get; set; } public HostProperty Property { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; }

public class Message
{
    public int Id { get; set; }
    public string SenderId { get; set; } public ApplicationUser Sender { get; set; }
    public string ReceiverId { get; set; } public ApplicationUser Receiver { get; set; }
    [Required] public string Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}

public class ActivityLog
{
    public int Id { get; set; }
    public string UserId { get; set; } public ApplicationUser User { get; set; }
    [StringLength(100)] public string Action { get; set; }
    public ActivityType ActivityType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } public ApplicationUser User { get; set; }
    [StringLength(100)] public string Title { get; set; }
    public string Body { get; set; }
    public string Link { get; set; }
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class HostApplication
{
    public int Id { get; set; }
    public string ApplicationUserId { get; set; } public ApplicationUser ApplicationUser { get; set; }
    public int PropertyId { get; set; } public HostProperty Property { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}

public class CheckInProcess
{
    public int Id { get; set; }
    public int PropertyId { get; set; } public HostProperty Property { get; set; }
    [StringLength(200)] public string Title { get; set; }
    public string StepsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class GuestCheckIn
{
    public int Id { get; set; }
    public int BookingId { get; set; } public Booking Booking { get; set; }
    public int CheckInProcessId { get; set; } public CheckInProcess CheckInProcess { get; set; }
    public CheckInStatus Status { get; set; } = CheckInStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<GuestDocument> Documents { get; set; } = new();
}

public class GuestDocument
{
    public int Id { get; set; }
    public int GuestCheckInId { get; set; } public GuestCheckIn GuestCheckIn { get; set; }
    [StringLength(100)] public string DocumentType { get; set; }
    [StringLength(200)] public string FileName { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
}
