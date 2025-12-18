using payment_service.Models.Payment;
using Stripe;

namespace payment_service.Interfaces;

public interface IPaymentService
{
    Task<List<Payment>> GetPaymentsAsync(PaymentFilter filter);
    Task<Payment?> GetPaymentByIdAsync(int paymentId);
    Task<PaymentIntent?> InsertPaymentAsync(PaymentCreateRequestDTO dto);
    Task<int?> UpdatePaymentAsync(int paymentId, PaymentUpdateRequestDTO dto);
    Task<int?> DeletePaymentByIdAsync(int paymentId);
    Task<bool> ConfirmPaymentAsync(int paymentId);
    Task<bool> CancelPaymentAsync(int paymentId);
}
