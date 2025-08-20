using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampWeb.Models;

public class Camp
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = "";
    
    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = "";
    
    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = "";
    
    [Required]
    public decimal Price { get; set; }
    
    [Required]
    public int AvailableSpots { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string AgeGroup { get; set; } = "";
    
    [MaxLength(500)]
    public string ShortDescription { get; set; } = "";
    
    public string Description { get; set; } = "";
    
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    
    // This will be stored as JSONB in PostgreSQL
    public List<string> Activities { get; set; } = new();
    [Required]
    public int Capacity { get; set; }
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }

    // Navigation properties
    public virtual ICollection<CampPhoto> Photos { get; set; } = new List<CampPhoto>();
    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    public virtual ICollection<LiveUpdate> LiveUpdates { get; set; } = new List<LiveUpdate>();
    
    // Computed properties
    [NotMapped]
    public int RegisteredCount => Registrations?.Count(r => r.Status != RegistrationStatus.Cancelled) ?? 0;
    
    [NotMapped]
    public bool IsFull => RegisteredCount >= AvailableSpots;
    
    [NotMapped]
    public int DaysLeft => (StartDate - DateTime.Now).Days;
}

public class Registration
{
    public int Id { get; set; }
    
    [Required]
    public int CampId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ChildName { get; set; } = "";
    
    [Required]
    [MaxLength(100)]
    public string ChildSurname { get; set; } = "";
    
    [Required]
    public DateTime ChildBirthDate { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string ParentName { get; set; } = "";
    
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string ParentEmail { get; set; } = "";
    
    [Required]
    [Phone]
    [MaxLength(20)]
    public string ParentPhone { get; set; } = "";
    
    public string SpecialRequirements { get; set; } = "";
    
    public bool HasMedicalIssues { get; set; }
    
    public string MedicalIssuesDescription { get; set; } = "";
    
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    
    [Required]
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;
    
    [Required]
    [MaxLength(8)]
    public string AccessCode { get; set; } = "";

    // Foreign key to Identity User (nullable - guest registrations allowed)
    public string? UserId { get; set; }
    
    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual Camp Camp { get; set; } = null!;
    
    // Computed properties
    [NotMapped]
    public string ChildFullName => $"{ChildName} {ChildSurname}".Trim();
    
    [NotMapped]
    public int ChildAge => DateTime.Now.Year - ChildBirthDate.Year - 
                          (DateTime.Now.DayOfYear < ChildBirthDate.DayOfYear ? 1 : 0);
}

public enum RegistrationStatus
{
    Pending,
    Confirmed,
    Paid,
    Cancelled
}

public class CampPhoto
{
    public int Id { get; set; }
    
    [Required]
    public int CampId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = "";
    
    [MaxLength(500)]
    public string Description { get; set; } = "";
    
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    
    public bool IsPublic { get; set; } = true;

    // Navigation property
    public virtual Camp Camp { get; set; } = null!;
    
    // Computed properties
    [NotMapped]
    public string FilePath => $"/uploads/camps/{CampId}/{FileName}";
}

public class LiveUpdate
{
    public int Id { get; set; }
    
    [Required]
    public int CampId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";
    
    [Required]
    public string Content { get; set; } = "";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    // Navigation property
    public virtual Camp Camp { get; set; } = null!;
}