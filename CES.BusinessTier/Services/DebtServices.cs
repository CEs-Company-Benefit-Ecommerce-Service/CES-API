using AutoMapper;
using AutoMapper.QueryableExtensions;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using LAK.Sdk.Core.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Services
{
    public interface IDebtServices
    {
        Task<DynamicResponse<DebtTicketResponseModel>> GetsAsync(DebtTicketResponseModel filter, PagingModel paging);
        Task<DynamicResponse<DebtTicketResponseModel>> GetsWithCompanyAsync(DebtTicketResponseModel filter, PagingModel paging, int companyId);
        BaseResponseViewModel<DebtTicketResponseModel> GetById(Guid id);
        Task<BaseResponseViewModel<DebtTicketResponseModel>> CreateAsync(int companyId);
        Task<BaseResponseViewModel<DebtTicketResponseModel>> DeleteAsync(Guid debtId);
    }
    public class DebtServices : IDebtServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IOrderServices _orderServices;

        public DebtServices(IUnitOfWork unitOfWork, IMapper mapper, IOrderServices orderServices)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _orderServices = orderServices;
        }

        public async Task<DynamicResponse<DebtTicketResponseModel>> GetsAsync(DebtTicketResponseModel filter, PagingModel paging)
        {
            try
            {
                var debts = _unitOfWork.Repository<DebtTicket>().GetAll()
               .ProjectTo<DebtTicketResponseModel>(_mapper.ConfigurationProvider)
               .DynamicFilter(filter)
               .DynamicSort(paging.Sort, paging.Order)
               .PagingQueryable(paging.Page, paging.Size);

                return new DynamicResponse<DebtTicketResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "Ok",
                    MetaData = new PagingMetaData
                    {
                        Page = paging.Page,
                        Size = paging.Size,
                        Total = debts.Item1
                    },
                    Data = await debts.Item2.ToListAsync()
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        public async Task<DynamicResponse<DebtTicketResponseModel>> GetsWithCompanyAsync(DebtTicketResponseModel filter, PagingModel paging, int companyId)
        {
            var debts = _unitOfWork.Repository<DebtTicket>().AsQueryable(x => x.CompanyId == companyId)
                           .ProjectTo<DebtTicketResponseModel>(_mapper.ConfigurationProvider)
                           .DynamicFilter(filter)
                           .DynamicSort(paging.Sort, paging.Order)
                           .PagingQueryable(paging.Page, paging.Size);

            if (debts.Item2 == null)
            {
                return new DynamicResponse<DebtTicketResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "Ok",
                    MetaData =
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = debts.Item1
                },
                };
            }

            return new DynamicResponse<DebtTicketResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "Ok",
                MetaData = new PagingMetaData
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = debts.Item1
                },
                Data = await debts.Item2.ToListAsync()
            };
        }
        public BaseResponseViewModel<DebtTicketResponseModel> GetById(Guid id)
        {
            var debt = _unitOfWork.Repository<DebtTicket>().GetByIdGuid(id).Result;
            if (debt == null)
            {
                return new BaseResponseViewModel<DebtTicketResponseModel>()
                {
                    Code = StatusCodes.Status404NotFound,
                    Message = "Not Found",
                };
            }
            return new BaseResponseViewModel<DebtTicketResponseModel>()
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = _mapper.Map<DebtTicketResponseModel>(debt)
            };
        }
        public async Task<BaseResponseViewModel<DebtTicketResponseModel>> CreateAsync(int companyId)
        {
            var totalOrderResult = _orderServices.GetTotal(companyId).Result;
            var debt = new DebtTicket()
            {
                CompanyId = companyId,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
                Name = "Tat toan ....",
                InfoPayment = "MoMo: .... \nSTK: ...",
                Status = (int)DebtStatusEnums.New,
                Total = totalOrderResult.Total,
                UpdatedAt = TimeUtils.GetCurrentSEATime(),

                //OrderId = totalOrderResult.OrderIds.ToString(),
            };
            var updateOrder = new List<Order>();
            foreach (var item in totalOrderResult.OrderIds)
            {
                var order = _unitOfWork.Repository<Order>().GetByIdGuid(item).Result;
                order.DebtId = debt.Id;
                updateOrder.Add(order);
            }
            try
            {
                _unitOfWork.Repository<Order>().UpdateRange(updateOrder.AsQueryable());
                await _unitOfWork.Repository<DebtTicket>().InsertAsync(debt);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<DebtTicketResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "OK",
                    Data = _mapper.Map<DebtTicketResponseModel>(debt)
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseViewModel<DebtTicketResponseModel>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = "Bad Request" + "||" + ex.Message,
                };
            }
        }
        //public async Task<BaseResponseViewModel<DebtTicketResponseModel>> UpdateAsync(Guid debtId, )
        //{
        //    var existedDebt = _unitOfWork.Repository<DebtTicket>().GetByIdGuid(debtId).Result;
        //    if (existedDebt == null)
        //    {
        //        return new BaseResponseViewModel<DebtTicketResponseModel>()
        //        {
        //            Code = StatusCodes.Status404NotFound,
        //            Message = "Not Found",
        //        };
        //    }

        //}
        public async Task<BaseResponseViewModel<DebtTicketResponseModel>> DeleteAsync(Guid debtId)
        {
            var existedDebt = _unitOfWork.Repository<DebtTicket>().GetByIdGuid(debtId).Result;
            if (existedDebt == null)
            {
                return new BaseResponseViewModel<DebtTicketResponseModel>()
                {
                    Code = StatusCodes.Status404NotFound,
                    Message = "Not Found",
                };
            }
            existedDebt.Status = (int)DebtStatusEnums.Cancel;
            try
            {
                await _unitOfWork.Repository<DebtTicket>().UpdateDetached(existedDebt);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<DebtTicketResponseModel>()
                {
                    Code = StatusCodes.Status204NoContent,
                    Message = "No content",
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseViewModel<DebtTicketResponseModel>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = "Bad Request" + "||" + ex.Message,
                };
            }
        }
    }
}
