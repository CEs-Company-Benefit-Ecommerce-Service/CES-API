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
    public class LoginController : ControllerBase
    {
        private ILoginServices _loginServices;
        private INotificationServices _notificationServices;

        public LoginController(ILoginServices loginServices, INotificationServices notificationServices)
        {
            _loginServices = loginServices;
            _notificationServices = notificationServices;
        }

        [SwaggerOperation(summary: "Login with email/user name")]
        [HttpPost]
        public IActionResult Login([FromBody] LoginModel loginModel)
        {
            var result = _loginServices.Login(loginModel).Result;
            return StatusCode((int)result.Code, result);
        }

        [SwaggerOperation(summary: "Get current login account")]
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<AccountResponseWithCompany>> GetCurrentLoginUser()
        {
            return await _loginServices.GetCurrentLoginAccount();
        }

        [SwaggerOperation(summary: "Get notification of current login account")]
        [Authorize]
        [HttpGet("me/notification")]
        public async Task<ActionResult<AccountResponseModel>> GetNotificationOfCurrentLoginUser([FromQuery] NotificationResponseModel filter, [FromQuery] PagingModel paging)
        {
            var result = _notificationServices.GetsAsync(filter, paging).Result;
            return StatusCode((int)result.Code, result);
        }

        [SwaggerOperation(summary: "Get notification of current login account by notification id")]
        [Authorize]
        [HttpGet("me/notification/{notificationId}")]
        public async Task<ActionResult<AccountResponseModel>> GetNotificationOfCurrentLoginUser(Guid notificationId)
        {
            var result = _notificationServices.GetAsync(notificationId).Result;
            return StatusCode((int)result.Code, result);
        }

        [SwaggerOperation(summary: "Read all noti of current login account")]
        [Authorize]
        [HttpGet("me/notification/read-all")]
        public async Task<ActionResult<AccountResponseModel>> ReadAllNoti()
        {
            var result = _notificationServices.ReadAllNoti().Result;
            return StatusCode((int)result.Code, result);
        }
        [HttpPost("testnotification")]
        public async Task CreateNotificationForEmployeesInActive()
        {
            await _notificationServices.CreateNotificationForEmployeesInActive();
        }
    }
}
