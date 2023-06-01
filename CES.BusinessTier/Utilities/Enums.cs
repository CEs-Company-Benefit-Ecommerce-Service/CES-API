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
    public enum Status
    {
        [Display(Name = "Inactive")] Inactive,
        [Display(Name = "Active")] Active,
        [Display(Name = "Banned")] Banned
    }

    public enum LoginEnums
    {
        [Display(Name = "Login Success!")] Success = (int)StatusCodes.Status200OK,
        [Display(Name = "Login Failed!")] Failed = (int)StatusCodes.Status400BadRequest,
    }
    public enum WalletTypeEnums
    {
        a,
        [Display(Name = "Food Wallet")] FoodWallet,
        [Display(Name = "Stationery Wallet")] StationeryWallet
    }
}
