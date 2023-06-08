using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
        [Display(Name = "System Admin")] SystemAdmin,
        [Display(Name = "Supplier Admin")] SupplierAdmin,
        [Display(Name = "Enterprise Admin")] EnterpriseAdmin,
        [Display(Name = "Employee")] Employee
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
        [Display(Name = "Stationery Wallet")] StationeryWallet = 2
    }
    public enum WalletTransactionTypeEnums
    {
        [Display(Name = "Add Welfare")] AddWelfare = 1,
    }
}
