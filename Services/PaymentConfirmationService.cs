using Microsoft.EntityFrameworkCore;
using payment_service.Database;
using payment_service.Helpers;
using payment_service.Interfaces;
using payment_service.Models.PaymentConfirmation;

namespace payment_service.Services;

public class PaymentConfirmationService : IPaymentConfirmationService
{
    private readonly PaymentDbContext _context;
    private readonly IOrganizationContext _org;

    public PaymentConfirmationService(PaymentDbContext context, IOrganizationContext orgContext)
    {
        _context = context;
        _org = orgContext;
    }

    public async Task<PaymentConfirmation?> GetByIdAsync(int paymentId)
    {
        var orgId = _org.OrganizationId;

        return await _context.PaymentConfirmations
            .AsNoTracking()
            .Where(x => x.OrganizationId == orgId)
            .FirstOrDefaultAsync(x => x.PaymentId == paymentId);
    }

    public async Task<PaymentConfirmation> GenerateAsync(
        int? paymentId,
        int? organizationId,
        int? customerId,
        decimal? amount)
    {
        var orgId = _org.OrganizationId;

        var confirmation = new PaymentConfirmation
        {
            PaymentId = paymentId,
            OrganizationId = orgId,
            CustomerId = customerId,
            Amount = amount,
            TxtAmount = amount.HasValue ? PdfHelper.NumberToWords(amount.Value) : "",
            IssueDate = DateTime.UtcNow,
            InvoiceNumber = PdfHelper.GenerateInvoiceNumber(orgId),
        };

        _context.PaymentConfirmations.Add(confirmation);
        await _context.SaveChangesAsync();

        confirmation.PdfBlob = PdfHelper.GeneratePdfBlob(confirmation);

        await _context.SaveChangesAsync();

        return confirmation;
    }

    public async Task<byte[]> DownloadAsync(int paymentId)
    {
        var orgId = _org.OrganizationId;

        var confirmation = await _context.PaymentConfirmations
            .AsNoTracking()
            .Where(x => x.OrganizationId == orgId)
            .FirstOrDefaultAsync(x => x.PaymentId == paymentId);

        if (confirmation?.PdfBlob == null)
            return null;

        return confirmation.PdfBlob;
    }
}
