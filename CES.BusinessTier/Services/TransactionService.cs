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
    Task<bool> CreateTransaction(TransactionRequestModel request);
    Task<DynamicResponse<TransactionResponseModel>> GetsAsync(TransactionResponseModel filter, PagingModel paging, int? paymentType);
    Task<BaseResponseViewModel<Transaction>> GetById(Guid id);
    Task<BaseResponseViewModel<TransactionResponseModel>> UpdateAsync(Guid id, TransactionUpdateModel request);
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
            var result = _unitOfWork.Repository<Transaction>()
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
                }, include: x => x.Include(x => x.Company), predicate: x => x.Type == (int)WalletTransactionTypeEnums.VnPay || x.Type == (int)WalletTransactionTypeEnums.ZaloPay || x.Type == (int)WalletTransactionTypeEnums.Bank)
                .DynamicFilter(filter)
                .DynamicSort(paging.Sort, paging.Order)
                .PagingQueryable(paging.Page, paging.Size);
            return new DynamicResponse<TransactionResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                MetaData = new PagingMetaData
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = result.Item1
                },
                Data = await result.Item2.ToListAsync(),
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

    public async Task<BaseResponseViewModel<TransactionResponseModel>> UpdateAsync(Guid id, TransactionUpdateModel request)
    {
        try
        {
            var transaction = await _unitOfWork.Repository<Transaction>().AsQueryable(x => x.Id == id).FirstOrDefaultAsync();
            if (transaction == null) throw new ErrorResponse(StatusCodes.Status404NotFound, 404, "");
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value;
            if (role == Roles.EnterpriseAdmin.GetDisplayName())
            {
                transaction.ImageUrl = request.ImageUrl;
                transaction.UpdatedAt = TimeUtils.GetCurrentSEATime();
                await _unitOfWork.Repository<Transaction>().UpdateDetached(transaction);
                await _unitOfWork.CommitAsync();
                return new BaseResponseViewModel<TransactionResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "OK",
                    Data = _mapper.Map<TransactionResponseModel>(transaction),
                };
            }
            transaction.UpdatedAt = TimeUtils.GetCurrentSEATime();
            transaction.Status = request.Status;
            await _unitOfWork.Repository<Transaction>().UpdateDetached(transaction);
            //await _unitOfWork.Repository<Transaction>().UpdateDetached(_mapper.Map<TransactionUpdateModel, Transaction>(request, transaction));

            if (transaction.Status == (int)OrderStatusEnums.Complete)
            {
                var result = _walletServices.ResetAllAfterEAPayment((int)transaction.CompanyId).Result;
                if (result.Code != 200)
                {
                    return new BaseResponseViewModel<TransactionResponseModel>
                    {
                        Code = StatusCodes.Status400BadRequest,
                        Message = "Reset EA failed",
                        Data = _mapper.Map<TransactionResponseModel>(transaction),
                    };
                }
                // lấy ra order có debt status = progressing và thuộc company thanh toán
                var ordersPayment = await _unitOfWork.Repository<Order>().AsQueryable(x => x.DebtStatus == (int)DebtStatusEnums.Progressing && x.CompanyId == transaction.CompanyId).ToListAsync();
                foreach (var order in ordersPayment)
                {
                    order.DebtStatus = (int)DebtStatusEnums.Complete;
                    await _unitOfWork.Repository<Order>().UpdateDetached(order);
                }
                // check xem có bất kỳ order mới nào được tạo lúc thanh toán không, rồi trừ lại trong balance của EA, tăng used EA lên
                var orderPaymentNew = await _unitOfWork.Repository<Order>().AsQueryable(x => x.CompanyId == transaction.CompanyId && x.DebtStatus == (int)DebtStatusEnums.New).ToListAsync();
                var enterprise = await _unitOfWork.Repository<Enterprise>().AsQueryable(x => x.CompanyId == transaction.CompanyId)
                                                            .Include(x => x.Account).ThenInclude(x => x.Wallets).FirstOrDefaultAsync();
                if (orderPaymentNew.Count() > 0)
                {
                    var sumOrderPrice = orderPaymentNew.Select(x => x.Total).Sum();
                    enterprise.Account.Wallets.FirstOrDefault().Balance -= sumOrderPrice;
                    enterprise.Account.Wallets.FirstOrDefault().Used += sumOrderPrice;
                    await _unitOfWork.Repository<Wallet>().UpdateDetached(enterprise.Account.Wallets.FirstOrDefault());
                }
                await _unitOfWork.CommitAsync();
                // todo cập nhật lại balance EA, kiểm tra các đơn hàng có debtid chưa hoàn thành cộng vào used và trừ balance
            }
            return new BaseResponseViewModel<TransactionResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = _mapper.Map<TransactionResponseModel>(transaction),
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseViewModel<TransactionResponseModel>
            {
                Code = StatusCodes.Status400BadRequest,
                Message = "Reset EA failed || " + ex.Message,
            };
        }
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

    public async Task<bool> CreateTransaction(TransactionRequestModel request)
    {
        Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var wallet = await _unitOfWork.Repository<Wallet>().AsQueryable(x => x.AccountId == accountLoginId && x.Status == (int)Status.Active)
            .FirstOrDefaultAsync();
        int companyId = Int32.Parse(_contextAccessor.HttpContext?.User.FindFirst("CompanyId").Value);
        var transaction = _mapper.Map<Transaction>(request);
        transaction.Id = Guid.NewGuid();
        transaction.Type = (int)WalletTransactionTypeEnums.Bank;
        transaction.Status = (int)OrderStatusEnums.New;
        transaction.CreatedAt = TimeUtils.GetCurrentSEATime();
        transaction.CompanyId = companyId;
        transaction.SenderId = accountLoginId;
        transaction.WalletId = wallet.Id;

        var orderPayments = _unitOfWork.Repository<Order>().AsQueryable(x =>
                x.CompanyId == companyId && x.DebtStatus == (int)DebtStatusEnums.New);
        foreach (var order in orderPayments)
        {
            order.DebtStatus = (int)DebtStatusEnums.Progressing;
            await _unitOfWork.Repository<Order>().UpdateDetached(order);
        }
        try
        {
            await _unitOfWork.Repository<Transaction>().InsertAsync(transaction);
            await _unitOfWork.CommitAsync();
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
                x.CompanyId == enterprise.CompanyId && x.DebtStatus == (int)DebtStatusEnums.New /*&&
                x.Status == (int)OrderStatusEnums.Complete*/).ToListAsync();
            foreach (var order in orders)
            {
                order.DebtStatus = (int)DebtStatusEnums.Complete;
                await _unitOfWork.Repository<Order>().UpdateDetached(order);
            }
        }

        return await _unitOfWork.CommitAsync() > 0;
    }
}