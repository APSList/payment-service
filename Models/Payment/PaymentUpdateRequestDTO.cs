namespace payment_service.Models.Payment;

public class PaymentUpdateRequestDTO
{
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}
