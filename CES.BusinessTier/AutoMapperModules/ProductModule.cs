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
    public static class ProductModule
    {
        public static void ConfigProductModule(this IMapperConfigurationExpression mc)
        {
            mc.CreateMap<Product, ProductRequestModel>().ReverseMap()
                .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null));
            mc.CreateMap<Product, ProductResponseModel>().ReverseMap();
            mc.CreateMap<Product, ProductUpdateModel>().ReverseMap()
                .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null));

        }
    }
}
