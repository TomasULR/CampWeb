using CampWeb.Data;
using CampWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace CampWeb.Services;

public interface IPaymentService
{
    // OPRAVENO: Přidané return typy Task<PaymentResult>
    Task<PaymentResult> ProcessGooglePayPaymentAsync(int registrationId, string paymentToken, decimal amount);
    Task<PaymentResult> ProcessBankTransferAsync(int registrationId, string transactionId);
}

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentService> _logger;
    private readonly IConfiguration _configuration;

    public PaymentService(
        ApplicationDbContext context,
        ILogger<PaymentService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<PaymentResult> ProcessGooglePayPaymentAsync(int registrationId, string paymentToken, decimal amount)
    {
        try
        {
            _logger.LogInformation("Processing Google Pay payment for registration {RegistrationId}, amount {Amount}",
                registrationId, amount);

            var registration = await _context.Registrations
                .Include(r => r.Camp)
                .FirstOrDefaultAsync(r => r.Id == registrationId);

            if (registration == null)
            {
                return PaymentResult.Failed("Registrace nenalezena.");
            }

            if (registration.Status == RegistrationStatus.Paid)
            {
                return PaymentResult.Failed("Registrace je již zaplacena.");
            }

            var paymentProcessed = await ProcessPaymentWithProvider(paymentToken, amount, registration);

            if (paymentProcessed)
            {
                var payment = new Payment
                {
                    RegistrationId = registrationId,
                    Amount = amount,
                    Currency = "CZK",
                    PaymentMethod = "GooglePay",
                    PaymentToken = paymentToken,
                    Status = PaymentStatus.Completed,
                    ProcessedAt = DateTime.UtcNow,
                    TransactionId = GenerateTransactionId()
                };

                _context.Payments.Add(payment);
                registration.Status = RegistrationStatus.Paid;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment completed successfully for registration {RegistrationId}, transaction {TransactionId}",
                    registrationId, payment.TransactionId);

                return PaymentResult.Success(payment.TransactionId);
            }
            else
            {
                return PaymentResult.Failed("Platba byla odmítnuta platebním poskytovatelem.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Google Pay payment for registration {RegistrationId}", registrationId);
            return PaymentResult.Failed("Došlo k chybě při zpracování platby.");
        }
    }

    public async Task<PaymentResult> ProcessBankTransferAsync(int registrationId, string transactionId)
    {
        try
        {
            var registration = await _context.Registrations
                .Include(r => r.Camp)
                .FirstOrDefaultAsync(r => r.Id == registrationId);

            if (registration == null)
            {
                return PaymentResult.Failed("Registrace nenalezena.");
            }

            var payment = new Payment
            {
                RegistrationId = registrationId,
                Amount = registration.Camp.Price,
                Currency = "CZK",
                PaymentMethod = "BankTransfer",
                Status = PaymentStatus.Pending,
                TransactionId = transactionId,
                ProcessedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            registration.Status = RegistrationStatus.Confirmed;
            await _context.SaveChangesAsync();

            return PaymentResult.Success(transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bank transfer for registration {RegistrationId}", registrationId);
            return PaymentResult.Failed("Došlo k chybě při zpracování převodu.");
        }
    }

    private async Task<bool> ProcessPaymentWithProvider(string paymentToken, decimal amount, Registration registration)
    {
        await Task.Delay(2000);
        var random = new Random();
        return random.Next(1, 101) <= 95;
    }

    private string GenerateTransactionId()
    {
        return $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }
}

public class PaymentResult
{
    public bool IsSuccess { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }

    public static PaymentResult Success(string transactionId) => new()
    {
        IsSuccess = true,
        TransactionId = transactionId
    };

    public static PaymentResult Failed(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}
