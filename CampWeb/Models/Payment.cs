using System.ComponentModel.DataAnnotations;

namespace CampWeb.Models;

public class Payment
{
    public int Id { get; set; }
    
    [Required]
    public int RegistrationId { get; set; }
    
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "CZK";
    
    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "";
    
    [MaxLength(500)]
    public string? PaymentToken { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string TransactionId { get; set; } = "";
    
    [Required]
    public PaymentStatus Status { get; set; }
    
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public virtual Registration Registration { get; set; } = null!;
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded,
    Cancelled
}