using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Services;
using CES.BusinessTier.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CES.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IDebtServices _debtServices;

        public PaymentController(ITransactionService transactionService, IDebtServices debtServices)
        {
            _transactionService = transactionService;
            _debtServices = debtServices;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Create payment gateway. \
        /// "used" : Amount to paid. \
        /// "accountId" : Id of account use this api. \
        /// "paymentId" : Id of payment ( use get list paymentId to have this field). \
        /// return the redirect url
        /// </remarks>
        /// <param name="createPaymentRequest"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePaymentUrl([FromBody] CreatePaymentRequest createPaymentRequest)
        {
            var url = await _transactionService.CreatePayment(createPaymentRequest);
            return Ok(url);
        }

        [Authorize]
        [HttpPost("vnpay")]
        public async Task<IActionResult> CreateVnPayPaymentUrl([FromBody] CreatePaymentRequest createPaymentRequest)
        {
            var url = await _transactionService.CreatePayment(createPaymentRequest);
            return Ok(url);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Zalo callback api. \
        /// return redirect Url payment complete
        /// </remarks>
        /// <param name="amount"></param>
        /// <param name="status"></param>
        /// <param name="apptransid"></param>
        /// <returns></returns>
        [HttpGet(ApiEndPointConstant.Payment.ZaloPayEndpoint)]
        public async Task<IActionResult> ZaloPayPaymentCallBack(double? amount, int? status, string? apptransid)
        {
            var isSuccessful = await _transactionService.ExecuteZaloPayCallBack(amount, status, apptransid);

            if (isSuccessful && status == 1)
            {
                //return RedirectPermanent("https://firebasestorage.googleapis.com/v0/b/pos-system-47f93.appspot.com/o/files%2Fpayment-done.png?alt=media&token=284c1b35-e4f2-417e-90e4-a339c4cd7a4e");
                return RedirectPermanent("https://ces-web.vercel.app/payment-success/");
            }
            else
            {
                return RedirectPermanent("https://firebasestorage.googleapis.com/v0/b/pos-system-47f93.appspot.com/o/files%2Fpayment-fail.png?alt=media&token=2b7e58ee-c18f-4ec3-9363-ad1ec83ffc6c");
            }
        }

        [HttpGet(ApiEndPointConstant.Payment.VnPayEndpoint)]
        public async Task<IActionResult> VnPayPaymentCallBack(double? vnp_Amount, string? vnp_ResponseCode, string? vnp_TxnRef)
        {
            var isSuccessful = await _transactionService.ExecuteVnPayCallBack(vnp_Amount, vnp_ResponseCode, vnp_TxnRef);

            if (isSuccessful && vnp_ResponseCode == "00")
            {
                //return RedirectPermanent("https://firebasestorage.googleapis.com/v0/b/pos-system-47f93.appspot.com/o/files%2Fpayment-done.png?alt=media&token=284c1b35-e4f2-417e-90e4-a339c4cd7a4e");
                return RedirectPermanent("https://ces-web.vercel.app/payment-success/");
            }
            else
            {
                return RedirectPermanent("https://firebasestorage.googleapis.com/v0/b/pos-system-47f93.appspot.com/o/files%2Fpayment-fail.png?alt=media&token=2b7e58ee-c18f-4ec3-9363-ad1ec83ffc6c");
            }
        }

        [HttpGet("total-order/{companyId}")]
        public async Task<ActionResult> GetTotalOrder(int companyId, [FromQuery] PagingModel paging)
        {
            var result = await _debtServices.GetValueForPayment(companyId, paging);
            return StatusCode((int)result.Code, result);
        }
    }
}
