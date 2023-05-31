using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
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

        public LoginController(ILoginServices loginServices)
        {
            _loginServices = loginServices;
        }

        [SwaggerOperation(summary: "Login with email/user name")]
        [HttpPost]
        public IActionResult Login([FromBody] LoginModel loginModel)
        {
            var result = _loginServices.Login(loginModel);
            return StatusCode((int)result.Code, result);
        }

        [SwaggerOperation(summary: "Get current login account")]
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<AccountResponseModel>> GetCurrentLoginUser()
        {
            return await _loginServices.GetCurrentLoginAccount();
        }
    }
}
