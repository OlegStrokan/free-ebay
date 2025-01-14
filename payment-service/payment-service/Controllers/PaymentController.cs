using Microsoft.AspNetCore.Mvc;
using payment_service.Entities;
using payment_service.Services;

namespace payment_service.Controllers;

    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentService _paymentService;

        public PaymentController(PaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost]
        public IActionResult ProcessPayment([FromBody] Payment payment)
        {
            if (payment == null)
            {
                return BadRequest(new { Message = "Payment data is required." });
            }

            var result = _paymentService.ProcessMessage(payment);

            return Ok(result);
        }
    }
