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
    public class OrderController : ControllerBase
    {
        private readonly IOrderServices _orderServices;

        public OrderController(IOrderServices orderServices)
        {
            _orderServices = orderServices;
        }

        [HttpGet]
        public async Task<ActionResult> Gets([FromQuery] OrderResponseModel filter, [FromQuery] PagingModel paging)
        {
            var result = await _orderServices.GetsAsync(filter, paging);
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> Get(Guid id)
        {
            var result = await _orderServices.GetById(id);
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] List<OrderDetailsRequestModel>? orderDetails, [FromQuery] string? notes)
        {
            var result = _orderServices.CreateOrder(orderDetails, notes).Result;
            return StatusCode((int)result.Code, result);
        }
    }
}
