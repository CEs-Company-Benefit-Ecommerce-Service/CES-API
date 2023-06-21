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
        private readonly IReceiptServices _receiptServices;

        public DebtAndReceiptController(IDebtServices debtServices, IReceiptServices receiptServices)
        {
            _debtServices = debtServices;
            _receiptServices = receiptServices;
        }

        [HttpGet("debt")]
        public async Task<ActionResult> GetDebts([FromQuery] DebtNotesResponseModel filter, [FromQuery] PagingModel paging)
        {
            var result = await _debtServices.GetsAsync(filter, paging);
            return Ok(result);
        }
        [HttpGet("debt/company/{companyId}")]
        public async Task<ActionResult> GetDebtsWithCompanyId([FromQuery] DebtNotesResponseModel filter, [FromQuery] PagingModel paging, int companyId)
        {
            var result = await _debtServices.GetsWithCompanyAsync(filter, paging, companyId);
            return Ok(result);
        }
        [HttpGet("debt/{debtId}")]
        public async Task<ActionResult> GetDebtsWithCompanyId([FromQuery] DebtNotesResponseModel filter, [FromQuery] PagingModel paging, Guid debtId)
        {
            var result = _debtServices.GetById(debtId);
            return Ok(result);
        }
        [HttpPost]
        public async Task<ActionResult> Post(int companyId)
        {
            var result = await _debtServices.CreateAsync(companyId);
            return StatusCode((int)result.Code, result);
        }
        //[HttpGet("receipt")]
        //public async Task<ActionResult> GetReceipts([FromQuery] ReceiptResponseModel filter, [FromQuery] PagingModel paging)
        //{
        //    var result = await _receiptServices.GetsAsync(filter, paging);
        //    return Ok(result);
        //}
        //[HttpGet("receipt/{companyId}")]
        //public async Task<ActionResult> GetReceipts([FromQuery] ReceiptResponseModel filter, [FromQuery] PagingModel paging, int companyId)
        //{
        //    var result = await _receiptServices.GetsWithCompanyAsync(filter, paging, companyId);
        //    return Ok(result);
        //}
    }
}
