using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        [Display(Name = "Employee")] Employee = 4
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
        [Display(Name = "Food Wallet")] FoodWallet = 1,
        [Display(Name = "Stationery Wallet")] StationeryWallet = 2,
        [Display(Name = "General Wallet")] GeneralWallet = 3
    }
    public enum WalletTransactionTypeEnums
    {
        [Display(Name = "Add Welfare")] AddWelfare = 1,
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
        [Display(Name = "Shipping")] Shipping = 2,
        [Display(Name = "Complete")] Complete = 3,
        [Display(Name = "Cancel")] Cancel = 4,
    }
    public enum DebtStatusEnums
    {
        [Display(Name = "New")] New = 1,
        [Display(Name = "Complete")] Complete = 2,
        [Display(Name = "Cancel")] Cancel = 4,
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
}
