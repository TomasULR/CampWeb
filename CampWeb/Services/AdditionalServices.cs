using CampWeb.Models;
using CampWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;

namespace CampWeb.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendRegistrationConfirmationAsync(string email, string childName, string campName, string accessCode);
    Task SendCampUpdateNotificationAsync(string email, string childName, string campName, string updateTitle);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            // V development módu pouze logujeme
            _logger.LogInformation("Email would be sent to: {To}", to);
            _logger.LogInformation("Subject: {Subject}", subject);
            _logger.LogInformation("Body: {Body}", body);
            
            // V produkci implementujte skutečné odesílání emailů
            await Task.Delay(100); // Simulace
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }

    public async Task SendRegistrationConfirmationAsync(string email, string childName, string campName, string accessCode)
    {
        var subject = $"Potvrzení registrace do tábora - {campName}";
        var body = $@"
            <h2>Potvrzení registrace</h2>
            <p>Dobrý den,</p>
            <p>Vaše dítě <strong>{childName}</strong> bylo úspěšně zaregistrováno do tábora <strong>{campName}</strong>.</p>
            <p><strong>Přístupový kód:</strong> {accessCode}</p>
            <p>Tento kód můžete použít pro přístup k fotkám a aktuálním informacím z tábora.</p>
            <p>S pozdravem,<br>Tým Letních táborů Plzeň</p>
        ";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendCampUpdateNotificationAsync(string email, string childName, string campName, string updateTitle)
    {
        var subject = $"Nová aktualizace z tábora - {campName}";
        var body = $@"
            <h2>Nová aktualizace z tábora</h2>
            <p>Dobrý den,</p>
            <p>Pro váš tábor <strong>{campName}</strong> byla přidána nová aktualizace: <strong>{updateTitle}</strong></p>
            <p>Podrobnosti najdete na našich stránkách.</p>
            <p>S pozdravem,<br>Tým Letních táborů Plzeň</p>
        ";

        await SendEmailAsync(email, subject, body);
    }
}

public interface IRegistrationService
{
    Task<string> CreateRegistrationAsync(Registration registration);
    Task<Registration?> GetRegistrationByAccessCodeAsync(string accessCode);
    Task<bool> UpdateRegistrationStatusAsync(string accessCode, RegistrationStatus status);
}

public class RegistrationService : IRegistrationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(ApplicationDbContext context, ILogger<RegistrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> CreateRegistrationAsync(Registration registration)
    {
        try
        {
            registration.AccessCode = GenerateAccessCode();
            registration.RegistrationDate = DateTime.UtcNow;
            registration.Status = RegistrationStatus.Pending;

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            return registration.AccessCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating registration");
            throw;
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

    public async Task<bool> UpdateRegistrationStatusAsync(string accessCode, RegistrationStatus status)
    {
        try
        {
            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.AccessCode == accessCode);

            if (registration != null)
            {
                registration.Status = status;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating registration status");
            return false;
        }
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
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PhotoService> _logger;

    public PhotoService(ApplicationDbContext context, ILogger<PhotoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<CampPhoto>> GetCampPhotosAsync(int campId)
    {
        try
        {
            return await _context.CampPhotos
                .Where(p => p.CampId == campId && p.IsPublic)
                .OrderByDescending(p => p.UploadDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting camp photos for camp {CampId}", campId);
            return new List<CampPhoto>();
        }
    }

    public async Task<List<LiveUpdate>> GetLiveUpdatesAsync(int campId)
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
            _logger.LogError(ex, "Error getting live updates for camp {CampId}", campId);
            return new List<LiveUpdate>();
        }
    }

    public async Task<bool> ValidateAccessCodeAsync(string accessCode)
    {
        try
        {
            return await _context.Registrations
                .AnyAsync(r => r.AccessCode == accessCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating access code");
            return false;
        }
    }
}