using payment_service.Models.Stripe;
using Stripe;

namespace payment_service.Interfaces;

public interface IStripeIntegrationService
{
    Task<PaymentIntent> CreatePaymentIntentAsync(StripeCreatePaymentDTO stripeCreatePaymentDTO);
    Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId);
    Task<PaymentIntent> ConfirmPaymentIntentAsync(string paymentIntentId);
    Task<PaymentIntent> CancelPaymentIntentAsync(string paymentIntentId);
}
