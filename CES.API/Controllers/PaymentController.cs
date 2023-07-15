using CES.BusinessTier.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CES.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IDebtServices _debtService;

        public PaymentController(IDebtServices debtServices)
        {
            _debtService = debtServices;
        }

        [HttpGet("{companyId}")]
        public async Task<ActionResult> GetValuePayment(int companyId)
        {
            var result = _debtService.GetValueForPayment(companyId).Result;
            return StatusCode((int)result.Code, result);
        }
    }
}
