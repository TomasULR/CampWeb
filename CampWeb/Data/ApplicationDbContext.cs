using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CampWeb.Models;
using Microsoft.AspNetCore.Identity;

namespace CampWeb.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Your custom entities
    public DbSet<Camp> Camps { get; set; }
    public DbSet<Registration> Registrations { get; set; }
    public DbSet<CampPhoto> CampPhotos { get; set; }
    public DbSet<LiveUpdate> LiveUpdates { get; set; }
    public DbSet<Payment> Payments { get; set; }

    // Note: Users (ApplicationUser) is already included via IdentityDbContext<ApplicationUser>
    // Note: Roles, UserRoles, UserClaims, etc. are automatically included via IdentityDbContext

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // IMPORTANT: Call base first for Identity tables
        base.OnModelCreating(modelBuilder);

        // Configure Camp entity
        modelBuilder.Entity<Camp>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AgeGroup).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            
            // Configure Activities as JSONB
            entity.Property(e => e.Activities)
                .HasColumnType("jsonb");

            entity.HasMany(e => e.Photos)
                .WithOne(p => p.Camp)
                .HasForeignKey(p => p.CampId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Registrations)
                .WithOne(r => r.Camp)
                .HasForeignKey(r => r.CampId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.LiveUpdates)
                .WithOne(u => u.Camp)
                .HasForeignKey(u => u.CampId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Registration entity
        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChildName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ChildSurname).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ParentName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ParentEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ParentPhone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.AccessCode).IsRequired().HasMaxLength(8);
            
            entity.HasIndex(e => e.AccessCode).IsUnique();
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Registrations)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure CampPhoto entity
        modelBuilder.Entity<CampPhoto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Configure LiveUpdate entity
        modelBuilder.Entity<LiveUpdate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.PhotoUrl).HasMaxLength(500);
        });

        // Configure ApplicationUser entity (additional properties)
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        });
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TransactionId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.TransactionId).IsUnique();
    
            entity.HasOne(e => e.Registration)
                .WithMany()
                .HasForeignKey(e => e.RegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}