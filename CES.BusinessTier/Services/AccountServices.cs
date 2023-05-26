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

namespace CES.BusinessTier.Services
{
    public interface IAccountServices
    {
        DynamicResponse<AccountResponseModel> Gets(PagingModel paging);
        BaseResponseViewModel<AccountResponseModel> Get(Guid id);
        Task<BaseResponseViewModel<AccountResponseModel>> UpdateAccountAsync(Guid id, AccountRequestModel requestModel);
        Task<BaseResponseViewModel<AccountResponseModel>> DeleteAccountAsync(Guid id);
        Task<BaseResponseViewModel<AccountResponseModel>> CreateAccountAsync(AccountRequestModel requestModel);
        Account GetAccountByEmail(string email);
    }
    public class AccountServices : IAccountServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public AccountServices(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public BaseResponseViewModel<AccountResponseModel> Get(Guid id)
        {
            var account = _unitOfWork.Repository<Account>().GetByIdGuid(id);
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

        public DynamicResponse<AccountResponseModel> Gets(PagingModel paging)
        {
            var accounts = _unitOfWork.Repository<Account>().GetAll()
                .ProjectTo<AccountResponseModel>(_mapper.ConfigurationProvider)
                .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging)
                ;

            return new DynamicResponse<AccountResponseModel>
            {
                Code = 200,
                Message = "OK",
                MetaData = new PagingMetaData(),
                Data = accounts.Item2.ToList()
            };
        }

        public async Task<BaseResponseViewModel<AccountResponseModel>> UpdateAccountAsync(Guid id, AccountRequestModel requestModel)
        {
            var existedAccount = _unitOfWork.Repository<Account>().GetByIdGuid(id);
            if (existedAccount == null)
            {
                return new BaseResponseViewModel<AccountResponseModel>
                {
                    Code = 404,
                    Message = "Not Found"
                };
            }
            try
            {
                var temp = _mapper.Map<AccountRequestModel, Account>(requestModel, existedAccount.Result);
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
            newAccount.CreatedAt = DateTime.Now;

            await _unitOfWork.Repository<Account>().InsertAsync(newAccount);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<AccountResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = _mapper.Map<AccountResponseModel>(newAccount),
            };
        }

        public async Task<BaseResponseViewModel<AccountResponseModel>> DeleteAccountAsync(Guid id)
        {
            var account = _unitOfWork.Repository<Account>().GetByIdGuid(id);
            if (account == null)
            {
                return new BaseResponseViewModel<AccountResponseModel>
                {
                    Code = 404,
                    Message = "Not Found",
                };
            }
            try
            {
                _unitOfWork.Repository<Account>().Delete(account.Result);
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

