using Microsoft.Extensions.Options;
using payment_service.Interfaces;
using payment_service.Models.Payment;
using payment_service.Models.Stripe;
using payment_service.Options;
using Stripe;
using Stripe.Checkout;

public class StripeIntegrationService : IStripeIntegrationService
{
    private readonly StripeOptions _stripeOptions;
    private readonly PaymentIntentService _paymentIntentService;

    public StripeIntegrationService(IOptions<StripeOptions> stripeOptions)
    {
        _stripeOptions = stripeOptions.Value;
        StripeConfiguration.ApiKey = _stripeOptions.ApiKey;
        _paymentIntentService = new PaymentIntentService();
    }

    public async Task<PaymentIntent> CreatePaymentIntentAsync(StripeCreatePaymentDTO stripeCreatePaymentDTO)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(stripeCreatePaymentDTO.Amount * 100), // Stripe uporablja cent-e
            Currency = "eur",
            PaymentMethodTypes = ["card"],
            Description = stripeCreatePaymentDTO.Description
        };

        return await _paymentIntentService.CreateAsync(options);
    }

    public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
    {
        return await _paymentIntentService.GetAsync(paymentIntentId);
    }

    public async Task<PaymentIntent> ConfirmPaymentIntentAsync(string paymentIntentId)
    {
        return await _paymentIntentService.ConfirmAsync(paymentIntentId);
    }

    public async Task<PaymentIntent> CancelPaymentIntentAsync(string paymentIntentId)
    {
        return await _paymentIntentService.CancelAsync(paymentIntentId);
    }

    public async Task<Session> CreateCheckoutSessionAsync(PaymentCreateRequestDTO dto)
    {
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = "https://hostflow.software/ui/payment-success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = "https://hostflow.software/ui/payment-cancel",
            Expand = new List<string> { "payment_intent" },
            LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
        {
            new()
            {
                Quantity = 1,
                PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                {
                    Currency = "eur",
                    UnitAmount = (long)(dto.Amount!.Value * 100),
                    ProductData = new()
                    {
                        Name = "Reservation payment"
                    }
                }
            }
        },
            Metadata = new Dictionary<string, string>
            {
                ["reservationId"] = dto.ReservationId!.Value.ToString(),
                ["organizationId"] = dto.OrganizationId!.Value.ToString()
            }
        };

        var service = new Stripe.Checkout.SessionService();
        return await service.CreateAsync(options);
    }

}
