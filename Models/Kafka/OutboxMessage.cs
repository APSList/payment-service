namespace payment_service.Models.Kafka;

public class OutboxMessage
{
    public long Id { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public string Topic { get; set; } = default!;
    public string Key { get; set; } = default!;

    public string MessageType { get; set; } = default!;
    public Guid MessageId { get; set; }
    public string CorrelationId { get; set; } = default!;

    /// <summary>Serialized MessageEnvelope bytes.</summary>
    public byte[] Payload { get; set; } = default!;

    public DateTimeOffset? SentAtUtc { get; set; }
    public string? LastError { get; set; }
}

