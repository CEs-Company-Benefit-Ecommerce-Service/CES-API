﻿using AutoMapper;
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
    public static class OrderModule
    {
        public static void ConfigOrderModule(this IMapperConfigurationExpression mc)
        {
            mc.CreateMap<Order, OrderResponseModel>().ReverseMap();
            mc.CreateMap<Order, OrderRequestModel>().ReverseMap();
            mc.CreateMap<Order, OrderToPaymentResponse>().ReverseMap();
        }
    }
}
