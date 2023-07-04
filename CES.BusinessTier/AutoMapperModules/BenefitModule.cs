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
    public static class BenefitModule
    {
        public static void ConfigBenefitModule(this IMapperConfigurationExpression mc)
        {
            mc.CreateMap<Benefit, BenefitRequestModel>().ReverseMap();
            mc.CreateMap<Benefit, BenefitResponseModel>().ReverseMap();
            mc.CreateMap<Benefit, BenefitUpdateModel>().ReverseMap()
                .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null));
        }
    }
}
