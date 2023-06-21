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
        Task<DynamicResponse<DebtNotesResponseModel>> GetsAsync(DebtNotesResponseModel filter, PagingModel paging);
        Task<DynamicResponse<DebtNotesResponseModel>> GetsWithCompanyAsync(DebtNotesResponseModel filter, PagingModel paging, int companyId);
        BaseResponseViewModel<DebtNotesResponseModel> GetById(Guid id);
        Task<BaseResponseViewModel<DebtNotesResponseModel>> CreateAsync(int companyId);
        Task<BaseResponseViewModel<DebtNotesResponseModel>> DeleteAsync(Guid debtId);
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

        public async Task<DynamicResponse<DebtNotesResponseModel>> GetsAsync(DebtNotesResponseModel filter, PagingModel paging)
        {
            try
            {
                var debts = _unitOfWork.Repository<DebtNotes>().GetAll()
               .ProjectTo<DebtNotesResponseModel>(_mapper.ConfigurationProvider)
               .DynamicFilter(filter)
               .DynamicSort(paging.Sort, paging.Order)
               .PagingQueryable(paging.Page, paging.Size);

                return new DynamicResponse<DebtNotesResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "Ok",
                    MetaData =
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = debts.Item1
                },
                    Data = await debts.Item2.ToListAsync()
                };
            }
            catch (Exception)
            {
                throw;
            }

        }
        public async Task<DynamicResponse<DebtNotesResponseModel>> GetsWithCompanyAsync(DebtNotesResponseModel filter, PagingModel paging, int companyId)
        {
            var debts = _unitOfWork.Repository<DebtNotes>().AsQueryable(x => x.CompanyId == companyId)
                           .ProjectTo<DebtNotesResponseModel>(_mapper.ConfigurationProvider)
                           .DynamicFilter(filter)
                           .DynamicSort(paging.Sort, paging.Order)
                           .PagingQueryable(paging.Page, paging.Size);

            if (debts.Item2 == null)
            {
                return new DynamicResponse<DebtNotesResponseModel>
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

            return new DynamicResponse<DebtNotesResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "Ok",
                MetaData =
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = debts.Item1
                },
                Data = await debts.Item2.ToListAsync()
            };
        }
        public BaseResponseViewModel<DebtNotesResponseModel> GetById(Guid id)
        {
            var debt = _unitOfWork.Repository<DebtNotes>().GetByIdGuid(id);
            if (debt == null)
            {
                return new BaseResponseViewModel<DebtNotesResponseModel>()
                {
                    Code = StatusCodes.Status404NotFound,
                    Message = "Not Found",
                };
            }
            return new BaseResponseViewModel<DebtNotesResponseModel>()
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = _mapper.Map<DebtNotesResponseModel>(debt)
            };
        }
        public async Task<BaseResponseViewModel<DebtNotesResponseModel>> CreateAsync(int companyId)
        {
            var totalOrderResult = _orderServices.GetTotal(companyId).Result;
            var debt = new DebtNotes()
            {
                Id = Guid.NewGuid(),
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
                await _unitOfWork.Repository<DebtNotes>().InsertAsync(debt);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<DebtNotesResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "OK",
                    Data = _mapper.Map<DebtNotesResponseModel>(debt)
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseViewModel<DebtNotesResponseModel>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = "Bad Request" + "||" + ex.Message,
                };
            }
        }
        //public async Task<BaseResponseViewModel<DebtNotesResponseModel>> UpdateAsync(Guid debtId, )
        //{
        //    var existedDebt = _unitOfWork.Repository<DebtNotes>().GetByIdGuid(debtId).Result;
        //    if (existedDebt == null)
        //    {
        //        return new BaseResponseViewModel<DebtNotesResponseModel>()
        //        {
        //            Code = StatusCodes.Status404NotFound,
        //            Message = "Not Found",
        //        };
        //    }

        //}
        public async Task<BaseResponseViewModel<DebtNotesResponseModel>> DeleteAsync(Guid debtId)
        {
            var existedDebt = _unitOfWork.Repository<DebtNotes>().GetByIdGuid(debtId).Result;
            if (existedDebt == null)
            {
                return new BaseResponseViewModel<DebtNotesResponseModel>()
                {
                    Code = StatusCodes.Status404NotFound,
                    Message = "Not Found",
                };
            }
            existedDebt.Status = (int)DebtStatusEnums.Cancel;
            try
            {
                await _unitOfWork.Repository<DebtNotes>().UpdateDetached(existedDebt);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<DebtNotesResponseModel>()
                {
                    Code = StatusCodes.Status204NoContent,
                    Message = "No content",
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseViewModel<DebtNotesResponseModel>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = "Bad Request" + "||" + ex.Message,
                };
            }
        }
    }
}
