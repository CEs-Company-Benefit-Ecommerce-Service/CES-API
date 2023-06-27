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
        //Task<DynamicResponse<WalletResponseModel>> GetsByLoginUser(PagingModel paging);
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

        //public async Task<DynamicResponse<WalletResponseModel>> Gets(PagingModel paging)
        //{
        //    var walletTrans = _unitOfWork.Repository<WalletTransaction>().GetAll()
        //    .ProjectTo<WalletResponseModel>(_mapper.ConfigurationProvider)
        //    .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging)
        //    ;

        //    return new DynamicResponse<WalletResponseModel>
        //    {
        //        Code = 200,
        //        Message = "OK",
        //        MetaData = new PagingMetaData()
        //        {
        //            Total = walletTrans.Item1
        //        },
        //        Data = walletTrans.Item2.ToList()
        //    };
        //}
        //public async Task<DynamicResponse<WalletResponseModel>> GetsByLoginUser(PagingModel paging)
        //{
        //    Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
        //    var accountLogin = await _unitOfWork.Repository<Account>().GetAll().Include(x => x.Wallet).ThenInclude(x => x.WalletTransaction).Where(x => x.Id == accountLoginId).FirstOrDefaultAsync();
        //    var walletTransction = new List<WalletTransaction>();
        //    foreach (var wallet in accountLogin.Wallet)
        //    {
        //        foreach (var transaction in wallet.WalletTransaction)
        //        {
        //            walletTransction.Add(transaction);
        //        }
        //    }
        //    //var walletTrans = _unitOfWork.Repository<WalletTransaction>().GetAll().Where(x => x.Wallet.AccountId == accountLoginId)
        //    //.ProjectTo<WalletResponseModel>(_mapper.ConfigurationProvider)
        //    //.PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging)
        //    //;

        //    var result = walletTransction.AsQueryable().ProjectTo<WalletResponseModel>(_mapper.ConfigurationProvider)
        //    .PagingQueryable(paging.Page, paging.Size, Constants.LimitPaging, Constants.DefaultPaging);

        //    return new DynamicResponse<WalletResponseModel>
        //    {
        //        Code = 200,
        //        Message = "OK",
        //        MetaData = new PagingMetaData()
        //        {
        //            Total = result.Item1
        //        },
        //        Data = result.Item2.ToList()
        //    };
        //}
        //public bool CreateNew(WalletTransaction walletTransaction)
        //{
        //    try
        //    {
        //        _unitOfWork.Repository<WalletTransaction>().Insert(walletTransaction);
        //        _unitOfWork.Commit();
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}
    }
}
