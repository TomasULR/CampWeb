using CampWeb.Models;
using CampWeb.Data;
using Microsoft.EntityFrameworkCore;

namespace CampWeb.Services;

public interface ITimelineService
{
    Task<List<LiveUpdate>> GetCampUpdatesAsync(int campId);
    Task<List<LiveUpdate>> GetUpdatesByAccessCodeAsync(string accessCode);
    Task<LiveUpdate?> CreateUpdateAsync(LiveUpdate update);
    Task<bool> DeleteUpdateAsync(int updateId);
    Task<CampPhoto?> UploadPhotoAsync(int campId, string fileName, string? description);
    Task<List<CampPhoto>> GetPhotosByAccessCodeAsync(string accessCode);
    Task<bool> ValidateAccessCodeAsync(string accessCode);
    Task<string?> GetCampNameByAccessCodeAsync(string accessCode);
}

public class TimelineService : ITimelineService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TimelineService> _logger;

    public TimelineService(ApplicationDbContext context, ILogger<TimelineService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<LiveUpdate>> GetCampUpdatesAsync(int campId)
    {
        try
        {
            return await _context.LiveUpdates
                .Where(u => u.CampId == campId)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting updates for camp {CampId}", campId);
            return new List<LiveUpdate>();
        }
    }
    
    public async Task<LiveUpdate?> CreateUpdateAsync(LiveUpdate update)
    {
        try
        {
            update.CreatedAt = DateTime.UtcNow;
            _context.LiveUpdates.Add(update);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created new update for camp {CampId}: {Title}", 
                update.CampId, update.Title);
            
            return update;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating update");
            return null;
        }
    }

    public async Task<bool> DeleteUpdateAsync(int updateId)
    {
        try
        {
            var update = await _context.LiveUpdates.FindAsync(updateId);
            if (update == null)
                return false;

            _context.LiveUpdates.Remove(update);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Deleted update {UpdateId}", updateId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting update {UpdateId}", updateId);
            return false;
        }
    }

    public async Task<CampPhoto?> UploadPhotoAsync(int campId, string fileName, string? description)
    {
        try
        {
            var photo = new CampPhoto
            {
                CampId = campId,
                FileName = fileName,
                Description = description ?? "",
                UploadDate = DateTime.UtcNow,
                IsPublic = true
            };

            _context.CampPhotos.Add(photo);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Uploaded photo for camp {CampId}: {FileName}", 
                campId, fileName);
            
            return photo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading photo");
            return null;
        }
    }

    public async Task<List<CampPhoto>> GetPhotosByAccessCodeAsync(string accessCode)
    {
        try
        {
            int? campId = null;

            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.AccessCode == accessCode);

            if (registration != null)
            {
                campId = registration.CampId;
            }
            else
            {
                var camp = await _context.Camps
                    .FirstOrDefaultAsync(c => c.AccessCode == accessCode);
                if (camp != null) campId = camp.Id;
            }

            if (campId == null)
            {
                _logger.LogWarning("No registration or camp found for access code {AccessCode}", accessCode);
                return new List<CampPhoto>();
            }

            return await _context.CampPhotos
                .Include(p => p.Camp)
                .Where(p => p.CampId == campId && p.IsPublic)
                .OrderByDescending(p => p.UploadDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting photos by access code");
            return new List<CampPhoto>();
        }
    }

    public async Task<List<LiveUpdate>> GetUpdatesByAccessCodeAsync(string accessCode)
    {
        try
        {
            int? campId = null;

            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.AccessCode == accessCode);

            if (registration != null)
            {
                campId = registration.CampId;
            }
            else
            {
                var camp = await _context.Camps
                    .FirstOrDefaultAsync(c => c.AccessCode == accessCode);
                if (camp != null) campId = camp.Id;
            }

            if (campId == null)
            {
                _logger.LogWarning("No registration or camp found for access code {AccessCode}", accessCode);
                return new List<LiveUpdate>();
            }

            return await _context.LiveUpdates
                .Include(u => u.Camp)
                .Where(u => u.CampId == campId)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting updates by access code");
            return new List<LiveUpdate>();
        }
    }

    public async Task<bool> ValidateAccessCodeAsync(string accessCode)
    {
        try
        {
            // Check if access code exists in Registrations
            var registrationExists = await _context.Registrations
                .AnyAsync(r => r.AccessCode == accessCode);
            
            if (registrationExists) return true;

            // Check if access code exists in Camps
            var campExists = await _context.Camps
                .AnyAsync(c => c.AccessCode == accessCode);
            
            return campExists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating access code");
            return false;
        }
    }

    public async Task<string?> GetCampNameByAccessCodeAsync(string accessCode)
    {
        try
        {
            // First check registrations
            var registration = await _context.Registrations
                .Include(r => r.Camp)
                .FirstOrDefaultAsync(r => r.AccessCode == accessCode);
            
            if (registration?.Camp != null)
                return registration.Camp.Name;

            // Then check camps directly
            var camp = await _context.Camps
                .FirstOrDefaultAsync(c => c.AccessCode == accessCode);
            
            return camp?.Name;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting camp name by access code");
            return null;
        }
    }
}