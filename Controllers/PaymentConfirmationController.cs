using Microsoft.AspNetCore.Mvc;
using payment_service.Interfaces;
using payment_service.Models.PaymentConfirmation;

namespace payment_service.Controllers;

[ApiController]
[Route("api/payment_confirmation")]
public class PaymentConfirmationController : ControllerBase
{
    private readonly IPaymentConfirmationService _service;

    public PaymentConfirmationController(IPaymentConfirmationService service)
    {
        _service = service;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var confirmation = await _service.GetByIdAsync(id);
        if (confirmation == null)
            return NotFound();

        return Ok(confirmation);
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        try
        {
            var pdfBytes = await _service.DownloadAsync(id);

            if(pdfBytes is null)
            {
                return NotFound();

            }

            return File(
                pdfBytes,
                "application/pdf",
                fileDownloadName: $"payment_confirmation_{id}.pdf"
            );
        }
        catch (FileNotFoundException)
        {
            return NotFound("File not found");
        }
    }
}
