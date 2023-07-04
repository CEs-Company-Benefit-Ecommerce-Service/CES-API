namespace CES.BusinessTier.RequestModels;

public class SupplierRequestModel
{
    public string SupplierName { get; set; } = null!;
    public Guid AccountId { get; set; }
    public string? SupplierAddress { get; set; }
}