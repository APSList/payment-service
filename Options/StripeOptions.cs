using System.ComponentModel.DataAnnotations;
namespace payment_service.Options;

public class StripeOptions
{
    public const string SectionName = "Stripe";

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}
