﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using FirebaseAdmin.Messaging;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using LAK.Sdk.Core.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Services
{
    public interface IOrderServices
    {
        Task<DynamicResponse<OrderResponseModel>> GetsAsync(OrderResponseModel filter, PagingModel paging, int? type, FilterFromTo filterFromTo, Guid accountId);
        Task<BaseResponseViewModel<OrderResponseModel>> UpdateOrderStatus(Guid orderId, int status);
        Task<BaseResponseViewModel<OrderResponseModel>> CreateOrder(List<OrderDetailsRequestModel> orderDetails, string? note);
        Task<BaseResponseViewModel<OrderResponseModel>> GetById(Guid id);
        Task<TotalOrderResponse> GetTotal(int companyId);
        Task<DynamicResponse<OrderResponseModel>> GetsBySupplierId(Guid id, OrderResponseModel filter, PagingModel paging);

    }
    public class OrderServices : IOrderServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderDetailServices _orderDetailServices;
        private readonly IProductService _productServices;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ITransactionService _transactionService;
        private readonly IWalletServices _walletServices;

        public OrderServices(IUnitOfWork unitOfWork, IMapper mapper, IOrderDetailServices orderDetailServices, IProductService productServices, IHttpContextAccessor httpContextAccessor, ITransactionService transactionService, IWalletServices walletServices)
        {
            _mapper = mapper;
            _contextAccessor = httpContextAccessor;
            _unitOfWork = unitOfWork;
            _orderDetailServices = orderDetailServices;
            _productServices = productServices;
            _transactionService = transactionService;
            _walletServices = walletServices;
        }

        public async Task<DynamicResponse<OrderResponseModel>> GetsAsync(OrderResponseModel filter, PagingModel paging, int? type, FilterFromTo filterFromTo, Guid accountId)
        {
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
            var order = _unitOfWork.Repository<Order>().AsQueryable()
                           .Include(x => x.Employee)
                           .ProjectTo<OrderResponseModel>(_mapper.ConfigurationProvider)
                           .DynamicFilter(filter)
                           .DynamicSort(paging.Sort, paging.Order)
                           .PagingQueryable(paging.Page, paging.Size);
            DateTime? from = new DateTime();
            DateTime? to = new DateTime();
            if (paging.Sort == "CreatedAt" && paging.Order == "DESC" && filter.DebtStatus == 0)
            {
                from = order.Item2.LastOrDefault().CreatedAt;
                to = order.Item2.FirstOrDefault().CreatedAt;
            }
            if (accountId != Guid.Empty)
            {
                order = _unitOfWork.Repository<Order>().AsQueryable()
                           .Include(x => x.Employee)
                           .Where(w => w.Employee.AccountId == accountId)
                           .ProjectTo<OrderResponseModel>(_mapper.ConfigurationProvider)
                           .DynamicFilter(filter)
                           .DynamicSort(paging.Sort, paging.Order)
                           .PagingQueryable(paging.Page, paging.Size);
            }
            var account = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == accountLoginId).Include(x => x.Employees).FirstOrDefaultAsync();
            if (filterFromTo.To != null && filterFromTo.From != null)
            {
                order.Item2 = order.Item2.Where(x => x.CreatedAt.Value >= filterFromTo.From && x.CreatedAt.Value <= TimeUtils.GetEndOfDate((DateTime)filterFromTo.To));
            }
            if (account.Role == Roles.Employee.GetDisplayName())
            {
                var result = order.Item2.Where(x => x.EmployeeId == account.Employees.FirstOrDefault().Id);
                if (type == (int)TypeOfGetAllOrder.InComing)
                {
                    result = result.Where(x => x.Status == (int)OrderStatusEnums.New || x.Status == (int)OrderStatusEnums.Ready || x.Status == (int)OrderStatusEnums.Shipping);
                }
                else if (type == (int)TypeOfGetAllOrder.History)
                {
                    result = result.Where(x => x.Status == (int)OrderStatusEnums.Complete || x.Status == (int)OrderStatusEnums.Cancel);
                }
                return new DynamicResponse<OrderResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    SystemCode = "000",
                    Message = "OK",
                    MetaData = new PagingMetaData
                    {
                        Page = paging.Page,
                        Size = paging.Size,
                        Total = result.Count()
                    },
                    Data = await result.ToListAsync(),
                };
            }
            else if (account.Role == Roles.Shipper.GetDisplayName())
            {
                var result = order.Item2;
                if (type == (int)TypeOfGetAllOrder.InComing)
                {
                    result = result.Where(x => x.Status == (int)OrderStatusEnums.New || x.Status == (int)OrderStatusEnums.Ready || x.Status == (int)OrderStatusEnums.Shipping);
                }
                else if (type == (int)TypeOfGetAllOrder.History)
                {
                    result = result.Where(x => x.Status == (int)OrderStatusEnums.Complete || x.Status == (int)OrderStatusEnums.Cancel);
                }
                return new DynamicResponse<OrderResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    SystemCode = "000",
                    Message = "OK",
                    MetaData = new PagingMetaData
                    {
                        Page = paging.Page,
                        Size = paging.Size,
                        Total = result.Count()
                    },
                    Data = await result.ToListAsync(),
                };
            }
            return new DynamicResponse<OrderResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = from.ToString() + "-" + to.ToString(),
                MetaData = new PagingMetaData
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = order.Item1
                },
                Data = await order.Item2.ToListAsync(),
            };
        }

        public async Task<BaseResponseViewModel<OrderResponseModel>> GetById(Guid id)
        {
            // var orderDetail = await _unitOfWork.Repository<Order>().AsQueryable()
            //     .Include(x => x.Account)
            //     .Include(x => x.OrderDetail)
            //     .ThenInclude(x => x.Product)
            //     .Where(x => x.Id == id).FirstOrDefaultAsync();
            var orderDetail = await _unitOfWork.Repository<Order>().AsQueryable(x => x.Id == id).Include(x => x.Employee).ThenInclude(x => x.Account).ThenInclude(x => x.Wallets).Include(x => x.OrderDetails).ThenInclude(x => x.Product).ThenInclude(x => x.Supplier).ThenInclude(x => x.Account)
                .FirstOrDefaultAsync();
            return new BaseResponseViewModel<OrderResponseModel>
            {
                Code = StatusCodes.Status200OK,
                SystemCode = "000",
                Message = "OK",
                Data = _mapper.Map<OrderResponseModel>(orderDetail)
            };
        }
        public async Task<BaseResponseViewModel<OrderResponseModel>> UpdateOrderStatus(Guid orderId, int status)
        {
            var existedOrder = await _unitOfWork.Repository<Order>().AsQueryable(x => x.Id == orderId).FirstOrDefaultAsync();
            if (existedOrder == null)
            {
                return new BaseResponseViewModel<OrderResponseModel>
                {
                    Code = StatusCodes.Status404NotFound,
                    SystemCode = "031",
                    Message = "Not found",
                };
            }

            if (existedOrder.Status != (int)OrderStatusEnums.New && status == (int)OrderStatusEnums.Cancel)
            {
                throw new ErrorResponse(StatusCodes.Status400BadRequest, 400,
                    "Order is waiting for ship, you can not cancel order");
            }

            existedOrder.Status = status;
            existedOrder.UpdatedAt = TimeUtils.GetCurrentSEATime();

            var stringStatus = Commons.ConvertIntOrderStatusToString(existedOrder.Status);

            var employee = await _unitOfWork.Repository<Employee>().AsQueryable(x => x.Id == existedOrder.EmployeeId).FirstOrDefaultAsync();

            var accountEmp = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == employee.AccountId)
                .Include(x => x.Wallets)
                .FirstOrDefaultAsync();

            var empNotification = new DataTier.Models.Notification()
            {
                Id = Guid.NewGuid(),
                Title = "Cập nhật trạng thái đơn hàng",
                Description = "Đơn hàng của bạn đã chuyển sang trạng thái: " + stringStatus,
                OrderId = existedOrder.Id,
                IsRead = false,
                CreatedAt = TimeUtils.GetCurrentSEATime(),
                AccountId = accountEmp.Id
            };

            // send noti
            if (accountEmp.FcmToken != null && !String.IsNullOrWhiteSpace(accountEmp.FcmToken))
            {
                var messaging = FirebaseMessaging.DefaultInstance;
                var response = await messaging.SendAsync(new Message
                {
                    Token = accountEmp.FcmToken,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = "Cập nhật trạng thái đơn hàng",
                        Body = "Đơn hàng của bạn đã chuyển sang trạng thái: " + stringStatus,
                    },
                });
                if (response == null)
                {
                    System.Console.WriteLine("Send noti failed");
                }
            }

            if (existedOrder.Status == (int)OrderStatusEnums.Cancel)
            {
                var eaAccount = await _unitOfWork.Repository<Account>().AsQueryable()
                    .Include(x => x.Enterprises)
                    .Include(x => x.Wallets)
                    .Where(x => x.Enterprises.Select(x => x.CompanyId).FirstOrDefault() == existedOrder.CompanyId)
                    .FirstOrDefaultAsync();
                var eaNotification = new DataTier.Models.Notification()
                {
                    Id = Guid.NewGuid(),
                    Title = "",
                    Description = "Đơn hàng của " + accountEmp.Name + "đã bị hủy",
                    OrderId = existedOrder.Id,
                    IsRead = false,
                    CreatedAt = TimeUtils.GetCurrentSEATime(),
                    AccountId = eaAccount.Id
                };
                await _unitOfWork.Repository<DataTier.Models.Notification>().InsertAsync(eaNotification);

                #region Hoàn tiền emp

                var cashBackTotal = existedOrder.Total - Constants.ServiceFee;
                accountEmp.Wallets.First().Balance += cashBackTotal;
                var walletTransaction = new Transaction()
                {
                    Id = Guid.NewGuid(),
                    WalletId = accountEmp.Wallets.First().Id,
                    Type = (int)WalletTransactionTypeEnums.CashBack,
                    Description = $"Hoàn {cashBackTotal} từ đơn hàng {existedOrder.OrderCode}",
                    OrderId = existedOrder.Id,
                    RecieveId = accountEmp.Id,
                    Total = (double)cashBackTotal,
                    CompanyId = employee.CompanyId,
                    CreatedAt = TimeUtils.GetCurrentSEATime(),
                };
                await _unitOfWork.Repository<Transaction>().InsertAsync(walletTransaction);

                #endregion
            }
            else if (existedOrder.Status == (int)OrderStatusEnums.Ready)
            {
                var eaAccount = await _unitOfWork.Repository<Account>().AsQueryable()
                    .Include(x => x.Enterprises)
                    .ThenInclude(x => x.Company)
                    .Include(x => x.Wallets)
                    .Where(x => x.Enterprises.Select(x => x.CompanyId).FirstOrDefault() == existedOrder.CompanyId)
                    .FirstOrDefaultAsync();
                var useTotal = existedOrder.Total;
                eaAccount.Wallets.First().Used += useTotal;
                eaAccount.UpdatedAt = TimeUtils.GetCurrentSEATime();

                var limit = eaAccount.Enterprises.First().Company.Limits;
                var fiftyPercent = 50 / 100;
                var seventyFivePercent = 75 / 100;
                var onehundredPercent = 100 / 100;
                var halfOfLimit =  limit / 2;

                if (useTotal >= halfOfLimit)
                {
                    var usedPercent = useTotal / limit;
                    if (usedPercent >= onehundredPercent)
                    {
                        var eaNotification = new DataTier.Models.Notification()
                        {
                            Id = Guid.NewGuid(),
                            Title = "Used 100% of the value of the limit",
                            Description = "Used 100% of the value of the limit",
                            IsRead = false,
                            CreatedAt = TimeUtils.GetCurrentSEATime(),
                            AccountId = eaAccount.Id
                        };
                        await _unitOfWork.Repository<DataTier.Models.Notification>().UpdateDetached(eaNotification);
                    } else if (usedPercent >= seventyFivePercent)
                    {
                        var eaNotification = new DataTier.Models.Notification()
                        {
                            Id = Guid.NewGuid(),
                            Title = "75% of the value of the limit has been used",
                            Description = "75% of the value of the limit has been used",
                            IsRead = false,
                            CreatedAt = TimeUtils.GetCurrentSEATime(),
                            AccountId = eaAccount.Id
                        };
                        await _unitOfWork.Repository<DataTier.Models.Notification>().UpdateDetached(eaNotification);
                    }
                    else
                    {
                        var eaNotification = new DataTier.Models.Notification()
                        {
                            Id = Guid.NewGuid(),
                            Title = "Used 50% of the value of the limit",
                            Description = "Used 50% of the value of the limit",
                            IsRead = false,
                            CreatedAt = TimeUtils.GetCurrentSEATime(),
                            AccountId = eaAccount.Id
                        };
                        await _unitOfWork.Repository<DataTier.Models.Notification>().UpdateDetached(eaNotification);
                    }
                }

                await _unitOfWork.Repository<Account>().UpdateDetached(eaAccount);
            }
            await _unitOfWork.Repository<DataTier.Models.Notification>().InsertAsync(empNotification);
            await _unitOfWork.Repository<Order>().UpdateDetached(existedOrder);
            await _unitOfWork.CommitAsync();

            return new BaseResponseViewModel<OrderResponseModel>
            {
                Code = StatusCodes.Status204NoContent,
                SystemCode = "032",
                Message = "No content",
            };
        }

        public async Task<BaseResponseViewModel<OrderResponseModel>> CreateOrder(List<OrderDetailsRequestModel> orderDetails, string? note)
        {
            #region valid order datetime

            // var current = TimeUtils.GetCurrentSEATime();
            // var startOfDate = new DateTime(current.Year, current.Month, current.Day, 6, 0, 0);
            // var endOfDate = new DateTime(current.Year, current.Month, current.Day, 18, 0, 0);
            // if (current < startOfDate || current > endOfDate)
            // {
            //     throw new ErrorResponse(StatusCodes.Status400BadRequest, 400, "Can not order at this time");
            // }

            #endregion

            #region get logined account + company +  EA account + EA wallet
            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
            var accountLogin = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == accountLoginId).Include(x => x.Wallets).FirstOrDefaultAsync();

            var companyId = await GetCompany(accountLoginId);
            var company = _unitOfWork.Repository<Company>().GetWhere(x => x.Id == companyId).Result.FirstOrDefault();

            // var enterprise = await _unitOfWork.Repository<Enterprise>().AsQueryable(x => x.CompanyId == companyId)
            //                                                             .Include(x => x.Account).ThenInclude(x => x.Wallets)
            //                                                             .FirstOrDefaultAsync();
            // var enterpriseWallet = enterprise.Account.Wallets.FirstOrDefault();
            #endregion

            #region caculate orderDetail price + total
            foreach (var orderDetail in orderDetails)
            {
                var product = _productServices.GetProductAsync((Guid)orderDetail.ProductId, new ProductResponseModel()).Result;
                if (product.Data.Quantity < orderDetail.Quantity)
                {
                    return new BaseResponseViewModel<OrderResponseModel>
                    {
                        Code = StatusCodes.Status400BadRequest,
                        Message = "Product quantity not enough",
                    };
                }
                orderDetail.Price = orderDetail.Quantity * product.Data.Price;
            }

            var total = orderDetails.Select(x => x.Price).Sum();
            // get accountLogin wallet
            var wallet = accountLogin.Wallets.Where(x => x.AccountId == accountLoginId).FirstOrDefault();
            if (wallet.Balance < total)
            {
                return new BaseResponseViewModel<OrderResponseModel>
                {
                    Code = StatusCodes.Status400BadRequest,
                    Message = "Balance in wallet not enough",
                };
            }
            #endregion
            try
            {
                var employee = _unitOfWork.Repository<Employee>().GetWhere(x => x.AccountId == accountLoginId).Result.FirstOrDefault();
                var orderCountCurrentDay = _unitOfWork.Repository<Order>().AsQueryable(x => x.CreatedAt.Date == TimeUtils.GetCurrentSEATime().Date).Count();
                var orderCode = "CES-" + TimeUtils.GetCurrentSEATime().ToString("ddMMyy") + orderCountCurrentDay;
                // create order
                var finalTotal = (double)total + Constants.ServiceFee;
                var newOrder = new Order()
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = TimeUtils.GetCurrentSEATime(),
                    EmployeeId = employee.Id,
                    Status = (int)OrderStatusEnums.New,
                    Total = finalTotal,
                    Address = company.Address,
                    CompanyName = company.Name,
                    Notes = note,
                    DebtStatus = (int)DebtStatusEnums.New,
                    CompanyId = companyId,
                    OrderCode = orderCode
                };
                await _unitOfWork.Repository<Order>().InsertAsync(newOrder);

                // create order details
                if (!await _orderDetailServices.Create(orderDetails, newOrder.Id))
                {
                    return new BaseResponseViewModel<OrderResponseModel>
                    {
                        Code = StatusCodes.Status400BadRequest,
                        SystemCode = "033",
                        Message = "Create order details failed",
                    };
                }

                // update Emp wallet balance + EA wallet used
                wallet.Balance -= finalTotal;
                wallet.UpdatedAt = TimeUtils.GetCurrentSEATime();

                // enterpriseWallet.Used += finalTotal;
                // enterpriseWallet.UpdatedAt = TimeUtils.GetCurrentSEATime();

                //create new transaction
                var walletTransaction = new Transaction()
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    Type = (int)WalletTransactionTypeEnums.Order,
                    Description = "Shopping",
                    OrderId = newOrder.Id,
                    RecieveId = accountLoginId,
                    Total = (double)finalTotal,
                    CompanyId = companyId,
                    CreatedAt = TimeUtils.GetCurrentSEATime(),
                };
                foreach (var orderDetail in orderDetails)
                {
                    var product = _unitOfWork.Repository<Product>().GetByIdGuid(orderDetail.ProductId);
                    product.Result.Quantity = product.Result.Quantity - (int)orderDetail.Quantity;
                    await _unitOfWork.Repository<Product>().UpdateDetached(product.Result);
                }
                // create notification for employee
                var empNotification = new DataTier.Models.Notification()
                {
                    Id = Guid.NewGuid(),
                    Title = "Shopping",
                    Description = "You have successfully placed an order",
                    AccountId = accountLoginId,
                    OrderId = newOrder.Id,
                    IsRead = false,
                    CreatedAt = TimeUtils.GetCurrentSEATime(),
                };

                await _unitOfWork.Repository<DataTier.Models.Notification>().InsertAsync(empNotification);

                await _unitOfWork.Repository<Transaction>().InsertAsync(walletTransaction);

                await _unitOfWork.Repository<Wallet>().UpdateDetached(wallet);

                // await _unitOfWork.Repository<Wallet>().UpdateDetached(enterpriseWallet);

                await _unitOfWork.CommitAsync();

                return new BaseResponseViewModel<OrderResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    SystemCode = "034",
                    Message = "OK",
                };
            }
            catch (Exception)
            {
                return new BaseResponseViewModel<OrderResponseModel>
                {
                    Code = StatusCodes.Status400BadRequest,
                    SystemCode = "033",
                    Message = "Something was wrong!",
                };

            }
        }
        public async Task<TotalOrderResponse> GetTotal(int companyId)
        {
            var ordersOfCompany = _unitOfWork.Repository<Order>()
                .AsQueryable();
            var sum = await ordersOfCompany.Select(x => x.Total).SumAsync();
            var listOrderId = await ordersOfCompany.Select(x => x.Id).ToListAsync();
            return new TotalOrderResponse()
            {
                Total = sum,
                OrderIds = listOrderId
            };
        }

        private async Task<int> GetCompany(Guid accountLoginId)
        {
            var accountLogin = await _unitOfWork.Repository<Account>().AsQueryable(x => x.Id == accountLoginId)
                                                                        .FirstOrDefaultAsync();
            if (accountLogin.Role == Roles.EnterpriseAdmin.GetDisplayName())
            {
                var user = _unitOfWork.Repository<Enterprise>().GetWhere(x => x.AccountId == accountLoginId).Result.FirstOrDefault();
                return user.CompanyId;
            }
            else if (accountLogin.Role == Roles.Employee.GetDisplayName())
            {
                var user = _unitOfWork.Repository<Employee>().GetWhere(x => x.AccountId == accountLoginId).Result.FirstOrDefault();
                return user.CompanyId;
            }
            return 0;
        }
        public async Task<DynamicResponse<OrderResponseModel>> GetsBySupplierId(Guid id, OrderResponseModel filter, PagingModel paging)
        {
            var orders = _unitOfWork.Repository<Order>().AsQueryable().Include(x => x.OrderDetails).ThenInclude(x => x.Product)
                                                        .Where(x => x.OrderDetails.FirstOrDefault().Product.SupplierId == id)
                                                        .ProjectTo<OrderResponseModel>(_mapper.ConfigurationProvider)
                                                        .DynamicFilter(filter)
                                                        .DynamicSort(paging.Sort, paging.Order)
                                                        .PagingQueryable(paging.Page, paging.Size);
            //if (orders.Item1 == 0)
            //{
            //    return new DynamicResponse<OrderResponseModel>()
            //    {
            //        Code = StatusCodes.Status404NotFound,
            //        SystemCode = "...",
            //        Message = "...",
            //        MetaData = new PagingMetaData()
            //        {
            //            Page = 1,
            //            Size = 1,
            //            Total = 0
            //        },
            //    };
            //}
            return new DynamicResponse<OrderResponseModel>()
            {
                Code = StatusCodes.Status200OK,
                SystemCode = "...",
                Message = "...",
                MetaData = new PagingMetaData()
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = orders.Item1
                },
                Data = _mapper.Map<List<OrderResponseModel>>(orders.Item2)
            };

        }

    }
}
