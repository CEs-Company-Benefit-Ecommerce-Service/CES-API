using AutoMapper;
using CES.BusinessTier.Services;
using CES.BusinessTier.UnitOfWork;
using CES.DataTier.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CES.BusinessTier.Utilities;

public static class Commons
{
    public static string RemoveSpaces(string input)
    {
        return input.Replace(" ", string.Empty);
    }

    public static int ConvertStringRoleToInt(string role)
    {
        if (role == Roles.SystemAdmin.GetDisplayName())
            return (int)Roles.SystemAdmin;
        else if (role == Roles.EnterpriseAdmin.GetDisplayName())
            return (int)Roles.EnterpriseAdmin;
        else if (role == Roles.Employee.GetDisplayName())
            return (int)Roles.Employee;
        else if (role == Roles.SupplierAdmin.GetDisplayName())
            return (int)Roles.SupplierAdmin;
        else
            return (int)Roles.Shipper;
    }
    public static string ConvertIntRoleToString(int role)
    {
        if (role == (int)Roles.SystemAdmin)
            return Roles.SystemAdmin.GetDisplayName();
        else if (role == (int)Roles.EnterpriseAdmin)
            return Roles.EnterpriseAdmin.GetDisplayName();
        else if (role == (int)Roles.Employee)
            return Roles.Employee.GetDisplayName();
        else if (role == (int)Roles.SupplierAdmin)
            return Roles.SupplierAdmin.GetDisplayName();
        else
            return Roles.Shipper.GetDisplayName();
    }
    public static string ConvertIntOrderStatusToString(int status)
    {
        if (status == (int)OrderStatusEnums.Ready)
            return OrderStatusEnums.Ready.GetDisplayName();

        else if (status == (int)OrderStatusEnums.Shipping)
            return OrderStatusEnums.Shipping.GetDisplayName();

        else if (status == (int)OrderStatusEnums.Complete)
            return OrderStatusEnums.Complete.GetDisplayName();

        else
            return OrderStatusEnums.Cancel.GetDisplayName();
    }
    public static double GetLimitInCompany(Account account)
    {
        if (account.Enterprises.Count > 0)
        {
            return (double)account.Enterprises.FirstOrDefault().Company.Limits;
        }
        else
            return 0;
    }

    public static bool ValidateAmount(double amount)
    {
        if (amount < 0) return false;
        return true;

    }
    
    public static string GetEnumDisplayNameFromValue<T>(int value) where T : Enum
    {
        foreach (T enumValue in Enum.GetValues(typeof(T)))
        {
            if (Convert.ToInt32(enumValue) == value)
            {
                return enumValue.GetDisplayName();
            }
        }

        throw new ArgumentException($"No enum value found with value '{value}'", nameof(value));
    }
}