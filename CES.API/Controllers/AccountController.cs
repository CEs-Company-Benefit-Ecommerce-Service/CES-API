using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CES.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountServices _accountServices;
        public AccountController(IAccountServices accountServices)
        {
            _accountServices = accountServices;
        }
        [HttpGet]
        public IActionResult Get([FromQuery] PagingModel pagingModel)
        {
            var result = _accountServices.Gets(pagingModel);
            return StatusCode((int)result.Code, result);
        }
        [HttpGet("{id}")]
        public IActionResult Get(Guid id)
        {
            var result = _accountServices.Get(id);
            return StatusCode((int)result.Code, result);
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var result = await _accountServices.DeleteAsync(id);
            return StatusCode((int)result.Code, result);
        }
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] AccountRequestModel requestModel)
        {
            var result = await _accountServices.UpdateAsync(id, requestModel);
            return StatusCode((int)result.Code, result);
        }
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] AccountRequestModel requestModel)
        {
            var result = await _accountServices.CreateAsync(requestModel);
            return StatusCode((int)result.Code, result);
        }
    }
}
