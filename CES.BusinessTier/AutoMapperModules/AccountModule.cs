using AutoMapper;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
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
            mc.CreateMap<Account, AccountResponseModel>().ReverseMap();

            mc.CreateMap<Project, ProjectRequestModel>().ReverseMap();
            mc.CreateMap<Project, ProjectResponseModel>().ReverseMap();

            mc.CreateMap<ProjectAccount, ProjectAccountResponse>().ReverseMap();
            //mc.CreateMap<Account, AccountUpdateModel>().ReverseMap()
            //    .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null));
        }
    }
}
