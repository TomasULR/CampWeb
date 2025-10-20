using CampWeb.Models;
using CampWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using System.Text;

namespace CampWeb.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody);
    Task<bool> SendRegistrationConfirmationAsync(Registration registration, Camp camp);
    Task<bool> SendCampUpdateNotificationAsync(string email, string childName, string campName, string updateTitle);
    Task<bool> SendPhotoGalleryAccessAsync(Registration registration, Camp camp);
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

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var smtpServer = _configuration["Email:SmtpServer"];
            var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 587);
            var smtpUsername = _configuration["Email:Username"];
            var smtpPassword = _configuration["Email:Password"];
            var fromEmail = _configuration["Email:FromEmail"] ?? "info@letnitabory.cz";
            var fromName = _configuration["Email:FromName"] ?? "Letní Tábory Plzeň";

            // If SMTP not configured, log and return
            if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUsername))
            {
                _logger.LogWarning("SMTP not configured. Email would be sent to: {To}", to);
                _logger.LogInformation("Subject: {Subject}", subject);
                _logger.LogInformation("Body preview: {Body}", htmlBody.Substring(0, Math.Min(200, htmlBody.Length)));
                return true; // Return true in development mode
            }

            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail, fromName);
            message.To.Add(new MailAddress(to));
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;

            using var client = new SmtpClient(smtpServer, smtpPort);
            client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
            client.EnableSsl = true;

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendRegistrationConfirmationAsync(Registration registration, Camp camp)
    {
        var subject = $"Potvrzení registrace - {camp.Name}";
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost";
        var photoGalleryUrl = $"{baseUrl}/fotky/{registration.AccessCode}";

        var htmlBody = GetRegistrationConfirmationTemplate(
            registration.ParentName,
            registration.ChildFullName,
            camp,
            registration.AccessCode,
            photoGalleryUrl
        );

        return await SendEmailAsync(registration.ParentEmail, subject, htmlBody);
    }

    public async Task<bool> SendPhotoGalleryAccessAsync(Registration registration, Camp camp)
    {
        var subject = $"Přístup k fotogalerii z tábora - {camp.Name}";
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost";
        var photoGalleryUrl = $"{baseUrl}/fotky/{registration.AccessCode}";

        var htmlBody = GetPhotoGalleryAccessTemplate(
            registration.ParentName,
            registration.ChildFullName,
            camp,
            registration.AccessCode,
            photoGalleryUrl
        );

        return await SendEmailAsync(registration.ParentEmail, subject, htmlBody);
    }

    public async Task<bool> SendCampUpdateNotificationAsync(string email, string childName, string campName, string updateTitle)
    {
        var subject = $"Nová aktualizace z tábora - {campName}";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; }}
        .footer {{ background: #f5f5f5; padding: 20px; text-align: center; border-radius: 0 0 10px 10px; font-size: 12px; color: #666; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🏕️ Nová aktualizace z tábora</h1>
        </div>
        <div class='content'>
            <p>Dobrý den,</p>
            <p>Pro tábor <strong>{campName}</strong> byla přidána nová aktualizace:</p>
            <h3 style='color: #667eea;'>{updateTitle}</h3>
            <p>Pro více informací a fotky navštivte naše stránky pomocí vašeho přístupového kódu.</p>
        </div>
        <div class='footer'>
            <p>S pozdravem,<br><strong>Tým Letních táborů Plzeň</strong></p>
        </div>
    </div>
</body>
</html>
";

        return await SendEmailAsync(email, subject, htmlBody);
    }

    private string GetRegistrationConfirmationTemplate(
        string parentName,
        string childName,
        Camp camp,
        string accessCode,
        string photoGalleryUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; }}
        .info-box {{ background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .access-code {{ background: #667eea; color: white; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; border-radius: 5px; letter-spacing: 3px; margin: 20px 0; }}
        .footer {{ background: #f5f5f5; padding: 20px; text-align: center; border-radius: 0 0 10px 10px; font-size: 12px; color: #666; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .detail-row {{ padding: 8px 0; border-bottom: 1px solid #e0e0e0; }}
        .detail-label {{ font-weight: bold; color: #667eea; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Děkujeme za registraci!</h1>
            <p>Těšíme se na vaše dítě v našem táboře</p>
        </div>
        <div class='content'>
            <p>Dobrý den, <strong>{parentName}</strong>,</p>
            
            <p>Vaše registrace byla úspěšně přijata! Dítě <strong>{childName}</strong> je zaregistrováno na tábor:</p>
            
            <div class='info-box'>
                <h3 style='color: #667eea; margin-top: 0;'>📋 Informace o táboře</h3>
                <div class='detail-row'>
                    <span class='detail-label'>Název tábora:</span> {camp.Name}
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Místo konání:</span> {camp.Location}
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Termín:</span> {camp.StartDate:dd.MM.yyyy} - {camp.EndDate:dd.MM.yyyy}
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Cena:</span> {camp.Price:N0} Kč
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Věková skupina:</span> {camp.AgeGroup}
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Typ tábora:</span> {camp.Type}
                </div>
            </div>

            <h3 style='color: #667eea;'>🔑 Váš přístupový kód</h3>
            <p>Tento kód použijete pro přístup k fotogalerii a aktualizacím z tábora:</p>
            <div class='access-code'>{accessCode}</div>

            <div style='text-align: center;'>
                <a href='{photoGalleryUrl}' class='button'>📸 Zobrazit fotogalerii</a>
            </div>

            <div class='info-box'>
                <h4 style='color: #667eea; margin-top: 0;'>ℹ️ Co dál?</h4>
                <ul style='margin: 0; padding-left: 20px;'>
                    <li>Uschovejte si tento přístupový kód - budete ho potřebovat pro přístup k fotkám</li>
                    <li>Během tábora budeme pravidelně přidávat fotky a aktualizace</li>
                    <li>Dostanete email s upozorněním na každou novou aktualizaci</li>
                    <li>V případě dotazů nás neváhejte kontaktovat</li>
                </ul>
            </div>

            <h3 style='color: #667eea;'>📝 Důležité informace</h3>
            <p><strong>Co vzít s sebou:</strong></p>
            <ul>
                <li>Hygienické potřeby</li>
                <li>Dostatek oblečení na celý týden</li>
                <li>Pláštěnku nebo bundu do deště</li>
                <li>Sportovní obuv a sandály</li>
                <li>Plavky a ručník</li>
                <li>Ochranný krém proti slunci</li>
            </ul>

            <p><strong>Začátek tábora:</strong> {camp.StartDate:dd.MM.yyyy} v 9:00 na místě {camp.Location}</p>
            <p><strong>Konec tábora:</strong> {camp.EndDate:dd.MM.yyyy} ve 16:00</p>

            <p style='margin-top: 30px;'>Těšíme se na skvělý týden plný zábavy a dobrodružství! 🌟</p>
        </div>
        <div class='footer'>
            <p>S pozdravem,<br><strong>Tým Letních táborů Plzeň</strong></p>
            <p style='margin-top: 10px; font-size: 11px;'>
                📧 info@letnitabory.cz | 📞 +420 123 456 789<br>
                🌐 www.letnitabory.cz
            </p>
        </div>
    </div>
</body>
</html>
";
    }

    private string GetPhotoGalleryAccessTemplate(
        string parentName,
        string childName,
        Camp camp,
        string accessCode,
        string photoGalleryUrl)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; }}
        .access-code {{ background: #667eea; color: white; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; border-radius: 5px; letter-spacing: 3px; margin: 20px 0; }}
        .footer {{ background: #f5f5f5; padding: 20px; text-align: center; border-radius: 0 0 10px 10px; font-size: 12px; color: #666; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .highlight-box {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📸 Přístup k fotogalerii</h1>
            <p>{camp.Name}</p>
        </div>
        <div class='content'>
            <p>Dobrý den, <strong>{parentName}</strong>,</p>
            
            <p>Děkujeme, že jste si pro své dítě <strong>{childName}</strong> vybrali náš tábor <strong>{camp.Name}</strong>!</p>
            
            <p>Během tábora budeme pravidelně přidávat fotografie a aktualizace, abyste byli stále v obraze o tom, jak se vašemu dítěti daří.</p>

            <h3 style='color: #667eea;'>🔑 Váš přístupový kód</h3>
            <div class='access-code'>{accessCode}</div>

            <div class='highlight-box'>
                <p style='margin: 0;'><strong>⚠️ Důležité:</strong> Tento kód uschovejte a nikomu ho nesdělujte. S jeho pomocí získáte přístup k fotkám z tábora.</p>
            </div>

            <div style='text-align: center;'>
                <a href='{photoGalleryUrl}' class='button'>📸 Zobrazit fotogalerii</a>
            </div>

            <h3 style='color: #667eea;'>ℹ️ Jak to funguje?</h3>
            <ul>
                <li>Klikněte na tlačítko výše nebo navštivte adresu: <strong>{photoGalleryUrl}</strong></li>
                <li>Zadejte svůj přístupový kód</li>
                <li>Prohlédněte si všechny fotky z tábora</li>
                <li>Fotky si můžete stáhnout nebo sdílet s rodinou</li>
            </ul>

            <h3 style='color: #667eea;'>📅 Informace o táboře</h3>
            <p><strong>Termín:</strong> {camp.StartDate:dd.MM.yyyy} - {camp.EndDate:dd.MM.yyyy}</p>
            <p><strong>Místo:</strong> {camp.Location}</p>
            
            <p style='margin-top: 30px;'>Budeme rádi, když budete sledovat aktualizace z tábora a uvidíte, jak skvělý čas vaše dítě prožívá! 🎉</p>
        </div>
        <div class='footer'>
            <p>S pozdravem,<br><strong>Tým Letních táborů Plzeň</strong></p>
            <p style='margin-top: 10px; font-size: 11px;'>
                📧 info@letnitabory.cz | 📞 +420 123 456 789<br>
                🌐 www.letnitabory.cz
            </p>
        </div>
    </div>
</body>
</html>
";
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
