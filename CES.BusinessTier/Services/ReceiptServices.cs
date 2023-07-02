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
    public interface IInvokeServices
    {
        Task<DynamicResponse<InvokeResponseModel>> GetsAsync(InvokeResponseModel filter, PagingModel paging);
        Task<DynamicResponse<InvokeResponseModel>> GetsWithCompanyAsync(InvokeResponseModel filter, PagingModel paging, int companyId);
        Task<BaseResponseViewModel<InvokeResponseModel>> Create(InvokeRequestModel request);
        Task<BaseResponseViewModel<InvokeResponseModel>> UpdateStatus(Guid receiptId, int status);

    }
    public class InvokeServices : IInvokeServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public InvokeServices(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<DynamicResponse<InvokeResponseModel>> GetsAsync(InvokeResponseModel filter, PagingModel paging)
        {
            List<InvokeResponseModel> a = new List<InvokeResponseModel>();
            // var receipts = _unitOfWork.Repository<Invoke>().AsQueryable()
            //     .ProjectTo<InvokeResponseModel>(_mapper.ConfigurationProvider)
            //     .DynamicFilter(filter)
            //     .DynamicSort(paging.Sort, paging.Order)
            //     .PagingQueryable(paging.Page, paging.Size);

            return new DynamicResponse<InvokeResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "Ok",
                MetaData = new PagingMetaData
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = 1
                },
                Data = a
            };
        }
        public async Task<DynamicResponse<InvokeResponseModel>> GetsWithCompanyAsync(InvokeResponseModel filter, PagingModel paging, int companyId)
        {
            List<InvokeResponseModel> a = new List<InvokeResponseModel>();
            // var receipts = _unitOfWork.Repository<Invoke>().AsQueryable(x => x.Debt.CompanyId == companyId)
            //                .ProjectTo<InvokeResponseModel>(_mapper.ConfigurationProvider)
            //                .DynamicFilter(filter)
            //                .DynamicSort(paging.Sort, paging.Order)
            //                .PagingQueryable(paging.Page, paging.Size);

            return new DynamicResponse<InvokeResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "Ok",
                MetaData = new PagingMetaData
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = 1
                },
                Data = a
            };
        }
        public async Task<BaseResponseViewModel<InvokeResponseModel>> Create(InvokeRequestModel request)
        {
            var debt = _unitOfWork.Repository<DebtTicket>().GetById((int)request.DebtId);

            // var receipt = new Invoke()
            // {
            //     CreatedAt = TimeUtils.GetCurrentSEATime(),
            //     UpdatedAt = TimeUtils.GetCurrentSEATime(),
            //     DebtId = (int)request.DebtId,
            //     ImageUrl = request.ImageUrl,
            //     Name = "Const string ....",
            //     Total = debt.Result.Total,
            //     Status = (int)ReceiptStatusEnums.New,
            // };
            try
            {
                // await _unitOfWork.Repository<Invoke>().InsertAsync(receipt);
                await _unitOfWork.CommitAsync();
                return new BaseResponseViewModel<InvokeResponseModel>()
                {
                    Code = StatusCodes.Status204NoContent,
                    Message = "Not Content",
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseViewModel<InvokeResponseModel>()
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = "Bad request",
                };
            }

        }
        public async Task<BaseResponseViewModel<InvokeResponseModel>> UpdateStatus(Guid receiptId, int status)
        {
            InvokeResponseModel a = new InvokeResponseModel();
            // var receipt = _unitOfWork.Repository<Invoke>().GetByIdGuid(receiptId).Result;
            // if (receipt == null)
            // {
            //     return new BaseResponseViewModel<InvokeResponseModel>()
            //     {
            //         Code = StatusCodes.Status404NotFound,
            //         Message = "Not Found",
            //     };
            // }
            switch (status)
            {
                case (int)ReceiptStatusEnums.Complete:
                    //update debt stauts
                    // var debt = _unitOfWork.Repository<DebtTicket>().GetById((int)receipt.DebtId).Result;
                    // debt.Status = (int)DebtStatusEnums.Complete;
                    // // update order - debt stauts
                    // var order = _unitOfWork.Repository<Order>().AsQueryable(x => x.DebtId == debt.Id);
                    // foreach (var item in order)
                    // {
                    //     item.DebtStatus = (int)DebtStatusEnums.Complete;
                    // }
                    // // update receipt stauts
                    // receipt.Status = (int)ReceiptStatusEnums.Complete;
                    try
                    {
                        // await _unitOfWork.Repository<DebtTicket>().UpdateDetached(debt);
                        // await _unitOfWork.Repository<Invoke>().UpdateDetached(receipt);
                        // _unitOfWork.Repository<Order>().UpdateRange(order);
                        await _unitOfWork.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        return new BaseResponseViewModel<InvokeResponseModel>()
                        {
                            Code = StatusCodes.Status400BadRequest,
                            Message = "Bad request" + "||" + ex.Message,
                        };
                    }
                    break;
                case (int)ReceiptStatusEnums.Cancel:
                    // update receipt stauts
                    // receipt.Status = (int)ReceiptStatusEnums.Cancel;
                    try
                    {
                        // await _unitOfWork.Repository<Invoke>().UpdateDetached(receipt);
                        await _unitOfWork.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        return new BaseResponseViewModel<InvokeResponseModel>()
                        {
                            Code = StatusCodes.Status400BadRequest,
                            Message = "Bad request" + "||" + ex.Message,
                        };
                    }
                    break;
                default:
                    break;
            }
            return new BaseResponseViewModel<InvokeResponseModel>()
            {
                Code = StatusCodes.Status204NoContent,
                Message = "Not Content",
            };
        }
    }
}
