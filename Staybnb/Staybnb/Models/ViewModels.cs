using System.ComponentModel.DataAnnotations;
namespace Staybnb.Models;
public class RegisterVM { [Required] public string FirstName { get; set; } [Required] public string LastName { get; set; } [Required, EmailAddress] public string Email { get; set; } [Required, DataType(DataType.Password)] public string Password { get; set; } [Compare("Password"), DataType(DataType.Password)] public string ConfirmPassword { get; set; } }
public class LoginVM { [Required, EmailAddress] public string Email { get; set; } [Required, DataType(DataType.Password)] public string Password { get; set; } }
public class ApplyVM
{
    [Required, StringLength(100)] public string Title { get; set; }
    [Required] public string Description { get; set; }
    [Range(1, 1000000)] public decimal PricePerNight { get; set; }
    [Range(0, 100000)] public decimal CleaningFee { get; set; }
    [Required] public string Address { get; set; }
    [Required] public string City { get; set; }
    [Required] public string PropertyType { get; set; } = "Apartment";
    [Range(1, 50)] public int MaxGuests { get; set; } = 2;
}
public class ProfileVM { [Required] public string FirstName { get; set; } [Required] public string LastName { get; set; } }
