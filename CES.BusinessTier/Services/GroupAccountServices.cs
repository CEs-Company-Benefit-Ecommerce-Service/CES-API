using AutoMapper;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using LAK.Sdk.Core.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using CES.BusinessTier.ResponseModels;
using Microsoft.AspNetCore.Http;
using FirebaseAdmin.Messaging;
using Notification = CES.DataTier.Models.Notification;
using System.Globalization;
using CES.BusinessTier.RequestModels;
using Hangfire;

namespace CES.BusinessTier.Services
{
    public interface IGroupAccountServices
    {
        Task<bool> Deleted(Guid id);
        Task<EmployeeGroupMapping> Created(Guid employId, Guid projectId);
        public IEnumerable<Group> Gets(PagingModel paging);
        Task<bool> CheckAccountInGroup(Guid employId, Guid projectId);
        Task<DynamicResponse<AccountResponseModel>> GetAccountsByGroupId(Guid benefitId, PagingModel paging);
        Task<DynamicResponse<UserResponseModel>> GetAllAccountsNotInGroup(UserResponseModel filter, Guid benefitId, PagingModel paging);
        Task UpdateBalanceForAccountsInGroup(Guid id, Guid enterpriseId);
        Task<bool> ScheduleUpdateBalanceForAccountsInGroup(Guid id, Guid enterpriseId);
    }

    public class GroupAccountServices : IGroupAccountServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public GroupAccountServices(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = contextAccessor;
        }

        public IEnumerable<Group> Gets(PagingModel paging)
        {
            var projectAccounts = _unitOfWork.Repository<Group>().GetAll()
                .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging);
            ;
            return projectAccounts.Item2.ToList();
        }

        public async Task<EmployeeGroupMapping> Created(Guid employId, Guid projectId)
        {
            var newGroupAccount = new EmployeeGroupMapping()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employId,
                GroupId = projectId,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
            };
            try
            {
                await _unitOfWork.Repository<EmployeeGroupMapping>().InsertAsync(newGroupAccount);
                await _unitOfWork.CommitAsync();
                return newGroupAccount;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> Deleted(Guid id)
        {
            try
            {
                var projectAccount = _unitOfWork.Repository<EmployeeGroupMapping>().GetByIdGuid(id).Result;
                _unitOfWork.Repository<EmployeeGroupMapping>().Delete(projectAccount);
                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CheckAccountInGroup(Guid employId, Guid projectId)
        {
            var projectAccounts = _unitOfWork.Repository<EmployeeGroupMapping>().GetWhere(x => x.GroupId == projectId)
                .Result;
            if (projectAccounts == null)
            {
                return false;
            }

            foreach (var account in projectAccounts)
            {
                if (account.EmployeeId == employId)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<DynamicResponse<AccountResponseModel>> GetAccountsByGroupId(Guid benefitId, PagingModel paging)
        {
            var group = await _unitOfWork.Repository<Group>()
                //.AsQueryable(x => x.Id == id && x.Status == (int)Status.Active)
                .AsQueryable()
                .Include(x => x.Benefit)
                .Where(x => x.BenefitId == benefitId).ToListAsync();
            if (group.Count() == 0) throw new ErrorResponse(StatusCodes.Status404NotFound, 404, "");
            var groupId = await _unitOfWork.Repository<Group>()
                //.AsQueryable(x => x.Id == id && x.Status == (int)Status.Active)
                .AsQueryable()
                .Include(x => x.Benefit)
                .Where(x => x.BenefitId == benefitId)
                .Select(x => x.Id).FirstOrDefaultAsync();
            var groupEmployees = _unitOfWork.Repository<EmployeeGroupMapping>()
                .AsQueryable(x => x.GroupId == groupId)
                .OrderBy(x => x.CreatedAt)
                .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging);
            var listEmployeeId = new List<string>();
            var listAccountId = new List<string>();
            var listAccount = new List<AccountResponseModel>();
            Dictionary<Guid, bool> groupEmployeeReceiveStatus = new Dictionary<Guid, bool>();
            int enterpriseCompanyId = group.FirstOrDefault().Benefit.CompanyId;

            foreach (var groupEmployee in groupEmployees.Item2)
            {
                listEmployeeId.Add(groupEmployee.EmployeeId.ToString());
                if (groupEmployee.IsReceived != null)
                {
                    groupEmployeeReceiveStatus.Add(groupEmployee.EmployeeId, (bool)groupEmployee.IsReceived);
                }
                else
                {
                    groupEmployeeReceiveStatus.Add(groupEmployee.EmployeeId, false);
                }
            }

            foreach (var employeeId in listEmployeeId)
            {
                var employee = _unitOfWork.Repository<Employee>()
                    .AsQueryable(x => x.Id == Guid.Parse(employeeId) && x.Status == (int)Status.Active)
                    .FirstOrDefault();
                if (employee != null)
                {
                    if (groupEmployeeReceiveStatus.ContainsKey(employee.Id))
                    {
                        var isReceived = groupEmployeeReceiveStatus[employee.Id];
                        groupEmployeeReceiveStatus.Add(employee.AccountId, isReceived ? isReceived : false);
                    }

                    listAccountId.Add(employee.AccountId.ToString());
                }
            }

            foreach (var accountId in listAccountId)
            {
                var account = _unitOfWork.Repository<Account>()
                    .AsQueryable(x => x.Id == Guid.Parse(accountId) && x.Status == (int)Status.Active)
                    .ProjectTo<AccountResponseModel>(_mapper.ConfigurationProvider)
                    .FirstOrDefault();
                if (account != null)
                {
                    if (groupEmployeeReceiveStatus.ContainsKey(account.Id))
                    {
                        account.IsReceived = groupEmployeeReceiveStatus[account.Id];
                    }

                    account.CompanyId = enterpriseCompanyId;
                    listAccount.Add(account);
                }
            }

            return new DynamicResponse<AccountResponseModel>()
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                MetaData = new PagingMetaData
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = groupEmployees.Item1
                },
                Data = listAccount
            };
        }
        public async Task<DynamicResponse<UserResponseModel>> GetAllAccountsNotInGroup(UserResponseModel filter, Guid benefitId, PagingModel paging)
        {
            var group = await _unitOfWork.Repository<Group>()
                //.AsQueryable(x => x.Id == id && x.Status == (int)Status.Active)
                .AsQueryable()
                .Include(x => x.Benefit)
                .Where(x => x.BenefitId == benefitId).ToListAsync();
            var employees = _unitOfWork.Repository<Employee>().AsQueryable()
                                       .Include(x => x.Account).Include(x => x.EmployeeGroupMappings)
                                       .Where(w => w.CompanyId == group.FirstOrDefault().Benefit.CompanyId)
                                       .ProjectTo<UserResponseModel>(_mapper.ConfigurationProvider)
                                       .DynamicFilter(filter)
                                       .DynamicSort(paging.Sort, paging.Order)
                                       .PagingQueryable(paging.Page, paging.Size);
            var groupMapping = _unitOfWork.Repository<EmployeeGroupMapping>().AsQueryable(x => x.GroupId == group.FirstOrDefault().Id);
            var listResult = new List<UserResponseModel>();
            foreach(var employee in employees.Item2)
            {
                var check = groupMapping.Any(a => a.EmployeeId == employee.Id);
                if (!check)
                {
                    listResult.Add(employee);
                }
            }
            return new DynamicResponse<UserResponseModel>()
            {
                Code = StatusCodes.Status200OK,
                Message = "...",
                MetaData = new PagingMetaData
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = listResult.Count()
                },
                Data = listResult
            };
        }

        public async Task UpdateBalanceForAccountsInGroup(Guid id, Guid enterpriseId)
        {
            var group = await _unitOfWork.Repository<Group>()
                .AsQueryable(x => x.Id == id && x.Status == (int)Status.Active)
                .Include(x => x.Benefit)
                .FirstOrDefaultAsync();
            if (group == null) throw new ErrorResponse(StatusCodes.Status404NotFound, 404, "");
            var enterprise = await _unitOfWork.Repository<Enterprise>().AsQueryable(x => x.AccountId == enterpriseId)
                .FirstOrDefaultAsync();
            var enterpriseAccount = await _unitOfWork.Repository<Account>()
                .AsQueryable(x => x.Id == enterprise.AccountId && x.Status == (int)Status.Active)
                .Include(x => x.Wallets)
                .FirstOrDefaultAsync();
            if (enterpriseAccount == null) throw new ErrorResponse(StatusCodes.Status404NotFound, 404, "");
            double enterpriseWalletBalance = (double)enterpriseAccount.Wallets.First().Balance;

            var groupEmployees = _unitOfWork.Repository<EmployeeGroupMapping>()
                .AsQueryable(x => x.GroupId == id)
                .OrderBy(x => x.CreatedAt);
            var listEmployeeId = new List<string>();
            var listAccountId = new List<string>();
            Dictionary<Guid, bool> groupEmployeeReceiveStatus = new Dictionary<Guid, bool>();

            foreach (var groupEmployee in groupEmployees)
            {
                listEmployeeId.Add(groupEmployee.EmployeeId.ToString());
                if (groupEmployee.IsReceived != null)
                {
                    groupEmployeeReceiveStatus.Add(groupEmployee.EmployeeId, (bool)groupEmployee.IsReceived);
                }
                else
                {
                    groupEmployeeReceiveStatus.Add(groupEmployee.EmployeeId, false);
                }
            }

            foreach (var employeeId in listEmployeeId)
            {
                var employee = await _unitOfWork.Repository<Employee>()
                    .AsQueryable(x => x.Id == Guid.Parse(employeeId) && x.Status == (int)Status.Active)
                    .FirstOrDefaultAsync();
                if (employee != null)
                {
                    if (groupEmployeeReceiveStatus.ContainsKey(employee.Id))
                    {
                        var isReceived = groupEmployeeReceiveStatus[employee.Id];
                        groupEmployeeReceiveStatus.Add(employee.AccountId, isReceived ? isReceived : false);
                    }

                    listAccountId.Add(employee.AccountId.ToString());
                }
            }

            //check balance
            var totalMoneyNeedTransfer = group.Benefit.UnitPrice * listAccountId.Count;
            if (enterpriseWalletBalance < totalMoneyNeedTransfer)
                throw new ErrorResponse(StatusCodes.Status400BadRequest, 400, "");

            //transfer money to employee
            CultureInfo cul = CultureInfo.GetCultureInfo("vi-VN");
            foreach (var accountId in listAccountId)
            {
                var account = _unitOfWork.Repository<Account>()
                    .AsQueryable(x => x.Id == Guid.Parse(accountId) && x.Status == (int)Status.Active)
                    .Include(x => x.Wallets)
                    .Include(x => x.Employees)
                    .ThenInclude(x => x.EmployeeGroupMappings)
                    .FirstOrDefault();
                if (account != null)
                {
                    var isReceived = account.Employees.First().EmployeeGroupMappings.Where(x => x.GroupId == id).First()
                        .IsReceived;
                    if (isReceived == false || isReceived == null)
                    {
                        account.Wallets.First().Balance += group.Benefit.UnitPrice;
                        enterpriseWalletBalance -= group.Benefit.UnitPrice;
                        //enterpriseAccount.Wallets.First().Used += group.Benefit.UnitPrice;

                        account.Employees.First().EmployeeGroupMappings.Where(x => x.GroupId == id).First()
                            .IsReceived = true;

                        var walletTransactionForReceiver = new Transaction()
                        {
                            Id = Guid.NewGuid(),
                            SenderId = enterpriseId,
                            RecieveId = account.Id,
                            WalletId = account.Wallets.First().Id,
                            Type = (int)WalletTransactionTypeEnums.AddWelfare,
                            Description = "Nhận tiền từ " + group.Benefit.Name,
                            Total = group.Benefit.UnitPrice,
                            CreatedAt = TimeUtils.GetCurrentSEATime(),
                            CompanyId = group.Benefit.CompanyId,
                        };

                        var walletTransactionForSender = new Transaction()
                        {
                            Id = Guid.NewGuid(),
                            SenderId = enterpriseId,
                            RecieveId = account.Id,
                            WalletId = enterpriseAccount.Wallets.First().Id,
                            Type = (int)WalletTransactionTypeEnums.AllocateWelfare,
                            Description = "Chuyển tiền cho " + account.Name,
                            Total = group.Benefit.UnitPrice,
                            CreatedAt = TimeUtils.GetCurrentSEATime(),
                            CompanyId = group.Benefit.CompanyId,
                        };
                        var empNotification = new Notification()
                        {
                            Id = Guid.NewGuid(),
                            AccountId = account.Id,
                            TransactionId = walletTransactionForReceiver.Id,
                            Title = "Bạn đã nhận được tiền từ " + group.Benefit.Name,
                            Description = "Số tiền nhận được: " + String.Format(cul, "{0:c}", group.Benefit.UnitPrice),
                            IsRead = false,
                            CreatedAt = TimeUtils.GetCurrentSEATime(),
                        };

                        // send noti
                        var messaging = FirebaseMessaging.DefaultInstance;
                        if (account.FcmToken != null && !String.IsNullOrWhiteSpace(account.FcmToken))
                        {
                            var response = await messaging.SendAsync(new Message
                            {
                                Token = account.FcmToken,
                                Notification = new FirebaseAdmin.Messaging.Notification
                                {
                                    Title = "Ting Ting",
                                    Body = "Bạn vừa nhận được số tiền: " +
                                           String.Format(cul, "{0:c}", group.Benefit.UnitPrice),
                                },
                            });
                        }

                        try
                        {
                            await _unitOfWork.Repository<Account>().UpdateDetached(account);

                            await _unitOfWork.Repository<Transaction>().InsertAsync(walletTransactionForReceiver);
                            await _unitOfWork.Repository<Transaction>().InsertAsync(walletTransactionForSender);
                            await _unitOfWork.Repository<Notification>().InsertAsync(empNotification);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                }
            }



            enterpriseAccount.Wallets.First().Balance = enterpriseWalletBalance;
            await _unitOfWork.Repository<Account>().UpdateDetached(enterpriseAccount);
            await _unitOfWork.CommitAsync();
            _ = await ScheduleUpdateBalanceForAccountsInGroup(id, enterpriseId);
        }

        public async Task<bool> ScheduleUpdateBalanceForAccountsInGroup(Guid id, Guid enterpriseId)
        {
            var group = await _unitOfWork.Repository<Group>()
                .AsQueryable(x => x.Id == id && x.Status == (int)Status.Active)
                .Include(x => x.Benefit)
                .FirstOrDefaultAsync();
            var now = TimeUtils.GetCurrentSEATime();
            var formattedDateTime = new DateTime(now.Year, now.Month, now.Day, group.TimeFilter.Value.Hour, group.TimeFilter.Value.Minute, group.TimeFilter.Value.Second);
            switch (group.Type)
            {
                case (int)GroupTypes.Daily:
                    DateTimeOffset nowDateTimeOffset = new DateTimeOffset(now);
                    DateTimeOffset dateTimeOffset = new DateTimeOffset(formattedDateTime);
                    if (dateTimeOffset <= nowDateTimeOffset)
                    {
                        // dateTimeOffset = dateTimeOffset.AddDays(1);
                        dateTimeOffset = dateTimeOffset.AddDays(1);
                    }
                    else
                    {
                        dateTimeOffset = dateTimeOffset.AddHours(-7);
                    }
                    
                    if (group.EndDate == null || (group.EndDate != null && group.EndDate > formattedDateTime))
                    {
                        BackgroundJob.Schedule(() => UpdateBalanceForAccountsInGroup(group.Id, enterpriseId),
                            dateTimeOffset);
                        return true;
                    }
                    else if (group.EndDate != null && group.EndDate <= formattedDateTime)
                    {
                        var existedBenefit = _unitOfWork.Repository<Benefit>().FindAsync(x => x.Id == group.Benefit.Id)
                            .Result;
                        existedBenefit.Status = (int)Status.Inactive;
                        await _unitOfWork.Repository<Benefit>().UpdateDetached(existedBenefit);
                        group.Status = (int)Status.Inactive;
                        await _unitOfWork.Repository<Group>().UpdateDetached(group);
                        await _unitOfWork.CommitAsync();
                    }
                    break;
                case (int)GroupTypes.Weekly:
                    if (group.DayFilter == null)
                    {
                        throw new ErrorResponse(StatusCodes.Status400BadRequest, 400, "Please provide Day");
                    }
                    int currentDayOfWeekValue = (int)formattedDateTime.DayOfWeek;
                    int daysToAdd = ((int)group.DayFilter - currentDayOfWeekValue + 7) % 7;
                    DateTime resultDate = formattedDateTime;
                    DateTimeOffset dateTimeOffsetWeekly = new DateTimeOffset(resultDate);
                    if (formattedDateTime > now)
                    {
                        dateTimeOffsetWeekly = new DateTimeOffset(formattedDateTime);
                    }
                    else if (daysToAdd == 0 && formattedDateTime <= now)
                    {
                        dateTimeOffsetWeekly = dateTimeOffsetWeekly.AddDays(7);
                    }

                    dateTimeOffsetWeekly = dateTimeOffsetWeekly.AddHours(-7);
                    if (group.EndDate == null || (group.EndDate != null && group.EndDate > formattedDateTime))
                    {
                        BackgroundJob.Schedule(() => UpdateBalanceForAccountsInGroup(group.Id, enterpriseId),
                            dateTimeOffsetWeekly);
                        return true;
                    }
                    else if (group.EndDate != null && group.EndDate <= formattedDateTime)
                    {
                        var existedBenefit = _unitOfWork.Repository<Benefit>().FindAsync(x => x.Id == group.Benefit.Id)
                            .Result;
                        existedBenefit.Status = (int)Status.Inactive;
                        await _unitOfWork.Repository<Benefit>().UpdateDetached(existedBenefit);
                        group.Status = (int)Status.Inactive;
                        await _unitOfWork.Repository<Group>().UpdateDetached(group);
                        await _unitOfWork.CommitAsync();
                    }
                    break;
                case (int)GroupTypes.Monthly:
                    if (group.DateFilter == null)
                    {
                        throw new ErrorResponse(StatusCodes.Status400BadRequest, 400, "Please provide Date");
                    }
                    DateTime resultDateMonthly = formattedDateTime.AddMonths(1);
                    DateTime formattedDayOfMonthly = new DateTime(resultDateMonthly.Year, resultDateMonthly.Month, (int)group.DateFilter, resultDateMonthly.Hour, resultDateMonthly.Minute, resultDateMonthly.Second);
                    DateTimeOffset nowDateTimeOffsetMonthly = new DateTimeOffset(now);
                    DateTimeOffset nowFormattedDateTimeOffsetMonthly = new DateTimeOffset(formattedDateTime);
                    DateTimeOffset dateTimeOffsetMonthly = new DateTimeOffset(formattedDayOfMonthly);
                    if (nowFormattedDateTimeOffsetMonthly > nowDateTimeOffsetMonthly)
                    {
                        dateTimeOffsetMonthly = new DateTimeOffset(formattedDateTime);
                    }

                    dateTimeOffsetMonthly = dateTimeOffsetMonthly.AddHours(-7);
                    if (group.EndDate == null || (group.EndDate != null && group.EndDate > formattedDateTime))
                    {
                        BackgroundJob.Schedule(() => UpdateBalanceForAccountsInGroup(group.Id, enterpriseId),
                            dateTimeOffsetMonthly);
                        return true;
                    }
                    else if (group.EndDate != null && group.EndDate <= formattedDateTime)
                    {
                        var existedBenefit = _unitOfWork.Repository<Benefit>().FindAsync(x => x.Id == group.Benefit.Id)
                            .Result;
                        existedBenefit.Status = (int)Status.Inactive;
                        await _unitOfWork.Repository<Benefit>().UpdateDetached(existedBenefit);
                        group.Status = (int)Status.Inactive;
                        await _unitOfWork.Repository<Group>().UpdateDetached(group);
                        await _unitOfWork.CommitAsync();
                    }
                    break;
            }
            return false;
        }
        //public async Task<bool> DeleteRange(Guid projectId)
        //{
        //    try
        //    {
        //        var projectAccounts = _unitOfWork.Repository<GroupAccount>().GetWhere()
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //}
    }
}