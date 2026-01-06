using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using payment_service.Database;
using payment_service.Helpers;
using payment_service.Interfaces;
using payment_service.Options;
using Serilog;
using Stripe;

namespace payment_service.Services;

public static class StripeEventTypes
{
    public const string CheckoutSessionCompleted = "checkout.session.completed";
    public const string PaymentIntentPaymentFailed = "payment_intent.payment_failed";
}

public class StripeWebhookService : IStripeWebhookService
{
    private readonly PaymentDbContext _context;
    private readonly StripeOptions _stripeOptions;
    private readonly IPaymentService _paymentService;
    private readonly IStripeIntegrationService _stripeIntegrationService;
    private readonly IPaymentConfirmationService _paymentConfirmationService;

    public StripeWebhookService(
        PaymentDbContext context,
        IOptions<StripeOptions> stripeOptions,
        IPaymentService paymentService,
        IStripeIntegrationService stripeIntegrationService,
        IPaymentConfirmationService paymentConfirmationService)
    {
        _context = context;
        _stripeOptions = stripeOptions.Value;
        _paymentService = paymentService;
        _stripeIntegrationService = stripeIntegrationService;
        _paymentConfirmationService = paymentConfirmationService;
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
                break;
        }
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;

        // 1. ISKANJE PO SESSION ID
        var payment = await _context.Payments
            .FirstOrDefaultAsync(x => x.SessionId == session.Id);

        if (payment == null)
        {
            Log.Error("StripeWebhookService.HandleCheckoutSessionCompleted; No payment found for SessionId {SessionId}", session.Id);
            return;
        }

        var paymentIntent = await _stripeIntegrationService.GetPaymentIntentAsync(session.PaymentIntentId);

        if (paymentIntent == null)
        {
            Log.Error("StripeWebhookService.HandleCheckoutSessionCompleted; No paymentIntent found for PaymentIntentId {PaymentIntentId}", session.PaymentIntentId);
            return;
        }

        // 2. POSODOBITEV
        payment.PaymentIntentId = session.PaymentIntentId;
        payment.Status = paymentIntent.Status;
        payment.PaymentMethod = "card"; //zaenkrat hard coded
        payment.UpdatedAt = DateTime.UtcNow;
        payment.UpdatedBy = "STRIPE WEBHOOK";

        await _context.SaveChangesAsync();

        // 3. GENERIRANJE POTRDIL
        await _paymentConfirmationService.GenerateAsync(payment.Id, payment.OrganizationId, payment.CustomerId, payment.Amount);

        // 4. OBVESTILO O PAYMENT AKCIJI    
        await _paymentService.SendPaymentActionToBooking(payment, paymentIntent);
    }

    private async Task HandlePaymentFailed(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null)
        {
            Log.Error("StripeWebhookService.HandlePaymentFailed; No paymentIntent found for PaymentIntentId {Id}", paymentIntent.Id);
        }

        var payment = await _context.Payments.FirstOrDefaultAsync(x => x.PaymentIntentId == paymentIntent.Id);


        if (payment == null)
        {
            Log.Error("StripeWebhookService.HandlePaymentFailed; No payment found for PaymentIntentId {Id}", paymentIntent.Id);
            return;
        }

        payment.Status = paymentIntent.Status;
        payment.UpdatedAt = DateTime.UtcNow;
        payment.UpdatedBy = "STRIPE WEBHOOK";

        await _context.SaveChangesAsync();
        await _paymentService.SendPaymentActionToBooking(payment, paymentIntent);
    }
}