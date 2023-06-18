using AutoMapper;
using AutoMapper.QueryableExtensions;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using LAK.Sdk.Core.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Security.Principal;

namespace CES.BusinessTier.Services
{
    public interface IAccountServices
    {
        DynamicResponse<AccountAllResponseModel> Gets(PagingModel paging);
        BaseResponseViewModel<AccountResponseModel> Get(Guid id);
        Task<BaseResponseViewModel<AccountResponseModel>> UpdateAccountAsync(Guid id, AccountUpdateModel updateModel);
        Task<BaseResponseViewModel<AccountResponseModel>> DeleteAccountAsync(Guid id);
        Task<BaseResponseViewModel<AccountResponseModel>> CreateAccountAsync(AccountRequestModel requestModel);
        Task<BaseResponseViewModel<string>> ChangeAccountPassword(string newPassword, string oldPassword);
        Account GetAccountByEmail(string email);
    }
    public class AccountServices : IAccountServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;

        public AccountServices(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
        }

        public BaseResponseViewModel<AccountResponseModel> Get(Guid id)
        {
            //var account = _unitOfWork.Repository<Account>().GetByIdGuid(id);
            var account = _unitOfWork.Repository<Account>().GetAll().Include(x => x.Wallets).Where(x => x.Id == id).FirstOrDefaultAsync();
            if (account.Result == null)
            {
                return new BaseResponseViewModel<AccountResponseModel>
                {
                    Code = 404,
                    Message = "Not found",

                };
            }
            return new BaseResponseViewModel<AccountResponseModel>
            {
                Code = 200,
                Message = "OK",
                Data = _mapper.Map<AccountResponseModel>(account.Result)
            };
        }

        public DynamicResponse<AccountAllResponseModel> Gets(PagingModel paging)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role == Roles.EnterpriseAdmin.GetDisplayName())
            {
                Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
                var accountLogin = _unitOfWork.Repository<Account>().FindAsync(x => x.Id == accountLoginId);

                var emplAccounts = _unitOfWork.Repository<Account>().GetAll().Where(x => x.CompanyId == accountLogin.Result.CompanyId && x.RoleId == (int)Roles.Employee)
                .ProjectTo<AccountAllResponseModel>(_mapper.ConfigurationProvider)
                .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging)
                ;

                return new DynamicResponse<AccountAllResponseModel>
                {
                    Code = 200,
                    Message = "OK",
                    MetaData = new PagingMetaData()
                    {
                        Total = emplAccounts.Item1
                    },
                    Data = emplAccounts.Item2.ToList()
                };
            }
            var accounts = _unitOfWork.Repository<Account>().GetAll().Where(x => x.RoleId == (int)Roles.EnterpriseAdmin || x.RoleId == (int)Roles.SupplierAdmin)
                .ProjectTo<AccountAllResponseModel>(_mapper.ConfigurationProvider)
                .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging)
                ;

            return new DynamicResponse<AccountAllResponseModel>
            {
                Code = 200,
                Message = "OK",
                MetaData = new PagingMetaData()
                {
                    Total = accounts.Item1
                },
                Data = accounts.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<AccountResponseModel>> UpdateAccountAsync(Guid id, AccountUpdateModel updateModel)
        {

            var existedAccount = _unitOfWork.Repository<Account>().GetByIdGuid(id);
            if (existedAccount == null)
            {
                throw new ErrorResponse(404, (int)AccountErrorEnums.NOT_FOUND_ID,
                    AccountErrorEnums.NOT_FOUND_ID.GetDisplayName());
            }
            try
            {
                var temp = _mapper.Map<AccountUpdateModel, Account>(updateModel, existedAccount.Result);
                temp.UpdatedAt = TimeUtils.GetCurrentSEATime();
                temp.Id = id;
                await _unitOfWork.Repository<Account>().UpdateDetached(temp);
                await _unitOfWork.CommitAsync();
                return new BaseResponseViewModel<AccountResponseModel>
                {
                    Code = 200,
                    Message = "OK",
                    Data = _mapper.Map<AccountResponseModel>(temp),
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseViewModel<AccountResponseModel>
                {
                    Code = 400,
                    Message = "Bad request" + "||" + ex.Message,
                };
            }


        }
        public async Task<BaseResponseViewModel<AccountResponseModel>> CreateAccountAsync(AccountRequestModel requestModel)
        {
            #region validate value
            var validatePermission = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value;
            switch (validatePermission)
            {
                case "Employee":
                    throw new ErrorResponse(StatusCodes.Status403Forbidden, (int)AccountErrorEnums.NOT_HAVE_PERMISSION, AccountErrorEnums.NOT_HAVE_PERMISSION.GetDisplayName());
                case "Enterprise Admin":
                    if (requestModel.RoleId != (int)Roles.Employee)
                    {
                        throw new ErrorResponse(StatusCodes.Status403Forbidden, (int)AccountErrorEnums.NOT_HAVE_PERMISSION, AccountErrorEnums.NOT_HAVE_PERMISSION.GetDisplayName());
                    };
                    Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
                    var accountLogin = await _unitOfWork.Repository<Account>().FindAsync(x => x.Id == accountLoginId);
                    if (accountLogin.CompanyId != requestModel.CompanyId)
                    {
                        throw new ErrorResponse(StatusCodes.Status403Forbidden, (int)CompanyErrorEnums.INVALID_COMPANY_ID, CompanyErrorEnums.INVALID_COMPANY_ID.GetDisplayName());
                    }
                    break;
                case "Supplier Admin":
                    throw new ErrorResponse(StatusCodes.Status403Forbidden, (int)AccountErrorEnums.NOT_HAVE_PERMISSION, AccountErrorEnums.NOT_HAVE_PERMISSION.GetDisplayName());
                default:
                    break;
            }
            #endregion

            var checkEmailAccount = _unitOfWork.Repository<Account>().GetAll().Any(x => x.Email.Equals(requestModel.Email));
            if (checkEmailAccount)
            {
                throw new ErrorResponse(StatusCodes.Status400BadRequest, StatusCodes.Status400BadRequest, "Email already existed!");
            }
            var hashPassword = Authen.HashPassword(requestModel.Password);
            var newAccount = _mapper.Map<Account>(requestModel);
            newAccount.Password = hashPassword;
            newAccount.Id = Guid.NewGuid();
            newAccount.Status = (int)Status.Active;
            newAccount.CreatedAt = TimeUtils.GetCurrentSEATime();
            if (newAccount.RoleId == (int)Roles.Employee)
            {
                var wallets = new List<Wallet>()
                {
                    new Wallet
                    {
                        AccountId = newAccount.Id,
                        Balance = 0,
                        CreatedAt = TimeUtils.GetCurrentSEATime(),
                        Id = Guid.NewGuid(),
                        Name = WalletTypeEnums.FoodWallet.GetDisplayName(),
                        Type = (int)WalletTypeEnums.FoodWallet,
                        Limit = Constants.LimitWallet, //5k point
                    },
                    new Wallet
                    {
                        AccountId = newAccount.Id,
                        Balance = 0,
                        CreatedAt = TimeUtils.GetCurrentSEATime(),
                        Id = Guid.NewGuid(),
                        Name = WalletTypeEnums.StationeryWallet.GetDisplayName(),
                        Type = (int)WalletTypeEnums.StationeryWallet,
                        Limit = Constants.LimitWallet,
                    },
                    new Wallet
                    {
                        AccountId = newAccount.Id,
                        Balance = 0,
                        CreatedAt = TimeUtils.GetCurrentSEATime(),
                        Id = Guid.NewGuid(),
                        Name = WalletTypeEnums.GeneralWallet.GetDisplayName(),
                        Type = (int)WalletTypeEnums.GeneralWallet,
                    }
                };
                newAccount.Wallets = wallets;
            }
            try
            {
                await _unitOfWork.Repository<Account>().InsertAsync(newAccount);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<AccountResponseModel>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = "Bad Request",
                };
            }
            return new BaseResponseViewModel<AccountResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = _mapper.Map<AccountResponseModel>(newAccount),
            };
        }

        public async Task<BaseResponseViewModel<AccountResponseModel>> DeleteAccountAsync(Guid id)
        {
            var account = _unitOfWork.Repository<Account>().GetAll().Include(x => x.Wallets).Where(x => x.Id == id).FirstOrDefaultAsync().Result;
            if (account == null)
            {
                return new BaseResponseViewModel<AccountResponseModel>
                {
                    Code = 404,
                    Message = "Not Found",
                };
            }
            account.Status = (int)Status.Banned;
            account.UpdatedAt = TimeUtils.GetCurrentSEATime();
            //var wallets = account.Wallets;
            //foreach (var wallet in wallets)
            //{
            //    wallet.
            //}
            try
            {
                await _unitOfWork.Repository<Account>().UpdateDetached(account);
                await _unitOfWork.CommitAsync();
                return new BaseResponseViewModel<AccountResponseModel>
                {
                    Code = 200,
                    Message = "OK",
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseViewModel<AccountResponseModel>
                {
                    Code = 400,
                    Message = "Bad request" + "||" + ex.Message,
                };
            }
        }


        public async Task<BaseResponseViewModel<string>> ChangeAccountPassword(string newPassword, string oldPassword)
        {

            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());

            var existedAccount = _unitOfWork.Repository<Account>().GetByIdGuid(accountLoginId).Result;
            if (existedAccount == null)
            {
                return new BaseResponseViewModel<string>
                {
                    Code = 404,
                    Message = "Not Found",
                };
            }
            if (!Authen.VerifyHashedPassword(existedAccount.Password, oldPassword))
            {
                return new BaseResponseViewModel<string>
                {
                    Code = 400,
                    Message = "Bad Request",
                    Data = "Wrong confirm password"
                };
            }
            var newHashPassword = Authen.HashPassword(newPassword);
            existedAccount.Password = newHashPassword;
            existedAccount.UpdatedAt = TimeUtils.GetCurrentSEATime();
            try
            {
                await _unitOfWork.Repository<Account>().UpdateDetached(existedAccount);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<string>
                {
                    Code = 200,
                    Message = "OK",
                    Data = "Success"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseViewModel<string>
                {
                    Code = 400,
                    Message = "Bad request" + "||" + ex.Message,
                };
            }
        }

        public Account GetAccountByEmail(string email)
        {
            var account = _unitOfWork.Repository<Account>().GetAll().Include(x => x.Role)
                .Where(x => x.Email.Equals(email) || x.Name.ToLower().Equals(email.ToLower()))
                .FirstOrDefault();
            if (account == null)
            {
                return null;
            }
            return account;
        }
    }
}

