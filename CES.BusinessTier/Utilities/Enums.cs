using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace CES.BusinessTier.Utilities
{
    public static class EnumUtils
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            string text = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault()
                .GetCustomAttribute<DisplayAttribute>()?.GetName();
            if (string.IsNullOrEmpty(text))
            {
                text = enumValue.ToString();
            }

            return text;
        }
    }

    public enum Roles
    {
        [Display(Name = "System Admin")] SystemAdmin = 1,
        [Display(Name = "Supplier Admin")] SupplierAdmin = 2,
        [Display(Name = "Enterprise Admin")] EnterpriseAdmin = 3,
        [Display(Name = "Employee")] Employee = 4,
        [Display(Name = "Shipper")] Shipper = 5
    }
    //Status for account
    public enum Status
    {
        [Display(Name = "Active")] Active = 1,
        [Display(Name = "Inactive")] Inactive = 2,
        [Display(Name = "Banned")] Banned = 3
    }

    public enum LoginEnums
    {
        [Display(Name = "Login Success!")] Success = (int)StatusCodes.Status200OK,
        [Display(Name = "Login Failed!")] Failed = (int)StatusCodes.Status400BadRequest,
    }
    public enum WalletTypeEnums
    {
        [Display(Name = "General Wallet")] GeneralWallet = 1,
        [Display(Name = "Stationery Wallet")] StationeryWallet = 2,
        [Display(Name = "Food Wallet")] FoodWallet = 3
    }
    public enum WalletTransactionTypeEnums
    {
        [Display(Name = "Add Welfare")] AddWelfare = 1,
        [Display(Name = "Order")] Order = 2,
        [Display(Name = "ZaloPay")] ZaloPay = 3,
        [Display(Name = "Allocate Welfare")] AllocateWelfare = 4,
    }

    public enum TransactionTypeEnums
    {
        [Display(Name = "Default")]
        Default = 1,
        [Display(Name = "Rollback")]
        RollBack = 2,
        [Display(Name = "ActiveCard")]
        ActiveCard = 3,
    }
    public enum OrderStatusEnums
    {
        [Display(Name = "New")] New = 1,
        [Display(Name = "Ready")] Ready = 2,
        [Display(Name = "Shipping")] Shipping = 3,
        [Display(Name = "Complete")] Complete = 4,
        [Display(Name = "Cancel")] Cancel = 5,
    }
    public enum DebtStatusEnums
    {
        [Display(Name = "New")] New = 0,
        [Display(Name = "Complete")] Complete = 1,
        [Display(Name = "Cancel")] Cancel = 2,
    }
    public enum ReceiptStatusEnums
    {
        [Display(Name = "New")] New = 1,
        [Display(Name = "Complete")] Complete = 2,
        [Display(Name = "Cancel")] Cancel = 4,
    }
    public enum BenefitTypeEnums
    {
        [Display(Name = "Thưởng")] Welfare = 1,
    }
    
    public enum PaymentType
    {
        CASH,
        MOMO,
        VIETQR,
        VNPAY,
        ZALOPAY
    }
    
    public enum CreatePaymentReturnType
    {
        Url,
        Qr,
        Message
    }
    
    public enum ZaloPayHMAC
    {
        HMACMD5,
        HMACSHA1,
        HMACSHA256,
        HMACSHA512
    }

    public class HmacHelper
    {
        public static string Compute(ZaloPayHMAC algorithm = ZaloPayHMAC.HMACSHA256, string key = "", string message = "")
        {
            byte[] keyByte = System.Text.Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
            byte[] hashMessage = null;

            switch (algorithm)
            {
                case ZaloPayHMAC.HMACMD5:
                    hashMessage = new HMACMD5(keyByte).ComputeHash(messageBytes);
                    break;
                case ZaloPayHMAC.HMACSHA1:
                    hashMessage = new HMACSHA1(keyByte).ComputeHash(messageBytes);
                    break;
                case ZaloPayHMAC.HMACSHA256:
                    hashMessage = new HMACSHA256(keyByte).ComputeHash(messageBytes);
                    break;
                case ZaloPayHMAC.HMACSHA512:
                    hashMessage = new HMACSHA512(keyByte).ComputeHash(messageBytes);
                    break;
                default:
                    hashMessage = new HMACSHA256(keyByte).ComputeHash(messageBytes);
                    break;
            }
            
            return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
        }
    }
    
    public class HttpHelper
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<T> PostAsync<T>(string uri, HttpContent content)
        {  
            var response = await httpClient.PostAsync(uri, content);          
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseString);
        }

        public static Task<Dictionary<string, object>> PostAsync(string uri, HttpContent content)
        {
            return PostAsync<Dictionary<string, object>>(uri, content);
        }

        public static Task<T> PostFormAsync<T>(string uri, Dictionary<string, string> data)
        {
            return PostAsync<T>(uri, new FormUrlEncodedContent(data));
        }

        public static Task<Dictionary<string, object>> PostFormAsync(string uri, Dictionary<string, string> data)
        {
            return PostFormAsync<Dictionary<string, object>>(uri, data);
        }

        public static async Task<T> GetJson<T>(string uri)
        {
            var response = await httpClient.GetAsync(uri);
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseString);
        }

        public static Task<Dictionary<string, object>> GetJson(string uri)
        {
            return GetJson<Dictionary<string, object>>(uri);
        }
    }
    
    public enum TransactionStatus
    {
        Pending,
        Paid,
        Fail
    }
}
