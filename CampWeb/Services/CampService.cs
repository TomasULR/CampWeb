using CampWeb.Models;
using CampWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace CampWeb.Services;

public interface ICampService
{
    Task<List<Camp>> GetAllCampsAsync();
    Task<List<Camp>> GetPopularCampsAsync();
    Task<Camp?> GetCampByIdAsync(int id);
    Task<List<Camp>> FilterCampsAsync(string? type = null, string? ageGroup = null, int? maxPrice = null);
    Task<List<Camp>> GetAvailableCampsAsync();
    Task<List<Camp>> SearchCampsAsync(string? searchTerm, string? type, int? minPrice, int? maxPrice);
    Task<bool> RegisterForCampAsync(Registration registration);
    Task<List<Registration>> GetUserRegistrationsAsync(string userId);
    Task<Registration?> GetRegistrationByAccessCodeAsync(string accessCode);

    // Admin:
    Task<Camp> CreateAsync(Camp camp);
    Task<Camp?> UpdateAsync(Camp camp);
    Task<bool> DeleteAsync(int id);
    Task<bool> UpdateCapacityAsync(int campId, int capacity);
    Task<bool> UpdatePriceAsync(int campId, decimal price);
    Task<bool> RecalculateAvailableSpotsAsync(int campId);
    Task<Camp?> DuplicateAsync(int sourceCampId, DateTime? newStart = null, DateTime? newEnd = null);
    Task<string> EnsureCampAccessCodeAsync(int campId);
}

public class CampService : ICampService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ILogger<CampService> _logger;

    public CampService(IDbContextFactory<ApplicationDbContext> dbFactory, ILogger<CampService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    // ========= CREATE / UPDATE / DELETE =========

    public async Task<Camp> CreateAsync(Camp camp)
    {
        await using var ctx = await _dbFactory.CreateDbContextAsync();

        // pokud není nastaveno, nastav AvailableSpots = Capacity, ať na dashboardu nesvítí 100 %
        if (camp.AvailableSpots < 0 || camp.AvailableSpots > camp.Capacity)
            camp.AvailableSpots = camp.Capacity;

        ctx.Camps.Add(camp);
        await ctx.SaveChangesAsync();
        return camp;
    }

    public async Task<Camp?> UpdateAsync(Camp camp)
    {
        await using var ctx = await _dbFactory.CreateDbContextAsync();
        var db = await ctx.Camps
            .Include(c => c.Registrations)
            .SingleOrDefaultAsync(c => c.Id == camp.Id);

        if (db == null) return null;

        db.Name              = camp.Name;
        db.Location          = camp.Location;
        db.Type              = camp.Type;
        db.Price             = camp.Price;
        db.Capacity          = camp.Capacity;
        db.AvailableSpots    = (camp.AvailableSpots >= 0 && camp.AvailableSpots <= camp.Capacity)
                                ? camp.AvailableSpots
                                : Math.Max(0, camp.Capacity - (db.Registrations?.Count(r => r.Status != RegistrationStatus.Cancelled) ?? 0));
        db.AgeGroup          = camp.AgeGroup;
        db.ShortDescription  = camp.ShortDescription;
        db.Description       = camp.Description;
        db.Latitude          = camp.Latitude;
        db.Longitude         = camp.Longitude;
        db.StartDate         = camp.StartDate;
        db.EndDate           = camp.EndDate;

        await ctx.SaveChangesAsync();
        return db;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var ctx = await _dbFactory.CreateDbContextAsync();
        var camp = await ctx.Camps
            .Include(c => c.Photos)
            .Include(c => c.Registrations)
            .Include(c => c.LiveUpdates)
            .SingleOrDefaultAsync(c => c.Id == id);

        if (camp == null) return false;

        if (camp.Photos?.Any() == true)        ctx.CampPhotos.RemoveRange(camp.Photos);
        if (camp.Registrations?.Any() == true) ctx.Registrations.RemoveRange(camp.Registrations);
        if (camp.LiveUpdates?.Any() == true)   ctx.LiveUpdates.RemoveRange(camp.LiveUpdates);

        ctx.Camps.Remove(camp);
        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdatePriceAsync(int campId, decimal price)
    {
        await using var ctx = await _dbFactory.CreateDbContextAsync();
        var camp = await ctx.Camps.FindAsync(campId);
        if (camp == null) return false;

        camp.Price = price;
        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateCapacityAsync(int campId, int capacity)
    {
        await using var ctx = await _dbFactory.CreateDbContextAsync();
        var camp = await ctx.Camps
            .Include(c => c.Registrations)
            .SingleOrDefaultAsync(c => c.Id == campId);

        if (camp == null) return false;

        camp.Capacity = capacity;
        var reserved = camp.Registrations?.Count(r => r.Status != RegistrationStatus.Cancelled) ?? 0;
        camp.AvailableSpots = Math.Max(0, capacity - reserved);

        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RecalculateAvailableSpotsAsync(int campId)
    {
        await using var ctx = await _dbFactory.CreateDbContextAsync();
        var camp = await ctx.Camps
            .Include(c => c.Registrations)
            .SingleOrDefaultAsync(c => c.Id == campId);

        if (camp == null) return false;

        var reserved = camp.Registrations?.Count(r => r.Status != RegistrationStatus.Cancelled) ?? 0;
        camp.AvailableSpots = Math.Max(0, camp.Capacity - reserved);

        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<Camp?> DuplicateAsync(int sourceCampId, DateTime? newStart = null, DateTime? newEnd = null)
    {
        await using var ctx = await _dbFactory.CreateDbContextAsync();
        var src = await ctx.Camps.SingleOrDefaultAsync(c => c.Id == sourceCampId);
        if (src == null) return null;

        var copy = new Camp
        {
            Name             = src.Name,
            Location         = src.Location,
            Type             = src.Type,
            Price            = src.Price,
            Capacity         = src.Capacity,
            AvailableSpots   = src.Capacity, // start s plnou kapacitou
            AgeGroup         = src.AgeGroup,
            ShortDescription = src.ShortDescription,
            Description      = src.Description,
            Latitude         = src.Latitude,
            Longitude        = src.Longitude,
            StartDate        = newStart ?? src.StartDate,
            EndDate          = newEnd   ?? src.EndDate
        };

        ctx.Camps.Add(copy);
        await ctx.SaveChangesAsync();
        return copy;
    }

    public async Task<string> EnsureCampAccessCodeAsync(int campId)
    {
        await using var ctx = await _dbFactory.CreateDbContextAsync();
        var camp = await ctx.Camps.SingleOrDefaultAsync(c => c.Id == campId);
        if (camp == null) return string.Empty;

        if (string.IsNullOrWhiteSpace(camp.AccessCode))
        {
            camp.AccessCode = await GenerateUniqueCampAccessCodeAsync(ctx);
            await ctx.SaveChangesAsync();
        }

        return camp.AccessCode!;
    }

    private static async Task<string> GenerateUniqueCampAccessCodeAsync(ApplicationDbContext ctx)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var rnd = new Random();
        string code;
        do
        {
            code = new string(Enumerable.Repeat(chars, 8).Select(s => s[rnd.Next(s.Length)]).ToArray());
        } while (await ctx.Camps.AnyAsync(c => c.AccessCode == code));
        return code;
        // (alternativně můžeš použít Guid.substr(0,8).ToUpper, ale tohle je čitelnější pro rodiče)
    }

    // ========= READ METHODS =========

    public async Task<List<Camp>> GetAllCampsAsync()
    {
        try
        {
            await using var ctx = await _dbFactory.CreateDbContextAsync();
            return await ctx.Camps
                .Include(c => c.Photos)
                .Include(c => c.Registrations)
                .Include(c => c.LiveUpdates)
                .AsNoTracking()
                .OrderBy(c => c.StartDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all camps from database");
            return new List<Camp>();
        }
    }

    public async Task<List<Camp>> GetPopularCampsAsync()
    {
        try
        {
            await using var ctx = await _dbFactory.CreateDbContextAsync();
            var camps = await ctx.Camps
                .Include(c => c.Registrations)
                .Where(c => c.AvailableSpots > 0 && c.StartDate > DateTime.UtcNow)
                .AsNoTracking()
                .ToListAsync();

            return camps
                .OrderByDescending(c => c.Registrations?.Count ?? 0)
                .Take(3)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular camps");
            return new List<Camp>();
        }
    }

    public async Task<Camp?> GetCampByIdAsync(int id)
    {
        try
        {
            await using var ctx = await _dbFactory.CreateDbContextAsync();
            return await ctx.Camps
                .Include(c => c.Photos)
                .Include(c => c.Registrations)
                .Include(c => c.LiveUpdates)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting camp by ID: {CampId}", id);
            return null;
        }
    }

    public async Task<List<Camp>> FilterCampsAsync(string? type = null, string? ageGroup = null, int? maxPrice = null)
    {
        try
        {
            await using var ctx = await _dbFactory.CreateDbContextAsync();
            var query = ctx.Camps.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(c => c.Type == type);

            if (!string.IsNullOrWhiteSpace(ageGroup))
                query = query.Where(c => MatchesAgeGroup(c.AgeGroup, ageGroup));

            if (maxPrice.HasValue)
                query = query.Where(c => c.Price <= maxPrice.Value);

            return await query.OrderBy(c => c.StartDate).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering camps");
            return new List<Camp>();
        }
    }

    public async Task<List<Camp>> GetAvailableCampsAsync()
    {
        try
        {
            await using var ctx = await _dbFactory.CreateDbContextAsync();
            return await ctx.Camps
                .AsNoTracking()
                .Where(c => c.AvailableSpots > 0 && c.StartDate > DateTime.UtcNow)
                .OrderBy(c => c.StartDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available camps");
            return new List<Camp>();
        }
    }

    public async Task<List<Camp>> SearchCampsAsync(string? searchTerm, string? type, int? minPrice, int? maxPrice)
    {
        try
        {
            await using var ctx = await _dbFactory.CreateDbContextAsync();
            var query = ctx.Camps.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var s = searchTerm.Trim().ToLowerInvariant();
                query = query.Where(c =>
                    (c.Name ?? "").ToLower().Contains(s) ||
                    (c.Description ?? "").ToLower().Contains(s) ||
                    (c.ShortDescription ?? "").ToLower().Contains(s) ||
                    (c.Location ?? "").ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(c => c.Type == type);

            if (minPrice.HasValue)
                query = query.Where(c => c.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(c => c.Price <= maxPrice.Value);

            return await query.OrderBy(c => c.StartDate).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching camps");
            return new List<Camp>();
        }
    }

    // ========= REGISTRATIONS =========

    public async Task<bool> RegisterForCampAsync(Registration registration)
    {
        try
        {
            await using var ctx = await _dbFactory.CreateDbContextAsync();

            var camp = await ctx.Camps.SingleOrDefaultAsync(c => c.Id == registration.CampId);
            if (camp == null || camp.AvailableSpots <= 0)
            {
                _logger.LogWarning("Camp {CampId} not found or no available spots", registration.CampId);
                return false;
            }

            // duplicitní registrace
            var exists = await ctx.Registrations
                .AnyAsync(r => r.UserId == registration.UserId && r.CampId == registration.CampId);
            if (exists)
            {
                _logger.LogWarning("User {UserId} already registered for camp {CampId}", registration.UserId, registration.CampId);
                return false;
            }

            // unikátní access code
            string accessCode;
            do { accessCode = GenerateAccessCode(); }
            while (await ctx.Registrations.AnyAsync(r => r.AccessCode == accessCode));
            registration.AccessCode = accessCode;

            // DateTime do UTC (pro konzistenci)
            registration.RegistrationDate = DateTime.SpecifyKind(registration.RegistrationDate, DateTimeKind.Utc);
            registration.ChildBirthDate  = DateTime.SpecifyKind(registration.ChildBirthDate,  DateTimeKind.Utc);

            ctx.Registrations.Add(registration);

            // snížení volných míst
            camp.AvailableSpots = Math.Max(0, camp.AvailableSpots - 1);

            await ctx.SaveChangesAsync();
            _logger.LogInformation("Registration successful: {AccessCode} for camp {CampId}", accessCode, registration.CampId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering for camp {CampId}", registration.CampId);
            return false;
        }
    }

    private static string GenerateAccessCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var rnd = new Random();
        return new string(Enumerable.Repeat(chars, 8).Select(s => s[rnd.Next(s.Length)]).ToArray());
    }

    public async Task<List<Registration>> GetUserRegistrationsAsync(string userId)
    {
        try
        {
            await using var ctx = await _dbFactory.CreateDbContextAsync();
            return await ctx.Registrations
                .Include(r => r.Camp)
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RegistrationDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user registrations for user {UserId}", userId);
            return new List<Registration>();
        }
    }

    public async Task<Registration?> GetRegistrationByAccessCodeAsync(string accessCode)
    {
        try
        {
            await using var ctx = await _dbFactory.CreateDbContextAsync();
            return await ctx.Registrations
                .Include(r => r.Camp)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.AccessCode == accessCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registration by access code");
            return null;
        }
    }

    // ========= helpers =========

    private static bool MatchesAgeGroup(string campAge, string filterAge)
    {
        // rychlý, ale tolerantní matcher (ponechávám dle tvé logiky)
        if (string.IsNullOrWhiteSpace(filterAge)) return true;
        campAge ??= "";
        return filterAge switch
        {
            "6-9"    => campAge.Contains("6") || campAge.Contains("7") || campAge.Contains("8") || campAge.Contains("9"),
            "10-13"  => campAge.Contains("10") || campAge.Contains("11") || campAge.Contains("12") || campAge.Contains("13"),
            "14-17"  => campAge.Contains("14") || campAge.Contains("15") || campAge.Contains("16") || campAge.Contains("17"),
            _        => true
        };
    }
}
