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

    [Required]
    public bool EnableKafka { get; init; } = true;

    // SASL/SCRAM (za Helm setup)
    public string? SaslUsername { get; init; }
    public string? SaslPassword { get; init; }
    public string? SecurityProtocol { get; init; }   // npr. "SaslPlaintext"
    public string? SaslMechanism { get; init; }      // npr. "ScramSha256"
}

