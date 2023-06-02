using CES.BusinessTier.Middlewares;
using CES.BusinessTier.Services;
using CES.BusinessTier.UnitOfWork;
using CES.DataTier.Models;
using Microsoft.EntityFrameworkCore;

namespace CES.API.AppStarts
{
    public static class Installers
    {
        public static void InstallService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddRouting(options =>
            {
                options.LowercaseUrls = true; ;
                options.LowercaseQueryStrings = true;
            });
            services.AddDbContext<CEsData_devContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });

            //services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddTransient<ExceptionMiddleware>();

            services.AddScoped<IAccountServices, AccountServices>();
            services.AddScoped<ILoginServices, LoginServices>();

            services.AddScoped<IProjectAccountServices, ProjectAccountServices>();

            services.AddScoped<IProjectServices, ProjectServices>();

            services.AddScoped<IWalletServices, WalletServices>();

            services.AddScoped<ICategoryService, CategoryService>();

        }
    }
}
