using AutoMapper;
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
            mc.CreateMap<Account, AccountResponseModel>()
                .ForMember(x => x.Role, opt => opt.MapFrom(src => Utilities.Commons.ConvertStringRoleToInt(src.Role)))
                .ReverseMap();
            mc.CreateMap<Account, AccountAllResponseModel>()
                .ForMember(x => x.Role, opt => opt.MapFrom(src => Utilities.Commons.ConvertStringRoleToInt(src.Role)))
                .ReverseMap();
            mc.CreateMap<Account, AccountUpdateModel>().ReverseMap()
                .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null));

            mc.CreateMap<Employee, UserResponseModel>().ReverseMap()
                .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null)); ;
            mc.CreateMap<Enterprise, UserResponseModel>().ReverseMap()
                .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null)); ;
            mc.CreateMap<Supplier, UserResponseModel>().ReverseMap()
                .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null)); ;
            //mc.CreateMap<Supplier, SupplierResponseModel>().ReverseMap();
            //.ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null)); ;
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
