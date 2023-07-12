using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CES.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebtAndReceiptController : ControllerBase
    {
        private readonly IDebtServices _debtServices;
        private readonly IInvokeServices _receiptServices;

        public DebtAndReceiptController(IDebtServices debtServices, IInvokeServices receiptServices)
        {
            _debtServices = debtServices;
            _receiptServices = receiptServices;
        }

        [HttpGet("debt")]
        public async Task<ActionResult> GetDebts([FromQuery] DebtTicketResponseModel filter, [FromQuery] PagingModel paging)
        {
            var result = await _debtServices.GetsAsync(filter, paging);
            return Ok(result);
        }
        [HttpGet("debt/company/{companyId}")]
        public async Task<ActionResult> GetDebtsWithCompanyId(int companyId, [FromQuery] DebtTicketResponseModel filter, [FromQuery] PagingModel paging)
        {
            var result = await _debtServices.GetsWithCompanyAsync(filter, paging, companyId);
            return Ok(result);
        }
        [HttpGet("debt/{debtId}")]
        public async Task<ActionResult> GetDebtsId(int debtId, [FromQuery] DebtTicketResponseModel filter, [FromQuery] PagingModel paging)
        {
            var result = _debtServices.GetById(debtId);
            return Ok(result);
        }
        [HttpPost("debt")]
        public async Task<ActionResult> Post(int companyId)
        {
            var result = await _debtServices.CreateAsync(companyId);
            return StatusCode((int)result.Code, result);
        }
        [HttpDelete("debt/{debtId}")]
        public async Task<ActionResult> Delete(int debtId)
        {
            var result = await _debtServices.DeleteAsync(debtId);
            return StatusCode((int)result.Code, result);
        }
    }
}
