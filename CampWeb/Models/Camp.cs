namespace CampWeb.Models;

public class Camp
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Location { get; set; } = "";
    public string Type { get; set; } = "";
    public int Price { get; set; }
    public int AvailableSpots { get; set; }
    public string AgeGroup { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public string Description { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public List<string> Images { get; set; } = new();
    public List<string> Activities { get; set; } = new();
    public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(3);
    public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(3).AddDays(7);
}

public class Registration
{
    public int Id { get; set; }
    public int CampId { get; set; }
    public string ChildName { get; set; } = "";
    public string ChildSurname { get; set; } = "";
    public DateTime ChildBirthDate { get; set; }
    public string ParentName { get; set; } = "";
    public string ParentEmail { get; set; } = "";
    public string ParentPhone { get; set; } = "";
    public string SpecialRequirements { get; set; } = "";
    public bool HasMedicalIssues { get; set; }
    public string MedicalIssuesDescription { get; set; } = "";
    public DateTime RegistrationDate { get; set; } = DateTime.Now;
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;
    public string AccessCode { get; set; } = "";
    
    // Foreign key to Identity User
    public string? UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }
    
    // Navigation to Camp
    public virtual Camp? Camp { get; set; }
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
    public int CampId { get; set; }
    public string FileName { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime UploadDate { get; set; } = DateTime.Now;
    public bool IsPublic { get; set; } = true;
    
    // Navigation to Camp
    public virtual Camp? Camp { get; set; }
}

public class LiveUpdate
{
    public int Id { get; set; }
    public int CampId { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? PhotoUrl { get; set; }
    
    // Navigation to Camp
    public virtual Camp? Camp { get; set; }
}