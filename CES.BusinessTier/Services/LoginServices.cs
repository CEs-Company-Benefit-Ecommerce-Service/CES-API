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

namespace CES.BusinessTier.Services
{
    public interface ILoginServices
    {
        BaseResponseViewModel<LoginResponseModel> Login(LoginModel loginModel);
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
            if (!String.IsNullOrEmpty(companyStringId))
            {
                companyId = Int32.Parse(companyStringId);
                companyLimits = (double)_unitOfWork.Repository<Company>().GetById(companyId).Result.Limits;

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
                //result.Wallets.FirstOrDefault().Limits = companyLimits;
            }
            else if (account.Role.Equals(Roles.Employee.GetDisplayName()))
            {
                result.CompanyId = companyId;
            }
            return result;
        }

        public BaseResponseViewModel<LoginResponseModel> Login(LoginModel loginModel)
        {
            var account = _accountServices.GetAccountByEmail(loginModel.Email);
            if (account == null)
            {
                throw new ErrorResponse(StatusCodes.Status400BadRequest, (int)AccountErrorEnums.NOT_FOUND, AccountErrorEnums.NOT_FOUND.GetDisplayName());
            }
            if (!Authen.VerifyHashedPassword(account.Password, loginModel.Password))
            {
                throw new ErrorResponse(StatusCodes.Status400BadRequest, StatusCodes.Status400BadRequest, "Login failed");
            }

            var user = GetUserInfo(account);
            var newToken = Authen.GenerateToken(account, user, _configuration);
            var result = new LoginResponseModel()
            {
                Account = _mapper.Map<AccountResponseModel>(account),
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
    }
}
