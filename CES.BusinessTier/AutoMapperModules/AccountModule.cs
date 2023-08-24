﻿using AutoMapper;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.AutoMapperModules
{
    public static class AccountModule
    {
        public static void ConfigAccountModule(this IMapperConfigurationExpression mc)
        {
            mc.CreateMap<Account, AccountRequestModel>().ReverseMap();
            mc.CreateMap<AccountRequestModel, Account>()
                .ForMember(x => x.Role, opt => opt.MapFrom(src => Utilities.Commons.ConvertIntRoleToString((int)src.Role)))
                .ReverseMap();
            mc.CreateMap<Account, AccountResponseModel>()
                .ForMember(x => x.Role, opt => opt.MapFrom(src => Utilities.Commons.ConvertStringRoleToInt(src.Role)))
                //.ForMember(x => x.CompanyId, opt => opt.MapFrom(src => src.Enterprises
                //                                                .Where(x => x.AccountId == src.Id)
                //                                                .Select(x => x.CompanyId)
                //                                                .FirstOrDefault()))
                .ReverseMap();
            mc.CreateMap<Account, AccountAllResponseModel>()
                .ForMember(x => x.Role, opt => opt.MapFrom(src => Utilities.Commons.ConvertStringRoleToInt(src.Role)))
                .ReverseMap();
            mc.CreateMap<Account, AccountUpdateModel>().ReverseMap()
                .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null));

            mc.CreateMap<AccountUpdateModel, Account>()
               .ForMember(x => x.Role, opt => opt.MapFrom(src => Utilities.Commons.ConvertIntRoleToString((int)src.Role)))
               .ReverseMap();

            mc.CreateMap<Employee, UserResponseModel>().ReverseMap()
                .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null)); ;
            mc.CreateMap<Enterprise, UserResponseModel>().ReverseMap()
                .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null)); ;
            mc.CreateMap<Supplier, UserResponseModel>().ReverseMap()
                .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null)); ;
            //mc.CreateMap<Supplier, SupplierResponseModel>().ReverseMap();
            //.ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null)); ;
            mc.CreateMap<AccountResponseModel, AccountResponseWithCompany>().ReverseMap();
        }

        private static object ConvertStringRoleToInt(string? role)
        {
            if (role == Roles.SystemAdmin.GetDisplayName())
                return (int)Roles.SystemAdmin;
            else if (role == Roles.EnterpriseAdmin.GetDisplayName())
                return (int)Roles.EnterpriseAdmin;
            else if (role == Roles.Employee.GetDisplayName())
                return (int)Roles.Employee;
            else
                return (int)Roles.SupplierAdmin;
        }
    }
}
