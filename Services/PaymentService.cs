using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using payment_service.Database; // DbContext namespace
using payment_service.Helpers;
using payment_service.Interfaces;
using payment_service.Models.Kafka;
using payment_service.Models.Payment;
using payment_service.Models.Stripe;
using payment_service.Options;
using Stripe;

namespace payment_service.Services;

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;
    private readonly IStripeIntegrationService _stripeService;
    private readonly IPaymentConfirmationService _paymentConfirmationService;
    private readonly KafkaOptions _kafkaOptions;
    public PaymentService(PaymentDbContext context, IStripeIntegrationService stripeService, IPaymentConfirmationService paymentConfirmationService, IOptions<KafkaOptions> kafkaOptions)
    {
        _context = context;
        _stripeService = stripeService;
        _paymentConfirmationService = paymentConfirmationService;
        _kafkaOptions = kafkaOptions.Value;
    }

    // GET /payments
    public async Task<List<Payment>> GetPaymentsAsync(PaymentFilter filter)
    {
        return await _context.Payments
            .AsNoTracking()
            .ToListAsync();
    }

    // GET /payments/{id}
    public async Task<Payment?> GetPaymentByIdAsync(int paymentId)
    {
        return await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paymentId);
    }

    // POST /payments
    public async Task<PaymentIntent?> InsertPaymentAsync(PaymentCreateRequestDTO dto)
    {
        //TODO VALIDATIONS

        var createPaymentDTO = new StripeCreatePaymentDTO()
        {
            Amount = dto.Amount!.Value
        };

        var paymentIntent = await _stripeService.CreatePaymentIntentAsync(createPaymentDTO);

        var paymentToInsert = new Payment()
        {
            OrganizationId = dto.OrganizationId!.Value,
            ReservationId = dto.ReservationId!.Value,
            Amount = dto.Amount!.Value,
            Status = paymentIntent.Status,
            PaymentIntentId = paymentIntent.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "SYSTEM"
        };

        _context.Payments.Add(paymentToInsert);
        await _context.SaveChangesAsync();

        return paymentIntent;
    }

    // PUT /payments/{id}
    public async Task<int?> UpdatePaymentAsync(int paymentId, PaymentUpdateRequestDTO dto)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment == null)
            return null;

        payment.Amount = dto.Amount;
        payment.Status = dto.Status;
        payment.UpdatedBy = "TODO";
        payment.UpdatedAt = DateTime.UtcNow;

        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();

        return payment.Id.GetHashCode();
    }

    // DELETE /payments/{id}
    public async Task<int?> DeletePaymentByIdAsync(int paymentId)
    {
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
            return null;

        _context.Payments.Remove(payment);
        await _context.SaveChangesAsync();

        return paymentId;
    }

    public async Task<bool> ConfirmPaymentAsync(int paymentId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(x => x.Id == paymentId);

        if (payment == null)
            return false;

        // Confirm on Stripe
        var intent = await _stripeService.ConfirmPaymentIntentAsync(payment.PaymentIntentId);

        // Update status based on Stripe result
        payment.Status = intent.Status;
        payment.UpdatedAt = DateTime.UtcNow;

        await _paymentConfirmationService.GenerateAsync(payment.Id, payment.OrganizationId, 1, payment.Amount);

        await _context.SaveChangesAsync();

        SendToBooking(payment, intent);
        return true;
    }

    public async Task<bool> CancelPaymentAsync(int paymentId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(x => x.Id == paymentId);

        if (payment == null)
            return false;

        // Cancel on Stripe
        var intent = await _stripeService.ConfirmPaymentIntentAsync(payment.PaymentIntentId);

        payment.Status = intent.Status;
        payment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private void SendToBooking(Payment payment, PaymentIntent intent)
    {
        if (string.Equals(intent.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
        {
            var correlationId = payment.ReservationId.ToString(); // ali pa HTTP request correlation id

            var evt = new PaymentSucceeded(
                PaymentId: payment.Id,
                OrganizationId: payment.OrganizationId,
                ReservationId: payment.ReservationId,
                Amount: payment.Amount,
                StripePaymentIntentId: payment.PaymentIntentId,
                StripeStatus: intent.Status,
                PaidAtUtc: DateTimeOffset.UtcNow
            );

            var envelope = new MessageEnvelope<PaymentSucceeded>(
                MessageId: Guid.NewGuid(),
                MessageType: nameof(PaymentSucceeded),
                OccurredAt: DateTimeOffset.UtcNow,
                CorrelationId: correlationId,
                CausationId: payment.PaymentIntentId,
                Payload: evt,
                SchemaVersion: 1
            );

            // Outbox insert (topic + key za ordering po reservation)

            var outbox = OutboxEnqueuerHelper.Create(_kafkaOptions.PaymentsTopic, key: payment.ReservationId.ToString(), envelope);
            _context.OutboxMessages.Add(outbox);
        }
    }
}
