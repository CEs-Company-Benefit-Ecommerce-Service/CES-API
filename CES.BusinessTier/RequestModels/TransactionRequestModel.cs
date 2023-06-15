namespace CES.BusinessTier.RequestModels;

public class TransactionRequestModel
{
    public double? Total { get; set; }
    public string? Description { get; set; }
    public int Type { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? WalletId { get; set; }

    // public virtual Order? Order { get; set; }
    // public virtual Wallet? Wallet { get; set; }
}