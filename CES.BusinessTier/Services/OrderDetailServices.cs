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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Services
{
    public interface IOrderDetailServices
    {
        Task<DynamicResponse<OrderDetailsResponseModel>> Gets(OrderDetailsResponseModel filter, PagingModel paging);
        Task<bool> Create(List<OrderDetailsRequestModel> request, Guid orderId);
        Task<BaseResponseViewModel<OrderDetailsResponseModel>> GetById(Guid id);
    }
    public class OrderDetailServices : IOrderDetailServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        public OrderDetailServices(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _contextAccessor = httpContextAccessor;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<DynamicResponse<OrderDetailsResponseModel>> Gets(OrderDetailsResponseModel filter, PagingModel paging)
        {
            var orderDetails = _unitOfWork.Repository<OrderDetail>().AsQueryable()
                .ProjectTo<OrderDetailsResponseModel>(_mapper.ConfigurationProvider)
                .DynamicFilter(filter)
                .DynamicSort(paging.Sort, paging.Order)
                .PagingQueryable(paging.Page, paging.Size);

            return new DynamicResponse<OrderDetailsResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                MetaData = new PagingMetaData
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = orderDetails.Item1
                },
                Data = await orderDetails.Item2.ToListAsync(),
            };
        }
        public async Task<BaseResponseViewModel<OrderDetailsResponseModel>> GetById(Guid id)
        {
            var orderDetail = _unitOfWork.Repository<OrderDetail>().FindAsync(x => x.Id == id);

            return new BaseResponseViewModel<OrderDetailsResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = _mapper.Map<OrderDetailsResponseModel>(orderDetail)
            };
        }
        public async Task<bool> Create(List<OrderDetailsRequestModel> requests, Guid orderId)
        {
            foreach (var request in requests)
            {
                var newOrderDetail = _mapper.Map<OrderDetail>(request);
                newOrderDetail.Id = Guid.NewGuid();
                newOrderDetail.OrderId = orderId;
                //newOrderDetail.Quantity = quantity;
                newOrderDetail.CreatedAt = TimeUtils.GetCurrentSEATime();
                try
                {
                    await _unitOfWork.Repository<OrderDetail>().InsertAsync(newOrderDetail);
                    //await _unitOfWork.CommitAsync();
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
