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
        //[HttpGet("receipt")]
        //public async Task<ActionResult> GetReceipts([FromQuery] InvokeResponseModel filter, [FromQuery] PagingModel paging)
        //{
        //    var result = await _receiptServices.GetsAsync(filter, paging);
        //    return Ok(result);
        //}
        //[HttpGet("receipt/{companyId}")]
        //public async Task<ActionResult> GetReceipts([FromQuery] InvokeResponseModel filter, [FromQuery] PagingModel paging, int companyId)
        //{
        //    var result = await _receiptServices.GetsWithCompanyAsync(filter, paging, companyId);
        //    return Ok(result);
        //}
        //[HttpPost("receipt")]
        //public async Task<ActionResult> PostReceipt([FromBody] InvokeRequestModel request)
        //{
        //    var result = _receiptServices.Create(request).Result;
        //    return StatusCode((int)result.Code, result);
        //}
        //[HttpPut("receipt/{id}")]
        //public async Task<ActionResult> PutReceipt(Guid id, int status)
        //{
        //    var result = _receiptServices.UpdateStatus(id, status).Result;
        //    return StatusCode((int)result.Code, result);
        //}
    }
}
