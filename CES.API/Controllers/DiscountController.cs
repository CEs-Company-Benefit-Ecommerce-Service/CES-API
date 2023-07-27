using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class DiscountController : ControllerBase
    {
        private readonly IDiscountServices _discountServices;

        public DiscountController(IDiscountServices discountServices)
        {
            _discountServices = discountServices;
        }
        
        /// <summary>
        /// Get discounts
        /// </summary>
        /// <remarks>
        /// Get tất cả các discount đang available cho mỗi product ở thời điểm hiện tại 
        /// </remarks>
        /// <param name="filter"></param>
        /// <param name="paging"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult<DynamicResponse<DiscountResponse>>> GetAllDiscountAsync([FromQuery] DiscountResponse filter, [FromQuery] PagingModel paging)
        {
            return Ok(await _discountServices.GetAllDiscountAsync(filter, paging));
        }

        [HttpGet("{id}", Name = "Get")]
        public async Task<ActionResult<BaseResponseViewModel<DiscountResponse>>> GetDiscountAsync(int id, [FromQuery] DiscountResponse filter)
        {
            return Ok(await _discountServices.GetDiscountAsync(id, filter));
        }

        /// <summary>
        /// Create discount
        /// </summary>
        /// <remarks>
        /// Chỉ có thể tạo mới discount cho 1 product khi không có discount nào đang available cho product đó
        /// - type:
        ///     - 1: Amount
        /// </remarks>
        /// <param name="discount"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<DiscountResponse>>> CreateDiscountAsync([FromBody] DiscountRequest discount)
        {
            return Ok(await _discountServices.CreateDiscountAsync(discount));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResponseViewModel<DiscountResponse>>> UpdateDiscountAsync(int id, [FromBody] DiscountRequest discountUpdate)
        {
            return Ok(await _discountServices.UpdateDiscountAsync(id, discountUpdate));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResponseViewModel<DiscountResponse>>> DeleteDiscountAsync(int id)
        {
            return Ok(await _discountServices.DeleteDiscountAsync(id));
        }
    }
}
