using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.PaymentModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Services.VnPayServices
{
    public interface IVnPayPaymentStrategy
    {
        Task<CreatePaymentResponse> ExecutePayment(string? systemAccountId, string? accountLoginId, string? walletId, string? companyId);
    }

    public class VnPayPaymentStrategy : IVnPayPaymentStrategy
    {
        private readonly double _used;
        private readonly Guid _enterpriseId;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        public VnPayPaymentStrategy(double used, Guid enterpriseId, IHttpContextAccessor httpContextAccessor, IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _used = used;
            _enterpriseId = enterpriseId;
            _httpContextAccessor = httpContextAccessor;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }
        public async Task<CreatePaymentResponse> ExecutePayment(string? systemAccountId, string? accountLoginId, string? walletId, string? companyId)
        {
            DateTime currentTime = TimeUtils.GetCurrentSEATime();
            string currentTimeStamp = TimeUtils.GetTimestamp(currentTime);
            var txnRef = TimeUtils.ConvertDateTimeToVietNamTimeZone().ToString("yyMMdd") + "_" + currentTimeStamp;
            var pay = new VnPayLibrary();
            var urlCallBack = _configuration["VnPayPaymentCallBack:ReturnUrl"];
            pay.AddRequestData("vnp_Version", _configuration["Vnpay:Version"]);
            pay.AddRequestData("vnp_Command", _configuration["Vnpay:Command"]);
            pay.AddRequestData("vnp_TmnCode", _configuration["Vnpay:TmnCode"]);
            pay.AddRequestData("vnp_Amount", ((int)_used * 100).ToString());
            pay.AddRequestData("vnp_CreateDate", currentTime.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", _configuration["Vnpay:CurrCode"]);
            pay.AddRequestData("vnp_IpAddr", pay.GetIpAddress(_httpContextAccessor.HttpContext));
            pay.AddRequestData("vnp_Locale", _configuration["Vnpay:Locale"]);
            pay.AddRequestData("vnp_OrderInfo", $"Doanh nghiep {companyId} voi account {accountLoginId} thanh toan cac don hang tu wallet {walletId} sang wallet {systemAccountId}");
            pay.AddRequestData("vnp_OrderType", "other");
            pay.AddRequestData("vnp_ReturnUrl", urlCallBack);
            pay.AddRequestData("vnp_TxnRef", txnRef);

            var paymentUrl = pay.CreateRequestUrl(_configuration["Vnpay:BaseUrl"], _configuration["Vnpay:HashSecret"]);
            CreatePaymentResponse createPaymentResponse = new CreatePaymentResponse()
            {
                Message = "Đang tiến hành thanh toán VnPay",
                Url = paymentUrl,
                DisplayType = CreatePaymentReturnType.Url
            };
            Transaction transaction = new Transaction()
            {
                Id = Guid.NewGuid(),
                SenderId = Guid.Parse(accountLoginId),
                RecieveId = Guid.Parse(systemAccountId),
                InvoiceId = txnRef,
                WalletId = Guid.Parse(walletId),
                Description = $"Đang tiến hành thanh toán VnPay mã đơn {txnRef}",
                Status = (int)DebtStatusEnums.New,
                Type = (int)WalletTransactionTypeEnums.VnPay,
                Total = (double)_used,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
                CompanyId = Int32.Parse(companyId),
            };

            try
            {
                await _unitOfWork.Repository<Transaction>().InsertAsync(transaction);
                await _unitOfWork.CommitAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return createPaymentResponse;
        }
    }
}
