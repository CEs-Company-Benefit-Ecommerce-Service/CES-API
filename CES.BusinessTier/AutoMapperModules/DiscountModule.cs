using AutoMapper;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.DataTier.Models;

namespace CES.BusinessTier.AutoMapperModules;

public static class DiscountModule
{
    public static void ConfigDiscountModule(this IMapperConfigurationExpression mc)
    {
        mc.CreateMap<Discount, DiscountResponse>().ReverseMap();
        mc.CreateMap<Discount, DiscountRequest>().ReverseMap()
            .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null));
    }
}