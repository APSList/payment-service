using Microsoft.EntityFrameworkCore;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

    }
}
