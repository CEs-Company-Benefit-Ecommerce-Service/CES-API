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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Services
{
    public interface IReceiptServices
    {
        Task<DynamicResponse<ReceiptResponseModel>> GetsAsync(ReceiptResponseModel filter, PagingModel paging);
        Task<DynamicResponse<ReceiptResponseModel>> GetsWithCompanyAsync(ReceiptResponseModel filter, PagingModel paging, int companyId);
        Task<BaseResponseViewModel<ReceiptResponseModel>> Create(ReceiptRequestModel request);

    }
    public class ReceiptServices : IReceiptServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReceiptServices(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<DynamicResponse<ReceiptResponseModel>> GetsAsync(ReceiptResponseModel filter, PagingModel paging)
        {
            var receipts = _unitOfWork.Repository<Receipt>().AsQueryable()
                .ProjectTo<ReceiptResponseModel>(_mapper.ConfigurationProvider)
                .DynamicFilter(filter)
                .DynamicSort(paging.Sort, paging.Order)
                .PagingQueryable(paging.Page, paging.Size);

            return new DynamicResponse<ReceiptResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "Ok",
                MetaData =
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = receipts.Item1
                },
                Data = await receipts.Item2.ToListAsync()
            };
        }
        public async Task<DynamicResponse<ReceiptResponseModel>> GetsWithCompanyAsync(ReceiptResponseModel filter, PagingModel paging, int companyId)
        {
            var receipts = _unitOfWork.Repository<Receipt>().AsQueryable(x => x.CompanyId == companyId)
                           .ProjectTo<ReceiptResponseModel>(_mapper.ConfigurationProvider)
                           .DynamicFilter(filter)
                           .DynamicSort(paging.Sort, paging.Order)
                           .PagingQueryable(paging.Page, paging.Size);

            return new DynamicResponse<ReceiptResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "Ok",
                MetaData =
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = receipts.Item1
                },
                Data = await receipts.Item2.ToListAsync()
            };
        }
        public async Task<BaseResponseViewModel<ReceiptResponseModel>> Create(ReceiptRequestModel request)
        {
            var debt = _unitOfWork.Repository<DebtNotes>().GetByIdGuid((Guid)request.DebtId);

            var receipt = new Receipt()
            {
                Id = Guid.NewGuid(),
                CreatedAt = TimeUtils.GetCurrentSEATime(),
                UpdatedAt = TimeUtils.GetCurrentSEATime(),
                DebtId = request.DebtId,
                ImageUrl = request.ImageUrl,
                PaymentCode = request.PaymentCode,
                Name = "Const string ....",
                Total = debt.Result.Total,
                Status = (int)ReceiptStatusEnums.New,
                CompanyId = debt.Result.CompanyId
            };
            return new BaseResponseViewModel<ReceiptResponseModel>()
            {
                Code = StatusCodes.Status204NoContent,
                Message = "Not Content",
            };
        }
        public async Task<BaseResponseViewModel<ReceiptResponseModel>> UpdateStatus(Guid receiptId, int status)
        {
            var receipt = _unitOfWork.Repository<Receipt>().GetByIdGuid(receiptId).Result;
            if (receipt == null)
            {
                return new BaseResponseViewModel<ReceiptResponseModel>()
                {
                    Code = StatusCodes.Status404NotFound,
                    Message = "Not Found",
                };
            }
            switch (status)
            {
                case (int)ReceiptStatusEnums.Complete:
                    //update debt stauts
                    var debt = _unitOfWork.Repository<DebtNotes>().GetByIdGuid((Guid)receipt.DebtId).Result;
                    debt.Status = (int)DebtStatusEnums.Complete;
                    // update order - debt stauts
                    var order = _unitOfWork.Repository<Order>().AsQueryable(x => x.DebtId == debt.Id);
                    foreach (var item in order)
                    {
                        item.DebtStatus = (int)DebtStatusEnums.Complete;
                    }
                    // update receipt stauts
                    receipt.Status = (int)ReceiptStatusEnums.Complete;
                    try
                    {
                        await _unitOfWork.Repository<DebtNotes>().UpdateDetached(debt);
                        await _unitOfWork.Repository<Receipt>().UpdateDetached(receipt);
                        _unitOfWork.Repository<Order>().UpdateRange(order);
                        await _unitOfWork.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        return new BaseResponseViewModel<ReceiptResponseModel>()
                        {
                            Code = StatusCodes.Status400BadRequest,
                            Message = "Bad request" + "||" + ex.Message,
                        };
                    }
                    break;
                case (int)ReceiptStatusEnums.Cancel:
                    // update receipt stauts
                    receipt.Status = (int)ReceiptStatusEnums.Cancel;
                    try
                    {
                        await _unitOfWork.Repository<Receipt>().UpdateDetached(receipt);
                        await _unitOfWork.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        return new BaseResponseViewModel<ReceiptResponseModel>()
                        {
                            Code = StatusCodes.Status400BadRequest,
                            Message = "Bad request" + "||" + ex.Message,
                        };
                    }
                    break;
                default:
                    break;
            }
            return new BaseResponseViewModel<ReceiptResponseModel>()
            {
                Code = StatusCodes.Status204NoContent,
                Message = "Not Content",
            };
        }
    }
}
