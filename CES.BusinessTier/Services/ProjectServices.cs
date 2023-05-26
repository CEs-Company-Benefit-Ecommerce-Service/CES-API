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
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Services
{
    public interface IProjectServices
    {
        DynamicResponse<ProjectResponseModel> Gets(PagingModel paging);
        Task<BaseResponseViewModel<ProjectResponseModel>> Get(Guid id);
        Task<BaseResponseViewModel<ProjectResponseModel>> AddEmployee(List<ProjectMemberRequestModel> requestModel);
        Task<BaseResponseViewModel<ProjectResponseModel>> RemoveEmployee(List<ProjectMemberRequestModel> requestModel);
        Task<BaseResponseViewModel<ProjectResponseModel>> Update(Guid id, ProjectRequestModel request);
        Task<BaseResponseViewModel<ProjectResponseModel>> Create(ProjectRequestModel request);
        Task<BaseResponseViewModel<ProjectResponseModel>> Delete(Guid id);
    }
    public class ProjectServices : IProjectServices
    {
        private IProjectAccountServices _projectAccountServices;
        private IUnitOfWork _unitOfWork;
        private IMapper _mapper;
        public ProjectServices(IUnitOfWork unitOfWork, IMapper mapper, IProjectAccountServices projectAccountServices)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _projectAccountServices = projectAccountServices;
        }
        public DynamicResponse<ProjectResponseModel> Gets(PagingModel paging)
        {
            var projects = _unitOfWork.Repository<Project>().GetAll().Include(x => x.ProjectAccount).ThenInclude(y => y.Account)
                .ProjectTo<ProjectResponseModel>(_mapper.ConfigurationProvider)
                .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging);
            return new DynamicResponse<ProjectResponseModel>
            {
                Code = 200,
                Message = "OK",
                Data = projects.Item2.ToList()
            };
        }
        public async Task<BaseResponseViewModel<ProjectResponseModel>> Get(Guid id)
        {
            var project = await _unitOfWork.Repository<Project>().GetAll().Include(x => x.ProjectAccount).ThenInclude(y => y.Account).Where(x => x.Id == id).FirstOrDefaultAsync();
            return new BaseResponseViewModel<ProjectResponseModel>
            {
                Code = 200,
                Message = "OK",
                Data = _mapper.Map<ProjectResponseModel>(project)
            };
        }
        public async Task<BaseResponseViewModel<ProjectResponseModel>> Update(Guid id, ProjectRequestModel request)
        {
            var existedProject = _unitOfWork.Repository<Project>().GetByIdGuid(id).Result;
            if (existedProject == null)
            {
                return new BaseResponseViewModel<ProjectResponseModel>
                {
                    Code = 404,
                    Message = "Not found",
                };
            }
            try
            {
                var updateProject = _mapper.Map<ProjectRequestModel, Project>(request, existedProject);
                await _unitOfWork.Repository<Project>().UpdateDetached(updateProject);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<ProjectResponseModel>
                {
                    Code = 204,
                    Message = "No content",
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<ProjectResponseModel>
                {
                    Code = 400,
                    Message = "Bad request",
                };
            }
        }
        public async Task<BaseResponseViewModel<ProjectResponseModel>> Create(ProjectRequestModel request)
        {
            var newProject = _mapper.Map<Project>(request);
            newProject.Id = Guid.NewGuid();
            try
            {
                await _unitOfWork.Repository<Project>().InsertAsync(newProject);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<ProjectResponseModel>
                {
                    Code = 204,
                    Message = "No content",
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<ProjectResponseModel>
                {
                    Code = 400,
                    Message = "Bad request",
                };
            }
        }
        public async Task<BaseResponseViewModel<ProjectResponseModel>> Delete(Guid id)
        {
            var project = _unitOfWork.Repository<Project>().GetAll().Include(x => x.ProjectAccount).Where(x => x.Id == id).FirstOrDefault();
            if (project == null)
            {
                return new BaseResponseViewModel<ProjectResponseModel>
                {
                    Code = 404,
                    Message = "Not found",
                };
            }
            try
            {
                foreach(var projectAccount in project.ProjectAccount)
                {
                    var deleteProjectAccountResult = _projectAccountServices.Deleted(projectAccount.Id);
                }
                _unitOfWork.Repository<Project>().Delete(project);
                await _unitOfWork.CommitAsync();
                return new BaseResponseViewModel<ProjectResponseModel>
                {
                    Code = 204,
                    Message = "No content",
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<ProjectResponseModel>()
                {
                    Code = 400,
                    Message = "Bad request",
                };
            }
        }
        public async Task<BaseResponseViewModel<ProjectResponseModel>> AddEmployee(List<ProjectMemberRequestModel> requestModel)
        {
            foreach (var member in requestModel)
            {
                var newProjectAccount = await _projectAccountServices.Created(member.AccountId, member.ProjectId);
                if (newProjectAccount == null)
                {
                    return new BaseResponseViewModel<ProjectResponseModel>()
                    {
                        Code = 400,
                        Message = "Bad request",
                    };
                }
            }

            //var project = await Get(requestModel.Select(x => x.ProjectId).FirstOrDefault());

            return new BaseResponseViewModel<ProjectResponseModel>()
            {
                Code = 200,
                Message = "OK",
            }; ;
        }
        public async Task<BaseResponseViewModel<ProjectResponseModel>> RemoveEmployee(List<ProjectMemberRequestModel> requestModel)
        {
            try
            {
                var project = await Get(requestModel.Select(x => x.ProjectId).FirstOrDefault());
                foreach (var member in requestModel)
                {
                    var projectAccount = project.Data.ProjectAccount.Where(x => x.AccountId == member.AccountId).FirstOrDefault();
                    if (projectAccount != null)
                    {
                        var deleteProjectAccoutnResult = await _projectAccountServices.Deleted(projectAccount.Id);
                    }
                }
                return new BaseResponseViewModel<ProjectResponseModel>()
                {
                    Code = 204,
                    Message = "No content"
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<ProjectResponseModel>()
                {
                    Code = 400,
                    Message = "Bad request",
                };
            }
        }
    }
}
