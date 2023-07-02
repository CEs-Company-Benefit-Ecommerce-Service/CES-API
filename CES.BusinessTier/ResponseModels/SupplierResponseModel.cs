using CES.DataTier.Models;

namespace CES.BusinessTier.ResponseModels;

public class SupplierResponseModel
{
    public Guid Id { get; set; }
    public string SupplierName { get; set; } = null!;
    public Guid AccountId { get; set; }
    public string? SupplierAddress { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}