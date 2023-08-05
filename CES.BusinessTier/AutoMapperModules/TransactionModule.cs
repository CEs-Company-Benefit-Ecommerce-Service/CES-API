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
    public static class TransactionModule
    {
        public static void ConfigTransactionModule(this IMapperConfigurationExpression mc)
        {
            mc.CreateMap<Transaction, TransactionRequestModel>().ReverseMap();
            mc.CreateMap<Transaction, TransactionResponseModel>().ReverseMap();
            mc.CreateMap<Transaction, TransactionUpdateModel>().ReverseMap()
                .ForAllMembers(opts => opts.Condition((_, _, srcMember) => srcMember != null));
        }
    }
}
