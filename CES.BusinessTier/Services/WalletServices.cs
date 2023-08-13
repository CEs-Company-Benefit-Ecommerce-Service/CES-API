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
using System.ComponentModel.Design;
using FirebaseAdmin.Messaging;
using Notification = CES.DataTier.Models.Notification;
using System.Globalization;

namespace CES.BusinessTier.Services
{
    public interface IWalletServices
    {
        Task<DynamicResponse<WalletResponseModel>> GetsAsync(PagingModel pagingModel);
        BaseResponseViewModel<WalletResponseModel> Get(Guid id);
        Task<BaseResponseViewModel<List<WalletResponseModel>>> GetWalletsAccount(Guid accountId);
        Task<BaseResponseViewModel<WalletResponseModel>> CreateAsync(WalletRequestModel request);
        Task<BaseResponseViewModel<WalletResponseModel>> UpdateWalletInfoAsync(Guid id, WalletInfoRequestModel request);
        Task<BaseResponseViewModel<WalletResponseModel>> UpdateWalletBalanceAsync(WalletUpdateBalanceModel request);
        Task ScheduleUpdateWalletBalanceForGroupAsync(WalletUpdateBalanceModel request, DateTime time);
        Task CreateWalletForAccountDontHaveEnough();
        Task<BaseResponseViewModel<string>> ResetAllAfterEAPayment(int companyId);
        Task<BaseResponseViewModel<string>> ResetAllAfterExpired();
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
            //var wa = _unitOfWork.Repository<Wallet>().AsQueryable();
            return new DynamicResponse<WalletResponseModel>
            {
                Code = 200,
                Message = "OK",
                MetaData = new PagingMetaData(),
                Data = await wallets.Item2.ToListAsync()
            };
        }

        public async Task<BaseResponseViewModel<List<WalletResponseModel>>> GetWalletsAccount(Guid accountId)
        {
            var account = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == accountId).Include(x => x.Wallets).FirstOrDefaultAsync();
            return new BaseResponseViewModel<List<WalletResponseModel>>
            {
                Code = 200,
                Message = "OK",
                Data = _mapper.Map<List<WalletResponseModel>>(account.Wallets)
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
            var accountLogin = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == accountLoginId).Include(x => x.Wallets).FirstOrDefaultAsync();
            var accountLoginWallet = accountLogin.Wallets.FirstOrDefault();

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
                            Message = "No found benefit",
                        };
                    }
                    if (accountLoginWallet.Balance < benefit.UnitPrice)
                    {
                        return new BaseResponseViewModel<WalletResponseModel>
                        {
                            Code = (int)StatusCodes.Status400BadRequest,
                            Message = "Not have enough balance in your wallet",
                        };
                    }
                    accountLoginWallet.Balance -= benefit.UnitPrice;
                    existedWallet.Balance += benefit.UnitPrice;

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
            CultureInfo cul = CultureInfo.GetCultureInfo("vi-VN");
            var walletTransactionForReceiver = new Transaction()
            {
                Id = Guid.NewGuid(),
                SenderId = accountLoginId,
                RecieveId = existedWallet.Account.Id,
                WalletId = existedWallet.Id,
                Type = (int)WalletTransactionTypeEnums.AddWelfare,
                Description = "Nhận tiền từ " + benefit.Name,
                Total = benefit.UnitPrice,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
                CompanyId = benefit.CompanyId,
            };
            var walletTransactionForSender = new Transaction()
            {
                Id = Guid.NewGuid(),
                SenderId = accountLoginId,
                RecieveId = existedWallet.Account.Id,
                WalletId = accountLoginWallet.Id,
                Type = (int)WalletTransactionTypeEnums.AllocateWelfare,
                Description = "Chuyển tiền cho " + existedWallet.Account.Name + " - " + benefit.Name,
                Total = benefit.UnitPrice,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
                CompanyId = benefit.CompanyId,
            };
            var empNotification = new Notification()
            {
                Id = Guid.NewGuid(),
                AccountId = existedWallet.AccountId,
                TransactionId = walletTransactionForReceiver.Id,
                Title = "Bạn đã nhận được tiền từ " + benefit.Name,
                Description = "Số tiền nhận được: " + String.Format(cul, "{0:c}", benefit.UnitPrice),
                IsRead = false,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
            };

            // send noti
            var messaging = FirebaseMessaging.DefaultInstance;
            var response = messaging.SendAsync(new Message
            {
                Token = existedWallet.Account.FcmToken,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = "Ting Ting",
                    Body = "Bạn vừa nhận được số tiền: " + String.Format(cul, "{0:c}", benefit.UnitPrice),
                },
            });

            if (response.Result == null)
            {
                System.Console.WriteLine("Send noti failed");
            }
            try
            {
                await _unitOfWork.Repository<Transaction>().InsertAsync(walletTransactionForReceiver);

                await _unitOfWork.Repository<Transaction>().InsertAsync(walletTransactionForSender);

                await _unitOfWork.Repository<Notification>().InsertAsync(empNotification);

                await _unitOfWork.Repository<Wallet>().UpdateDetached(existedWallet);

                await _unitOfWork.Repository<Wallet>().UpdateDetached(accountLoginWallet);

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

            DateTimeOffset currentDateTimeOffset = new DateTimeOffset(TimeUtils.GetCurrentSEATime());
            DateTimeOffset dateTimeOffset = new DateTimeOffset(time);
            if (dateTimeOffset <= currentDateTimeOffset)
            {
                dateTimeOffset = currentDateTimeOffset.AddMinutes(2);
            }
            dateTimeOffset = dateTimeOffset.AddHours(-7);
            BackgroundJob.Schedule(() => UpdateWalletBalanceForGroupAsync(request, accountLoginId), dateTimeOffset);
            // RecurringJob.AddOrUpdate(() => UpdateWalletBalanceForGroupAsync(request, accountLoginId), TimeUtils.ToCronExpression(time), TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        }

        public async Task<BaseResponseViewModel<WalletResponseModel>> UpdateWalletBalanceForGroupAsync(
            WalletUpdateBalanceModel request, Guid accountLoginId)
        {
            var group = _unitOfWork.Repository<EmployeeGroupMapping>().AsQueryable(x => x.GroupId == request.Id)
                .Include(x => x.Employee)
                .ThenInclude(x => x.Account)
                .ThenInclude(x => x.Wallets);

            var accountLogin = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == accountLoginId).Include(x => x.Wallets).FirstOrDefaultAsync();
            var accountLoginWallet = accountLogin.Wallets.FirstOrDefault();

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
                            if (accountLoginWallet.Balance < benefit.UnitPrice)
                            {
                                return new BaseResponseViewModel<WalletResponseModel>
                                {
                                    Code = (int)StatusCodes.Status400BadRequest,
                                    Message = "Not have enough balance in your wallet",
                                };
                            }
                            accountLoginWallet.Balance -= benefit.UnitPrice;
                            accountLoginWallet.UpdatedAt = TimeUtils.GetCurrentSEATime();
                            wallet.Balance += benefit.UnitPrice;
                            wallet.UpdatedAt = TimeUtils.GetCurrentSEATime();

                            var walletTransactionForSender = new Transaction()
                            {
                                Id = Guid.NewGuid(),
                                SenderId = accountLoginId,
                                RecieveId = wallet.Account.Id,
                                WalletId = wallet.Id,
                                Type = (int)WalletTransactionTypeEnums.AllocateWelfare,
                                Description = "Chuyển tiền cho " + wallet.Account.Name + " - " + benefit.Name,
                                Total = benefit.UnitPrice,
                                CreatedAt = TimeUtils.GetCurrentSEATime(),
                                CompanyId = benefit.CompanyId,
                            };
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
                            var empNotification = new Notification()
                            {
                                Id = Guid.NewGuid(),
                                AccountId = wallet.AccountId,
                                TransactionId = walletTransaction.Id,
                                Title = "Bạn đã nhận được tiền từ " + benefit.Name,
                                Description = "Số tiền nhận được: " + benefit.UnitPrice + " VNĐ",
                                IsRead = false,
                                CreatedAt = TimeUtils.GetCurrentSEATime(),
                            };
                            // send noti
                            var messaging = FirebaseMessaging.DefaultInstance;
                            var response = messaging.SendAsync(new Message
                            {
                                Token = wallet.Account.FcmToken,
                                Notification = new FirebaseAdmin.Messaging.Notification
                                {
                                    Title = "Ting Ting",
                                    Body = "Bạn vừa nhận được số tiền: " + benefit.UnitPrice + " VNĐ",
                                },
                            });

                            await _unitOfWork.Repository<Wallet>().UpdateDetached(wallet);
                            await _unitOfWork.Repository<Wallet>().UpdateDetached(accountLoginWallet);
                            await _unitOfWork.Repository<Transaction>().InsertAsync(walletTransactionForSender);
                            await _unitOfWork.Repository<Transaction>().InsertAsync(walletTransaction);
                            await _unitOfWork.Repository<Notification>().InsertAsync(empNotification);
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
        public async Task<BaseResponseViewModel<string>> ResetAllAfterEAPayment(int companyId)
        {   // this function will call immediately after EA use payment function

            var employees = await _unitOfWork.Repository<Employee>().AsQueryable(x => x.CompanyId == companyId)
                                                            .Include(x => x.Account).ThenInclude(x => x.Wallets).Include(x => x.EmployeeGroupMappings).ToListAsync();
            var enterprise = await _unitOfWork.Repository<Enterprise>().AsQueryable(x => x.CompanyId == companyId)
                                                            .Include(x => x.Account).ThenInclude(x => x.Wallets).FirstOrDefaultAsync();
            var company = _unitOfWork.Repository<Company>().GetById(companyId);

            try
            {
                // Reset balance in Emp wallet = 0
                foreach (var emp in employees)
                {
                    var empWallet = emp.Account.Wallets.FirstOrDefault();
                    var total = empWallet.Balance;
                    
                    // emp.EmployeeGroupMappings.Where(x => x.)
                    foreach (var group in emp.EmployeeGroupMappings)
                    {
                        group.IsReceived = false;
                    }
                    var walletTransaction = new Transaction()
                    {
                        Id = Guid.NewGuid(),
                        WalletId = empWallet.Id,
                        Type = (int)WalletTransactionTypeEnums.AddWelfare,
                        Description = "Reset",
                        RecieveId = enterprise.AccountId,
                        SenderId = emp.AccountId,
                        Total = -(double)total,
                        CompanyId = companyId,
                        CreatedAt = TimeUtils.GetCurrentSEATime(),
                    };
                    var empNotification = new DataTier.Models.Notification()
                    {
                        Id = Guid.NewGuid(),
                        Title = "Reset theo định kỳ",
                        Description = "Số tiền trong ví bạn đã được cập nhật",
                        AccountId = emp.AccountId,
                        IsRead = false,
                        CreatedAt = TimeUtils.GetCurrentSEATime(),
                    };
                    empWallet.Balance = 0;
                    await _unitOfWork.Repository<Transaction>().InsertAsync(walletTransaction);
                    await _unitOfWork.Repository<Notification>().InsertAsync(empNotification);
                    await _unitOfWork.Repository<Employee>().UpdateDetached(emp);
                    await _unitOfWork.Repository<Wallet>().UpdateDetached(empWallet);
                }
                // update EA balance = Company limits
                var EAWallet = enterprise.Account.Wallets.FirstOrDefault();
                EAWallet.Balance = company.Result.Limits;
                EAWallet.Used = 0;
                var dateCheck = company.Result.ExpiredDate.Value.AddDays(-5);
                if (TimeUtils.GetCurrentSEATime().GetStartOfDate() >= dateCheck.GetStartOfDate())
                {
                    company.Result.ExpiredDate = company.Result.ExpiredDate.Value.AddMonths(1);
                    company.Result.UpdatedAt = TimeUtils.GetCurrentSEATime();
                }
                await _unitOfWork.Repository<Company>().UpdateDetached(company.Result);
                await _unitOfWork.Repository<Wallet>().UpdateDetached(EAWallet);

                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<string>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "OK",
                    Data = "Reset done!"
                };
            }
            catch (Exception ex)
            {

                return new BaseResponseViewModel<string>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = "Bad Request",
                    Data = ex.Message
                };
            }


        }

        public async Task<BaseResponseViewModel<string>> ResetAllAfterExpired()
        { // this function use for backgroud job
            var companies = await _unitOfWork.Repository<Company>().AsQueryable(x => x.Status == (int)Status.Active).ToListAsync();
            foreach (var company in companies)
            {
                var employees = await _unitOfWork.Repository<Employee>().AsQueryable(x => x.CompanyId == company.Id)
                                                                            .Include(x => x.Account).ThenInclude(x => x.Wallets).ToListAsync();
                var enterprise = await _unitOfWork.Repository<Enterprise>().AsQueryable(x => x.CompanyId == company.Id)
                                                                .Include(x => x.Account).ThenInclude(x => x.Wallets).FirstOrDefaultAsync();

                if (company.ExpiredDate.Value.GetStartOfDate() == TimeUtils.GetCurrentSEATime().GetStartOfDate())
                {
                    try
                    {
                        // Reset balance in Emp wallet = 0
                        foreach (var emp in employees)
                        {
                            var empWallet = emp.Account.Wallets.FirstOrDefault();
                            empWallet.Balance = 0;
                            empWallet.UpdatedAt = TimeUtils.GetCurrentSEATime();
                            await _unitOfWork.Repository<Wallet>().UpdateDetached(empWallet);
                        }
                        var EAWallet = enterprise.Account.Wallets.FirstOrDefault();
                        EAWallet.Balance = 0;
                        EAWallet.UpdatedAt = TimeUtils.GetCurrentSEATime();
                        await _unitOfWork.Repository<Wallet>().UpdateDetached(EAWallet);

                        await _unitOfWork.CommitAsync();
                    }
                    catch (Exception ex)
                    {

                        return new BaseResponseViewModel<string>
                        {
                            Code = StatusCodes.Status400BadRequest,
                            Message = "Bad Request",
                            Data = ex.Message
                        };
                    }
                }
            }

            return new BaseResponseViewModel<string>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = "Reset done!"
            };
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