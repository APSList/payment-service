using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using payment_service.Interfaces;
using payment_service.Models.PaymentConfirmation;

namespace payment_service.Controllers;

[ApiController]
[Route("payment_confirmation")]

public class PaymentConfirmationController : ControllerBase
{
    private readonly IPaymentConfirmationService _service;

    public PaymentConfirmationController(IPaymentConfirmationService service)
    {
        _service = service;
    }

    [HttpGet("{paymentId:int}")]
    [Authorize(Policy = "OrgRequired")]

    public async Task<IActionResult> GetById(int id)
    {
        var confirmation = await _service.GetByIdAsync(id);
        if (confirmation == null)
            return NotFound();

        return Ok(confirmation);
    }

    [HttpGet("{paymentId:int}/download")]
    [Authorize(Policy = "OrgRequired")]

    public async Task<IActionResult> Download(int paymentId)
    {
        try
        {
            var pdfBytes = await _service.DownloadAsync(paymentId);

            if(pdfBytes is null)
            {
                return NotFound();

            }

            return File(
                pdfBytes,
                "application/pdf",
                fileDownloadName: $"payment_confirmation_{paymentId}.pdf"
            );
        }
        catch (FileNotFoundException)
        {
            return NotFound("File not found");
        }
    }
}
