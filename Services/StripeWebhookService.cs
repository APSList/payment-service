using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using payment_service.Database;
using payment_service.Interfaces;
using payment_service.Options;
using Stripe;

namespace payment_service.Services;

public static class StripeEventTypes
{
    public const string CheckoutSessionCompleted = "checkout.session.completed";
    public const string PaymentIntentPaymentFailed = "payment_intent.payment_failed";
}

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
        var stripeEvent = EventUtility.ConstructEvent(
            json,
            signature,
            _stripeOptions.WebhookSecret
        );

        switch (stripeEvent.Type)
        {
            case StripeEventTypes.CheckoutSessionCompleted:
                await HandleCheckoutSessionCompleted(stripeEvent);
                break;

            case StripeEventTypes.PaymentIntentPaymentFailed:
                await HandlePaymentFailed(stripeEvent);
                break;
            default:
                // TODO: log ignored event
                break;
        }
    }

    private async Task HandlePaymentFailed(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as Stripe.PaymentIntent;
        if (paymentIntent == null)
            return;
        var payment = await _context.Payments
            .FirstOrDefaultAsync(x => x.PaymentIntentId == paymentIntent.Id);
        if (payment == null)
            return;
        payment.Status = "failed";
        payment.UpdatedAt = DateTime.UtcNow;
        payment.UpdatedBy = "Stripe webhook";
        await _context.SaveChangesAsync();
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null)
            return;

        var paymentIntentId = session.PaymentIntentId;

        var payment = await _context.Payments
            .FirstOrDefaultAsync(x => x.PaymentIntentId == paymentIntentId);

        if (payment == null)
            return;

        payment.Status = "paid";
        payment.UpdatedAt = DateTime.UtcNow;
        payment.UpdatedBy = "Stripe webhook";

        await _context.SaveChangesAsync();
    }


}
