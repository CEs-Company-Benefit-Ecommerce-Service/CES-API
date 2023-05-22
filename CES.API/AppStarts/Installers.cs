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
            services.AddDbContext<CEsData_v1Context>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });

            //services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));
            //services.AddTransient<ExceptionMiddleware>();

        }
    }
}
