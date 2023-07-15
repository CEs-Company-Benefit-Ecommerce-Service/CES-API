using CES.BusinessTier.Utilities;

namespace CES.BusinessTier.ResponseModels;

public class CreatePaymentResponse
{
    public string? Message { get; set; }
    public string? Url { get; set; }
    public CreatePaymentReturnType DisplayType { get; set; }
}