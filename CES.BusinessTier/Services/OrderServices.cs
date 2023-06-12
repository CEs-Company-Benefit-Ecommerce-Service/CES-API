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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Services
{
    public interface IOrderServices
    {
        Task<DynamicResponse<OrderResponseModel>> GetsAsync(OrderResponseModel filter, PagingModel paging);
        Task<BaseResponseViewModel<OrderResponseModel>> UpdateOrderStatus(Guid orderId, int status);
        Task<BaseResponseViewModel<OrderResponseModel>> CreateOrder(List<OrderDetailsRequestModel> orderDetails, string? note);
        Task<BaseResponseViewModel<OrderResponseModel>> GetById(Guid id);
    }
    public class OrderServices : IOrderServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderDetailServices _orderDetailServices;
        private readonly IProductService _productServices;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public OrderServices(IUnitOfWork unitOfWork, IMapper mapper, IOrderDetailServices orderDetailServices, IProductService productServices, IHttpContextAccessor httpContextAccessor)
        {
            _mapper = mapper;
            _contextAccessor = httpContextAccessor;
            _unitOfWork = unitOfWork;
            _orderDetailServices = orderDetailServices;
            _productServices = productServices;
        }

        public async Task<DynamicResponse<OrderResponseModel>> GetsAsync(OrderResponseModel filter, PagingModel paging)
        {
            var orderDetails = _unitOfWork.Repository<Order>().AsQueryable()
                           .ProjectTo<OrderResponseModel>(_mapper.ConfigurationProvider)
                           .DynamicFilter(filter)
                           .DynamicSort(paging.Sort, paging.Order)
                           .PagingQueryable(paging.Page, paging.Size);

            return new DynamicResponse<OrderResponseModel>
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

        public async Task<BaseResponseViewModel<OrderResponseModel>> GetById(Guid id)
        {
            var orderDetail = await _unitOfWork.Repository<Order>().AsQueryable().Include(x => x.OrderDetails).Include(x => x.Account).Where(x => x.Id == id).FirstOrDefaultAsync();

            return new BaseResponseViewModel<OrderResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = _mapper.Map<OrderResponseModel>(orderDetail)
            };
        }
        public async Task<BaseResponseViewModel<OrderResponseModel>> UpdateOrderStatus(Guid orderId, int status)
        {
            try
            {
                var existedOrder = _unitOfWork.Repository<Order>().FindAsync(x => x.Id == orderId).Result;
                if (existedOrder == null)
                {
                    return new BaseResponseViewModel<OrderResponseModel>
                    {
                        Code = StatusCodes.Status404NotFound,
                        Message = "Not found",
                    };
                }
                existedOrder.Status = status;

                await _unitOfWork.Repository<Order>().UpdateDetached(existedOrder);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<OrderResponseModel>
                {
                    Code = StatusCodes.Status204NoContent,
                    Message = "No content",
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<OrderResponseModel>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = "Bad request",
                };
            }
        }

        public async Task<BaseResponseViewModel<OrderResponseModel>> CreateOrder(List<OrderDetailsRequestModel> orderDetails, string? note)
        {
            // get logined account
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
            var accountLogin = _unitOfWork.Repository<Account>().FindAsync(x => x.Id == accountLoginId);
            var companyAddress = _unitOfWork.Repository<Company>().GetWhere(x => x.Id == accountLogin.Result.CompanyId).Result.Select(x => x.Address).FirstOrDefault();

            #region caculate orderDetail price + total
            foreach (var orderDetail in orderDetails)
            {
                var product = _productServices.GetProductAsync((Guid)orderDetail.ProductId, new ProductResponseModel());
                orderDetail.Price = orderDetail.Quantity * product.Result.Data.Price;
            }

            var total = orderDetails.Select(x => x.Price).Sum();
            #endregion
            try
            {
                // create order
                var newOrder = new Order()
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = TimeUtils.GetCurrentSEATime(),
                    AccountId = accountLoginId,
                    Status = 1,
                    Total = total,
                    Address = companyAddress,
                    Note = note,
                };
                await _unitOfWork.Repository<Order>().InsertAsync(newOrder);

                // create order details
                if (!await _orderDetailServices.Create(orderDetails, newOrder.Id))
                {
                    return new BaseResponseViewModel<OrderResponseModel>
                    {
                        Code = StatusCodes.Status400BadRequest,
                        Message = "Create order details failed",
                    };
                }

                // create transaction
                // to do .///

                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<OrderResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "OK",
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<OrderResponseModel>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = "Something was wrong!",
                };

            }
        }
    }
}
