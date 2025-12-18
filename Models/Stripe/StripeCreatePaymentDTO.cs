namespace payment_service.Models.Stripe;

public class StripeCreatePaymentDTO
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = String.Empty;
}
