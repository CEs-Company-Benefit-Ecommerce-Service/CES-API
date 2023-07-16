using AutoMapper;
using AutoMapper.QueryableExtensions;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using LAK.Sdk.Core.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Services
{
    public interface IWalletTransaction
    {
        //bool CreateNew(WalletTransaction walletTransaction);
        //Task<DynamicResponse<WalletResponseModel>> Gets(PagingModel paging);
        Task<DynamicResponse<TransactionResponseModel>> GetsTransOfWalletByLoginUser(TransactionResponseModel filter, PagingModel paging);
    }
    public class WalletTransactionServices : IWalletTransaction
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;
        public WalletTransactionServices(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor accessor, IConfiguration configuration)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _contextAccessor = accessor;
            _configuration = configuration;
        }

        public async Task<DynamicResponse<TransactionResponseModel>> Gets(TransactionResponseModel filter, PagingModel paging)
        {
            var walletTrans = _unitOfWork.Repository<Transaction>().GetAll()
            .ProjectTo<TransactionResponseModel>(_mapper.ConfigurationProvider)
            .DynamicFilter<TransactionResponseModel>(filter)
            .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging)
            ;

            return new DynamicResponse<TransactionResponseModel>
            {
                Code = 200,
                Message = "OK",
                MetaData = new PagingMetaData()
                {
                    Total = walletTrans.Item1
                },
                Data = walletTrans.Item2.ToList()
            };
        }
        public async Task<DynamicResponse<TransactionResponseModel>> GetsTransOfWalletByLoginUser(TransactionResponseModel filter, PagingModel paging)
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
            var accountLogin = await _unitOfWork.Repository<Account>().GetAll().Include(x => x.Wallets).Include(x => x.Enterprises).Include(x => x.Employees).Where(x => x.Id == accountLoginId).FirstOrDefaultAsync();

            if (accountLogin.Role.Equals(Roles.SystemAdmin.GetDisplayName()))
            {
                var transactions = _unitOfWork.Repository<Transaction>().AsQueryable(x => x.RecieveId == accountLoginId)
                                .ProjectTo<TransactionResponseModel>(_mapper.ConfigurationProvider)
                                .DynamicFilter<TransactionResponseModel>(filter)
                                .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging);
                return new DynamicResponse<TransactionResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "Ok",
                    Data = await transactions.Item2.ToListAsync()
                };
            }
            else if (accountLogin.Role.Equals(Roles.EnterpriseAdmin.GetDisplayName()))
            {
                var transactions = _unitOfWork.Repository<Transaction>().AsQueryable(x => x.SenderId == accountLoginId)
                        .ProjectTo<TransactionResponseModel>(_mapper.ConfigurationProvider)
                        .DynamicFilter<TransactionResponseModel>(filter)
                        .DynamicSort<TransactionResponseModel>(paging.Sort, paging.Order)
                        .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging);
                return new DynamicResponse<TransactionResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "Ok",
                    Data = await transactions.Item2.ToListAsync()
                };
            }
            else if (accountLogin.Role.Equals(Roles.Employee.GetDisplayName()))
            {
                var transactions = _unitOfWork.Repository<Transaction>().AsQueryable(x => x.RecieveId == accountLoginId && (x.Type == (int)WalletTransactionTypeEnums.AddWelfare || x.Type == (int)WalletTransactionTypeEnums.Order))
                       .ProjectTo<TransactionResponseModel>(_mapper.ConfigurationProvider)
                       .DynamicFilter<TransactionResponseModel>(filter)
                       .DynamicSort<TransactionResponseModel>(paging.Sort, paging.Order)
                       .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging);
                return new DynamicResponse<TransactionResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "Ok",
                    Data = await transactions.Item2.ToListAsync()
                };
            }
            return new DynamicResponse<TransactionResponseModel>
            {
                Code = StatusCodes.Status400BadRequest,
                Message = "Bad reuqest",
            };
        }

    }

}

