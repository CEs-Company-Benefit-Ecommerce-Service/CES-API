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
        Task<BaseResponseViewModel<GroupResponseModel>> Update(Guid id, GroupUpdateModel request);
        Task<BaseResponseViewModel<GroupResponseModel>> Create(GroupRequestModel request);
        Task<BaseResponseViewModel<GroupResponseModel>> Delete(Guid id);
        Task<DynamicResponse<GroupResponseModel>> GetGroupsByEmployeeId(Guid accountId);
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
        public async Task<BaseResponseViewModel<GroupResponseModel>> Update(Guid id, GroupUpdateModel request)
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
                var updateGroup = _mapper.Map<GroupUpdateModel, Group>(request, existedGroup);
                await _unitOfWork.Repository<Group>().UpdateDetached(updateGroup);
                await _unitOfWork.CommitAsync();

                if (updateGroup.Status == (int)Status.Active)
                {
                    Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value);
                    _ = await _projectAccountServices.ScheduleUpdateBalanceForAccountsInGroup(id, accountLoginId);
                }

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
            // check total balance in group when allocate
            var memberInGroup = _unitOfWork.Repository<EmployeeGroupMapping>().AsQueryable(x => x.GroupId == requestModel.GroupId).Count();
            var newMembers = requestModel.AccountId.Count();
            var group = await _unitOfWork.Repository<Group>().AsQueryable(x => x.Id == requestModel.GroupId).Include(x => x.Benefit).FirstOrDefaultAsync();
            var eaWallet = await _unitOfWork.Repository<Benefit>().AsQueryable(x => x.Id == group.BenefitId).Include(x => x.Company).ThenInclude(x => x.Enterprises).ThenInclude(x => x.Account).ThenInclude(x => x.Wallets).Select(x => x.Company.Enterprises.FirstOrDefault().Account.Wallets.FirstOrDefault()).FirstOrDefaultAsync();
            //if (group.Benefit.UnitPrice * (memberInGroup + newMembers) > eaWallet.Balance)
            //{
            //    return new BaseResponseViewModel<GroupResponseModel>()
            //    {
            //        Code = 400,
            //        Message = "Total balance to allocate was higher than your balance!",
            //    };
            //}


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
                if (group.Benefit.Status == (int)Status.Active)
                {
                    var totalThat1EmpHave = (group.Benefit.EstimateTotal - group.Benefit.TotalReceive) / memberInGroup;
                    if (totalThat1EmpHave > eaWallet.Balance)
                    {
                        return new BaseResponseViewModel<GroupResponseModel>()
                        {
                            Code = 400,
                            Message = "Total balance to allocate was higher than your balance!",
                        };
                    }
                    eaWallet.Balance -= totalThat1EmpHave * newMembers;
                    eaWallet.UpdatedAt = TimeUtils.GetCurrentSEATime();
                    await _unitOfWork.Repository<Wallet>().UpdateDetached(eaWallet);
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
                var empNotification = new DataTier.Models.Notification()
                {
                    Id = Guid.NewGuid(),
                    Title = $"Join {group.Name}",
                    Description = $"You have just been added to the {group.Name}",
                    AccountId = accountId,
                    IsRead = false,
                    CreatedAt = TimeUtils.GetCurrentSEATime(),
                };
                await _unitOfWork.Repository<DataTier.Models.Notification>().InsertAsync(empNotification);
            }

            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<GroupResponseModel>()
            {
                Code = 200,
                Message = "OK",
            }; ;
        }
        public async Task<BaseResponseViewModel<GroupResponseModel>> RemoveEmployee(GroupMemberRequestModel requestModel)
        {
            var memberInGroup = _unitOfWork.Repository<EmployeeGroupMapping>().AsQueryable(x => x.GroupId == requestModel.GroupId).Count();
            var removeMembers = requestModel.AccountId.Count();
            var group = await _unitOfWork.Repository<Group>().AsQueryable(x => x.Id == requestModel.GroupId).Include(x => x.Benefit).FirstOrDefaultAsync();
            var eaWallet = await _unitOfWork.Repository<Benefit>().AsQueryable(x => x.Id == group.BenefitId).Include(x => x.Company).ThenInclude(x => x.Enterprises).ThenInclude(x => x.Account).ThenInclude(x => x.Wallets).Select(x => x.Company.Enterprises.FirstOrDefault().Account.Wallets.FirstOrDefault()).FirstOrDefaultAsync();

            var totalThat1EmpHave = (group.Benefit.EstimateTotal - group.Benefit.TotalReceive) / memberInGroup;
            var totalNeedDecrease = totalThat1EmpHave * removeMembers;
            try
            {
                eaWallet.Balance += totalNeedDecrease;
                eaWallet.UpdatedAt = TimeUtils.GetCurrentSEATime();
                await _unitOfWork.Repository<Wallet>().UpdateDetached(eaWallet);

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
        public async Task<DynamicResponse<GroupResponseModel>> GetGroupsByEmployeeId(Guid accountId)
        {
            var employee = await _unitOfWork.Repository<Employee>().AsQueryable(x => x.AccountId == accountId).FirstOrDefaultAsync();

            var groups = _unitOfWork.Repository<EmployeeGroupMapping>().AsQueryable(x => x.EmployeeId == employee.Id).Include(x => x.Group).Select(x => x.Group);
            var result = _mapper.Map<List<GroupResponseModel>>(groups);
            return new DynamicResponse<GroupResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "Ok",
                MetaData = new PagingMetaData
                {
                    Page = 1,
                    Size = 5,
                    Total = groups.Count()
                },
                Data = result
            };
        }
    }
}
