using CES.BusinessTier.ResponseModels;

namespace CES.BusinessTier.Services;

public interface IPaymentStrategy
{
    Task<CreatePaymentResponse> ExecutePayment(string? systemAccountId, string? accountLoginId, string? walletId, string? companyId);
}