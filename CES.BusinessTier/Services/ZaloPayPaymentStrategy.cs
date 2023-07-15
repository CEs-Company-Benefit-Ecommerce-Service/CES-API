using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.PaymentModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace CES.BusinessTier.Services;

public class ZaloPayPaymentStrategy : IPaymentStrategy
{
    private readonly ZaloPayConfig _zaloPayConfig;
    private readonly double _used;
    private readonly Guid _enterpriseId;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWork _unitOfWork;
    public ZaloPayPaymentStrategy(string zaloPayConfigJson, double used, Guid enterpriseId, IHttpContextAccessor httpContextAccessor, IUnitOfWork unitOfWork)
    {
        _used = used;
        _enterpriseId = enterpriseId;
        _httpContextAccessor = httpContextAccessor;
        _zaloPayConfig = JsonConvert.DeserializeObject<ZaloPayConfig>(zaloPayConfigJson) ?? throw new InvalidOperationException();
        _unitOfWork = unitOfWork;
    }
    public async Task<CreatePaymentResponse> ExecutePayment(string? systemAccountId, string? accountLoginId, string? walletId, string? companyId)
    {
        DateTime currentTime = TimeUtils.GetCurrentSEATime();
        string currentTimeStamp = TimeUtils.GetTimestamp(currentTime);
        var embeddata = new { merchantinfo = "CEs", redirecturl = "https://" + _httpContextAccessor.HttpContext.Request.Host.Value + ApiEndPointConstant.Payment.ZaloPayEndpoint };
        var items = new[]{
            new { itemid = "it1", itemname = "Thanh toan don hang", itemprice = _used, itemquantity = 1 } };
        var appTransid = TimeUtils.ConvertDateTimeToVietNamTimeZone().ToString("yyMMdd") + "_" + currentTimeStamp;
        var param = new Dictionary<string, string>();
        param.Add("appid", _zaloPayConfig.AppId);
        param.Add("appuser", _enterpriseId.ToString());
        param.Add("amount", _used.ToString());
        param.Add("apptime", TimeUtils.GetTimeStamp().ToString());
        param.Add("apptransid", appTransid);
        param.Add("embeddata", JsonConvert.SerializeObject(embeddata));
        param.Add("item", JsonConvert.SerializeObject(items));
        param.Add("bankcode", _zaloPayConfig.BankCode);
        param.Add("callbackurl", "https://" + _httpContextAccessor.HttpContext.Request.Host.Value + ApiEndPointConstant.Payment.ZaloPayEndpoint);
        
        var data = _zaloPayConfig.AppId + "|" + param["apptransid"] + "|" + param["appuser"] + "|" + param["amount"] + "|"
                   + param["apptime"] + "|" + param["embeddata"] + "|" + param["item"];
        param.Add("mac", HmacHelper.Compute(ZaloPayHMAC.HMACSHA256, _zaloPayConfig.Key1, data));

        var result = await HttpHelper.PostFormAsync(_zaloPayConfig.BaseUrl, param);
        CreatePaymentResponse createPaymentResponse = new CreatePaymentResponse()
        {
            Message = "Đang tiến hành thanh toán ZaloPay"
        };
        createPaymentResponse.DisplayType = CreatePaymentReturnType.Url;
        foreach (var entry in result)
        {
            if (entry.Key == "orderurl")
            {
                createPaymentResponse.Url = entry.Value.ToString();
            }
            else if (entry.Key == "returnmessage")
            {
                createPaymentResponse.Message = entry.Value.ToString();
            }
        }
        Transaction transaction = new Transaction()
        {
            Id = Guid.NewGuid(),
            SenderId = Guid.Parse(accountLoginId),
            RecieveId = Guid.Parse(systemAccountId),
            InvoiceId = appTransid,
            WalletId = Guid.Parse(walletId),
            Description = $"Đang tiến hành thanh toán ZaloPay mã đơn {appTransid}",
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