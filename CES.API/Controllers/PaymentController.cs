using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
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

        public PaymentController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreatePaymentUrl([FromBody] CreatePaymentRequest createPaymentRequest)
        { 
            var url = await _transactionService.CreatePayment(createPaymentRequest);
            return Ok(url);
        }
        
        [HttpGet(ApiEndPointConstant.Payment.ZaloPayEndpoint)]
        public async Task<IActionResult> ZaloPayPaymentCallBack(double? amount, int? status, string? apptransid)
        {
            var isSuccessful = await _transactionService.ExecuteZaloPayCallBack(amount, status, apptransid);

            if (isSuccessful && status == 1)
            {
                return RedirectPermanent("https://firebasestorage.googleapis.com/v0/b/pos-system-47f93.appspot.com/o/files%2Fpayment-done.png?alt=media&token=284c1b35-e4f2-417e-90e4-a339c4cd7a4e");
            }
            else
            {
                return RedirectPermanent("https://firebasestorage.googleapis.com/v0/b/pos-system-47f93.appspot.com/o/files%2Fpayment-fail.png?alt=media&token=2b7e58ee-c18f-4ec3-9363-ad1ec83ffc6c");
            }
        }
    }
}