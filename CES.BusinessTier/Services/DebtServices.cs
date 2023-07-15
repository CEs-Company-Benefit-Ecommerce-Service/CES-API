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
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Services
{
    public interface IDebtServices
    {
        Task<DynamicResponse<DebtTicketResponseModel>> GetsAsync(DebtTicketResponseModel filter, PagingModel paging);
        Task<DynamicResponse<DebtTicketResponseModel>> GetsWithCompanyAsync(DebtTicketResponseModel filter, PagingModel paging, int companyId);
        BaseResponseViewModel<DebtTicketResponseModel> GetById(int id);
        Task<BaseResponseViewModel<DebtTicketResponseModel>> CreateAsync(int companyId);
        Task<BaseResponseViewModel<DebtTicketResponseModel>> DeleteAsync(int debtId);
        Task<BaseResponseViewModel<object>> GetValueForPayment(int companyId);

    }
    public class DebtServices : IDebtServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IOrderServices _orderServices;

        public DebtServices(IUnitOfWork unitOfWork, IMapper mapper, IOrderServices orderServices)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _orderServices = orderServices;
        }

        public async Task<DynamicResponse<DebtTicketResponseModel>> GetsAsync(DebtTicketResponseModel filter, PagingModel paging)
        {
            try
            {
                var debts = _unitOfWork.Repository<DebtTicket>().AsQueryable(x => x.Status != (int)DebtStatusEnums.Cancel)
               .ProjectTo<DebtTicketResponseModel>(_mapper.ConfigurationProvider)
               .DynamicFilter(filter)
               .DynamicSort(paging.Sort, paging.Order)
               .PagingQueryable(paging.Page, paging.Size);

                return new DynamicResponse<DebtTicketResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "Ok",
                    MetaData = new PagingMetaData
                    {
                        Page = paging.Page,
                        Size = paging.Size,
                        Total = debts.Item1
                    },
                    Data = await debts.Item2.ToListAsync()
                };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        public async Task<DynamicResponse<DebtTicketResponseModel>> GetsWithCompanyAsync(DebtTicketResponseModel filter, PagingModel paging, int companyId)
        {
            var debts = _unitOfWork.Repository<DebtTicket>().AsQueryable(x => x.CompanyId == companyId && x.Status != (int)DebtStatusEnums.Cancel)
                           .Include(x => x.Company)
                           .ProjectTo<DebtTicketResponseModel>(_mapper.ConfigurationProvider)
                           .DynamicFilter(filter)
                           .DynamicSort(paging.Sort, paging.Order)
                           .PagingQueryable(paging.Page, paging.Size);

            if (debts.Item2 == null)
            {
                return new DynamicResponse<DebtTicketResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "Ok",
                    MetaData =
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = debts.Item1
                },
                };
            }

            return new DynamicResponse<DebtTicketResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "Ok",
                MetaData = new PagingMetaData
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = debts.Item1
                },
                Data = await debts.Item2.ToListAsync()
            };
        }
        public BaseResponseViewModel<DebtTicketResponseModel> GetById(int id)
        {
            var debt = _unitOfWork.Repository<DebtTicket>().AsQueryable(x => x.Id == id)
                           .Include(x => x.Company)
                           .ProjectTo<DebtTicketResponseModel>(_mapper.ConfigurationProvider)
                           .FirstOrDefault();
            if (debt == null)
            {
                return new BaseResponseViewModel<DebtTicketResponseModel>()
                {
                    Code = StatusCodes.Status404NotFound,
                    Message = "Not Found",
                };
            }
            return new BaseResponseViewModel<DebtTicketResponseModel>()
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = _mapper.Map<DebtTicketResponseModel>(debt)
            };
        }
        public async Task<BaseResponseViewModel<DebtTicketResponseModel>> CreateAsync(int companyId)
        {
            var totalOrderResult = _orderServices.GetTotal(companyId).Result;
            var enterprise = _unitOfWork.Repository<Enterprise>().GetWhere(x => x.CompanyId == companyId && x.Status == (int)Status.Active).Result.FirstOrDefault();
            var accountWallet = _unitOfWork.Repository<Account>().GetAll().Include(x => x.Wallets)
                                                                .Where(x => x.Id == enterprise.AccountId)
                                                                .Select(x => x.Wallets.FirstOrDefault())
                                                                .FirstOrDefault();
            if (accountWallet.Balance >= 0)
            {
                return new BaseResponseViewModel<DebtTicketResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "This company have not debt",
                };
            }

            var debt = new DebtTicket()
            {
                CompanyId = companyId,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
                Name = "Tat toan ....",
                InfoPayment = "MoMo: .... \nSTK: ...",
                Status = (int)DebtStatusEnums.New,
                Total = Math.Abs((double)accountWallet.Balance),
                UpdatedAt = TimeUtils.GetCurrentSEATime(),
            };

            try
            {
                await _unitOfWork.Repository<DebtTicket>().InsertAsync(debt);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<DebtTicketResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    Message = "OK",
                    Data = _mapper.Map<DebtTicketResponseModel>(debt)
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseViewModel<DebtTicketResponseModel>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = "Bad Request" + "||" + ex.Message,
                };
            }
        }
        public async Task<BaseResponseViewModel<DebtTicketResponseModel>> UpdateAsync(Guid debtId, int status)
        {
            var existedDebt = _unitOfWork.Repository<DebtTicket>().GetByIdGuid(debtId).Result;
            if (existedDebt == null)
            {
                return new BaseResponseViewModel<DebtTicketResponseModel>()
                {
                    Code = StatusCodes.Status404NotFound,
                    Message = "Not Found",
                };
            }
            if (status == (int)DebtStatusEnums.Complete)
                existedDebt.Status = (int)DebtStatusEnums.Complete;
            else if (status == (int)DebtStatusEnums.Cancel)
                existedDebt.Status = (int)DebtStatusEnums.Cancel;
            try
            {
                await _unitOfWork.Repository<DebtTicket>().UpdateDetached(existedDebt);
                await _unitOfWork.CommitAsync();
                return new BaseResponseViewModel<DebtTicketResponseModel>()
                {
                    Code = StatusCodes.Status204NoContent,
                    Message = "No content",
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<DebtTicketResponseModel>()
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = "Bad request",
                };
            }
        }
        public async Task<BaseResponseViewModel<DebtTicketResponseModel>> DeleteAsync(int debtId)
        {
            var existedDebt = _unitOfWork.Repository<DebtTicket>().GetById(debtId).Result;
            if (existedDebt == null)
            {
                return new BaseResponseViewModel<DebtTicketResponseModel>()
                {
                    Code = StatusCodes.Status404NotFound,
                    Message = "Not Found",
                };
            }
            existedDebt.Status = (int)DebtStatusEnums.Cancel;
            try
            {
                await _unitOfWork.Repository<DebtTicket>().UpdateDetached(existedDebt);
                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<DebtTicketResponseModel>()
                {
                    Code = StatusCodes.Status204NoContent,
                    Message = "No content",
                };
            }
            catch (Exception ex)
            {
                return new BaseResponseViewModel<DebtTicketResponseModel>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = "Bad Request" + "||" + ex.Message,
                };
            }
        }
        public async Task<BaseResponseViewModel<object>> GetValueForPayment(int companyId)
        {
            var enterprise = await _unitOfWork.Repository<Enterprise>().AsQueryable(x => x.CompanyId == companyId)
                                                            .Include(x => x.Account).ThenInclude(x => x.Wallets).FirstOrDefaultAsync();
            var EAWallet = enterprise.Account.Wallets.FirstOrDefault();
            var company = _unitOfWork.Repository<Company>().GetById(companyId).Result;

            // Lấy tất cả order đã đặt mà chưa thanh toán của company
            var orders = await _unitOfWork.Repository<Order>().AsQueryable(x => x.CompanyId == companyId && x.DebtStatus == (int)DebtStatusEnums.New && x.Status == (int)OrderStatusEnums.Complete).ToListAsync();

            var bill = new
            {
                Total = EAWallet.Used,
                //TimeOut = company.TimeOut,
                Orders = orders
            };

            return new BaseResponseViewModel<object>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = bill
            };
        }
    }
}
