using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace payment_service.Models.Payment;

[Table("payment")]
public class Payment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }  // int ID

    [Column("organization_id")]
    public int? OrganizationId { get; set; }

    [Column("reservation_id")]
    public int? ReservationId { get; set; }

    [Column("amount")]
    public decimal? Amount { get; set; }

    [Column("payment_intent_id")]
    public string PaymentIntentId { get; set; } = string.Empty;

    [Column("payment_method")]
    public string PaymentMethod { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [Column("updated_by")]
    public string UpdatedBy { get; set; } = string.Empty;
}
