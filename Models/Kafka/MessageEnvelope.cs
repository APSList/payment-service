namespace payment_service.Models.Kafka;

public sealed record MessageEnvelope<T>(
    Guid MessageId,
    string MessageType,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string? CausationId,
    T Payload,
    int SchemaVersion = 1
);

