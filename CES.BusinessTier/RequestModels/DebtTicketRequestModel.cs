namespace CES.BusinessTier.RequestModels;

public class DebtTicketRequestModel
{
    public string? Name { get; set; }
    public double Total { get; set; }
    public string? InfoPayment { get; set; }
    public int CompanyId { get; set; }
}