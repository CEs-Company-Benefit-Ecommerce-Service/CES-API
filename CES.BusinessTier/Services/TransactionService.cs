using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;

namespace CES.BusinessTier.Services;

public interface ITransactionService
{
    Task<BaseResponseViewModel<TransactionResponseModels>> CreateTransaction(TransactionRequestModel transactionRequest);
}

public class TransactionService : ITransactionService
{
    public Task<BaseResponseViewModel<TransactionResponseModels>> CreateTransaction(TransactionRequestModel transactionRequest)
    {
        throw new NotImplementedException();
    }
}