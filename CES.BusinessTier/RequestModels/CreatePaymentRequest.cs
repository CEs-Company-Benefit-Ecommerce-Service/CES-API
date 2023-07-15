namespace CES.BusinessTier.RequestModels;

public class CreatePaymentRequest
{
    public double Used { get; set; }
    public Guid AccountId { get; set; }
    public Guid PaymentId { get; set; }
}