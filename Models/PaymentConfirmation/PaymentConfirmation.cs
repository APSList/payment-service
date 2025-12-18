using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace payment_service.Models.PaymentConfirmation;

[Table("payment_confirmation")]
public class PaymentConfirmation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int? OrganizationId { get; set; }

    [Column("payment_id")]
    public int? PaymentId { get; set; }

    [Column("invoice_number")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Column("customer_id")]
    public int? CustomerId { get; set; }

    [Column("issue_date")]
    public DateTime? IssueDate { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("amount")]
    public decimal? Amount { get; set; }

    [Column("txt_amount")]
    public string TxtAmount { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    // NEW: PDF stored in database as byte array
    [Column("pdf_blob")]
    public byte[]? PdfBlob { get; set; }
}
