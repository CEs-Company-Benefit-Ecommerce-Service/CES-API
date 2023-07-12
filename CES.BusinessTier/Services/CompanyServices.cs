using AutoMapper;
using AutoMapper.QueryableExtensions;
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
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace CES.BusinessTier.Services
{
    public interface ICompanyServices
    {
        Task<BaseResponseViewModel<CompanyResponseModel>> CreateNew(CompanyRequestModel request);
        Task<DynamicResponse<CompanyResponseModel>> Gets(CompanyResponseModel filter, PagingModel paging);
        Task<BaseResponseViewModel<CompanyResponseModel>> GetById(int id);
        Task<BaseResponseViewModel<CompanyResponseModel>> Update(int id, CompanyRequestModel request);
    }
    public class CompanyServices : ICompanyServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public CompanyServices(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _contextAccessor = contextAccessor;
        }

        public async Task<DynamicResponse<CompanyResponseModel>> Gets(CompanyResponseModel filter, PagingModel paging)
        {
            var company = _unitOfWork.Repository<Company>().AsQueryable()
                .Include(x => x.Enterprises).ThenInclude(x => x.Account).ThenInclude(x => x.Wallets)
                .ProjectTo<CompanyResponseModel>(_mapper.ConfigurationProvider)
                .DynamicFilter(filter)
                .DynamicSort(paging.Sort, paging.Order)
                .PagingQueryable(paging.Page, paging.Size);

            return new DynamicResponse<CompanyResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                MetaData = new PagingMetaData
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = company.Item1
                },
                Data = await company.Item2.ToListAsync()
            };
        }
        public async Task<BaseResponseViewModel<CompanyResponseModel>> GetById(int id)
        {
            var company = await _unitOfWork.Repository<Company>().AsQueryable(x => x.Id == id)
                .Include(x => x.Enterprises).ThenInclude(x => x.Account).ThenInclude(x => x.Wallets)
                .ProjectTo<CompanyResponseModel>(_mapper.ConfigurationProvider).FirstOrDefaultAsync();

            return new BaseResponseViewModel<CompanyResponseModel>
            {
                Code = 200,
                Message = "OK",
                Data = company
            };

        }

        public async Task<BaseResponseViewModel<CompanyResponseModel>> CreateNew(CompanyRequestModel request)
        {
            var newCompany = _mapper.Map<Company>(request);
            newCompany.Status = (int)Status.Active;
            newCompany.CreatedAt = TimeUtils.GetCurrentSEATime();
            newCompany.CreatedBy = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
            newCompany.ExpiredDate = ((DateTime)newCompany.ExpiredDate).GetEndOfDate();
            try
            {
                await _unitOfWork.Repository<Company>().InsertAsync(newCompany);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<CompanyResponseModel>
                {
                    Code = 200,
                    Message = "OK",
                    Data = _mapper.Map<CompanyResponseModel>(newCompany)
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<CompanyResponseModel>
                {
                    Code = 400,
                    Message = StatusCodes.Status400BadRequest.ToString(),
                };
            }
        }
        public async Task<BaseResponseViewModel<CompanyResponseModel>> Update(int id, CompanyRequestModel request)
        {
            var existedCompany = _unitOfWork.Repository<Company>().FindAsync(x => x.Id == id).Result;
            //_mapper.Map<CompanyRequestModel, Company>(request, existedCompany);

            existedCompany.UpdatedAt = TimeUtils.GetCurrentSEATime();
            try
            {
                await _unitOfWork.Repository<Company>().UpdateDetached(_mapper.Map<CompanyRequestModel, Company>(request, existedCompany));
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<CompanyResponseModel>
                {
                    Code = 200,
                    Message = StatusCodes.Status200OK.ToString(),
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<CompanyResponseModel>
                {
                    Code = 400,
                    Message = StatusCodes.Status400BadRequest.ToString(),
                };
            }
        }
    }
}
