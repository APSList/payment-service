namespace payment_service.Interfaces;

public interface IStripeWebhookService
{
    Task ProcessEventAsync(string json, string signature);
}
