using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // Required for logging
using payment_service.Entities;
using payment_service.Services;

namespace payment_service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        // Inject ILogger into the controller
        public PaymentController(PaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost("ProcessPayment")]
        public async Task<IActionResult> ProcessPayment([FromBody] Payment payment)
        {
            _logger.LogInformation("Incoming request for ProcessPayment: {@Payment}", payment);

            try
            {
                var result = await _paymentService.ProcessPayment(payment);
                _logger.LogInformation("Payment processing succeeded: {@Result}", result);
                return Ok(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Payment processing failed for payment: {@Payment}", payment);
                return BadRequest(new { error = e.Message });
            }
        }
    }
}