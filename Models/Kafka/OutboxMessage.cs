using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace payment_service.Models.Kafka;

[Table("outbox_message")]
public class OutboxMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("created_at_utc")]
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    [Column("topic")]
    public string Topic { get; set; } = string.Empty;

    [Column("key")]
    public string Key { get; set; } = string.Empty;

    [Column("message_type")]
    public string MessageType { get; set; } = string.Empty;

    [Column("message_id")]
    public Guid MessageId { get; set; }

    [Column("correlation_id")]
    public string CorrelationId { get; set; } = string.Empty;

    [Column("payload")]
    public byte[] Payload { get; set; } = [];

    [Column("sent_at_utc")]
    public DateTimeOffset? SentAtUtc { get; set; }

    [Column("last_error")]
    public string? LastError { get; set; }
}
