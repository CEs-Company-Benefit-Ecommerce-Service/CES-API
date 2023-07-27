namespace CES.BusinessTier.RequestModels;

public class DiscountRequest
{
    public int? Type { get; set; }
    public double? Amount { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? ExpiredDate { get; set; }
    public Guid? ProductId { get; set; }
}