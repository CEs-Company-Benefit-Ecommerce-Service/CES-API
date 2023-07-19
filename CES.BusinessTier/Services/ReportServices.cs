﻿using AutoMapper;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using LAK.Sdk.Core.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Services
{
    public interface IReportServices
    {
        Task<BaseResponseViewModel<ReportEAResponseModel>> GetReportForEA(ReportRequestModel request);
        Task<BaseResponseViewModel<ReportSAResponseModel>> GetReportForSA(ReportRequestModel request);
    }
    public class ReportServices : IReportServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public ReportServices(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _contextAccessor = httpContextAccessor;
        }

        public async Task<BaseResponseViewModel<ReportEAResponseModel>> GetReportForEA(ReportRequestModel request)
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());

            var accountLogin = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == accountLoginId).Include(x => x.Wallets).FirstOrDefaultAsync();

            int enterpriseCompanyId = Int32.Parse(_contextAccessor.HttpContext?.User.FindFirst("CompanyId").Value);
            var orders = await _unitOfWork.Repository<Order>().AsQueryable(x => x.CompanyId == enterpriseCompanyId && x.Status == (int)OrderStatusEnums.Complete).ToListAsync();
            var orderCount = orders.Count();
            if (request.From != null || request.To != null)
            {
                orderCount = orders.Where(x => x.CreatedAt.GetStartOfDate() >= request.From.GetValueOrDefault().GetStartOfDate() && x.CreatedAt <= request.To.GetValueOrDefault().GetStartOfDate()).Count();
            }

            var result = new ReportEAResponseModel
            {
                OrderCount = orderCount,
                Used = accountLogin.Wallets.FirstOrDefault().Used
            };
            return new BaseResponseViewModel<ReportEAResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "Ok",
                Data = result
            };
        }

        public async Task<BaseResponseViewModel<ReportSAResponseModel>> GetReportForSA(ReportRequestModel request)
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());

            var accountLogin = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == accountLoginId).Include(x => x.Wallets).FirstOrDefaultAsync();

            var companies = await _unitOfWork.Repository<Company>().AsQueryable(x => x.Status == (int)Status.Active).ToListAsync();

            var companyCount = companies.Count();

            var totalCompanyUsed = 0.0;

            foreach (var company in companies)
            {
                var enterprise = await _unitOfWork.Repository<Enterprise>().AsQueryable(x => x.CompanyId == company.Id).FirstOrDefaultAsync();
                if (enterprise != null)
                {
                    var accountEnterprise = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == enterprise.AccountId && x.Status == (int)Status.Active).Include(x => x.Wallets).FirstOrDefaultAsync();
                    totalCompanyUsed += (double)accountEnterprise.Wallets.FirstOrDefault().Used;
                }
            }

            var employeeCount = await _unitOfWork.Repository<Employee>().AsQueryable(x => x.Status == (int)Status.Active).CountAsync();

            var result = new ReportSAResponseModel
            {
                CompanyCount = companyCount,
                EmployeeCount = employeeCount,
                TotalCompanyUsed = totalCompanyUsed,
            };

            return new BaseResponseViewModel<ReportSAResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "Ok",
                Data = result
            };

        }
    }
}