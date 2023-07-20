using AutoMapper;
using AutoMapper.QueryableExtensions;
using AutoMapper.QueryableExtensions.Impl;
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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Services
{
    public interface IGroupServices
    {
        DynamicResponse<GroupResponseModel> Gets(GroupResponseModel filter, PagingModel paging);
        Task<BaseResponseViewModel<GroupResponseModel>> Get(Guid id);
        Task<BaseResponseViewModel<GroupResponseModel>> AddEmployee(GroupMemberRequestModel requestModel);
        Task<BaseResponseViewModel<GroupResponseModel>> RemoveEmployee(GroupMemberRequestModel requestModel);
        Task<BaseResponseViewModel<GroupResponseModel>> Update(Guid id, GroupRequestModel request);
        Task<BaseResponseViewModel<GroupResponseModel>> Create(GroupRequestModel request);
        Task<BaseResponseViewModel<GroupResponseModel>> Delete(Guid id);
    }
    public class GroupServices : IGroupServices
    {
        private readonly IGroupAccountServices _projectAccountServices;
        private readonly IAccountServices _accountServices;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IMapper _mapper;
        public GroupServices(IUnitOfWork unitOfWork, IMapper mapper, IGroupAccountServices projectAccountServices, IHttpContextAccessor contextAccessor, IAccountServices accountServices)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _projectAccountServices = projectAccountServices;
            _contextAccessor = contextAccessor;
            _accountServices = accountServices;
        }
        public DynamicResponse<GroupResponseModel> Gets(GroupResponseModel filter, PagingModel paging)
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
            var account = _accountServices.Get(accountLoginId);
            var companyId = _contextAccessor.HttpContext?.User.FindFirst("CompanyId").Value.ToString();
            //var projects = _unitOfWork.Repository<Group>().AsQueryable().Include(x => x.Benefit)
            //    // .Include(x => x.GroupAccount).ThenInclude(y => y.Account)
            //    .ProjectTo<GroupResponseModel>(_mapper.ConfigurationProvider)
            //    .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging);
            //// var result = projects.Item2.Where(x => x.CompanyId == account.Data.CompanyId);
            ////var result = projects.Item2;

            var groups = _unitOfWork.Repository<Benefit>().AsQueryable(x => x.CompanyId == Int32.Parse(companyId)).Include(x => x.Groups).Select(x => x.Groups).ToList();
            var result = new List<GroupResponseModel>();
            foreach (var group in groups)
            {
                foreach (var item in group)
                {
                    result.Add(_mapper.Map<GroupResponseModel>(item));
                }
            }

            var resultReturn = result.AsQueryable()
                                   .DynamicFilter<GroupResponseModel>(filter)
                                   .DynamicSort<GroupResponseModel>(paging.Sort, paging.Order)
                                   .PagingQueryable(paging.Page, paging.Size);
            return new DynamicResponse<GroupResponseModel>
            {
                Code = 200,
                Message = "OK",
                MetaData = new PagingMetaData
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = resultReturn.Item1
                },
                Data = resultReturn.Item2.ToList()
            };
        }
        public async Task<BaseResponseViewModel<GroupResponseModel>> Get(Guid id)
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
            var account = _accountServices.Get(accountLoginId);
            var enterprise = _unitOfWork.Repository<Enterprise>().GetWhere(x => x.AccountId == accountLoginId).Result.FirstOrDefault();
            var group = await _unitOfWork.Repository<Group>().AsQueryable(x => x.Id == id)
                .Include(x => x.EmployeeGroupMappings)
                .ThenInclude(y => y.Employee)
                .FirstOrDefaultAsync();
            //var project = await _unitOfWork.Repository<Group>().GetAll()
            //    .FirstOrDefaultAsync();
            return new BaseResponseViewModel<GroupResponseModel>
            {
                Code = 200,
                Message = "OK",
                Data = _mapper.Map<GroupResponseModel>(group)
            };
        }
        public async Task<BaseResponseViewModel<GroupResponseModel>> Update(Guid id, GroupRequestModel request)
        {
            var existedGroup = _unitOfWork.Repository<Group>().GetByIdGuid(id).Result;
            if (existedGroup == null)
            {
                return new BaseResponseViewModel<GroupResponseModel>
                {
                    Code = 404,
                    Message = "Not found",
                };
            }
            try
            {
                var updateGroup = _mapper.Map<GroupRequestModel, Group>(request, existedGroup);
                await _unitOfWork.Repository<Group>().UpdateDetached(updateGroup);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<GroupResponseModel>
                {
                    Code = 204,
                    Message = "No content",
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<GroupResponseModel>
                {
                    Code = 400,
                    Message = "Bad request",
                };
            }
        }
        public async Task<BaseResponseViewModel<GroupResponseModel>> Create(GroupRequestModel request)
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
            var benefit = await _unitOfWork.Repository<Benefit>().AsQueryable(x => x.Id == request.BenefitId).FirstOrDefaultAsync();
            if (benefit == null)
            {
                throw new ErrorResponse(StatusCodes.Status404NotFound, 404, "Benefit does not exist");
            }

            var newGroup = _mapper.Map<Group>(request);
            newGroup.Id = Guid.NewGuid();
            newGroup.CreatedAt = TimeUtils.GetCurrentSEATime();
            newGroup.BenefitId = request.BenefitId;
            newGroup.CreatedBy = accountLoginId;
            newGroup.Status = (int)Status.Active;
            // newGroup.CompanyId = (int)account.Data.CompanyId;
            try
            {
                await _unitOfWork.Repository<Group>().InsertAsync(newGroup);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<GroupResponseModel>
                {
                    Code = 204,
                    Message = "No content",
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<GroupResponseModel>
                {
                    Code = 400,
                    Message = "Bad request",
                };
            }
        }
        public async Task<BaseResponseViewModel<GroupResponseModel>> Delete(Guid id)
        {
            var project = _unitOfWork.Repository<Group>().GetAll().Where(x => x.Id == id).FirstOrDefault();
            if (project == null)
            {
                return new BaseResponseViewModel<GroupResponseModel>
                {
                    Code = 404,
                    Message = "Not found",
                };
            }
            try
            {
                // foreach (var projectAccount in project.GroupAccount)
                // {
                //     var deleteGroupAccountResult = _projectAccountServices.Deleted(projectAccount.Id);
                // }
                _unitOfWork.Repository<Group>().Delete(project);
                await _unitOfWork.CommitAsync();
                return new BaseResponseViewModel<GroupResponseModel>
                {
                    Code = 204,
                    Message = "No content",
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<GroupResponseModel>()
                {
                    Code = 400,
                    Message = "Bad request",
                };
            }
        }
        public async Task<BaseResponseViewModel<GroupResponseModel>> AddEmployee(GroupMemberRequestModel requestModel)
        {
            foreach (var accountId in requestModel.AccountId)
            {
                var employee = _unitOfWork.Repository<Employee>().GetWhere(x => x.AccountId == accountId).Result.FirstOrDefault();

                if (_projectAccountServices.CheckAccountInGroup(employee.Id, requestModel.GroupId).Result)
                {
                    return new BaseResponseViewModel<GroupResponseModel>()
                    {
                        Code = 400,
                        Message = "This account was in group",
                    };
                }
                var newGroupAccount = await _projectAccountServices.Created(employee.Id, requestModel.GroupId);
                if (newGroupAccount == null)
                {
                    return new BaseResponseViewModel<GroupResponseModel>()
                    {
                        Code = 400,
                        Message = "Bad request",
                    };
                }
            }

            //var project = await Get(requestModel.Select(x => x.GroupId).FirstOrDefault());

            return new BaseResponseViewModel<GroupResponseModel>()
            {
                Code = 200,
                Message = "OK",
            }; ;
        }
        public async Task<BaseResponseViewModel<GroupResponseModel>> RemoveEmployee(GroupMemberRequestModel requestModel)
        {
            try
            {
                var project = await Get(requestModel.GroupId);
                foreach (var accountId in requestModel.AccountId)
                {
                    var employee = _unitOfWork.Repository<Employee>().GetWhere(x => x.AccountId == accountId).Result.FirstOrDefault();

                    var projectAccount = project.Data.EmployeeGroupMappings.Where(x => x.EmployeeId == employee.Id).FirstOrDefault();
                    if (projectAccount != null)
                    {
                        var deleteGroupAccoutnResult = await _projectAccountServices.Deleted(projectAccount.Id);
                    }
                }
                return new BaseResponseViewModel<GroupResponseModel>()
                {
                    Code = 204,
                    Message = "No content"
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<GroupResponseModel>()
                {
                    Code = 400,
                    Message = "Bad request",
                };
            }
        }
    }
}
