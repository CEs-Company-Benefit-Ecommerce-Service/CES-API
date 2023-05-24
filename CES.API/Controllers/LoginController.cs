using CES.BusinessTier.RequestModels;
using CES.BusinessTier.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        [HttpPost]
        public IActionResult Login([FromBody] LoginModel loginModel)
        {
            var result = _loginServices.Login(loginModel);
            return StatusCode((int)result.Code, result);
        }
    }
}
