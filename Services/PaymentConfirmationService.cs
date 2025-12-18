using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.EntityFrameworkCore;
using Npgsql.Internal;
using payment_service.Database;
using payment_service.Helpers;
using payment_service.Interfaces;
using payment_service.Models.PaymentConfirmation;
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace payment_service.Services;

public class PaymentConfirmationService : IPaymentConfirmationService
{
    private readonly PaymentDbContext _context;

    public PaymentConfirmationService(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentConfirmation?> GetByIdAsync(int id)
    {
        return await _context.PaymentConfirmations
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<PaymentConfirmation> GenerateAsync(
        int? paymentId,
        int? organizationId,
        int? customerId,
        decimal? amount)
    {
        var confirmation = new PaymentConfirmation
        {
            PaymentId = paymentId,
            OrganizationId = organizationId,
            CustomerId = customerId,
            Amount = amount,
            TxtAmount = amount.HasValue ? PdfHelper.NumberToWords(amount.Value) : "",
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(7),
            InvoiceNumber = organizationId.HasValue ? PdfHelper.GenerateInvoiceNumber(organizationId.Value) : "",
        };

        // First save (we need Id for invoice number if needed)
        _context.PaymentConfirmations.Add(confirmation);
        await _context.SaveChangesAsync();

        // Then generate PDF BLOB and store in DB
        confirmation.PdfBlob = PdfHelper.GeneratePdfBlob(confirmation);

        await _context.SaveChangesAsync();

        return confirmation;
    }

    public async Task<byte[]> DownloadAsync(int confirmationId)
    {
        var confirmation = await GetByIdAsync(confirmationId);

        if (confirmation == null || confirmation.PdfBlob == null)
            return null;

        return confirmation.PdfBlob;
    }

}
