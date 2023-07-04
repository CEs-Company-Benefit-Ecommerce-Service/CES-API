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
    public interface IGroupAccountServices
    {
        Task<bool> Deleted(Guid id);
        Task<EmployeeGroupMapping> Created(Guid employId, Guid projectId);
        public IEnumerable<Group> Gets(PagingModel paging);
        Task<bool> CheckAccountInGroup(Guid employId, Guid projectId);
    }
    public class GroupAccountServices : IGroupAccountServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public GroupAccountServices(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public IEnumerable<Group> Gets(PagingModel paging)
        {
            var projectAccounts = _unitOfWork.Repository<Group>().GetAll()
                .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging); ;
            return projectAccounts.Item2.ToList();
        }
        public async Task<EmployeeGroupMapping> Created(Guid employId, Guid projectId)
        {
            var newGroupAccount = new EmployeeGroupMapping()
            {
                Id = Guid.NewGuid(),
                EmployeeId = employId,
                GroupId = projectId,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
            };
            try
            {
                await _unitOfWork.Repository<EmployeeGroupMapping>().InsertAsync(newGroupAccount);
                await _unitOfWork.CommitAsync();
                return newGroupAccount;
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
                var projectAccount = _unitOfWork.Repository<EmployeeGroupMapping>().GetByIdGuid(id).Result;
                _unitOfWork.Repository<EmployeeGroupMapping>().Delete(projectAccount);
                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CheckAccountInGroup(Guid employId, Guid projectId)
        {
            var projectAccounts = _unitOfWork.Repository<EmployeeGroupMapping>().GetWhere(x => x.GroupId == projectId).Result;
            if (projectAccounts == null)
            {
                return false;
            }
            foreach (var account in projectAccounts)
            {
                if (account.EmployeeId == employId)
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
        //        var projectAccounts = _unitOfWork.Repository<GroupAccount>().GetWhere()
        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //}
    }
}
