using System.ComponentModel.DataAnnotations;

namespace payment_service.Options;

public class KafkaOptions
{
    public const string SectionName = "Kafka";
    [Required]
    public string BootstrapServers { get; init; } = default!;
    [Required]
    public string ClientId { get; init; } = "payment-service";
    [Required]
    public string PaymentsTopic { get; init; } = "booking.payments";
}

