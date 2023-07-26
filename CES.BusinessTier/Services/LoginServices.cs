using AutoMapper;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using FirebaseAdmin.Messaging;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin.Auth;

namespace CES.BusinessTier.Services
{
    public interface ILoginServices
    {
        Task<BaseResponseViewModel<LoginResponseModel>> Login(LoginModel loginModel);
        Task<AccountResponseModel> GetCurrentLoginAccount();
    }
    public class LoginServices : ILoginServices
    {
        private readonly IAccountServices _accountServices;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public LoginServices(IAccountServices accountServices, IConfiguration configuration, IHttpContextAccessor contextAccessor, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _accountServices = accountServices;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<AccountResponseModel> GetCurrentLoginAccount()
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
            var companyStringId = _contextAccessor.HttpContext?.User.FindFirst("CompanyId").Value;
            int companyId = 0;
            double companyLimits = 0;
            var company = new Company();
            if (!String.IsNullOrEmpty(companyStringId))
            {
                companyId = Int32.Parse(companyStringId);
                company = _unitOfWork.Repository<Company>().GetById(companyId).Result;

            }
            var account = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == accountLoginId).Include(x => x.Wallets).Include(x => x.Enterprises).Include(x => x.Employees).FirstOrDefaultAsync();

            if (account == null)
            {
                throw new ErrorResponse(404, (int)AccountErrorEnums.NOT_FOUND_ID,
                    AccountErrorEnums.NOT_FOUND_ID.GetDisplayName());
            }

            var result = _mapper.Map<AccountResponseModel>(account);

            //get company id in account response for EA and Emp; SA and SupA will get companyId = 0
            if (account.Role.Equals(Roles.EnterpriseAdmin.GetDisplayName()))
            {
                result.CompanyId = companyId;
                result.ExpiredDate = company.ExpiredDate;
                //result.Wallets.FirstOrDefault().Limits = company.Limits;
            }
            else if (account.Role.Equals(Roles.Employee.GetDisplayName()))
            {
                result.CompanyId = companyId;
            }
            return result;
        }

        public async Task<BaseResponseViewModel<LoginResponseModel>> Login(LoginModel loginModel)
        {
            if (loginModel.RefreshToken != null)
            {
                var existedUser = RefreshToken(loginModel);
                return new BaseResponseViewModel<LoginResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    Message = LoginEnums.Success.GetDisplayName(),
                    Data = existedUser.Result
                };
            }
            var account = _accountServices.GetAccountByEmail(loginModel.Email);
            if (account == null)
            {
                throw new ErrorResponse(StatusCodes.Status400BadRequest, (int)AccountErrorEnums.NOT_FOUND, AccountErrorEnums.NOT_FOUND.GetDisplayName());
            }
            if (!Authen.VerifyHashedPassword(account.Password, loginModel.Password))
            {
                throw new ErrorResponse(StatusCodes.Status400BadRequest, StatusCodes.Status400BadRequest, "Login failed");
            }
            // check on firebase đã có hay không
            // nếu có thì bỏ qua, không thì tạo data trên firebase + lấy fcm lưu về local db
            account.FcmToken = loginModel.FcmToken;
            var user = GetUserInfo(account);
            var newToken = Authen.GenerateToken(account, user, _configuration);
            account.RefreshToken = newToken.RefreshToken;
            try
            {
                await _unitOfWork.Repository<Account>().UpdateDetached(account);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<LoginResponseModel>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = LoginEnums.Failed.GetDisplayName(),
                };
            }

            var responseAccount = _mapper.Map<AccountResponseModel>(account);
            if (user.CompanyId != null)
            {
                responseAccount.CompanyId = (int)user.CompanyId;
            }
            var result = new LoginResponseModel()
            {
                Account = responseAccount,
                Token = newToken,
            };
            return new BaseResponseViewModel<LoginResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = LoginEnums.Success.GetDisplayName(),
                Data = result
            };
        }

        private UserResponseModel GetUserInfo(Account account)
        {
            UserResponseModel user = new UserResponseModel();
            switch (account.Role)
            {
                case "Employee":
                    user = _unitOfWork.Repository<Employee>().AsQueryable(x => x.AccountId == account.Id)
                        .ProjectTo<UserResponseModel>(_mapper.ConfigurationProvider)
                        .FirstOrDefault();
                    break;
                case "Enterprise Admin":
                    user = _unitOfWork.Repository<Enterprise>().AsQueryable(x => x.AccountId == account.Id)
                        .ProjectTo<UserResponseModel>(_mapper.ConfigurationProvider)
                        .FirstOrDefault();
                    break;
                case "Supplier Admin":
                    user = _unitOfWork.Repository<Supplier>().AsQueryable(x => x.AccountId == account.Id)
                        .ProjectTo<UserResponseModel>(_mapper.ConfigurationProvider)
                        .FirstOrDefault();
                    break;
                default:
                    break;
            }

            return user;
        }

        private async Task<LoginResponseModel> RefreshToken(LoginModel login)
        {
            var account = _unitOfWork.Repository<Account>().Find(x => x.RefreshToken == login.RefreshToken).FirstOrDefault();
            if (account == null)
            {
                throw new ErrorResponse(StatusCodes.Status404NotFound, StatusCodes.Status404NotFound, "Refresh Token does not exist");
            }

            TokenModel token = new TokenModel();
            var enterprise = _unitOfWork.Repository<Enterprise>().Find(x => x.AccountId == account.Id).FirstOrDefault();
            if (enterprise == null)
            {
                var employee = _unitOfWork.Repository<Employee>().Find(x => x.AccountId == account.Id).FirstOrDefault();
                if (employee == null) throw new ErrorResponse(StatusCodes.Status404NotFound, StatusCodes.Status404NotFound, "User does not exist");
                UserResponseModel userResponse = new UserResponseModel
                {
                    CompanyId = employee.CompanyId
                };
                token = Authen.GenerateToken(account, userResponse, _configuration);
            }
            else
            {
                UserResponseModel userResponse = new UserResponseModel
                {
                    CompanyId = enterprise.CompanyId
                };
                token = Authen.GenerateToken(account, userResponse, _configuration);
            }
            if (account.RefreshToken != token.RefreshToken)
            {
                account.RefreshToken = token.RefreshToken;
                await _unitOfWork.Repository<Account>().UpdateDetached(account);
                await _unitOfWork.CommitAsync();
            }
            return new LoginResponseModel()
            {
                Account = _mapper.Map<AccountResponseModel>(account),
                Token = token,
            };
        }
    }
}
