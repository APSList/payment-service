using Microsoft.EntityFrameworkCore;
using payment_service.Database; // DbContext namespace
using payment_service.Interfaces;
using payment_service.Models.Payment;
using payment_service.Models.Stripe;
using Stripe;

namespace payment_service.Services;

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;
    private readonly IStripeIntegrationService _stripeService;
    private readonly IPaymentConfirmationService _paymentConfirmationService;
    public PaymentService(PaymentDbContext context, IStripeIntegrationService stripeService, IPaymentConfirmationService paymentConfirmationService)
    {
        _context = context;
        _stripeService = stripeService;
        _paymentConfirmationService = paymentConfirmationService;
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
}
