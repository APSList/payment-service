using iText.Kernel.Pdf;
using iText.Layout.Element;
using payment_service.Models.PaymentConfirmation;

namespace payment_service.Helpers;

public static class PdfHelper
{
    public static byte[] GeneratePdfBlob(PaymentConfirmation model)
    {
        using var mem = new MemoryStream();

        using (var writer = new PdfWriter(mem))
        using (var pdf = new PdfDocument(writer))
        using (var doc = new iText.Layout.Document(pdf))
        {
            doc.Add(new Paragraph("PAYMENT CONFIRMATION")
                .SetFontSize(20)).SimulateBold();

            doc.Add(new Paragraph($"Invoice Number: {model.InvoiceNumber}"));
            doc.Add(new Paragraph($"Customer ID: {model.CustomerId}"));
            doc.Add(new Paragraph($"Payment ID: {model.PaymentId}"));
            doc.Add(new Paragraph($"Issue Date: {model.IssueDate:yyyy-MM-dd}"));
            doc.Add(new Paragraph($"Due Date: {model.DueDate:yyyy-MM-dd}"));
            doc.Add(new Paragraph($"Amount: {model.Amount:C}"));
            doc.Add(new Paragraph($"Amount in words: {model.TxtAmount}"));
            doc.Add(new Paragraph("\nThank you for your payment."));
        }

        return mem.ToArray();
    }

    public static string GenerateInvoiceNumber(int orgId)
    {
        return $"ORG-{orgId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    }

    public static string NumberToWords(decimal amount)
    {
        return $"{amount:F2} dollars"; // replace with full converter if needed
    }
}
