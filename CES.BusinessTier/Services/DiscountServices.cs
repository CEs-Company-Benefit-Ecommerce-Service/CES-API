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

namespace CES.BusinessTier.Services;

public interface IDiscountServices
{
    Task<DynamicResponse<DiscountResponse>> GetAllDiscountAsync(DiscountResponse filter, PagingModel paging);
    Task<BaseResponseViewModel<DiscountResponse>> GetDiscountAsync(int id, DiscountResponse filter);
    Task<BaseResponseViewModel<DiscountResponse>> CreateDiscountAsync(DiscountRequest discount);
    Task<BaseResponseViewModel<DiscountResponse>> UpdateDiscountAsync(int id, DiscountRequest discountUpdate);
    Task<BaseResponseViewModel<DiscountResponse>> DeleteDiscountAsync(int id);
}

public class DiscountServices : IDiscountServices
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public DiscountServices(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }
    
    public async Task<DynamicResponse<DiscountResponse>> GetAllDiscountAsync(DiscountResponse filter, PagingModel paging)
    {
        var result = _unitOfWork.Repository<Discount>()
            .AsQueryable(x => x.Status == (int)Status.Active && x.ExpiredDate >= TimeUtils.GetCurrentSEATime())
            .ProjectTo<DiscountResponse>(_mapper.ConfigurationProvider)
            .DynamicFilter(filter)
            .DynamicSort(paging.Sort, paging.Order)
            .PagingQueryable(paging.Page, paging.Size);

        return new DynamicResponse<DiscountResponse>
        {
            Code = StatusCodes.Status200OK,
            Message = "OK",
            MetaData = new PagingMetaData
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = result.Item1
            },
            Data = await result.Item2.ToListAsync(),
        };
    }

    public async Task<BaseResponseViewModel<DiscountResponse>> GetDiscountAsync(int id, DiscountResponse filter)
    {
        var discount = await _unitOfWork.Repository<Discount>().AsQueryable(x => x.Id == id && x.Status == (int)Status.Active && x.ExpiredDate >= TimeUtils.GetCurrentSEATime())
            .ProjectTo<DiscountResponse>(_mapper.ConfigurationProvider)
            .DynamicFilter(filter)
            .FirstOrDefaultAsync();
        if (discount == null) throw new ErrorResponse(StatusCodes.Status404NotFound, 404, "");
        return new BaseResponseViewModel<DiscountResponse>
        {
            Code = StatusCodes.Status200OK,
            Message = "OK",
            Data = discount
        };
    }

    public async Task<BaseResponseViewModel<DiscountResponse>> CreateDiscountAsync(DiscountRequest discount)
    {
        var activeDiscountOfProduct = _unitOfWork.Repository<Discount>()
            .AsQueryable(x => x.ExpiredDate > TimeUtils.GetCurrentSEATime() && x.Status == (int)Status.Active && x.ProductId == discount.ProductId).Any();
        if (activeDiscountOfProduct)
        {
            throw new ErrorResponse(StatusCodes.Status400BadRequest, 400, "Some benefit still available");
        }

        if (discount.ExpiredDate < TimeUtils.GetCurrentSEATime())
        {
            throw new ErrorResponse(StatusCodes.Status400BadRequest, 400, "ExpireDate is invalid");
        }
        var newDiscount = _mapper.Map<Discount>(discount);
        newDiscount.Status = (int)Status.Active;
        newDiscount.CreatedAt = TimeUtils.GetCurrentSEATime();
        await _unitOfWork.Repository<Discount>().InsertAsync(newDiscount);
        await _unitOfWork.CommitAsync();
        return new BaseResponseViewModel<DiscountResponse>
        {
            Code = StatusCodes.Status200OK,
            Message = "OK",
            Data = _mapper.Map<DiscountResponse>(newDiscount),
        };
    }

    public async Task<BaseResponseViewModel<DiscountResponse>> UpdateDiscountAsync(int id, DiscountRequest discountUpdate)
    {
        var discount = await _unitOfWork.Repository<Discount>().AsQueryable(x => x.Id == id && x.Status == (int)Status.Active && x.ExpiredDate >= TimeUtils.GetCurrentSEATime()).FirstOrDefaultAsync();
        if (discount == null) throw new ErrorResponse(StatusCodes.Status404NotFound, 404, "");
        discount.UpdatedAt = TimeUtils.GetCurrentSEATime();
        await _unitOfWork.Repository<Discount>().UpdateDetached(_mapper.Map<DiscountRequest, Discount>(discountUpdate, discount));
        await _unitOfWork.CommitAsync();
        return new BaseResponseViewModel<DiscountResponse>
        {
            Code = StatusCodes.Status200OK,
            Message = "OK",
            Data = _mapper.Map<DiscountResponse>(discount),
        };
    }

    public async Task<BaseResponseViewModel<DiscountResponse>> DeleteDiscountAsync(int id)
    {
        var discount = await _unitOfWork.Repository<Discount>().AsQueryable(x => x.Id == id && x.Status == (int)Status.Active && x.ExpiredDate >= TimeUtils.GetCurrentSEATime()).FirstOrDefaultAsync();
        if (discount == null) throw new ErrorResponse(StatusCodes.Status404NotFound, 404, "");
        discount.Status = (int)Status.Inactive;
        await _unitOfWork.Repository<Discount>().UpdateDetached(_mapper.Map<Discount>(discount));
        await _unitOfWork.CommitAsync();
        return new BaseResponseViewModel<DiscountResponse>
        {
            Code = StatusCodes.Status200OK,
            Message = "OK",
            Data = _mapper.Map<DiscountResponse>(discount),
        };
    }
}