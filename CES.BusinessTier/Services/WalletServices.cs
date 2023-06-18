﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using LAK.Sdk.Core.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Services
{
    public interface IWalletServices
    {
        Task<DynamicResponse<WalletResponseModel>> GetsAsync(PagingModel pagingModel);
        BaseResponseViewModel<WalletResponseModel> Get(Guid id);
        BaseResponseViewModel<List<WalletResponseModel>> GetWalletsAccount(Guid accountId);
        Task<BaseResponseViewModel<WalletResponseModel>> CreateAsync(WalletRequestModel request);
        Task<BaseResponseViewModel<WalletResponseModel>> UpdateWalletInfoAsync(Guid id, WalletInfoRequestModel request);
        Task<BaseResponseViewModel<WalletResponseModel>> UpdateWalletBalanceAsync(Guid id, double balance, int type);
    }
    public class WalletServices : IWalletServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;

        public WalletServices(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration, IHttpContextAccessor contextAccessor)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task<DynamicResponse<WalletResponseModel>> GetsAsync(PagingModel pagingModel)
        {
            var wallets = _unitOfWork.Repository<Wallet>().GetAll()
            .ProjectTo<WalletResponseModel>(_mapper.ConfigurationProvider)
               .PagingQueryable(pagingModel.Page, pagingModel.Size, Constants.LimitPaging, Constants.DefaultPaging);

            return new DynamicResponse<WalletResponseModel>
            {
                Code = 200,
                Message = "OK",
                MetaData = new PagingMetaData(),
                Data = await wallets.Item2.ToListAsync()
            };
        }
        public BaseResponseViewModel<List<WalletResponseModel>> GetWalletsAccount(Guid accountId)
        {
            var wallets = _unitOfWork.Repository<Wallet>().GetAll().Where(x => x.AccountId == accountId);
            if (wallets.Count() == 0)
            {
                return new BaseResponseViewModel<List<WalletResponseModel>>
                {
                    Code = 404,
                    Message = "Not found",
                };
            }
            return new BaseResponseViewModel<List<WalletResponseModel>>
            {
                Code = 200,
                Message = "OK",
                Data = _mapper.Map<List<WalletResponseModel>>(wallets)
            };
        }
        public BaseResponseViewModel<WalletResponseModel> Get(Guid id)
        {
            var wallet = _unitOfWork.Repository<Wallet>().GetByIdGuid(id);
            if (wallet.Result == null)
            {
                return new BaseResponseViewModel<WalletResponseModel>
                {
                    Code = 404,
                    Message = "Not found",
                };
            }
            return new BaseResponseViewModel<WalletResponseModel>
            {
                Code = 200,
                Message = "OK",
                Data = _mapper.Map<WalletResponseModel>(wallet.Result)
            };
        }

        public async Task<BaseResponseViewModel<WalletResponseModel>> CreateAsync(WalletRequestModel request)
        {
            //var accountId = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString();

            var newWallet = _mapper.Map<Wallet>(request);
            newWallet.CreatedAt = TimeUtils.GetCurrentSEATime();
            //newWallet.CreatedBy = new Guid(accountId);
            newWallet.Id = Guid.NewGuid();

            try
            {
                await _unitOfWork.Repository<Wallet>().InsertAsync(newWallet);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<WalletResponseModel>
                {
                    Code = 204,
                    Message = "No content",
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<WalletResponseModel>
                {
                    Code = 400,
                    Message = "Bad request",
                };
            }
        }
        public async Task<BaseResponseViewModel<WalletResponseModel>> UpdateWalletInfoAsync(Guid id, WalletInfoRequestModel request)
        {
            var existedWallet = _unitOfWork.Repository<Wallet>().GetByIdGuid(id).Result;
            if (existedWallet == null)
            {
                return new BaseResponseViewModel<WalletResponseModel>
                {
                    Code = 404,
                    Message = "Not found",
                };
            }
            _mapper.Map<WalletInfoRequestModel, Wallet>(request, existedWallet);
            existedWallet.UpdatedAt = TimeUtils.GetCurrentSEATime();
            try
            {
                await _unitOfWork.Repository<Wallet>().UpdateDetached(existedWallet);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<WalletResponseModel>
                {
                    Code = 204,
                    Message = "No content",
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<WalletResponseModel>
                {
                    Code = 400,
                    Message = "Bad request",
                };
            }
        }
        public async Task<BaseResponseViewModel<WalletResponseModel>> UpdateWalletBalanceAsync(Guid id, double balance, int type)
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());

            var existedWallet = _unitOfWork.Repository<Wallet>().GetByIdGuid(id).Result;
            if (existedWallet == null)
            {
                return new BaseResponseViewModel<WalletResponseModel>
                {
                    Code = 404,
                    Message = "Not found",
                };
            }
            switch (type)
            {
                case 1:
                    existedWallet.Balance += balance;
                    if (existedWallet.Balance > existedWallet.Limit)
                    {
                        existedWallet.Balance = existedWallet.Limit;
                    }
                    break;
                case 2:
                    if (existedWallet.Balance < balance)
                    {
                        existedWallet.Balance = 0;
                    }
                    else
                    {
                        existedWallet.Balance -= balance;
                    }
                    break;
                default:
                    break;
            }
            existedWallet.UpdatedAt = TimeUtils.GetCurrentSEATime();
            var walletTransaction = new WalletTransaction()
            {
                Id = Guid.NewGuid(),
                SenderId = accountLoginId,
                RecieverId = existedWallet.AccountId,
                Status = 4,
                WalletId = existedWallet.Id,
                Type = (int)WalletTransactionTypeEnums.AddWelfare,
                Description = "Gui tien phuc loi cho nhan vien",
                Total = balance,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
            };
            try
            {
                await _unitOfWork.Repository<WalletTransaction>().InsertAsync(walletTransaction);
                await _unitOfWork.Repository<Wallet>().UpdateDetached(existedWallet);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<WalletResponseModel>
                {
                    Code = 204,
                    Message = "No content",
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<WalletResponseModel>
                {
                    Code = 400,
                    Message = "Bad request",
                };
            }
        }
    }
}
