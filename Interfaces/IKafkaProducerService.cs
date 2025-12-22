using payment_service.Models.Kafka;

namespace payment_service.Interfaces;
public interface IKafkaProducer
{
    Task ProduceAsync<TPayload>(string topic, string key, MessageEnvelope<TPayload> envelope, CancellationToken ct = default);

    /// <summary>
    /// Sends already-serialized bytes (used by OutboxPublisher).
    /// </summary>
    Task ProduceRawAsync(string topic, string key, byte[] payload, CancellationToken ct = default);
}
