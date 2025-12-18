using Microsoft.Extensions.Options;
using payment_service.Interfaces;
using payment_service.Models.Stripe;
using payment_service.Options;
using Stripe;

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
}
