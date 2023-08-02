using System.ComponentModel.Design;
using System.Security.Claims;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.ResponseModels.PaymentModels;
using CES.BusinessTier.Services.VnPayServices;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using LAK.Sdk.Core.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CES.BusinessTier.Services;

public interface ITransactionService
{
    //Task<BaseResponseViewModel<Transaction>> CreateTransaction(TransactionRequestModel transactionRequest);
    Task<bool> CreateTransaction(Transaction request);
    Task<DynamicResponse<TransactionResponseModel>> GetsAsync(TransactionResponseModel filter, PagingModel paging, int? paymentType);
    Task<BaseResponseViewModel<Transaction>> GetById(Guid id);
    Task<BaseResponseViewModel<CreatePaymentResponse>> CreatePayment(CreatePaymentRequest createPaymentRequest);
    Task<bool> ExecuteZaloPayCallBack(double? used, int? status, string? apptransid);
    Task<bool> ExecuteVnPayCallBack(double? used, string? status, string? apptransid);
}

public class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IConfiguration _configuration;

    private readonly IWalletServices _walletServices;

    //private readonly IDebtServices _debtServices;
    public TransactionService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor,
        IWalletServices walletServices, IConfiguration configuration)
    {
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _contextAccessor = contextAccessor;
        _walletServices = walletServices;
        //_debtServices = debtServices;
        _configuration = configuration;
    }

    public async Task<DynamicResponse<TransactionResponseModel>> GetsAsync(TransactionResponseModel filter, PagingModel paging,
        int? paymentType)
    {
        // var transactions = _unitOfWork.Repository<Transaction>().AsQueryable()
        //                     .ProjectTo<TransactionResponseModel>(_mapper.ConfigurationProvider)
        //                    .DynamicFilter(filter)
        //                    .DynamicSort(paging.Sort, paging.Order)
        //                    .PagingQueryable(paging.Page, paging.Size);
        var transactions = _unitOfWork.Repository<Transaction>()
            .ObjectMapper(selector: x => new TransactionResponseModel()
            {
                Id = x.Id,
                Total = x.Total,
                Description = x.Description,
                Type = x.Type,
                CreatedAt = x.CreatedAt,
                SenderId = x.SenderId,
                RecieveId = x.RecieveId,
                OrderId = x.OrderId,
                WalletId = x.WalletId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company.Name,
                PaymentProviderId = x.PaymentProviderId,
                InvoiceId = x.InvoiceId,
                Status = x.Status
            }, include: x => x.Include(x => x.Company))
            .DynamicFilter(filter)
            .DynamicSort(paging.Sort, paging.Order)
            .PagingQueryable(paging.Page, paging.Size);

        if (paymentType == (int)TypeOfGetAllOrder.InComing)
        {
            var result = transactions.Item2;
            result = result.Where(x =>
                x.Type == (int)WalletTransactionTypeEnums.VnPay || x.Type == (int)WalletTransactionTypeEnums.ZaloPay);
            var a = await result.ToListAsync();
            return new DynamicResponse<TransactionResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                MetaData = new PagingMetaData
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = result.Count()
                },
                Data = await result.ToListAsync(),
            };
        }

        return new DynamicResponse<TransactionResponseModel>
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

    public async Task<BaseResponseViewModel<CreatePaymentResponse>> CreatePayment(
        CreatePaymentRequest createPaymentRequest)
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
        IVnPayPaymentStrategy vnPayPaymentStrategy;
        PaymentType paymentType = Enum.Parse<PaymentType>(paymentProvider.Type);
        switch (paymentType)
        {
            case PaymentType.VNPAY:
                vnPayPaymentStrategy = new VnPayPaymentStrategy((double)enterpriseWallet.Used, accountLoginId,
                    _contextAccessor, _unitOfWork, _configuration);
                var resultVnPay = await vnPayPaymentStrategy.ExecutePayment(systemAccount.Id.ToString(),
                    accountLoginId.ToString(), enterpriseWallet.Id.ToString(), enterprise.CompanyId.ToString());
                return new BaseResponseViewModel<CreatePaymentResponse>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "Ok",
                    Data = resultVnPay
                };
            case PaymentType.ZALOPAY:
                paymentStrategy = new ZaloPayPaymentStrategy(paymentProvider.Config, (double)enterpriseWallet.Used,
                    accountLoginId, _contextAccessor, _unitOfWork);
                var result = await paymentStrategy.ExecutePayment(systemAccount.Id.ToString(),
                    accountLoginId.ToString(), enterpriseWallet.Id.ToString(), enterprise.CompanyId.ToString());
                return new BaseResponseViewModel<CreatePaymentResponse>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "Ok",
                    Data = result
                };
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
        var enterprise = _unitOfWork.Repository<Enterprise>().GetWhere(x => x.AccountId == paymentTransaction.SenderId)
            .Result
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

            // Lấy tất cả order đã đặt mà chưa thanh toán của company
            var orders = await _unitOfWork.Repository<Order>().AsQueryable(x =>
                x.CompanyId == enterprise.CompanyId && x.DebtStatus == (int)DebtStatusEnums.New &&
                x.Status == (int)OrderStatusEnums.Complete).ToListAsync();
            foreach (var order in orders)
            {
                order.DebtStatus = (int)DebtStatusEnums.Complete;
                await _unitOfWork.Repository<Order>().UpdateDetached(order);
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

    public async Task<bool> ExecuteVnPayCallBack(double? used, string? status, string? apptransid)
    {
        var paymentTransaction = _unitOfWork.Repository<Transaction>()
            .AsQueryable(x => x.InvoiceId == apptransid)
            .FirstOrDefault();
        var enterprise = _unitOfWork.Repository<Enterprise>().GetWhere(x => x.AccountId == paymentTransaction.SenderId)
            .Result
            .FirstOrDefault();
        var enterpriseAccount = _unitOfWork.Repository<Account>()
            .AsQueryable(x => x.Id == enterprise.AccountId && x.Status == (int)Status.Active)
            .Include(x => x.Wallets)
            .FirstOrDefault();

        if (!status.Equals("00"))
        {
            paymentTransaction.Description = "Thanh toán detb VnPay thất bại";
            paymentTransaction.Status = (int)DebtStatusEnums.Cancel;


            await _unitOfWork.Repository<Transaction>().UpdateDetached(paymentTransaction);
        }
        else
        {
            paymentTransaction.Description = "Thanh toán detb VnPay thành công";
            paymentTransaction.Status = (int)DebtStatusEnums.Complete;

            await _unitOfWork.Repository<Transaction>().UpdateDetached(paymentTransaction);


            var resetResult = _walletServices.ResetAllAfterEAPayment(enterprise.CompanyId).Result;
            if (resetResult.Code != 200)
            {
                return false;
            }

            // Lấy tất cả order đã đặt mà chưa thanh toán của company
            var orders = await _unitOfWork.Repository<Order>().AsQueryable(x =>
                x.CompanyId == enterprise.CompanyId && x.DebtStatus == (int)DebtStatusEnums.New &&
                x.Status == (int)OrderStatusEnums.Complete).ToListAsync();
            foreach (var order in orders)
            {
                order.DebtStatus = (int)DebtStatusEnums.Complete;
                await _unitOfWork.Repository<Order>().UpdateDetached(order);
            }
        }

        return await _unitOfWork.CommitAsync() > 0;
    }
}