using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CampWeb.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string FirstName { get; set; } = "";
    
    [MaxLength(100)]
    public string LastName { get; set; } = "";
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation properties for registrations
    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    
    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Email je povinný")]
    [EmailAddress(ErrorMessage = "Neplatný formát emailu")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Heslo je povinné")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Jméno je povinné")]
    [MaxLength(100)]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Příjmení je povinné")]
    [MaxLength(100)]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Email je povinný")]
    [EmailAddress(ErrorMessage = "Neplatný formát emailu")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Heslo je povinné")]
    [StringLength(100, ErrorMessage = "Heslo musí mít alespoň {2} znaků", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Hesla se neshodují")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Telefon je povinný")]
    [Phone(ErrorMessage = "Neplatné telefonní číslo")]
    public string PhoneNumber { get; set; } = "";
}