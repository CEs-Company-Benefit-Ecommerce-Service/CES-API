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

namespace CES.BusinessTier.Services
{
    public interface IGroupAccountServices
    {
        Task<bool> Deleted(Guid id);
        Task<EmployeeGroupMapping> Created(Guid employId, Guid projectId);
        public IEnumerable<Group> Gets(PagingModel paging);
        Task<bool> CheckAccountInGroup(Guid employId, Guid projectId);
        Task<DynamicResponse<AccountResponseModel>> GetAccountsByGroupId(Guid id, PagingModel paging);
        Task UpdateBalanceForAccountsInGroup(Guid id);
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

        public async Task<DynamicResponse<AccountResponseModel>> GetAccountsByGroupId(Guid id, PagingModel paging)
        {
            var group = _unitOfWork.Repository<Group>()
                .AsQueryable(x => x.Id == id && x.Status == (int)Status.Active)
                .Include(x => x.Benefit)
                .Any();
            if (!group) throw new ErrorResponse(StatusCodes.Status404NotFound, 404, "");
            var groupEmployees = _unitOfWork.Repository<EmployeeGroupMapping>()
                .AsQueryable(x => x.GroupId == id)
                .OrderBy(x => x.CreatedAt)
                .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging);
            var listEmployeeId = new List<string>();
            var listAccountId = new List<string>();
            var listAccount = new List<AccountResponseModel>();
            Dictionary<Guid, bool> groupEmployeeReceiveStatus = new Dictionary<Guid, bool>();
            int enterpriseCompanyId = Int32.Parse(_contextAccessor.HttpContext?.User.FindFirst("CompanyId").Value);
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

        public async Task UpdateBalanceForAccountsInGroup(Guid id)
        {
            var group = _unitOfWork.Repository<Group>()
                .AsQueryable(x => x.Id == id && x.Status == (int)Status.Active)
                .Include(x => x.Benefit)
                .FirstOrDefault();
            if (group == null) throw new ErrorResponse(StatusCodes.Status404NotFound, 404, "");
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value
                .ToString());
            var enterprise = _unitOfWork.Repository<Enterprise>().GetWhere(x => x.AccountId == accountLoginId).Result
                .FirstOrDefault();
            var enterpriseAccount = _unitOfWork.Repository<Account>()
                .AsQueryable(x => x.Id == enterprise.AccountId && x.Status == (int)Status.Active)
                .Include(x => x.Wallets)
                .FirstOrDefault();
            if (enterpriseAccount == null) throw new ErrorResponse(StatusCodes.Status404NotFound, 404, "");
            double enterpriseWalletBalance = 0;
            foreach (var wallet in enterpriseAccount.Wallets)
            {
                enterpriseWalletBalance = (double)wallet.Balance;
            }

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
                            SenderId = accountLoginId,
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
                            SenderId = accountLoginId,
                            RecieveId = account.Id,
                            WalletId = enterpriseAccount.Wallets.First().Id,
                            Type = (int)WalletTransactionTypeEnums.AddWelfare,
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
                        var response = messaging.SendAsync(new Message
                        {
                            Token = account.FcmToken,
                            Notification = new FirebaseAdmin.Messaging.Notification
                            {
                                Title = "Ting Ting",
                                Body = "Bạn vừa nhận được số tiền: " + String.Format(cul, "{0:c}", group.Benefit.UnitPrice),
                            },
                        });
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