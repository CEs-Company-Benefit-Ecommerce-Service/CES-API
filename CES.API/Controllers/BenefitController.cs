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
    [Authorize(Roles = "Enterprise Admin")]
    public class BenefitController : ControllerBase
    {
        private readonly IBenefitServices _benefitServices;

        public BenefitController(IBenefitServices benefitServices)
        {
            _benefitServices = benefitServices;
        }

        [HttpGet]
        public async Task<ActionResult> Gets([FromQuery] BenefitResponseModel filter, [FromQuery] PagingModel paging)
        {
            var result = await _benefitServices.GetAllAsync(filter, paging);
            return Ok(result);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> Get(Guid id)
        {
            var result = await _benefitServices.GetById(id);
            return Ok(result);
        }
        
        /// <summary>
        /// Create Benefit
        /// </summary>
        /// <remarks>
        /// 1 - Active, 2 -  InActive \
        /// Type: 1 - Daily, 2 - Weekly, 3- Monthly \
        /// TimeFiler: 0: 00:00, 1: 01:00,.... 23: 23:00 \
        /// DateFilter: 1: Monday, 2: Tuesday,.... 7: Sunday \
        /// DayFilter: 1,.....31 \
        /// Type 1 require Timefilter \
        /// Type 2 require TimeFilter, DateFilter \
        /// Type 3 require TimeFilter, DayFilter
        /// </remarks>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [Authorize(Roles = "Enterprise Admin, System Admin")]
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] BenefitRequestModel request)
        {
            var result = await _benefitServices.CreateAsync(request);
            return StatusCode((int)result.Code, result);
        }
        
        [HttpPut("{id}")]
        [SwaggerOperation(summary: "Status Benefit", description: "1 - Active, 2 -  InActive")]
        public async Task<ActionResult> Put(Guid id, [FromBody] BenefitUpdateModel request)
        {
            var result = await _benefitServices.UpdateAsync(request, id);
            return StatusCode((int)result.Code, result);
        }
    }
}
