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
    public static class WalletModule
    {
        public static void ConfigWalletModule(this IMapperConfigurationExpression mc)
        {
            mc.CreateMap<Wallet, WalletResponseModel>().ReverseMap();
            //mc.CreateMap<Wallet, WalletRequestModel>().ReverseMap();
            //mc.CreateMap<Wallet, WalletInfoRequestModel>().ReverseMap();
        }
    }
    public static class WalletTransactionModule
    {
        public static void ConfigWalletTransactionModule(this IMapperConfigurationExpression mc)
        {
            //mc.CreateMap<WalletTransaction, WalletTransactionResponseModel>().ReverseMap();
        }
    }
}
