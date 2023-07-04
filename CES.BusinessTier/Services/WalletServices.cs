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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Hangfire;

namespace CES.BusinessTier.Services
{
    public interface IWalletServices
    {
        Task<DynamicResponse<WalletResponseModel>> GetsAsync(PagingModel pagingModel);
        BaseResponseViewModel<WalletResponseModel> Get(Guid id);
        BaseResponseViewModel<List<WalletResponseModel>> GetWalletsAccount(Guid accountId);
        Task<BaseResponseViewModel<WalletResponseModel>> CreateAsync(WalletRequestModel request);
        Task<BaseResponseViewModel<WalletResponseModel>> UpdateWalletInfoAsync(Guid id, WalletInfoRequestModel request);
        Task<BaseResponseViewModel<WalletResponseModel>> UpdateWalletBalanceAsync(WalletUpdateBalanceModel request);
        Task ScheduleUpdateWalletBalanceForGroupAsync(WalletUpdateBalanceModel request, DateTime time);
        Task CreateWalletForAccountDontHaveEnough();
    }

    public class WalletServices : IWalletServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;

        public WalletServices(IUnitOfWork unitOfWork, IMapper mapper, IConfiguration configuration,
            IHttpContextAccessor contextAccessor)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public async Task<DynamicResponse<WalletResponseModel>> GetsAsync(PagingModel pagingModel)
        {
            var wallets = _unitOfWork.Repository<Wallet>().AsQueryable()
                .ProjectTo<WalletResponseModel>(_mapper.ConfigurationProvider)
                .PagingQueryable(pagingModel.Page, pagingModel.Size, Constants.LimitPaging, Constants.DefaultPaging);
            var wa = _unitOfWork.Repository<Wallet>().AsQueryable();
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
            // var wallets = _unitOfWork.Repository<Wallet>().GetAll().Where(x => x.Account.Select(x => x.Id).FirstOrDefault() == accountId);
            var wallets = _unitOfWork.Repository<Wallet>().GetAll().FirstOrDefault();
            // if (wallets.Count() == 0)
            // {
            //     return new BaseResponseViewModel<List<WalletResponseModel>>
            //     {
            //         Code = 404,
            //         Message = "Not found",
            //     };
            // }
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

        public async Task<BaseResponseViewModel<WalletResponseModel>> UpdateWalletInfoAsync(Guid id,
            WalletInfoRequestModel request)
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

        public async Task<BaseResponseViewModel<WalletResponseModel>> UpdateWalletBalanceAsync(
            WalletUpdateBalanceModel request)
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value
                .ToString());
            if (request.BenefitId == null)
            {
                request.BenefitId = Guid.Empty;
            }

            var benefit = _unitOfWork.Repository<Benefit>().GetByIdGuid((Guid)request.BenefitId).Result;
            var existedWallet = await _unitOfWork.Repository<Wallet>().AsQueryable(x => x.Id == request.Id)
                .Include(x => x.Account).FirstOrDefaultAsync();
            if (existedWallet == null)
            {
                return new BaseResponseViewModel<WalletResponseModel>
                {
                    Code = 404,
                    Message = "Not found",
                };
            }

            switch (request.Type)
            {
                case 1:

                    if (benefit == null)
                    {
                        return new BaseResponseViewModel<WalletResponseModel>
                        {
                            Code = (int)StatusCodes.Status404NotFound,
                            Message = "No found",
                        };
                    }

                    if (request.Balance > benefit.UnitPrice)
                    {
                        return new BaseResponseViewModel<WalletResponseModel>
                        {
                            Code = (int)StatusCodes.Status400BadRequest,
                            Message = "Balance was higher than unit price of benefit",
                        };
                    }

                    existedWallet.Balance += request.Balance;
                    break;
                case 2:

                    if (existedWallet.Balance < request.Balance)
                    {
                        existedWallet.Balance = 0;
                    }
                    else
                    {
                        existedWallet.Balance -= request.Balance;
                    }

                    break;
                default:
                    break;
            }

            existedWallet.UpdatedAt = TimeUtils.GetCurrentSEATime();
            var walletTransaction = new Transaction()
            {
                Id = Guid.NewGuid(),
                SenderId = accountLoginId,
                RecieveId = existedWallet.Account.Id,
                WalletId = existedWallet.Id,
                Type = (int)WalletTransactionTypeEnums.AddWelfare,
                Description = "Nhận tiền từ " + benefit.Description,
                Total = request.Balance,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
                CompanyId = benefit.CompanyId,
            };
            // var walletTransactionLog = new TransactionWalletLog()
            // {
            //     Id = Guid.NewGuid(),
            //     CompanyId = benefit.CompanyId,
            //     TransactionId = walletTransaction.Id,
            //     Description = "Log Chuyển tiền || " + TimeUtils.GetCurrentSEATime(),
            //     CreatedAt = TimeUtils.GetCurrentSEATime(),
            // };
            try
            {
                await _unitOfWork.Repository<Transaction>().InsertAsync(walletTransaction);
                // await _unitOfWork.Repository<TransactionWalletLog>().InsertAsync(walletTransactionLog);
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

        public async Task ScheduleUpdateWalletBalanceForGroupAsync(
            WalletUpdateBalanceModel request, DateTime time)
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value
                .ToString());
            if (request.BenefitId == null)
            {
                request.BenefitId = Guid.Empty;
            }
            DateTimeOffset dateTimeOffset = new DateTimeOffset(time);
            if (time < TimeUtils.GetCurrentSEATime())
            {
                dateTimeOffset = new DateTimeOffset(TimeUtils.GetCurrentSEATime());
                dateTimeOffset = dateTimeOffset.AddMinutes(2);
            }
            BackgroundJob.Schedule(() => UpdateWalletBalanceForGroupAsync(request, accountLoginId), dateTimeOffset);
        }

        public async Task<BaseResponseViewModel<WalletResponseModel>> UpdateWalletBalanceForGroupAsync(
            WalletUpdateBalanceModel request, Guid accountLoginId)
        {
            var group = _unitOfWork.Repository<EmployeeGroupMapping>().AsQueryable(x => x.GroupId == request.Id)
                .Include(x => x.Employee)
                .ThenInclude(x => x.Account)
                .ThenInclude(x => x.Wallets);
            if (request.BenefitId == null)
            {
                request.BenefitId = Guid.Empty;
            }

            var benefit = _unitOfWork.Repository<Benefit>().GetByIdGuid((Guid)request.BenefitId).Result;
            switch (request.Type)
            {
                case 1:

                    if (benefit == null)
                    {
                        return new BaseResponseViewModel<WalletResponseModel>
                        {
                            Code = (int)StatusCodes.Status404NotFound,
                            Message = "No found",
                        };
                    }

                    if (request.Balance > benefit.UnitPrice)
                    {
                        return new BaseResponseViewModel<WalletResponseModel>
                        {
                            Code = (int)StatusCodes.Status400BadRequest,
                            Message = "Balance was higher than unit price of benefit",
                        };
                    }

                    foreach (var employeeGroup in group)
                    {
                        foreach (var wallet in employeeGroup.Employee.Account.Wallets)
                        {
                            wallet.Balance += request.Balance;
                            wallet.UpdatedAt = TimeUtils.GetCurrentSEATime();
                            var walletTransaction = new Transaction()
                            {
                                Id = Guid.NewGuid(),
                                SenderId = accountLoginId,
                                RecieveId = wallet.AccountId,
                                WalletId = wallet.Id,
                                Type = (int)WalletTransactionTypeEnums.AddWelfare,
                                Description = "Nhận tiền từ " + benefit.Description,
                                Total = request.Balance,
                                CreatedAt = TimeUtils.GetCurrentSEATime(),
                                CompanyId = benefit.CompanyId,
                            };
                            await _unitOfWork.Repository<Wallet>().UpdateDetached(wallet);
                            await _unitOfWork.Repository<Transaction>().InsertAsync(walletTransaction);
                        }
                    }

                    break;
                case 2:

                    foreach (var employeeGroup in group)
                    {
                        foreach (var wallet in employeeGroup.Employee.Account.Wallets)
                        {
                            if (wallet.Balance < request.Balance)
                            {
                                wallet.Balance = 0;
                                wallet.UpdatedAt = TimeUtils.GetCurrentSEATime();
                                var walletTransaction = new Transaction()
                                {
                                    Id = Guid.NewGuid(),
                                    SenderId = accountLoginId,
                                    RecieveId = wallet.AccountId,
                                    WalletId = wallet.Id,
                                    Type = (int)WalletTransactionTypeEnums.AddWelfare,
                                    Description = "Nhận tiền từ " + benefit.Description,
                                    Total = request.Balance,
                                    CreatedAt = TimeUtils.GetCurrentSEATime(),
                                    CompanyId = benefit.CompanyId,
                                };
                                await _unitOfWork.Repository<Wallet>().UpdateDetached(wallet);
                                await _unitOfWork.Repository<Transaction>().InsertAsync(walletTransaction);
                            }
                            else
                            {
                                wallet.Balance -= request.Balance;
                                wallet.UpdatedAt = TimeUtils.GetCurrentSEATime();
                                await _unitOfWork.Repository<Wallet>().UpdateDetached(wallet);
                            }
                        }
                    }

                    break;
                default:
                    break;
            }

            try
            {
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

        public async Task CreateWalletForAccountDontHaveEnough()
        {
            //var activeAccounts = _unitOfWork.Repository<Account>()
            //    .AsQueryable(x =>
            //        x.Status == (int)Status.Active &&
            //        (x.RoleId != (int)Roles.SupplierAdmin || x.RoleId != (int)Roles.SystemAdmin))
            //    .Include(x => x.Wallet)
            //    .ToList();
            //foreach (var account in activeAccounts)
            //{
            //    if (account.Wallet.Count == 0)
            //    {
            //        var wallets = new List<Wallet>()
            //        {
            //            new Wallet
            //            {
            //                AccountId = account.Id,
            //                Balance = 0,
            //                CreatedAt = TimeUtils.GetCurrentSEATime(),
            //                Id = Guid.NewGuid(),
            //                Name = WalletTypeEnums.FoodWallet.GetDisplayName(),
            //                Type = (int)WalletTypeEnums.FoodWallet,
            //            },
            //            new Wallet
            //            {
            //                AccountId = account.Id,
            //                Balance = 0,
            //                CreatedAt = TimeUtils.GetCurrentSEATime(),
            //                Id = Guid.NewGuid(),
            //                Name = WalletTypeEnums.StationeryWallet.GetDisplayName(),
            //                Type = (int)WalletTypeEnums.StationeryWallet,
            //            },
            //            new Wallet
            //            {
            //                AccountId = account.Id,
            //                Balance = 0,
            //                CreatedAt = TimeUtils.GetCurrentSEATime(),
            //                Id = Guid.NewGuid(),
            //                Name = WalletTypeEnums.GeneralWallet.GetDisplayName(),
            //                Type = (int)WalletTypeEnums.GeneralWallet,
            //            }
            //        };
            //        account.Wallet = wallets;
            //    }
            //    else if (account.Wallet.Count > 0)
            //    {
            //        var wallets = new List<Wallet>();
            //        List<int> walletTypes = new List<int>();
            //        walletTypes.Add((int)WalletTypeEnums.GeneralWallet);
            //        walletTypes.Add((int)WalletTypeEnums.StationeryWallet);
            //        walletTypes.Add((int)WalletTypeEnums.FoodWallet);
            //        foreach (var wallet in account.Wallet)
            //        {
            //            walletTypes.Remove((int)wallet.Type);
            //        }

            //        if (walletTypes.Count > 0)
            //        {
            //            foreach (var walletType in walletTypes)
            //            {
            //                var wallet = new Wallet
            //                {
            //                    AccountId = account.Id,
            //                    Balance = 0,
            //                    CreatedAt = TimeUtils.GetCurrentSEATime(),
            //                    Id = Guid.NewGuid(),
            //                    Name = walletType == 1 ? WalletTypeEnums.FoodWallet.GetDisplayName() :
            //                        walletType == 2 ? WalletTypeEnums.StationeryWallet.GetDisplayName() :
            //                        WalletTypeEnums.GeneralWallet.GetDisplayName(),
            //                    Type = walletType,
            //                };
            //                account.Wallet.Add(wallet);
            //            }
            //        }
            //    }
            //    await _unitOfWork.Repository<Account>().UpdateDetached(account);
            //}
            //await _unitOfWork.CommitAsync();
        }
    }
}