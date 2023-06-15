using AutoMapper;
using CES.BusinessTier.AutoMapperModules;

namespace CES.API.AppStarts
{
    public static class AutoMapperConfig
    {
        public static void ConfigureAutoMapper(this IServiceCollection services)
        {
            MapperConfiguration mapperConfiguration = new MapperConfiguration(mc =>
            {
                mc.ConfigAccountModule();
                mc.ConfigProjectModule();
                mc.ConfigWalletModule();
                mc.ConfigCategoryModule();
                mc.ConfigProductModule();
                mc.ConfigCompanyModule();
                mc.ConfigOrderDetailModule();
                mc.ConfigOrderModule();
                mc.ConfigTransactionModule();
            });
            IMapper mapper = mapperConfiguration.CreateMapper();
            services.AddSingleton(mapper);
        }
    }
}
