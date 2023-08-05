namespace CES.BusinessTier.RequestModels;

public class GroupUpdateModel
{
    public string? Name { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public Guid? BenefitId { get; set; }
    public int? Status { get; set; }
}