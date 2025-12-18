using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using payment_service.Database;
using payment_service.Interfaces;
using payment_service.Options;
using Stripe;

namespace payment_service.Services;

public class StripeWebhookService: IStripeWebhookService
{
    private readonly PaymentDbContext _context;
    private readonly StripeOptions _stripeOptions;
    public StripeWebhookService(PaymentDbContext context, IOptions<StripeOptions> stripeOptions)
    {
        _context = context;
        _stripeOptions = stripeOptions.Value;
    }

    public async Task ProcessEventAsync(string json, string signature)
    {
        var stripeEvent = EventUtility.ConstructEvent(json, signature, _stripeOptions.ApiKey);

        var paymentIntent = stripeEvent?.Data?.Object as PaymentIntent;

        //TODO: Add logging 
        if(paymentIntent is null)
        {
            return;
        }

        //Update payment with status
        
        var payment = await _context.Payments.FirstOrDefaultAsync(x => x.PaymentIntentId == paymentIntent.Id);
        if (payment == null)
        {
            return;
        }

        payment.Status = paymentIntent.Status;
        payment.UpdatedAt = DateTime.UtcNow;
        payment.UpdatedBy = "Stripe webhook";
        await _context.SaveChangesAsync();

    }
}
