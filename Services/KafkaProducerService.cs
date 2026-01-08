using Confluent.Kafka;
using Microsoft.Extensions.Options;
using payment_service.Helpers;
using payment_service.Interfaces;
using payment_service.Models.Kafka;
using payment_service.Options;

namespace payment_service.Services;

public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, byte[]> _producer;

    public KafkaProducer(IOptions<KafkaOptions> options)
    {
        var o = options.Value;

        var config = new ProducerConfig
        {
            BootstrapServers = o.BootstrapServers,
            ClientId = o.ClientId,

            // Production-friendly defaults:
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 10,
            RetryBackoffMs = 200,
            LingerMs = 5,
            CompressionType = CompressionType.Snappy
        };

        if (!string.IsNullOrWhiteSpace(o.SaslUsername) && !string.IsNullOrWhiteSpace(o.SaslPassword))
        {
            config.SecurityProtocol = SecurityProtocol.SaslSsl;
            config.SaslMechanism = SaslMechanism.Plain;
            config.SaslUsername = o.SaslUsername;
            config.SaslPassword = o.SaslPassword;
        }

        _producer = new ProducerBuilder<string, byte[]>(config).Build();
    }

    public async Task ProduceAsync<TPayload>(
        string topic,
        string key,
        MessageEnvelope<TPayload> envelope,
        CancellationToken ct = default)
    {
        var msg = new Message<string, byte[]>
        {
            Key = key,
            Value = JsonHelper.Serialize(envelope),
            Headers = new Headers
            {
                { "messageId", envelope.MessageId.ToByteArray() },
                { "messageType", System.Text.Encoding.UTF8.GetBytes(envelope.MessageType) },
                { "correlationId", System.Text.Encoding.UTF8.GetBytes(envelope.CorrelationId) },
                { "occurredAt", System.Text.Encoding.UTF8.GetBytes(envelope.OccurredAt.ToString("O")) },
                { "schemaVersion", System.Text.Encoding.UTF8.GetBytes(envelope.SchemaVersion.ToString()) }
            }
        };

        if (!string.IsNullOrWhiteSpace(envelope.CausationId))
            msg.Headers.Add("causationId", System.Text.Encoding.UTF8.GetBytes(envelope.CausationId));

        ct.ThrowIfCancellationRequested();
        var result = await _producer.ProduceAsync(topic, msg).ConfigureAwait(false);

        if (result.Status is not PersistenceStatus.Persisted)
            throw new KafkaException(new Error(ErrorCode.Local_MsgTimedOut, "Message not persisted."));
    }

    public async Task ProduceRawAsync(
    string topic,
    string key,
    byte[] payload,
    CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var msg = new Message<string, byte[]>
        {
            Key = key,
            Value = payload
        };

        var result = await _producer.ProduceAsync(topic, msg, ct);

        if (result.Status != PersistenceStatus.Persisted)
        {
            throw new Exception(
                $"Kafka message not persisted. Status={result.Status}"
            );
        }
    }


    public void Dispose() => _producer.Dispose();
}
