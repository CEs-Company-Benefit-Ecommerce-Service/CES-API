using CES.BusinessTier.Utilities;

namespace CES.BusinessTier.ResponseModels.PaymentModels;

public class OrderData
{
    public Guid Id { get; set; }
    public TransactionStatus TransactionStatus { get; set; }
}