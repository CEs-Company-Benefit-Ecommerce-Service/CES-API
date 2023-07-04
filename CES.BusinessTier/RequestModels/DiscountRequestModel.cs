namespace CES.BusinessTier.RequestModels;

public class DiscountRequestModel
{
    public int? Type { get; set; }
    public double? Amount { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? ExpiredDate { get; set; }
}