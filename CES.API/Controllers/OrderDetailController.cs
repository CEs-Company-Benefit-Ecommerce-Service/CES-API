using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CES.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderDetailController : ControllerBase
    {
        private IOrderDetailServices _orderDetailsServices;

        public OrderDetailController(IOrderDetailServices orderDetailServices)
        {
            _orderDetailsServices = orderDetailServices;
        }

        [HttpGet]
        public async Task<ActionResult> Gets([FromQuery] OrderDetailsResponseModel filter, [FromQuery] PagingModel paging)
        {
            var result = await _orderDetailsServices.Gets(filter, paging);
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> Get(Guid id)
        {
            var result = await _orderDetailsServices.GetById(id);
            return Ok(result);
        }
    }
}
