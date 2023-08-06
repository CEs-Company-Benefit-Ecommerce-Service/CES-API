using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Services;
using CES.BusinessTier.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using CES.BusinessTier.ResponseModels;

namespace CES.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupController : ControllerBase
    {
        private IGroupServices _projectServices;
        private IGroupAccountServices _groupAccountServices;
        private readonly IHttpContextAccessor _contextAccessor;
        public GroupController(IGroupServices projectServices, IHttpContextAccessor contextAccessor, IGroupAccountServices groupAccountServices)
        {
            _projectServices = projectServices;
            _contextAccessor = contextAccessor;
            _groupAccountServices = groupAccountServices;
        }
        /// <summary>
        /// Only Enterprise can use
        /// </summary>
        /// <param name="pagingModel"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Gets([FromQuery] GroupResponseModel filter, [FromQuery] PagingModel pagingModel)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.EnterpriseAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _projectServices.Gets(filter, pagingModel);
            return Ok(result);
        }
        /// <summary>
        /// Only enterprise can use
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public IActionResult Get(Guid id)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.EnterpriseAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _projectServices.Get(id).Result;
            return Ok(result);
        }
        /// <summary>
        /// Only enterprise can use
        /// </summary>
        /// <param name="requestModel"></param>
        /// <returns></returns>
        [SwaggerOperation(summary: "Create project", description: "Status: 0 - InActive, 1 - Active")]
        [HttpPost]
        public IActionResult CreateGroup([FromBody] GroupRequestModel requestModel)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.EnterpriseAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _projectServices.Create(requestModel).Result;
            return Ok(result);
        }
        /// <summary>
        /// Only enterprise can use
        /// </summary>
        /// <param name="id"></param>
        /// <param name="requestModel"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public IActionResult UpdateGroup(Guid id, [FromBody] GroupUpdateModel requestModel)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.EnterpriseAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _projectServices.Update(id, requestModel).Result;
            return Ok(result);
        }
        /// <summary>
        /// Only enterprise can use
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.EnterpriseAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _projectServices.Delete(id).Result;
            return Ok(result);
        }
        /// <summary>
        /// Only enterprise can use
        /// </summary>
        /// <param name="requestModel"></param>
        /// <returns></returns>
        [SwaggerOperation(summary: "Add member to project")]
        [HttpPost("members")]
        public IActionResult AddGroupMember([FromBody] GroupMemberRequestModel requestModel)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.EnterpriseAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _projectServices.AddEmployee(requestModel).Result;
            return Ok(result);
        }
        /// <summary>
        /// Only enterprise can use
        /// </summary>
        /// <param name="requestModel"></param>
        /// <returns></returns>
        [SwaggerOperation(summary: "Remove member of project")]
        [HttpDelete("members/remove")]
        public IActionResult RemoveGroupMember([FromBody] GroupMemberRequestModel requestModel)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.EnterpriseAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _projectServices.RemoveEmployee(requestModel).Result;
            return Ok(result);
        }


        /// <summary>
        /// Use for Enterprise get list account in group
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(Roles = "Enterprise Admin")]
        [HttpGet("{id}/employees")]
        public async Task<ActionResult<DynamicResponse<AccountResponseModel>>> GetAccountsByGroupId(Guid id, [FromQuery] PagingModel paging)
        {
            // var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            // if (role != Roles.EnterpriseAdmin.GetDisplayName())
            // {
            //     return StatusCode(401);
            // }

            var result = await _groupAccountServices.GetAccountsByGroupId(id, paging);
            return Ok(result);
        }

        /// <summary>
        /// Use for Enterprise transfer money for list account in group
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(Roles = "Enterprise Admin")]
        [HttpPost("{id}/employees")]
        public async Task UpdateBalanceForAccountsInGroup(Guid id)
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value);
            await _groupAccountServices.UpdateBalanceForAccountsInGroup(id, accountLoginId);
        }
        [HttpGet("get-by-employee/{accountId}")]
        public async Task<ActionResult> Test(Guid accountId)
        {
            var result = await _projectServices.GetGroupsByEmployeeId(accountId);
            return Ok(result);
        }
    }
}
