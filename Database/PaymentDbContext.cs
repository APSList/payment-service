using Microsoft.EntityFrameworkCore;
using payment_service.Models.Kafka;
using payment_service.Models.Payment;
using payment_service.Models.PaymentConfirmation;

namespace payment_service.Database;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentConfirmation> PaymentConfirmations { get; set; }
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(e =>
         {
             e.HasKey(x => x.Id);
             e.HasIndex(x => x.SentAtUtc);
             e.Property(x => x.Topic).IsRequired();
             e.Property(x => x.Key).IsRequired();
             e.Property(x => x.MessageType).IsRequired();
             e.Property(x => x.Payload).IsRequired();
             e.Property(x => x.CorrelationId).IsRequired();
             e.Property(x => x.CreatedAtUtc).IsRequired();
         });
    }
}
