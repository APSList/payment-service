using Microsoft.AspNetCore.Mvc;
using payment_service.Interfaces;
using payment_service.Models.Payment;

[ApiController]
[Route("payment")]
[Produces("application/json")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // GET /payment
    [HttpGet]
    [EndpointSummary("Get all payments")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Payment>))]
    public async Task<ActionResult<List<Payment>>> GetPayments([FromQuery] PaymentFilter filter)
    {
        var payments = await _paymentService.GetPaymentsAsync(filter);
        return Ok(payments);
    }

    // GET /payment/{id}
    [HttpGet("{id:int}")]
    [EndpointSummary("Retrieves the payment matching the specified ID.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Payment))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Payment>> GetById(int id)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(id);
        if (payment == null)
            return NotFound();

        return Ok(payment);
    }

    // POST /payment
    [HttpPost]
    [EndpointSummary("Inserts a new payment and returns its payment intent ID from Stripe.")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(int))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Consumes("application/json")]
    public async Task<ActionResult<int>> Insert([FromBody] PaymentCreateRequestDTO createRequestDTO)
    {
        var paymentIntent = await _paymentService.InsertPaymentAsync(createRequestDTO);
        return Ok(paymentIntent);
    }

    // PUT /payment/{id}
    [HttpPut("{id:int}")]
    [EndpointSummary("Updates the payment matching the specified ID.")]
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

    // DELETE /payment/{id}
    [HttpDelete("{id:int}")]
    [EndpointSummary("Deletes the payment matching the specified ID.")]
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
    public async Task<ActionResult> ConfirmPayment(int id)
    {
        var result = await _paymentService.ConfirmPaymentAsync(id);

        return result ? Ok() : NotFound();
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult> CancelPayment(int id)
    {
        var result = await _paymentService.CancelPaymentAsync(id);
        return result ? Ok() : NotFound();
    }
}
