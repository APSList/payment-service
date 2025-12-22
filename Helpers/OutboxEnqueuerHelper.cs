using payment_service.Models.Kafka;

namespace payment_service.Helpers;

public static class OutboxEnqueuerHelper
{
    /// <summary>
    /// Creates OutboxMessage where Payload contains serialized MessageEnvelope bytes.
    /// </summary>
    public static OutboxMessage Create<T>(
        string topic,
        string key,
        MessageEnvelope<T> envelope)
    {
        var bytes = JsonHelper.Serialize(envelope);

        return new OutboxMessage
        {
            CreatedAtUtc = envelope.OccurredAt,
            Topic = topic,
            Key = key,
            MessageType = envelope.MessageType,
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            Payload = bytes
        };
    }
}
