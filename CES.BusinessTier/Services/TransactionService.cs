using AutoMapper;
using AutoMapper.QueryableExtensions;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.UnitOfWork;
using CES.DataTier.Models;
using LAK.Sdk.Core.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CES.BusinessTier.Services;

public interface ITransactionService
{
    //Task<BaseResponseViewModel<TransactionResponseModels>> CreateTransaction(TransactionRequestModel transactionRequest);
    Task<bool> CreateTransaction(Transaction request);
    Task<DynamicResponse<TransactionResponseModels>> GetsAsync(TransactionResponseModels filter, PagingModel paging);
    Task<BaseResponseViewModel<TransactionResponseModels>> GetById(Guid id);
}

public class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    public TransactionService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<DynamicResponse<TransactionResponseModels>> GetsAsync(TransactionResponseModels filter, PagingModel paging)
    {
        var transactions = _unitOfWork.Repository<Transaction>().AsQueryable()
                           .ProjectTo<TransactionResponseModels>(_mapper.ConfigurationProvider)
                           .DynamicFilter(filter)
                           .DynamicSort(paging.Sort, paging.Order)
                           .PagingQueryable(paging.Page, paging.Size);

        return new DynamicResponse<TransactionResponseModels>
        {
            Code = StatusCodes.Status200OK,
            Message = "OK",
            MetaData = new PagingMetaData
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = transactions.Item1
            },
            Data = await transactions.Item2.ToListAsync(),
        };
    }

    public async Task<BaseResponseViewModel<TransactionResponseModels>> GetById(Guid id)
    {
        var transaction = await _unitOfWork.Repository<Transaction>().AsQueryable().Where(x => x.Id == id)
                .ProjectTo<TransactionResponseModels>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

        return new BaseResponseViewModel<TransactionResponseModels>
        {
            Code = StatusCodes.Status200OK,
            Message = "OK",
            Data = transaction
        };
    }
    public async Task<bool> CreateTransaction(Transaction request)
    {
        try
        {
            await _unitOfWork.Repository<Transaction>().InsertAsync(request);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}