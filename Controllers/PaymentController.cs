using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using payment_service.Interfaces;
using payment_service.Models.Payment;
using Microsoft.AspNetCore.Authentication.JwtBearer;

[ApiController]
[Route("payments")]
[Produces("application/json")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // GET /payments
    [HttpGet]
    [EndpointSummary("Get all payments")]
    [Authorize(Policy = "OrgRequired")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Payment>))]
    public async Task<ActionResult<List<Payment>>> GetPayments([FromQuery] PaymentFilter filter)
    {
        var payments = await _paymentService.GetPaymentsAsync(filter);
        return Ok(payments);
    }

    // GET /payments/{id}
    [HttpGet("{id:int}")]
    [EndpointSummary("Retrieves the payment matching the specified ID.")]
    [Authorize(Policy = "OrgRequired")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Payment))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Payment>> GetById(int id)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(id);
        if (payment == null)
            return NotFound();

        return Ok(payment);
    }

    // POST /payments
    [HttpPost]
    [EndpointSummary("Creates a new payment and returns the Stripe Checkout URL.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Consumes("application/json")]
    public async Task<ActionResult<string>> Insert([FromBody] PaymentCreateRequestDTO createRequestDTO)
    {
        var paymentUrl = await _paymentService.InsertPaymentAsync(createRequestDTO);

        if (string.IsNullOrWhiteSpace(paymentUrl))
        {
            return BadRequest("Unable to create payment (please contact support).");
        }

        return Ok(paymentUrl);
    }

    // PUT /payments/{id}
    [HttpPut("{id:int}")]
    [EndpointSummary("Updates the payment matching the specified ID.")]
    [Authorize(Policy = "OrgRequired")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Consumes("application/json")]
    public async Task<ActionResult<int>> Update(int id, [FromBody] PaymentUpdateRequestDTO updateRequestDTO)
    {
        var updated = await _paymentService.UpdatePaymentAsync(id, updateRequestDTO);
        if (updated is null || updated == 0)
            return NotFound();

        return Ok(updated);
    }

    // DELETE /payments/{id}
    [HttpDelete("{id:int}")]
    [EndpointSummary("Deletes the payment matching the specified ID.")]
    [Authorize(Policy = "OrgRequired")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id)
    {
        var deletedId = await _paymentService.DeletePaymentByIdAsync(id);
        if (deletedId == 0 || deletedId.HasValue)
            return NotFound();

        return Ok();
    }

    [HttpPost("{id:int}/confirm")]
    [Authorize(Policy = "OrgRequired")]
    [EndpointSummary("Manual payment confirm matching the specified ID.")]

    public async Task<ActionResult> ConfirmPayment(int id)
    {
        var result = await _paymentService.ConfirmPaymentAsync(id);

        return result ? Ok() : NotFound();
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Policy = "OrgRequired")]
    [EndpointSummary("Manual payment cancel matching the specified ID.")]

    public async Task<ActionResult> CancelPayment(int id)
    {
        var result = await _paymentService.CancelPaymentAsync(id);
        return result ? Ok() : NotFound();
    }
}
