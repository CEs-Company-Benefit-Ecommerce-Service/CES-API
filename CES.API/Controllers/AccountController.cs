using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
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
        public IActionResult Get([FromQuery] AccountAllResponseModel filter, [FromQuery] PagingModel pagingModel)
        {
            //var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            //if (role == Roles.Employee.GetDisplayName())
            //{
            //    return StatusCode(401);
            //}
            var result = _accountServices.Gets(filter, pagingModel);
            return Ok(result);
        }
        [Authorize(Roles = "Enterprise Admin, System Admin")]
        [HttpGet("employee-by-company-id")]
        public IActionResult GetAccountEmplByCompanyId([FromQuery] AccountAllResponseModel filter, [FromQuery] PagingModel pagingModel)
        {
            var result = _accountServices.GetsAccountByCompanyId(filter, pagingModel);
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
            return Ok(result);
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
            return Ok(result);
        }
        /// <summary>
        /// Employee can't use
        /// </summary>
        /// <param name="requestModel"></param>
        /// <returns></returns>
        [Authorize]
        [SwaggerOperation(summary: "Create account", description: "Role: 1-System Admin, 2-Supplier Admin, 3-Enterprise Admin, 4-Employee, 5-Shipper")]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] AccountRequestModel requestModel)
        {
            var result = await _accountServices.CreateAccountAsync(requestModel);
            return Ok(result);
        }
        [HttpPut("password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordModel request)
        {
            var result = await _accountServices.ChangeAccountPassword(request.NewPassword, request.OldPassword);
            return Ok(result);
        }
    }
}
