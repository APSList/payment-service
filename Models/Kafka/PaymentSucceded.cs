namespace payment_service.Models.Kafka;

public sealed record PaymentSucceeded(
    int? PaymentId,
    int? OrganizationId,
    int? ReservationId,
    decimal? Amount,
    string StripePaymentIntentId,
    string StripeStatus,
    DateTimeOffset? PaidAtUtc
);

