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
    public static class CompanyModule
    {
        public static void ConfigCompanyModule(this IMapperConfigurationExpression mc)
        {
            mc.CreateMap<Company, CompanyResponseModel>()
                .ForMember(x => x.Used, opt => 
                opt.MapFrom(src => src.Enterprises.Where(w => w.CompanyId == src.Id).FirstOrDefault().Account.Wallets.FirstOrDefault().Used))
                .ReverseMap();
            mc.CreateMap<Company, CompanyAllInfoResponse>().ReverseMap();
            mc.CreateMap<Company, CompanyRequestModel>().ReverseMap();
            mc.CreateMap<CompanyResponseModel, CompanyAllInfoResponse>().ReverseMap();
        }
    }
}
