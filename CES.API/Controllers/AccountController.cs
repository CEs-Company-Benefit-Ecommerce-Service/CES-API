using CES.BusinessTier.RequestModels;
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
    public class AccountController : ControllerBase
    {
        private readonly IAccountServices _accountServices;
        public AccountController(IAccountServices accountServices)
        {
            _accountServices = accountServices;
        }

        //[Authorize(Roles = "Admin")]
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
            var result = await _accountServices.DeleteAccountAsync(id);
            return StatusCode((int)result.Code, result);
        }
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] AccountRequestModel requestModel)
        {
            var result = await _accountServices.UpdateAccountAsync(id, requestModel);
            return StatusCode((int)result.Code, result);
        }

        [SwaggerOperation(summary: "Create account", description: "0 - System Admin, 1 - Supplier Admin, 2 - Enterprise Admin, 3 - Employee")]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] AccountRequestModel requestModel)
        {
            var result = await _accountServices.CreateAccountAsync(requestModel);
            return StatusCode((int)result.Code, result);
        }
    }
}
