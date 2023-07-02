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
