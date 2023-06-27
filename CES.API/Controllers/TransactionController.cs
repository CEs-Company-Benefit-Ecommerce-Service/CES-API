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

        public TransactionController(ITransactionService transactionService, IHttpContextAccessor httpContextAccessor)
        {
            _contextAccessor = httpContextAccessor;
            _transactionService = transactionService;
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
    }
}
