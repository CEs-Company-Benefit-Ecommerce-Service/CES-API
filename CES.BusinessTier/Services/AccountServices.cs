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
using Microsoft.AspNetCore.Mvc;

namespace CES.BusinessTier.Services
{
    public interface IAccountServices
    {
        DynamicResponse<AccountAllResponseModel> Gets(AccountAllResponseModel filter, PagingModel paging);
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
            var account = _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == id).Include(x => x.Wallets).Include(x => x.Enterprises).ThenInclude(x => x.Company).FirstOrDefault();
            if (account == null)
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
                Data = _mapper.Map<AccountResponseModel>(account)
                //Data = account
            };
        }

        public DynamicResponse<AccountAllResponseModel> Gets(AccountAllResponseModel filter, PagingModel paging)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value;
            if (role == Roles.EnterpriseAdmin.GetDisplayName())
            {
                int enterpriseCompanyId = Int32.Parse(_contextAccessor.HttpContext?.User.FindFirst("CompanyId").Value);
                var employees = _unitOfWork.Repository<Employee>()
                    .AsQueryable(x => x.CompanyId == enterpriseCompanyId && x.Status == (int)Status.Active)
                    .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging);

                var emplAccounts = new List<AccountAllResponseModel>();

                foreach (var employee in employees.Item2.ToList())
                {
                    var emplAccount = _unitOfWork.Repository<Account>()
                        .GetAll()
                        .Where(x => x.Id == employee.AccountId && x.Status == (int)Status.Active)
                        .ProjectTo<AccountAllResponseModel>(_mapper.ConfigurationProvider)
                        .DynamicFilter(filter)
                        .FirstOrDefault();
                    if (emplAccount != null)
                    {
                        emplAccount.CompanyId = enterpriseCompanyId;
                        emplAccounts.Add(emplAccount);
                    };
                }

                return new DynamicResponse<AccountAllResponseModel>
                {
                    Code = 200,
                    Message = "OK",
                    MetaData = new PagingMetaData()
                    {
                        Total = emplAccounts.Count()
                    },
                    Data = emplAccounts
                };
            }
            var accounts = _unitOfWork.Repository<Account>().GetAll()
                    .Where(x => x.Role == Roles.EnterpriseAdmin.GetDisplayName() || x.Role == Roles.SupplierAdmin.GetDisplayName() || x.Role == Roles.Shipper.GetDisplayName())
                    .ProjectTo<AccountAllResponseModel>(_mapper.ConfigurationProvider)
                    .DynamicFilter(filter)
                    .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging);

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
                if (updateModel.CompanyId != null && updateModel.CompanyId > 0)
                {
                    var company = _unitOfWork.Repository<Company>()
                        .Find(x => x.Id == updateModel.CompanyId && x.Status == (int)Status.Active).Any();
                    if (!company)
                    {
                        throw new ErrorResponse(StatusCodes.Status404NotFound, (int)AccountErrorEnums.NOT_FOUND_ID,
                            AccountErrorEnums.NOT_FOUND_ID.GetDisplayName());
                    }
                    if (Commons.RemoveSpaces(temp.Role).ToLower() ==
                        Commons.RemoveSpaces(Roles.EnterpriseAdmin.GetDisplayName()).ToLower())
                    {
                        var user = _unitOfWork.Repository<Enterprise>()
                            .AsQueryable(x => x.AccountId == temp.Id && x.Status == (int)Status.Active)
                            .FirstOrDefault();
                        if (user != null)
                        {
                            user.CompanyId = (int)updateModel.CompanyId;
                            user.UpdatedAt = TimeUtils.GetCurrentSEATime();
                            await _unitOfWork.Repository<Enterprise>().UpdateDetached(user);
                        }
                    }
                    else if (Commons.RemoveSpaces(temp.Role).ToLower() ==
                               Commons.RemoveSpaces(Roles.Employee.GetDisplayName()).ToLower())
                    {
                        var user = _unitOfWork.Repository<Employee>()
                            .AsQueryable(x => x.AccountId == temp.Id && x.Status == (int)Status.Active)
                            .FirstOrDefault();
                        if (user != null)
                        {
                            user.CompanyId = (int)updateModel.CompanyId;
                            user.UpdatedAt = TimeUtils.GetCurrentSEATime();
                            await _unitOfWork.Repository<Employee>().UpdateDetached(user);
                        }
                    }
                }
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
            var stringRole = Commons.ConvertIntRoleToString((int)requestModel.Role);
            #region validate value
            var validatePermission = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value;
            switch (validatePermission)
            {
                case "Employee":
                    throw new ErrorResponse(StatusCodes.Status403Forbidden, (int)AccountErrorEnums.NOT_HAVE_PERMISSION, AccountErrorEnums.NOT_HAVE_PERMISSION.GetDisplayName());
                case "Shipper":
                    throw new ErrorResponse(StatusCodes.Status403Forbidden, (int)AccountErrorEnums.NOT_HAVE_PERMISSION, AccountErrorEnums.NOT_HAVE_PERMISSION.GetDisplayName());
                case "Enterprise Admin":
                    if (Commons.RemoveSpaces(stringRole).ToLower() != Commons.RemoveSpaces(Roles.Employee.GetDisplayName()).ToLower())
                    {
                        throw new ErrorResponse(StatusCodes.Status403Forbidden, (int)AccountErrorEnums.NOT_HAVE_PERMISSION, AccountErrorEnums.NOT_HAVE_PERMISSION.GetDisplayName());
                    };
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


            try
            {
                HandleAccountRole(newAccount, requestModel.Company, 0);

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
            if (account.Wallets.Count > 0)
            {
                var wallets = account.Wallets;
                foreach (var wallet in wallets)
                {
                    wallet.Status = (int)Status.Banned;
                    wallet.UpdatedAt = TimeUtils.GetCurrentSEATime();
                }
            }

            if (Commons.RemoveSpaces(account.Role).ToLower() ==
                Commons.RemoveSpaces(Roles.EnterpriseAdmin.GetDisplayName()).ToLower())
            {
                var user = _unitOfWork.Repository<Enterprise>()
                    .Find(x => x.AccountId == account.Id && x.Status == (int)Status.Active).FirstOrDefault();
                if (user != null)
                {
                    user.Status = (int)Status.Banned;
                    user.UpdatedAt = TimeUtils.GetCurrentSEATime();
                    await _unitOfWork.Repository<Enterprise>().UpdateDetached(user);
                }
            }
            else if (Commons.RemoveSpaces(account.Role).ToLower() ==
                       Commons.RemoveSpaces(Roles.Employee.GetDisplayName()).ToLower())
            {
                var user = _unitOfWork.Repository<Employee>()
                    .Find(x => x.AccountId == account.Id && x.Status == (int)Status.Active).FirstOrDefault();
                if (user != null)
                {
                    user.Status = (int)Status.Banned;
                    user.UpdatedAt = TimeUtils.GetCurrentSEATime();
                    await _unitOfWork.Repository<Employee>().UpdateDetached(user);
                }
            }
            else if (Commons.RemoveSpaces(account.Role).ToLower() ==
                       Commons.RemoveSpaces(Roles.SupplierAdmin.GetDisplayName()).ToLower())
            {
                var user = _unitOfWork.Repository<Supplier>()
                    .Find(x => x.AccountId == account.Id && x.Status == (int)Status.Active).FirstOrDefault();
                if (user != null)
                {
                    user.Status = (int)Status.Banned;
                    user.UpdatedAt = TimeUtils.GetCurrentSEATime();
                    await _unitOfWork.Repository<Supplier>().UpdateDetached(user);
                }
            }
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
            var account = _unitOfWork.Repository<Account>().GetAll()
                .Where(x => x.Email.Equals(email))
                .FirstOrDefault();
            if (account == null)
            {
                return null;
            }
            return account;
        }

        private async Task HandleAccountRole(Account newAccount, CompanyRequestModel newCompany, int companyId = 0)
        {
            if (Commons.RemoveSpaces(newAccount.Role).ToLower() == Commons.RemoveSpaces(Roles.Employee.GetDisplayName()).ToLower())
            {
                Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value);
                int enterpriseCompanyId = Int32.Parse(_contextAccessor.HttpContext?.User.FindFirst("CompanyId").Value);
                newAccount.Role = Roles.Employee.GetDisplayName();
                UserResponseModel userResponse = new UserResponseModel
                {
                    CompanyId = enterpriseCompanyId
                };
                var userToken = Authen.GenerateToken(newAccount, userResponse, _configuration);
                newAccount.RefreshToken = userToken.RefreshToken;
                var user = new Employee()
                {
                    Id = Guid.NewGuid(),
                    CompanyId = enterpriseCompanyId,
                    AccountId = newAccount.Id,
                    Status = (int)Status.Active,
                    CreatedAt = TimeUtils.GetCurrentSEATime()
                };

                List<Wallet> wallets = new List<Wallet>()
                {
                    new Wallet()
                    {
                        Id = Guid.NewGuid(),
                        Name = WalletTypeEnums.GeneralWallet.GetDisplayName(),
                        Status = (int)Status.Active,
                        Balance = 0,
                        Used = 0,
                        CreatedAt = TimeUtils.GetCurrentSEATime(),
                        CreatedBy = accountLoginId,
                        AccountId = newAccount.Id
                    }
                };

                await _unitOfWork.Repository<Employee>().InsertAsync(user);
                await _unitOfWork.Repository<Wallet>().AddRangeAsync(wallets);
            }
            if (Commons.RemoveSpaces(newAccount.Role).ToLower() == Commons.RemoveSpaces(Roles.EnterpriseAdmin.GetDisplayName()).ToLower())
            {
                Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value);
                newAccount.Role = Roles.EnterpriseAdmin.GetDisplayName();

                //if (companyId != null && companyId > 0)
                //{
                //    var company = _unitOfWork.Repository<Company>().Find(x => x.Id == companyId && x.Status == (int)Status.Active)
                //        .FirstOrDefault();
                //    if (company == null)
                //    {
                //        throw new ErrorResponse(StatusCodes.Status404NotFound, (int)CompanyErrorEnums.INVALID_COMPANY_ID, CompanyErrorEnums.INVALID_COMPANY_ID.GetDisplayName());
                //    }
                //    company.ContactPersonId = newAccount.Id;
                //    await _unitOfWork.Repository<Company>().UpdateDetached(company);

                //    var user = new Enterprise()
                //    {
                //        Id = Guid.NewGuid(),
                //        CompanyId = (int)companyId,
                //        AccountId = newAccount.Id,
                //        Status = (int)Status.Active,
                //        CreatedAt = TimeUtils.GetCurrentSEATime()
                //    };

                //    List<Wallet> wallets = new List<Wallet>()
                //    {
                //        new Wallet()
                //        {
                //            Id = Guid.NewGuid(),
                //            Name = WalletTypeEnums.GeneralWallet.GetDisplayName(),
                //            Status = (int)Status.Active,
                //            Balance = 0,
                //            CreatedAt = TimeUtils.GetCurrentSEATime(),
                //            CreatedBy = accountLoginId,
                //            AccountId = newAccount.Id
                //        }
                //    };

                //    await _unitOfWork.Repository<Enterprise>().InsertAsync(user);
                //    await _unitOfWork.Repository<Wallet>().AddRangeAsync(wallets);
                //}
                if (newCompany != null)
                {
                    var company = _mapper.Map<Company>(newCompany);
                    company.CreatedAt = TimeUtils.GetCurrentSEATime();
                    company.Status = (int)Status.Active;
                    company.ContactPersonId = newAccount.Id;
                    company.CreatedBy = accountLoginId;
                    company.Used = 0;
                    var user = new Enterprise()
                    {
                        Id = Guid.NewGuid(),
                        Company = company,
                        AccountId = newAccount.Id,
                        Status = (int)Status.Active,
                        CreatedAt = TimeUtils.GetCurrentSEATime()
                    };

                    UserResponseModel userResponse = new UserResponseModel
                    {
                        CompanyId = company.Id
                    };
                    var userToken = Authen.GenerateToken(newAccount, userResponse, _configuration);
                    newAccount.RefreshToken = userToken.RefreshToken;

                    List<Wallet> wallets = new List<Wallet>()
                    {
                        new Wallet()
                        {
                            Id = Guid.NewGuid(),
                            Name = WalletTypeEnums.GeneralWallet.GetDisplayName(),
                            Status = (int)Status.Active,
                            Balance = newCompany.Limits,
                            Used = 0,
                            CreatedAt = TimeUtils.GetCurrentSEATime(),
                            CreatedBy = accountLoginId,
                            AccountId = newAccount.Id
                        }
                    };
                    try
                    {
                        await _unitOfWork.Repository<Enterprise>().InsertAsync(user);
                        await _unitOfWork.Repository<Wallet>().AddRangeAsync(wallets);
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                }
            }

            if (Commons.RemoveSpaces(newAccount.Role).ToLower() ==
                Commons.RemoveSpaces(Roles.SupplierAdmin.GetDisplayName()).ToLower())
            {
                newAccount.Role = Roles.SupplierAdmin.GetDisplayName();
                var user = new Supplier()
                {
                    Id = Guid.NewGuid(),
                    AccountId = newAccount.Id,
                    Status = (int)Status.Active,
                    CreatedAt = TimeUtils.GetCurrentSEATime()
                };
                await _unitOfWork.Repository<Supplier>().InsertAsync(user);
            }

            if (Commons.RemoveSpaces(newAccount.Role).ToLower() ==
                Commons.RemoveSpaces(Roles.Shipper.GetDisplayName()).ToLower())
            {
                newAccount.Role = Roles.Shipper.GetDisplayName();
            }
        }
    }
}

