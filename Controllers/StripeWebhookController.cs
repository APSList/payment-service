using Microsoft.AspNetCore.Mvc;
using payment_service.Interfaces;

namespace payment_service.Controllers;

[ApiController]
[Route("webhooks/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly IStripeWebhookService _stripeWebhookService;
    public StripeWebhookController(IStripeWebhookService stripeWebhookService )
    {
        _stripeWebhookService = stripeWebhookService;
    }

    [HttpPost("stripe-webhook")]
    public async Task<IActionResult> Handle()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];

        await _stripeWebhookService.ProcessEventAsync(json, signature);
        return Ok();
    }
}

