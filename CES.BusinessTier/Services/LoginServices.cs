using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Utilities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Services
{
    public interface ILoginServices
    {
        BaseResponseViewModel<TokenModel> Login(LoginModel loginModel);
    }
    public class LoginServices : ILoginServices
    {
        private IAccountServices _accountServices;
        private IConfiguration _configuration;
        public LoginServices(IAccountServices accountServices, IConfiguration configuration)
        {
            _accountServices = accountServices;
            _configuration = configuration;
        }

        public BaseResponseViewModel<TokenModel> Login(LoginModel loginModel)
        {
            try
            {
                var account = _accountServices.GetAccountByEmail(loginModel.Email);
                if (!Authen.VerifyHashedPassword(account.Password, loginModel.Password))
                {
                    return new BaseResponseViewModel<TokenModel>()
                    {
                        Code = 200,
                        Message = "Login failed",
                    };
                }
                var newToken = Authen.GenerateToken(account, account.Role.Name, _configuration);
                return new BaseResponseViewModel<TokenModel>
                {
                    Code = 200,
                    Message = "Login success",
                    Data = newToken
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseViewModel<TokenModel>
                {
                    Code = 400,
                    Message = "Something wrong" + "||" + ex.Message,
                };
            }
        }
    }
}
