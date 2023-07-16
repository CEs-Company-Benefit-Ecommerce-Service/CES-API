using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Services;
using CES.DataTier.Models;
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
        public async Task<ActionResult> Gets([FromQuery] Transaction filter, [FromQuery] PagingModel pagingModel)
        {
            var result = await _transactionService.GetsAsync(filter, pagingModel);
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
    }
}
