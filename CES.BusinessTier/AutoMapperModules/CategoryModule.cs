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
    public static class CategoryModule
    {
        public static void ConfigCategoryModule(this IMapperConfigurationExpression mc)
        {
            mc.CreateMap<Category, CategoryRequestModel>().ReverseMap();
            mc.CreateMap<Category, CategoryResponseModel>().ReverseMap();
            mc.CreateMap<Category, CategoryUpdateModel>().ReverseMap()
                .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null));
        }
    }
}
