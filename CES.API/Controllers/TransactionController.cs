using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Services;
using CES.DataTier.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CES.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ITransactionService _transactionService;
        private readonly IWalletTransaction _walletTransactionService;

        public TransactionController(ITransactionService transactionService, IHttpContextAccessor httpContextAccessor, IWalletTransaction walletTransactionService)
        {
            _contextAccessor = httpContextAccessor;
            _transactionService = transactionService;
            _walletTransactionService = walletTransactionService;
        }

        [HttpGet]
        public async Task<ActionResult> Gets([FromQuery] TransactionResponseModel filter, [FromQuery] PagingModel pagingModel, [FromQuery] int? paymentType)
        {
            var result = await _transactionService.GetsAsync(filter, pagingModel, paymentType);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> Get(Guid id)
        {
            var result = await _transactionService.GetById(id);
            return Ok(result);
        }
        [HttpGet("wallet-transaction")]
        public async Task<ActionResult> GetsWalletTransByAccountLogin([FromQuery] TransactionResponseModel filter, [FromQuery] PagingModel paging)
        {
            var result = await _walletTransactionService.GetsTransOfWalletByLoginUser(filter, paging);
            return StatusCode((int)result.Code, result);
        }
        
        [HttpPost]
        [Authorize(Roles = "Enterprise Admin")]
        public async Task<ActionResult<bool>> CreateTransaction([FromBody] TransactionRequestModel request)
        {
            var result = await _transactionService.CreateTransaction(request);
            return Ok(result);
        }
        
        [HttpPut("{id}")]
        [Authorize(Roles = "System Admin, Enterprise Admin")]
        public async Task<ActionResult<BaseResponseViewModel<TransactionResponseModel>>> UpdateAsync(Guid id, [FromBody] TransactionUpdateModel request)
        {
            var result = await _transactionService.UpdateAsync(id, request);
            return Ok(result);
        }
    }
}
