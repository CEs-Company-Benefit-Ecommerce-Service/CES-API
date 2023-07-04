namespace CES.BusinessTier.ResponseModels;

public class UserResponseModel
{
    public Guid? Id { get; set; }
    public int? CompanyId { get; set; }
    public Guid? AccountId { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierAddress { get; set; }
    public int? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public AccountResponseModel? Account { get; set; }

}