﻿using System.Configuration;
using CES.BusinessTier.Middlewares;
using CES.BusinessTier.Services;
using CES.BusinessTier.UnitOfWork;
using CES.DataTier.Models;
using Hangfire;
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

            // Add Hangfire services.
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(configuration.GetConnectionString("HangfireConnection")));

            // Add the processing server as IHostedService
            services.AddHangfireServer();

            //services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddTransient<ExceptionMiddleware>();

            services.AddScoped<IAccountServices, AccountServices>();
            services.AddScoped<ILoginServices, LoginServices>();

            services.AddScoped<IGroupAccountServices, GroupAccountServices>();

            services.AddScoped<IGroupServices, GroupServices>();

            services.AddScoped<IWalletServices, WalletServices>();

            services.AddScoped<ICategoryService, CategoryService>();

            services.AddScoped<IProductService, ProductService>();

            services.AddScoped<IWalletTransaction, WalletTransactionServices>();

            services.AddScoped<ICompanyServices, CompanyServices>();

            services.AddScoped<IOrderDetailServices, OrderDetailServices>();

            services.AddScoped<IOrderServices, OrderServices>();

            services.AddScoped<ITransactionService, TransactionService>();

            services.AddScoped<IExcelService, ExcelService>();

            services.AddScoped<IDebtServices, DebtServices>();

            services.AddScoped<IInvokeServices, InvokeServices>();

            services.AddScoped<IBenefitServices, BenefitServices>();

            services.AddScoped<IReportServices, ReportServices>();

            services.AddScoped<INotificationServices, NotificationServices>();
            
            services.AddScoped<IDiscountServices, DiscountServices>();
        }
    }
}
