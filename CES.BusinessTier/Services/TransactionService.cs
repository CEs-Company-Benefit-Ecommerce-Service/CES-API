using System.Security.Claims;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.ResponseModels.PaymentModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using LAK.Sdk.Core.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CES.BusinessTier.Services;

public interface ITransactionService
{
    //Task<BaseResponseViewModel<Transaction>> CreateTransaction(TransactionRequestModel transactionRequest);
    Task<bool> CreateTransaction(Transaction request);
    Task<DynamicResponse<Transaction>> GetsAsync(Transaction filter, PagingModel paging);
    Task<BaseResponseViewModel<Transaction>> GetById(Guid id);
    Task<CreatePaymentResponse> CreatePayment(CreatePaymentRequest createPaymentRequest);
    Task<bool> ExecuteZaloPayCallBack(double? used, int? status, string? apptransid);
}

public class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IWalletServices _walletServices;
    public TransactionService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IWalletServices walletServices)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _contextAccessor = contextAccessor;
        _walletServices = walletServices;
    }

    public async Task<DynamicResponse<Transaction>> GetsAsync(Transaction filter, PagingModel paging)
    {
        var transactions = _unitOfWork.Repository<Transaction>().AsQueryable()
                           .DynamicFilter(filter)
                           .DynamicSort(paging.Sort, paging.Order)
                           .PagingQueryable(paging.Page, paging.Size);

        return new DynamicResponse<Transaction>
        {
            Code = StatusCodes.Status200OK,
            Message = "OK",
            MetaData = new PagingMetaData
            {
                Page = paging.Page,
                Size = paging.Size,
                Total = transactions.Item1
            },
            Data = await transactions.Item2.ToListAsync(),
        };
    }

    public async Task<BaseResponseViewModel<Transaction>> GetById(Guid id)
    {
        var transaction = await _unitOfWork.Repository<Transaction>().AsQueryable().Where(x => x.Id == id)
                .ProjectTo<Transaction>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

        return new BaseResponseViewModel<Transaction>
        {
            Code = StatusCodes.Status200OK,
            Message = "OK",
            Data = transaction
        };
    }

    public async Task<CreatePaymentResponse> CreatePayment(CreatePaymentRequest createPaymentRequest)
    {
        var systemAccount = _unitOfWork.Repository<Account>()
            .AsQueryable(x => x.Role == (Roles.SystemAdmin.GetDisplayName()))
            .FirstOrDefault();
        Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value
            .ToString());
        var enterprise = _unitOfWork.Repository<Enterprise>().GetWhere(x => x.AccountId == accountLoginId).Result
            .FirstOrDefault();
        var enterpriseAccount = _unitOfWork.Repository<Account>()
            .AsQueryable(x => x.Id == enterprise.AccountId && x.Status == (int)Status.Active)
            .Include(x => x.Wallets)
            .FirstOrDefault();
        var enterpriseWallet = enterpriseAccount.Wallets.First();
        var paymentProvider = _unitOfWork.Repository<PaymentProvider>()
            .AsQueryable(x => x.Id == createPaymentRequest.PaymentId).FirstOrDefault();
        if (paymentProvider == null) throw new ErrorResponse(StatusCodes.Status404NotFound, 404, "");
        IPaymentStrategy paymentStrategy;
        PaymentType paymentType = Enum.Parse<PaymentType>(paymentProvider.Type);
        switch (paymentType)
        {
            // case PaymentType.VNPAY:
            //     paymentStrategy = new VnPayPaymentStrategy(brandPaymentConfig, _httpContextAccessor.HttpContext,
            //         createPaymentRequest.OrderId, createPaymentRequest.OrderDescription, createPaymentRequest.Amount,
            //         _configuration["VnPayPaymentCallBack:ReturnUrl"], _configuration["Vnpay:HashSecret"]);
            //     return await paymentStrategy.ExecutePayment();
            case PaymentType.ZALOPAY:
                paymentStrategy = new ZaloPayPaymentStrategy(paymentProvider.Config, (double)enterpriseWallet.Used, accountLoginId, _contextAccessor, _unitOfWork);
                return await paymentStrategy.ExecutePayment(systemAccount.Id.ToString(), accountLoginId.ToString(), enterpriseWallet.Id.ToString(), enterprise.CompanyId.ToString());
            // case PaymentType.VIETQR:
            //     paymentStrategy = new VietQRPaymentStrategy(brandPaymentConfig, createPaymentRequest.OrderDescription, createPaymentRequest.Amount);
            //     return await paymentStrategy.ExecutePayment();
            // case PaymentType.CASH:
            //     paymentStrategy = new CashPaymentStrategy(updatedTransaction, _unitOfWork, _distributedCache);
            //     return await paymentStrategy.ExecutePayment();
            default:
                throw new BadHttpRequestException("Không tìm thấy payment provider");
        }
        throw new NotImplementedException();
    }

    public async Task<bool> ExecuteZaloPayCallBack(double? amount, int? status, string? apptransid)
    {
        var paymentTransaction = _unitOfWork.Repository<Transaction>()
            .AsQueryable(x => x.InvoiceId == apptransid)
            .FirstOrDefault();
        var enterprise = _unitOfWork.Repository<Enterprise>().GetWhere(x => x.AccountId == paymentTransaction.SenderId).Result
            .FirstOrDefault();
        var enterpriseAccount = _unitOfWork.Repository<Account>()
            .AsQueryable(x => x.Id == enterprise.AccountId && x.Status == (int)Status.Active)
            .Include(x => x.Wallets)
            .FirstOrDefault();

        if (status != 1)
        {
            paymentTransaction.Description = "Thanh toán detb ZaloPay thất bại";
            paymentTransaction.Status = (int)DebtStatusEnums.Cancel;


            await _unitOfWork.Repository<Transaction>().UpdateDetached(paymentTransaction);
        }
        else
        {
            paymentTransaction.Description = "Thanh toán detb ZaloPay thành công";
            paymentTransaction.Status = (int)DebtStatusEnums.Complete;

            await _unitOfWork.Repository<Transaction>().UpdateDetached(paymentTransaction);


            var resetResult = _walletServices.ResetAllAfterEAPayment(enterprise.CompanyId).Result;
            if (resetResult.Code != 200)
            {
                return false;
            }
        }
        return await _unitOfWork.CommitAsync() > 0;
    }

    public async Task<bool> CreateTransaction(Transaction request)
    {
        try
        {
            await _unitOfWork.Repository<Transaction>().InsertAsync(request);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}