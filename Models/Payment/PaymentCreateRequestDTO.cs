using System;

namespace payment_service.Models.Payment;

public class PaymentCreateRequestDTO
{
    public int? OrganizationId { get; set; }
    public int? ReservationId { get; set; }
    public decimal? Amount { get; set; }
}
