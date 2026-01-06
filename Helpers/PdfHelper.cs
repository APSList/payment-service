using System;
using System.IO;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using payment_service.Models.PaymentConfirmation;

namespace payment_service.Helpers;

public static class PdfHelper
{
    public static byte[] GeneratePdfBlob(PaymentConfirmation model)
    {
        using var mem = new MemoryStream();

        using (var writer = new PdfWriter(mem))
        using (var pdf = new PdfDocument(writer))
        using (var doc = new iText.Layout.Document(pdf, PageSize.A4))
        {
            doc.SetMargins(30, 30, 30, 30);

            Color hostFlowGreen = new DeviceRgb(46, 204, 113);
            Color darkGray = new DeviceRgb(60, 60, 60);
            Color lightGray = new DeviceRgb(240, 240, 240);

            Table headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 })).UseAllAvailableWidth();

            Cell logoCell = new Cell().SetBorder(Border.NO_BORDER);
            try
            {
                string logoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "assets", "logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    Image logo = new Image(ImageDataFactory.Create(logoPath));
                    logo.SetMaxHeight(50);
                    logoCell.Add(logo);
                }
                else
                {
                    logoCell.Add(new Paragraph("HostFlow").SetFontSize(24).SimulateBold().SetFontColor(hostFlowGreen));
                }
            }
            catch
            {
                logoCell.Add(new Paragraph("HostFlow").SetFontSize(24).SimulateBold().SetFontColor(hostFlowGreen));
            }
            headerTable.AddCell(logoCell);

            Cell titleCell = new Cell().SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE);

            titleCell.Add(new Paragraph("PAYMENT CONFIRMATION")
                .SetFontSize(16)
                .SimulateBold()
                .SetFontColor(darkGray));
            titleCell.Add(new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd}")
                .SetFontSize(9).SetFontColor(ColorConstants.GRAY));

            headerTable.AddCell(titleCell);
            doc.Add(headerTable);

            LineSeparator ls = new LineSeparator(new SolidLine(2f));
            ls.SetFontColor(hostFlowGreen);
            ls.SetMarginTop(10);
            ls.SetMarginBottom(20);
            doc.Add(ls);

            Table infoTable = new Table(UnitValue.CreatePercentArray(new float[] { 2, 3 })).UseAllAvailableWidth();
            infoTable.SetMarginBottom(20);

            AddDataRow(infoTable, "Invoice Number:", model.InvoiceNumber);
            AddDataRow(infoTable, "Customer ID:", model.CustomerId?.ToString() ?? "");
            AddDataRow(infoTable, "Payment ID:", model.PaymentId?.ToString() ?? "");

            AddDataRow(infoTable, "Issue Date:", $"{model.IssueDate:yyyy-MM-dd}");

            if (model.IssueDate.HasValue)
            {
                AddDataRow(infoTable, "Reservation Date From:", $"{model.IssueDate.Value.AddDays(12):yyyy-MM-dd}");
                AddDataRow(infoTable, "Reservation Date To:", $"{model.IssueDate.Value.AddDays(16):yyyy-MM-dd}");
            }

            doc.Add(infoTable);

            Table totalTable = new Table(UnitValue.CreatePercentArray(new float[] { 1 })).UseAllAvailableWidth();

            Cell totalCell = new Cell()
                .SetBackgroundColor(hostFlowGreen) // Zeleno ozadje
                .SetPadding(15)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetBorder(Border.NO_BORDER);

            totalCell.Add(new Paragraph("TOTAL AMOUNT PAID")
                .SetFontColor(ColorConstants.WHITE)
                .SetFontSize(10)
                .SimulateBold());

            totalCell.Add(new Paragraph($"{model.Amount:C}")
                .SetFontColor(ColorConstants.WHITE)
                .SetFontSize(22)
                .SimulateBold());

            totalCell.Add(new Paragraph(model.TxtAmount)
                .SetFontColor(ColorConstants.WHITE)
                .SetFontSize(9)
                .SimulateBold());

            totalTable.AddCell(totalCell);
            doc.Add(totalTable);

            doc.Add(new Paragraph("\nThank you for choosing HostFlow.")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontColor(darkGray)
                .SetFontSize(10)
                .SetMarginTop(30));
        }

        return mem.ToArray();
    }

    private static void AddDataRow(Table table, string label, string value)
    {
        Cell labelCell = new Cell().Add(new Paragraph(label).SimulateBold().SetFontColor(ColorConstants.DARK_GRAY))
            .SetBorder(Border.NO_BORDER)
            .SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 0.5f))
            .SetPadding(8);

        Cell valueCell = new Cell().Add(new Paragraph(value).SetTextAlignment(TextAlignment.RIGHT))
            .SetBorder(Border.NO_BORDER)
            .SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 0.5f))
            .SetPadding(8);

        table.AddCell(labelCell);
        table.AddCell(valueCell);
    }

    public static string GenerateInvoiceNumber(int orgId)
    {
        return $"ORG-{orgId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
    }

    public static string NumberToWords(decimal amount)
    {
        return $"{amount:F2}";
    }
}