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
}

public class CampService : ICampService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CampService> _logger;

    public CampService(ApplicationDbContext context, ILogger<CampService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Camp>> GetAllCampsAsync()
    {
        try
        {
            return await _context.Camps
                .Include(c => c.Photos)
                .Include(c => c.Registrations)
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
            var camps = await _context.Camps
                .Include(c => c.Registrations)
                .Where(c => c.AvailableSpots > 0 && c.StartDate > DateTime.UtcNow)
                .ToListAsync();

            // Seřadit podle počtu registrací (nejvíce registrací = nejpopulárnější)
            return camps
                .OrderByDescending(c => c.Registrations.Count)
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
            return await _context.Camps
                .Include(c => c.Photos)
                .Include(c => c.Registrations)
                .Include(c => c.LiveUpdates)
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
            var query = _context.Camps.AsQueryable();

            if (!string.IsNullOrEmpty(type))
                query = query.Where(c => c.Type == type);

            if (!string.IsNullOrEmpty(ageGroup))
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
            return await _context.Camps
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
            var query = _context.Camps.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(lowerSearchTerm) ||
                    c.Description.ToLower().Contains(lowerSearchTerm) ||
                    c.ShortDescription.ToLower().Contains(lowerSearchTerm) ||
                    c.Location.ToLower().Contains(lowerSearchTerm));
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

    public async Task<bool> RegisterForCampAsync(Registration registration)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Zkontrolovat dostupnost míst
            var camp = await _context.Camps
                .FirstOrDefaultAsync(c => c.Id == registration.CampId);

            if (camp == null || camp.AvailableSpots <= 0)
            {
                _logger.LogWarning("Cannot register for camp {CampId} - no spots available", registration.CampId);
                return false;
            }

            // Vygenerovat přístupový kód
            registration.AccessCode = await GenerateUniqueAccessCodeAsync();
            // OPRAVENO: Použití DateTime.UtcNow místo DateTime.Now
            registration.RegistrationDate = DateTime.UtcNow;
            registration.Status = RegistrationStatus.Pending;

            // Přidat registraci
            _context.Registrations.Add(registration);

            // Snížit počet volných míst
            camp.AvailableSpots--;
            _context.Camps.Update(camp);

            // Uložit změny
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Successfully registered for camp {CampId} with access code {AccessCode}",
                registration.CampId, registration.AccessCode);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error registering for camp");
            return false;
        }
    }

    public async Task<List<Registration>> GetUserRegistrationsAsync(string userId)
    {
        try
        {
            return await _context.Registrations
                .Include(r => r.Camp)
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
            return await _context.Registrations
                .Include(r => r.Camp)
                .FirstOrDefaultAsync(r => r.AccessCode == accessCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registration by access code");
            return null;
        }
    }

    private static bool MatchesAgeGroup(string campAge, string filterAge)
    {
        return filterAge switch
        {
            "6-9" => campAge.Contains("6") || campAge.Contains("7") || campAge.Contains("8") || campAge.Contains("9"),
            "10-13" => campAge.Contains("10") || campAge.Contains("11") || campAge.Contains("12") || campAge.Contains("13"),
            "14-17" => campAge.Contains("14") || campAge.Contains("15") || campAge.Contains("16") || campAge.Contains("17"),
            _ => true
        };
    }

    private async Task<string> GenerateUniqueAccessCodeAsync()
    {
        string accessCode;
        bool isUnique;

        do
        {
            accessCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            isUnique = !await _context.Registrations
                .AnyAsync(r => r.AccessCode == accessCode);
        } while (!isUnique);

        return accessCode;
    }
}