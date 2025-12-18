using payment_service.Models.PaymentConfirmation;

namespace payment_service.Interfaces;

public interface IPaymentConfirmationService
{
    Task<PaymentConfirmation?> GetByIdAsync(int id);
    Task<PaymentConfirmation> GenerateAsync(int? paymentId, int? organizationId, int? customerId, decimal? amount);
    Task<byte[]> DownloadAsync(int confirmationId);
}
