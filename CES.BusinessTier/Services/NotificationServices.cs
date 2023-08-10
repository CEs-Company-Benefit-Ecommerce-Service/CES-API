﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using FirebaseAdmin;
//using FirebaseAdmin.Messaging;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using LAK.Sdk.Core.Utilities;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Services
{
    public interface INotificationServices
    {
        Task<DynamicResponse<NotificationResponseModel>> GetsAsync(NotificationResponseModel filter, PagingModel paging);
        Task<BaseResponseViewModel<NotificationResponseModel>> GetAsync(Guid id);
        Task CreateNotificationForEmployeesInActive();
        Task ScheduleNotificationWhenExpireDateIsComming(int type);
    }
    public class NotificationServices : INotificationServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public NotificationServices(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _contextAccessor = httpContextAccessor;
        }

        public async Task<DynamicResponse<NotificationResponseModel>> GetsAsync(NotificationResponseModel filter, PagingModel paging)
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());

            var notifications = _unitOfWork.Repository<DataTier.Models.Notification>().AsQueryable(x => x.AccountId == accountLoginId)
                                .ProjectTo<NotificationResponseModel>(_mapper.ConfigurationProvider)
                                .DynamicFilter<NotificationResponseModel>(filter)
                                .DynamicSort<NotificationResponseModel>(paging.Sort, paging.Order)
                                .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging);

            var accountLoginName = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == accountLoginId).Select(x => x.Name).FirstOrDefaultAsync();

            foreach (var notification in notifications.Item2)
            {
                notification.AccountName = accountLoginName;
            }
            return new DynamicResponse<NotificationResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "Ok",
                MetaData = new PagingMetaData()
                {
                    Total = notifications.Item1
                },
                Data = await notifications.Item2.ToListAsync()
            };
        }
        public async Task<BaseResponseViewModel<NotificationResponseModel>> GetAsync(Guid id)
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());

            var accountLoginName = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == accountLoginId).Select(x => x.Name).FirstOrDefaultAsync();

            var notification = await _unitOfWork.Repository<DataTier.Models.Notification>().AsQueryable(x => x.Id == id).Include(x => x.Transaction).Include(x => x.Order).FirstOrDefaultAsync();

            var result = _mapper.Map<NotificationResponseModel>(notification);
            result.AccountName = accountLoginName;

            notification.IsRead = true;
            notification.UpdatedAt = TimeUtils.GetCurrentSEATime();
            try
            {
                await _unitOfWork.Repository<DataTier.Models.Notification>().UpdateDetached(notification);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception ex)
            {
                return new BaseResponseViewModel<NotificationResponseModel>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = "Update IsRead Failed!",
                    Data = result
                };
            }
            return new BaseResponseViewModel<NotificationResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = result
            };
        }

        public async Task CreateNotificationForEmployeesInActive()
        {
            var messaging = FirebaseMessaging.DefaultInstance;
            var employees = await _unitOfWork.Repository<Employee>().AsQueryable(x => x.Status == (int)Status.Active).ToListAsync();
            foreach (var employee in employees)
            {
                var order = await _unitOfWork.Repository<Order>().AsQueryable(x => x.EmployeeId == employee.Id).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
                if (order == null)
                {
                    var account = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == employee.AccountId).FirstOrDefaultAsync();
                    if(account.FcmToken != null)
                    {
                        var response = messaging.SendAsync(new Message
                        {
                            Token = account.FcmToken,
                            Notification = new FirebaseAdmin.Messaging.Notification
                            {
                                Title = "Trở lại mua hàng nào bạn ơi",
                                Body = "Bạn đã không mua hàng đã lâu, nhiều món hàng đang chờ bạn"
                            },
                        });
                        if (response.Result == null)
                        {
                            System.Console.WriteLine("Send noti failed");
                        }
                    }
                } else if (order.CreatedAt < TimeUtils.GetCurrentSEATime().AddDays(-5))
                {
                    //var empNotification = new DataTier.Models.Notification()
                    //{
                    //    Id = Guid.NewGuid(),
                    //    Title = "Quay lại mu",
                    //    Description = "Đơn hàng của bạn đã chuyển sang trạng thái: " + stringStatus,
                    //    OrderId = existedOrder.Id,
                    //    IsRead = false,
                    //    CreatedAt = TimeUtils.GetCurrentSEATime(),
                    //    AccountId = accountEmp.Id
                    //};
                    var account = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == employee.AccountId).FirstOrDefaultAsync();
                    var response = messaging.SendAsync(new Message
                    {
                        Token = account.FcmToken,
                        Notification = new FirebaseAdmin.Messaging.Notification
                        {
                            Title = "Trở lại mua hàng nào bạn ơi",
                            Body = "Bạn đã không mua hàng đã lâu, nhiều món hàng đang chờ bạn"
                        },
                    });
                    if (response.Result == null)
                    {
                        System.Console.WriteLine("Send noti failed");
                    }
                }
            }
        }

        public async Task ScheduleNotificationWhenExpireDateIsComming(int type)
        {
            var lastDateOfCurrentMonth = TimeUtils.GetLastAndFirstDateInCurrentMonth().Item2.GetEndOfDate();
            var companies = await _unitOfWork.Repository<Company>()
                .AsQueryable(x => x.ExpiredDate <= lastDateOfCurrentMonth && x.Status == (int)Status.Active)
                .Include(x => x.Enterprises)
                .ToListAsync();
            switch (type)
            {
                case (int)ExpireDateNotifices.First:
                case (int)ExpireDateNotifices.Second:
                    foreach (var company in companies)
                    {
                        var enterpriseAccountId = company.Enterprises.First().AccountId;
                        var eaNotification = new DataTier.Models.Notification()
                        {
                            Id = Guid.NewGuid(),
                            Title = "Sắp đến hạn thanh toán",
                            Description = "Sắp đến hạn thanh toán, vui lòng thanh toán trước khi hết hạn để không gián đoạn quá trình sử dụng.",
                            IsRead = false,
                            CreatedAt = TimeUtils.GetCurrentSEATime(),
                            AccountId = enterpriseAccountId
                        };
                        await _unitOfWork.Repository<DataTier.Models.Notification>().UpdateDetached(eaNotification);
                    }
                    break;
                case (int)ExpireDateNotifices.Third:
                    foreach (var company in companies)
                    {
                        var enterpriseAccountId = company.Enterprises.First().AccountId;
                        var eaNotification = new DataTier.Models.Notification()
                        {
                            Id = Guid.NewGuid(),
                            Title = "Quá hạn thanh toán",
                            Description = "Đã quá hạn thanh toán, vui lòng thanh toán để tiếp tục sử dụng.",
                            IsRead = false,
                            CreatedAt = TimeUtils.GetCurrentSEATime(),
                            AccountId = enterpriseAccountId
                        };
                        // var account = await _unitOfWork.Repository<Account>()
                        //     .AsQueryable(x => x.Id == enterpriseAccountId).FirstOrDefaultAsync();
                        // account.Status = (int)Status.Inactive;
                        //
                        // await _unitOfWork.Repository<Account>().UpdateDetached(account);
                        await _unitOfWork.Repository<DataTier.Models.Notification>().UpdateDetached(eaNotification);
                    }
                    break;
                case (int)ExpireDateNotifices.Current:
                    foreach (var company in companies)
                    {
                        if (((DateTime)company.ExpiredDate).Date == lastDateOfCurrentMonth.Date)
                        {
                            var enterpriseAccountId = company.Enterprises.First().AccountId;
                            var eaNotification = new DataTier.Models.Notification()
                            {
                                Id = Guid.NewGuid(),
                                Title = "Đã đến hạn thanh toán",
                                Description = "Đã đến hạn thanh toán, vui lòng thanh toán trong hôm nay để không gián đoạn quá trình sử dụng.",
                                IsRead = false,
                                CreatedAt = TimeUtils.GetCurrentSEATime(),
                                AccountId = enterpriseAccountId
                            };
                            await _unitOfWork.Repository<DataTier.Models.Notification>().UpdateDetached(eaNotification);
                        }
                    }
                    break;
                default:
                    break;
            }

            await _unitOfWork.CommitAsync();
        }
    }
}
