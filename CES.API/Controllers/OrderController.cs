using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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

        /// <summary>
        /// Get orders
        /// </summary>
        /// <remarks>
        /// Status: \
        /// 1 - New \
        /// 2 - Ready \
        /// 3 - Shipping \
        /// 4 - Complete \
        /// 5 - Cancel
        /// </remarks>
        /// <param name="filter"></param>
        /// <param name="paging"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "System Admin, Supplier Admin, Employee, Enterprise Admin")]
        public async Task<ActionResult> Gets([FromQuery] OrderResponseModel filter, [FromQuery] PagingModel paging, [FromQuery] int type, [FromQuery] FilterFromTo filterFromTo)
        {
            var result = await _orderServices.GetsAsync(filter, paging, type, filterFromTo);
            return Ok(result);
        }
        [HttpGet("{id}")]
        [Authorize(Roles = "System Admin, Supplier Admin, Employee, Enterprise Admin")]
        public async Task<ActionResult> Get(Guid id)
        {
            var result = await _orderServices.GetById(id);
            return Ok(result);
        }
        [HttpGet("supplier/{supplierId}")]
        [Authorize(Roles = "System Admin, Supplier Admin")]
        public async Task<IActionResult> GetBySupplierId(Guid supplierId, [FromQuery] OrderResponseModel filter, [FromQuery] PagingModel paging)
        {
            var result = await _orderServices.GetsBySupplierId(supplierId, filter, paging);
            return StatusCode((int)result.Code, result);
        }
        [HttpPost]
        [Authorize(Roles = "Employee")]
        public async Task<ActionResult> Post([FromBody] List<OrderDetailsRequestModel>? orderDetails, [FromQuery] string? notes)
        {
            var result = _orderServices.CreateOrder(orderDetails, notes).Result;
            return StatusCode((int)result.Code, result);
        }

        [HttpPut("{id}")]
        [SwaggerOperation(summary: "Order status", description: "1 - New, 2 - Confirm, 3 - Waiting for ship, 4 - Complete, 5 - Cancel ")]
        public async Task<ActionResult> Put(Guid id, [FromQuery] int status)
        {
            var result = _orderServices.UpdateOrderStatus(id, status).Result;
            return StatusCode((int)result.Code, result);
        }
    }
}
