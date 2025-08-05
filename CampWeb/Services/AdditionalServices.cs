using CampWeb.Models;

namespace CampWeb.Services;

public interface IRegistrationService
{
    Task<string> CreateRegistrationAsync(Registration registration);
    Task<Registration?> GetRegistrationByAccessCodeAsync(string accessCode);
    Task<bool> UpdateRegistrationStatusAsync(string accessCode, RegistrationStatus status);
}

public class RegistrationService : IRegistrationService
{
    private readonly List<Registration> _registrations = new();

    public Task<string> CreateRegistrationAsync(Registration registration)
    {
        registration.Id = _registrations.Count + 1;
        registration.AccessCode = GenerateAccessCode();
        registration.RegistrationDate = DateTime.Now;
        registration.Status = RegistrationStatus.Pending;
        
        _registrations.Add(registration);
        
        return Task.FromResult(registration.AccessCode);
    }

    public Task<Registration?> GetRegistrationByAccessCodeAsync(string accessCode)
    {
        var registration = _registrations.FirstOrDefault(r => r.AccessCode == accessCode);
        return Task.FromResult(registration);
    }

    public Task<bool> UpdateRegistrationStatusAsync(string accessCode, RegistrationStatus status)
    {
        var registration = _registrations.FirstOrDefault(r => r.AccessCode == accessCode);
        if (registration != null)
        {
            registration.Status = status;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    private static string GenerateAccessCode()
    {
        return Guid.NewGuid().ToString("N")[..8].ToUpper();
    }
}

public interface IPhotoService
{
    Task<List<CampPhoto>> GetCampPhotosAsync(int campId);
    Task<List<LiveUpdate>> GetLiveUpdatesAsync(int campId);
    Task<bool> ValidateAccessCodeAsync(string accessCode);
}

public class PhotoService : IPhotoService
{
    public Task<List<CampPhoto>> GetCampPhotosAsync(int campId)
    {
        // Sample photos - in real app, this would come from database/storage
        var photos = new List<CampPhoto>
        {
            new() 
            { 
                Id = 1,
                CampId = campId,
                FileName = "https://images.unsplash.com/photo-1504851149312-7a075b496cc7?w=800", 
                Description = "Ráno v táboře", 
                UploadDate = DateTime.Now.AddDays(-2),
                IsPublic = true
            },
            new() 
            { 
                Id = 2,
                CampId = campId,
                FileName = "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800", 
                Description = "Turistika v lese", 
                UploadDate = DateTime.Now.AddDays(-2),
                IsPublic = true
            }
        };

        return Task.FromResult(photos);
    }

    public Task<List<LiveUpdate>> GetLiveUpdatesAsync(int campId)
    {
        var updates = new List<LiveUpdate>
        {
            new() 
            { 
                Id = 1,
                CampId = campId,
                Title = "Výlet na Šumavu", 
                Content = "Dnes jsme vyrazili na krásný výlet do přírody. Počasí nám přálo!",
                CreatedAt = DateTime.Now.AddHours(-3),
                PhotoUrl = "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=400"
            },
            new() 
            { 
                Id = 2,
                CampId = campId,
                Title = "Táborák", 
                Content = "Večer jsme si rozdělali táborák a zpívali jsme písničky.",
                CreatedAt = DateTime.Now.AddHours(-5),
                PhotoUrl = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=400"
            }
        };

        return Task.FromResult(updates);
    }

    public Task<bool> ValidateAccessCodeAsync(string accessCode)
    {
        // Simple validation - in real app, check against database
        return Task.FromResult(!string.IsNullOrEmpty(accessCode) && accessCode.Length == 8);
    }
}