using payment_service.Models.Payment;
using Stripe;

namespace payment_service.Interfaces;

public interface IPaymentService
{
    // CRUD
    Task<List<Payment>> GetPaymentsAsync(PaymentFilter filter);
    Task<Payment?> GetPaymentByIdAsync(int paymentId);
    Task<string> InsertPaymentAsync(PaymentCreateRequestDTO dto);
    Task<int?> UpdatePaymentAsync(int paymentId, PaymentUpdateRequestDTO dto);
    Task<int?> DeletePaymentByIdAsync(int paymentId);

    // Ročno upravljanje plačil
    Task<bool> ConfirmPaymentAsync(int paymentId);
    Task<bool> CancelPaymentAsync(int paymentId);

    // Poročanje o payment eventih naprej
    Task SendPaymentActionToBooking(Payment payment, PaymentIntent? intent);
}
