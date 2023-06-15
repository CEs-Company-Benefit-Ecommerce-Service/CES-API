using CES.DataTier.Models;

namespace CES.BusinessTier.ResponseModels;

public class TransactionResponseModels
{
    public Guid? Id { get; set; }
    public double? Total { get; set; }
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? Type { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? WalletId { get; set; }

    public OrderResponseModel? Order { get; set; }
    // public virtual Wallet? Wallet { get; set; }
}