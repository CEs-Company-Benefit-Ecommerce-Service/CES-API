using AutoMapper;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using LAK.Sdk.Core.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Services
{
    public interface IProjectAccountServices
    {
        Task<bool> Deleted(Guid id);
        Task<ProjectAccount> Created(Guid accountId, Guid projectId);
        public IEnumerable<ProjectAccount> Gets(PagingModel paging);
        Task<bool> CheckAccountInProject(Guid accountId, Guid projectId);
    }
    public class ProjectAccountServices : IProjectAccountServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public ProjectAccountServices(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public IEnumerable<ProjectAccount> Gets(PagingModel paging)
        {
            var projectAccounts = _unitOfWork.Repository<ProjectAccount>().GetAll().Include(x => x.Account).Include(x => x.Project)
                .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging); ;
            return projectAccounts.Item2.ToList();
        }
        public async Task<ProjectAccount> Created(Guid accountId, Guid projectId)
        {
            var newProjectAccount = new ProjectAccount()
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                ProjectId = projectId
            };
            try
            {
                await _unitOfWork.Repository<ProjectAccount>().InsertAsync(newProjectAccount);
                await _unitOfWork.CommitAsync();
                return newProjectAccount;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public async Task<bool> Deleted(Guid id)
        {
            try
            {
                var projectAccount = _unitOfWork.Repository<ProjectAccount>().GetByIdGuid(id).Result;
                _unitOfWork.Repository<ProjectAccount>().Delete(projectAccount);
                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CheckAccountInProject(Guid accountId, Guid projectId)
        {
            var projectAccounts = _unitOfWork.Repository<ProjectAccount>().GetWhere(x => x.ProjectId == projectId).Result;
            if (projectAccounts == null)
            {
                return false;
            }
            foreach (var account in projectAccounts)
            {
                if (account.AccountId == accountId)
                {
                    return true;
                }
            }
            return false;
            
        }
        //public async Task<bool> DeleteRange(Guid projectId)
        //{
        //    try
        //    {
        //        var projectAccounts = _unitOfWork.Repository<ProjectAccount>().GetWhere()
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //}
    }
}
