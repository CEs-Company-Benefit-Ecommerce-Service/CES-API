using AutoMapper;
using AutoMapper.QueryableExtensions;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using FirebaseAdmin;
//using FirebaseAdmin.Messaging;
using FirebaseAdmin.Auth;
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
    }
}
