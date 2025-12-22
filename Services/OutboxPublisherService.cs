using Microsoft.EntityFrameworkCore;
using payment_service.Database;
using payment_service.Interfaces;

namespace payment_service.Services;

/// <summary>
/// Background worker that publishes pending Outbox messages to Kafka.
/// </summary>
public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<OutboxPublisherService> _log;

    public OutboxPublisherService(IServiceProvider sp, ILogger<OutboxPublisherService> log)
    {
        _sp = sp;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
                var producer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();

                var batch = await db.OutboxMessages
                    .Where(x => x.SentAtUtc == null)
                    .OrderBy(x => x.Id)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var m in batch)
                {
                    try
                    {
                        await producer.ProduceRawAsync(m.Topic, m.Key, m.Payload, stoppingToken);
                        m.SentAtUtc = DateTimeOffset.UtcNow;
                        m.LastError = null;
                    }
                    catch (Exception ex)
                    {
                        m.LastError = ex.Message;
                        _log.LogError(ex, "Failed publishing outbox message {Id}", m.Id);
                    }
                }

                if (batch.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Outbox publisher loop failed");
            }

            // Poll interval (tune for throughput)
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
