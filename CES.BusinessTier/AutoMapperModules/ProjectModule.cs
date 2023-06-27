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
    public static class ProjectModule
    {
        public static void ConfigProjectModule(this IMapperConfigurationExpression mc)
        {
            mc.CreateMap<Group, GroupRequestModel>().ReverseMap();
            mc.CreateMap<Group, GroupResponseModel>().ReverseMap();

            mc.CreateMap<GroupAccount, GroupAccountResponse>().ReverseMap();
        }
    }
}
