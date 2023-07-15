using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Utilities
{
    public class Constants
    {
        public const int DefaultPaging = 50;
        public const int LimitPaging = 500;
        public const int LimitWallet = 5000; // 1 point = 1000VND
    }
    
    public static class ApiEndPointConstant
    {
        static ApiEndPointConstant()
        {

        }

        public const string RootEndPoint = "/api";
        public const string ApiVersion = "/v1";
        public const string ApiEndpoint = RootEndPoint + ApiVersion;

        public static class Order
        {
            public const string OrderEndpoint = ApiEndpoint + "/orders";
        }

        public static class Payment
        {
            public const string PaymentEndpoint = ApiEndpoint + "/payments";
            public const string ZaloPayEndpoint = PaymentEndpoint + "/zalopay";
            public const string VnPayEndpoint = PaymentEndpoint + "/vnpay";
            public const string PaymentProviderEndpoint = PaymentEndpoint + "/payment-providers";
            public const string CheckTransactionStatus = ApiEndpoint + "/check-transaction-status";
            public const string VietQrEndpoint = PaymentEndpoint + "/vietqr";
        }

        public static class Brand
        {
            public const string BrandEndpoint = ApiEndpoint + "/brands";

            public const string BrandPaymentProviderMappingEndPoint =
                BrandEndpoint + "/{id}/brandpaymentprovider";
        }

    }
}
