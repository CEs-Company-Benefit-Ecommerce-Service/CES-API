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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CES.BusinessTier.Services
{
    public interface IBenefitServices
    {
        Task<BaseResponseViewModel<BenefitResponseModel>> GetById(Guid id);
        Task<DynamicResponse<BenefitResponseModel>> GetAllAsync(BenefitResponseModel filter, PagingModel paging);
        Task<BaseResponseViewModel<BenefitResponseModel>> UpdateAsync(BenefitUpdateModel request, Guid benefitId);
        Task<BaseResponseViewModel<BenefitResponseModel>> CreateAsync(BenefitRequestModel request);
    }
    public class BenefitServices : IBenefitServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IGroupServices _groupServices;

        public BenefitServices(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor contextAccessor, IGroupServices groupServices)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _contextAccessor = contextAccessor;
            _groupServices = groupServices;
        }

        public async Task<DynamicResponse<BenefitResponseModel>> GetAllAsync(BenefitResponseModel filter, PagingModel paging)
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
            var companyId = _contextAccessor.HttpContext?.User.FindFirst("CompanyId").Value.ToString();
            var benefits = _unitOfWork.Repository<Benefit>().AsQueryable(x => x.CompanyId == Int32.Parse(companyId))
                .ProjectTo<BenefitResponseModel>(_mapper.ConfigurationProvider)
                .DynamicFilter(filter)
                .DynamicSort(paging.Sort, paging.Order)
                .PagingQueryable(paging.Page, paging.Size);

            return new DynamicResponse<BenefitResponseModel>
            {
                Code = StatusCodes.Status200OK,
                SystemCode = "000",
                Message = "Get success",
                MetaData = new PagingMetaData
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = benefits.Item1
                },
                Data = await benefits.Item2.ToListAsync()
            };
        }

        public async Task<BaseResponseViewModel<BenefitResponseModel>> GetById(Guid id)
        {
            var benefit = await _unitOfWork.Repository<Benefit>().AsQueryable(x => x.Id == id)
                .ProjectTo<BenefitResponseModel>(_mapper.ConfigurationProvider).FirstOrDefaultAsync();
            if (benefit == null)
            {
                return new BaseResponseViewModel<BenefitResponseModel>
                {
                    Code = StatusCodes.Status404NotFound,
                    SystemCode = "015",
                    Message = "Benefit was not found",
                };
            }
            return new BaseResponseViewModel<BenefitResponseModel>
            {
                Code = StatusCodes.Status200OK,
                SystemCode = "000",
                Message = "Get success",
                Data = benefit
            };
        }

        public async Task<BaseResponseViewModel<BenefitResponseModel>> CreateAsync(BenefitRequestModel request)
        {
            #region Validate amount
            if (!Commons.ValidateAmount(request.UnitPrice))
            {
                throw new ErrorResponse(StatusCodes.Status400BadRequest, 400, "Số tiền không hợp lệ");
            }
            #endregion
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
            var user = _unitOfWork.Repository<Enterprise>().FindAsync(x => x.AccountId == accountLoginId).Result;

            if (request.EndDate != null)
            {
                request.EndDate = new DateTime(request.EndDate.Value.Year, request.EndDate.Value.Month, request.EndDate.Value.Day, 23, 59, 0);
            }
            var newBenefit = _mapper.Map<Benefit>(request);
            newBenefit.Id = Guid.NewGuid();
            newBenefit.Status = (int)Status.Inactive;
            newBenefit.CreatedAt = TimeUtils.GetCurrentSEATime();
            newBenefit.CompanyId = (int)user.CompanyId;

            var now = TimeUtils.GetCurrentSEATime();
            if (request.TimeFilter == null)
            {
                request.TimeFilter = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            }
            else if (request.TimeFilter.Value.Hour >= now.Hour && request.TimeFilter.Value.Minute > now.Minute)
            {
                request.TimeFilter = new DateTime(now.Year, now.Month, now.Day, request.TimeFilter.Value.Hour, request.TimeFilter.Value.Minute, request.TimeFilter.Value.Second);
            }

            switch (request.Type)
            {
                case (int)GroupTypes.Daily:
                    break;
                case (int)GroupTypes.Weekly:
                    if (request.DayFilter == null)
                    {
                        throw new ErrorResponse(StatusCodes.Status400BadRequest, 016, "Please provide Day");
                    }
                    break;
                case (int)GroupTypes.Monthly:
                    if (request.DateFilter == null)
                    {
                        throw new ErrorResponse(StatusCodes.Status400BadRequest, 017, "Please provide Date");
                    }
                    break;
            }

            var group = new Group()
            {
                Id = Guid.NewGuid(),
                Name = $"Group {request.Name}",
                Status = (int)Status.Inactive,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
                CreatedBy = accountLoginId,
                BenefitId = newBenefit.Id,
                Type = request.Type,
                TimeFilter = request.TimeFilter,
                DateFilter = request.DateFilter,
                DayFilter = request.DayFilter,
                EndDate = request.EndDate
            };

            try
            {
                await _unitOfWork.Repository<Benefit>().InsertAsync(newBenefit);
                await _unitOfWork.Repository<Group>().InsertAsync(group);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<BenefitResponseModel>()
                {
                    Code = StatusCodes.Status204NoContent,
                    SystemCode = "018",
                    Message = "Create benefit successed",
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseViewModel<BenefitResponseModel>()
                {
                    Code = StatusCodes.Status400BadRequest,
                    SystemCode = "019",
                    Message = "Create benefit failed",
                };
            }
        }

        public async Task<BaseResponseViewModel<BenefitResponseModel>> UpdateAsync(BenefitUpdateModel request, Guid benefitId)
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
            var account = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == accountLoginId).Include(x => x.Wallets).FirstOrDefaultAsync();
            var eaWallet = account.Wallets.FirstOrDefault();

            var existedBenefit = _unitOfWork.Repository<Benefit>().FindAsync(x => x.Id == benefitId).Result;
            if (existedBenefit == null)
            {
                return new BaseResponseViewModel<BenefitResponseModel>
                {
                    Code = StatusCodes.Status404NotFound,
                    SystemCode = "015",
                    Message = "Benefit was not found"
                };
            }

            var temp = _mapper.Map<BenefitUpdateModel, Benefit>(request, existedBenefit);
            temp.UpdatedAt = TimeUtils.GetCurrentSEATime();
            var group = await _unitOfWork.Repository<Group>().AsQueryable(x => x.BenefitId == benefitId).FirstOrDefaultAsync();
            var memberInGroup = await _unitOfWork.Repository<EmployeeGroupMapping>().AsQueryable(x => x.GroupId == group.Id).CountAsync();
            var fromDate = TimeUtils.GetCurrentSEATime();
            var toDate = group.EndDate;
            var totalDays = 0;
            var neededBalance = 0.0;
            switch (temp.Type)
            {
                case 1:
                    if (fromDate > group.TimeFilter)
                    {
                        fromDate = fromDate.AddDays(1);
                    }
                    while (fromDate <= toDate)
                    {
                        totalDays++;
                        fromDate = fromDate.AddDays(1);
                    }
                    neededBalance = temp.UnitPrice * memberInGroup * totalDays;
                    if (neededBalance > eaWallet.Balance)
                    {
                        return new BaseResponseViewModel<BenefitResponseModel>()
                        {
                            Code = StatusCodes.Status400BadRequest,
                            SystemCode = "021",
                            Message = "Update benefit failed!\nYour wallet have not enough balance (Balance needed: {neededBalance})",
                        };
                    }
                    break;
                case 2:
                    var formattedDateTimeWeekly = new DateTime(fromDate.Year, fromDate.Month, fromDate.Day, group.TimeFilter.Value.Hour,
                        group.TimeFilter.Value.Minute, group.TimeFilter.Value.Second);
                    int currentDayOfWeekValue = (int)formattedDateTimeWeekly.DayOfWeek;
                    int daysToAdd = ((int)group.DayFilter - currentDayOfWeekValue + 7) % 7;
                    DateTime resultDate = formattedDateTimeWeekly.AddDays(daysToAdd);
                    DateTimeOffset dateTimeOffsetWeekly = new DateTimeOffset(resultDate);

                    if (resultDate >= fromDate)
                    {
                        fromDate = fromDate.AddDays(7);
                    }
                    while (fromDate <= toDate)
                    {
                        totalDays++;
                        fromDate = fromDate.AddDays(7);
                    }
                    neededBalance = temp.UnitPrice * memberInGroup * totalDays;
                    if (neededBalance > eaWallet.Balance)
                    {
                        return new BaseResponseViewModel<BenefitResponseModel>()
                        {
                            Code = StatusCodes.Status400BadRequest,
                            SystemCode = "021",
                            Message = "Update benefit failed!\nYour wallet have not enough balance (Balance needed: {neededBalance})",
                        };
                    }
                    break;
                case 3:
                    while (fromDate <= toDate)
                    {
                        totalDays++;
                        fromDate.AddMonths(1);
                    }
                    neededBalance = temp.UnitPrice * memberInGroup * totalDays;
                    if (neededBalance > eaWallet.Balance)
                    {
                        return new BaseResponseViewModel<BenefitResponseModel>()
                        {
                            Code = StatusCodes.Status400BadRequest,
                            SystemCode = "021",
                            Message = "Update benefit failed!\nYour wallet have not enough balance (Balance needed: {neededBalance})",
                        };
                    }
                    break;
                default:
                    break;
            }
            try
            {
                eaWallet.Balance -= neededBalance;
                eaWallet.UpdatedAt = TimeUtils.GetCurrentSEATime();

                await _unitOfWork.Repository<Wallet>().UpdateDetached(eaWallet);
                await _unitOfWork.Repository<Benefit>().UpdateDetached(temp);
                await _unitOfWork.CommitAsync();


                GroupUpdateModel groupUpdate = new GroupUpdateModel()
                {
                    Status = temp.Status
                };
                _ = await _groupServices.Update(group.Id, groupUpdate);

                return new BaseResponseViewModel<BenefitResponseModel>
                {
                    Code = StatusCodes.Status204NoContent,
                    SystemCode = "020",
                    Message = "Update benefit successed"
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseViewModel<BenefitResponseModel>()
                {
                    Code = StatusCodes.Status400BadRequest,
                    SystemCode = "021",
                    Message = "Update benefit failed",
                };
            }
        }
    }
}
