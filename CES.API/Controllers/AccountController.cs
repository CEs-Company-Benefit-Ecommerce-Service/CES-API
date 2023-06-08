using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Services;
using CES.BusinessTier.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace CES.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountServices _accountServices;
        private readonly IHttpContextAccessor _contextAccessor;
        public AccountController(IAccountServices accountServices, IHttpContextAccessor contextAccessor)
        {
            _accountServices = accountServices;
            _contextAccessor = contextAccessor;
        }
        /// <summary>
        /// Employee can't use
        /// </summary>
        /// <param name="pagingModel"></param>
        /// <returns></returns>
        [Authorize(Roles = "Enterprise Admin, System Admin")]
        [HttpGet]
        public IActionResult Get([FromQuery] PagingModel pagingModel)
        {
            //var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            //if (role == Roles.Employee.GetDisplayName())
            //{
            //    return StatusCode(401);
            //}
            var result = _accountServices.Gets(pagingModel);
            return StatusCode((int)result.Code, result);
        }
        [HttpGet("{id}")]
        public IActionResult Get(Guid id)
        {
            var result = _accountServices.Get(id);
            return StatusCode((int)result.Code, result);
        }
        /// <summary>
        /// Employee can't use
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role == Roles.Employee.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = await _accountServices.DeleteAccountAsync(id);
            return StatusCode((int)result.Code, result);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="updateModel"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(Guid id, [FromBody] AccountUpdateModel updateModel)
        {
            var result = await _accountServices.UpdateAccountAsync(id, updateModel);
            return StatusCode((int)result.Code, result);
        }
        /// <summary>
        /// Employee can't use
        /// </summary>
        /// <param name="requestModel"></param>
        /// <returns></returns>
        [Authorize]
        [SwaggerOperation(summary: "Create account", description: "0 - System Admin, 1 - Supplier Admin, 2 - Enterprise Admin, 3 - Employee")]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] AccountRequestModel requestModel)
        {
            var result = await _accountServices.CreateAccountAsync(requestModel);
            return StatusCode((int)result.Code, result);
        }
        [HttpPatch("password")]
        public async Task<ActionResult> ChangePassword([FromQuery] string oldPassword, [FromQuery] string newPassword)
        {
            var result = await _accountServices.ChangeAccountPassword(newPassword, oldPassword);
            return StatusCode((int)result.Code, result);
        }
    }
}
