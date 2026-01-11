using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using payment_service.Database;
using payment_service.Helpers;
using payment_service.Interfaces;
using payment_service.Models.Kafka;
using payment_service.Models.Payment;
using payment_service.Options;
using Serilog;
using Stripe;

namespace payment_service.Services;

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;
    private readonly IStripeIntegrationService _stripeService;
    private readonly IPaymentConfirmationService _paymentConfirmationService;
    private readonly IKafkaProducer _kafkaProducerService;
    private readonly KafkaOptions _kafkaOptions;
    private readonly IOrganizationContext _org;

    public PaymentService(
        PaymentDbContext context,
        IStripeIntegrationService stripeService,
        IPaymentConfirmationService paymentConfirmationService,
        IOptions<KafkaOptions> kafkaOptions,
        IKafkaProducer kafkaProducerService,
        IOrganizationContext orgContext)
    {
        _context = context;
        _stripeService = stripeService;
        _paymentConfirmationService = paymentConfirmationService;
        _kafkaOptions = kafkaOptions.Value;
        _kafkaProducerService = kafkaProducerService;
        _org = orgContext;
    }

    public async Task<List<Payment>> GetPaymentsAsync(PaymentFilter filter)
    {
        var orgId = _org.OrganizationId;

        var q = _context.Payments
            .AsNoTracking()
            .Where(p => p.OrganizationId == orgId);

        return await q.ToListAsync();
    }

    public async Task<Payment?> GetPaymentByIdAsync(int paymentId)
    {
        var orgId = _org.OrganizationId;

        return await _context.Payments
            .AsNoTracking()
            .Where(p => p.OrganizationId == orgId)
            .FirstOrDefaultAsync(p => p.Id == paymentId);
    }

    public async Task<string> InsertPaymentAsync(PaymentCreateRequestDTO dto)
    {
        if (dto.CustomerId is null || dto.OrganizationId is null || dto.ReservationId is null)
        {
            Log.Warning("PaymentService.InsertPaymentAsync; Validation failed all fields are required");
            return string.Empty;
        }

        try
        {
            var session = await _stripeService.CreateCheckoutSessionAsync(dto);

            if (session == null)
            {
                Log.Warning(
                    "PaymentService.InsertPaymentAsync; Failed to create Stripe Checkout session. OrganizationId {OrganizationId}, ReservationId {ReservationId}",
                    dto.OrganizationId,
                    dto.ReservationId
                );
                return string.Empty;
            }

            var payment = new Payment
            {
                SessionId = session.Id,
                OrganizationId = dto.OrganizationId,
                ReservationId = dto.ReservationId,
                CustomerId = dto.CustomerId,
                Amount = dto.Amount!.Value,
                Status = StripePaymentIntentHelper.Processing,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "SYSTEM"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return session.Url;
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "PaymentService.InsertPaymentAsync; Error while creating payment. OrganizationId {OrganizationId}, ReservationId {ReservationId}",
                dto.OrganizationId,
                dto.ReservationId
            );

            return string.Empty;
        }
    }

    public async Task<int?> UpdatePaymentAsync(int paymentId, PaymentUpdateRequestDTO dto)
    {
        var orgId = _org.OrganizationId;

        var payment = await _context.Payments
            .Where(p => p.OrganizationId == orgId)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            return null;

        payment.Amount = dto.Amount;
        payment.Status = dto.Status;
        payment.UpdatedBy = _org.Email ?? "USER";
        payment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return payment.Id;
    }

    public async Task<int?> DeletePaymentByIdAsync(int paymentId)
    {
        var orgId = _org.OrganizationId;

        var payment = await _context.Payments
            .Where(p => p.OrganizationId == orgId)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            return null;

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();

        return paymentId;
    }

    public async Task<bool> ConfirmPaymentAsync(int paymentId)
    {
        var orgId = _org.OrganizationId;

        var payment = await _context.Payments
            .Where(x => x.OrganizationId == orgId)
            .FirstOrDefaultAsync(x => x.Id == paymentId);

        if (payment == null)
            return false;

        try
        {
            var intent = await _stripeService.ConfirmPaymentIntentAsync(payment.PaymentIntentId);

            payment.Status = intent.Status;
            payment.UpdatedAt = DateTime.UtcNow;
            payment.UpdatedBy = _org.Email ?? "USER";

            if (StripePaymentIntentHelper.EqualsStatus(intent.Status, StripePaymentIntentHelper.Succeeded))
            {
                await _paymentConfirmationService.GenerateAsync(payment.Id, payment.OrganizationId, 1, payment.Amount);

                await _context.SaveChangesAsync();
                await SendPaymentActionToBooking(payment, intent);

                return true;
            }
            else
            {
                Log.Warning(
                    "PaymentService.ConfirmPaymentAsync; Payment intent not succeeded for PaymentId {PaymentId}. Status: {Status}",
                    paymentId,
                    intent.Status);
            }
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "PaymentService.ConfirmPaymentAsync; Error confirming payment intent for PaymentId {PaymentId}",
                paymentId);
        }

        return false;
    }

    public async Task<bool> CancelPaymentAsync(int paymentId)
    {
        var orgId = _org.OrganizationId;

        var payment = await _context.Payments
            .Where(x => x.OrganizationId == orgId)
            .FirstOrDefaultAsync(x => x.Id == paymentId);

        if (payment == null)
            return false;

        if (StripePaymentIntentHelper.EqualsStatus(payment.Status, StripePaymentIntentHelper.Succeeded))
        {
            Log.Warning(
                "PaymentService.CancelPaymentAsync; Payment intent already succeeded and cannot be canceled. PaymentId {PaymentId}",
                paymentId);
            return false;
        }

        try
        {
            var intent = await _stripeService.CancelPaymentIntentAsync(payment.PaymentIntentId);

            payment.Status = intent.Status;
            payment.UpdatedAt = DateTime.UtcNow;
            payment.UpdatedBy = _org.Email ?? "USER";

            if (StripePaymentIntentHelper.EqualsStatus(intent.Status, StripePaymentIntentHelper.Canceled))
            {
                await _context.SaveChangesAsync();
                await SendPaymentActionToBooking(payment, intent);
                return true;
            }
            else
            {
                await _context.SaveChangesAsync();

                Log.Warning(
                    "PaymentService.CancelPaymentAsync; Payment intent not canceled for PaymentId {PaymentId}. Status: {Status}",
                    paymentId,
                    intent.Status);
            }
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "PaymentService.CancelPaymentAsync; Error canceling payment intent for PaymentId {PaymentId}",
                paymentId);
        }

        return false;
    }

    public async Task SendPaymentActionToBooking(Payment payment, PaymentIntent? intent)
    {
        var correlationId = payment.ReservationId.ToString() ?? string.Empty;
        var stripeStatus = intent?.Status ?? payment.Status;

        var paymentAction = new PaymentAction(
            PaymentId: payment.Id,
            OrganizationId: payment.OrganizationId,
            ReservationId: payment.ReservationId,
            Amount: payment.Amount,
            StripePaymentIntentId: payment.PaymentIntentId,
            StripeStatus: stripeStatus,
            PaidAtUtc: DateTimeOffset.UtcNow
        );

        var envelope = new MessageEnvelope<PaymentAction>(
            MessageId: Guid.NewGuid(),
            MessageType: nameof(PaymentAction),
            OccurredAt: DateTimeOffset.UtcNow,
            CorrelationId: correlationId,
            CausationId: payment.PaymentIntentId,
            Payload: paymentAction,
            SchemaVersion: 1
        );

        var key = correlationId + payment.Id.ToString();
        var outbox = OutboxEnqueuerHelper.Create(_kafkaOptions.PaymentsTopic, key: key, envelope);

        try
        {
            await _kafkaProducerService.ProduceRawAsync(outbox.Topic, outbox.Key, outbox.Payload);
        }
        catch (Exception ex)
        {
            outbox.LastError = ex.Message;

            Log.Error(
                ex,
                "PaymentService.SendToBooking; Failed publishing payment message to Kafka, storing in outbox message. PaymentId {PaymentId}, Topic {Topic}, Key {Key}",
                payment.Id,
                outbox.Topic,
                outbox.Key);

            _context.OutboxMessages.Add(outbox);
            await _context.SaveChangesAsync();
        }
    }
}
