using AutoMapper;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
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
        BaseResponseViewModel<LoginResponseModel> Login(LoginModel loginModel);
    }
    public class LoginServices : ILoginServices
    {
        private IAccountServices _accountServices;
        private IMapper _mapper;
        private IConfiguration _configuration;
        public LoginServices(IMapper mapper, IAccountServices accountServices, IConfiguration configuration)
        {
            _accountServices = accountServices;
            _configuration = configuration;
            _mapper = mapper;
        }

        public BaseResponseViewModel<LoginResponseModel> Login(LoginModel loginModel)
        {
            try
            {
                var account = _accountServices.GetAccountByEmail(loginModel.Email);
                if (!Authen.VerifyHashedPassword(account.Password, loginModel.Password))
                {
                    return new BaseResponseViewModel<LoginResponseModel>()
                    {
                        Code = 200,
                        Message = "Login failed",
                    };
                }
                var newToken = Authen.GenerateToken(account, account.Role.Name, _configuration);
                var result = new LoginResponseModel()
                {
                    Account = _mapper.Map<AccountResponseModel>(account),
                    Token = newToken
                };
                return new BaseResponseViewModel<LoginResponseModel>
                {
                    Code = 200,
                    Message = "Login success",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseViewModel<LoginResponseModel>
                {
                    Code = 400,
                    Message = "Something wrong" + "||" + ex.Message,
                };
            }
        }
    }
}
