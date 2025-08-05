using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CampWeb.Models;

namespace CampWeb.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Camp> Camps { get; set; }
    public DbSet<Registration> Registrations { get; set; }
    public DbSet<CampPhoto> CampPhotos { get; set; }
    public DbSet<LiveUpdate> LiveUpdates { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Camp entity
        builder.Entity<Camp>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.AgeGroup).HasMaxLength(20);
            
            // Configure JSON columns for lists
            entity.Property(e => e.Images)
                .HasConversion(
                    v => string.Join(';', v),
                    v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());
            
            entity.Property(e => e.Activities)
                .HasConversion(
                    v => string.Join(';', v),
                    v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());
        });

        // Configure Registration entity
        builder.Entity<Registration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChildName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ChildSurname).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ParentName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ParentEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ParentPhone).IsRequired().HasMaxLength(20);
            entity.Property(e => e.AccessCode).IsRequired().HasMaxLength(8);
            entity.Property(e => e.SpecialRequirements).HasMaxLength(1000);
            entity.Property(e => e.MedicalIssuesDescription).HasMaxLength(1000);
            
            entity.HasIndex(e => e.AccessCode).IsUnique();
            entity.HasOne<Camp>().WithMany().HasForeignKey(e => e.CampId);
        });

        // Configure CampPhoto entity
        builder.Entity<CampPhoto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            entity.HasOne<Camp>().WithMany().HasForeignKey(e => e.CampId);
        });

        // Configure LiveUpdate entity
        builder.Entity<LiveUpdate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.PhotoUrl).HasMaxLength(500);
            
            entity.HasOne<Camp>().WithMany().HasForeignKey(e => e.CampId);
        });

        // Seed initial data
        SeedData(builder);
    }

    private static void SeedData(ModelBuilder builder)
    {
        // Seed camps
        builder.Entity<Camp>().HasData(
            new Camp
            {
                Id = 1,
                Name = "Dobrodružný tábor Šumava",
                Location = "Šumava, 35 km od Plzně",
                Type = "adventure",
                Price = 5900,
                AvailableSpots = 8,
                AgeGroup = "8-15",
                ShortDescription = "Turistika v krásné přírodě Šumavy s nocováním pod hvězdami.",
                Description = "Prožijte nezapomenutelné dobrodružství v srdci Šumavy! Náš tábor kombinuje aktivní turistiku s poznáváním přírody. Děti se naučí orientaci v terénu, založení táboráku a základní outdoorové dovednosti.",
                Latitude = 49.2847,
                Longitude = 13.5441,
                Images = ["https://images.unsplash.com/photo-1504851149312-7a075b496cc7?w=800;https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800"],
                Activities = ["Turistika;Táboráky;Orientace v terénu;Příroda;Stanování"],
                StartDate = DateTime.Now.AddMonths(3),
                EndDate = DateTime.Now.AddMonths(3).AddDays(7)
            },
            new Camp
            {
                Id = 2,
                Name = "Sportovní tábor Plzeň",
                Location = "Plzeň - Doubravka",
                Type = "sport",
                Price = 4500,
                AvailableSpots = 2,
                AgeGroup = "10-16",
                ShortDescription = "Fotbal, volejbal, atletika a další sporty.",
                Description = "Sportovní tábor pro mladé nadšence sportu. Program zahrnuje fotbal, volejbal, atletiku, plavání a další sporty. Kvalifikovaní trenéři a moderní sportovní zázemí.",
                Latitude = 49.7473,
                Longitude = 13.3776,
                Images = ["https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=800;https://images.unsplash.com/photo-1596464716127-f2a82984de30?w=800"],
                Activities = ["Fotbal;Volejbal;Atletika;Plavání;Basketbal"],
                StartDate = DateTime.Now.AddMonths(3),
                EndDate = DateTime.Now.AddMonths(3).AddDays(7)
            },
            new Camp
            {
                Id = 3,
                Name = "Kreativní tábor",
                Location = "Plzeň centrum",
                Type = "creative",
                Price = 5200,
                AvailableSpots = 12,
                AgeGroup = "6-14",
                ShortDescription = "Výtvarné dílo, hudba, divadlo a rukodělné aktivity.",
                Description = "Tábor pro malé umělce! Kreativní dílny, výtvarné techniky, hudební nástroje, divadelní představení a mnoho dalšího. Rozvíjíme fantazii a tvořivost dětí.",
                Latitude = 49.7384,
                Longitude = 13.3736,
                Images = ["https://images.unsplash.com/photo-1513475382585-d06e58bcb0e0?w=800;https://images.unsplash.com/photo-1503095396549-807759245b35?w=800"],
                Activities = ["Malování;Keramika;Hudba;Divadlo;Rukodělné práce"],
                StartDate = DateTime.Now.AddMonths(3),
                EndDate = DateTime.Now.AddMonths(3).AddDays(7)
            }
        );
    }
}