using AutoMapper;
using CES.BusinessTier.Services;
using CES.BusinessTier.UnitOfWork;
using CES.DataTier.Models;
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
        else if (role == (int)Roles.Employee)
            return Roles.SupplierAdmin.GetDisplayName();
        else
            return Roles.Shipper.GetDisplayName();
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
}