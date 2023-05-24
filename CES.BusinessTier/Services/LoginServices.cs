using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
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
            //if (loginModel.AccessToken != null)
            //{
            //    var tokenHandler = new JwtSecurityTokenHandler();
            //    var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            //    var tokenValidateParam = new TokenValidationParameters
            //    {
            //        IssuerSigningKey = new SymmetricSecurityKey(key),
            //        ValidateIssuer = false,
            //        ValidateAudience = false,
            //        ValidateLifetime = false,
            //        ValidateIssuerSigningKey = true
            //    };
            //    var tokenInVerification = tokenHandler.ValidateToken(loginModel.AccessToken, tokenValidateParam, out var validatedToken);
            //    if (validatedToken is JwtSecurityToken jwtSecurityToken)
            //    {
            //        var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
            //        if (!result)
            //        {
            //            throw new ErrorResponse(StatusCodes.Status400BadRequest, StatusCodes.Status400BadRequest, "Invalid Token");
            //        }

            //    }
            //    var utcExpireDate = long.Parse(tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
            //    var expireDate = Time.ConvertUnixTimeToDateTime(utcExpireDate);
            //    if (expireDate > DateTime.UtcNow)
            //    {
            //        throw new ErrorResponse(StatusCodes.Status204NoContent, StatusCodes.Status204NoContent, "Access Token has not expired yet");
            //    }
            //}
            var account = _accountServices.GetAccountByEmail(loginModel.Email);
            if (!Authen.VerifyHashedPassword(account.Password, loginModel.Password))
            {
                throw new ErrorResponse(StatusCodes.Status400BadRequest, StatusCodes.Status400BadRequest, "Login failed");
            }
            var newToken = Authen.GenerateToken(account, account.Role.Name, _configuration);
            return new BaseResponseViewModel<TokenModel>
            {
                Code = 200,
                Message = "Login success",
                Data = newToken
            };
        }
    }
}
