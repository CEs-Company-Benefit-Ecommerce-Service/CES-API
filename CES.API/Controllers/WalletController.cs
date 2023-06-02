﻿using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Services;
using CES.BusinessTier.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CES.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletServices _walletServices;
        private readonly IHttpContextAccessor _contextAccessor;
        public WalletController(IWalletServices walletServices, IHttpContextAccessor contextAccessor)
        {
            _walletServices = walletServices;
            _contextAccessor = contextAccessor;
        }
        /// <summary>
        /// Employee can't use
        /// </summary>
        /// <param name="pagingModel"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Gets([FromQuery] PagingModel pagingModel)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role == Roles.Employee.GetDisplayName())
            {
                return StatusCode(401);
            }

            var result = _walletServices.GetsAsync(pagingModel).Result;
            return StatusCode((int)result.Code, result);
        }
        /// <summary>
        /// Employee can't use
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public IActionResult Get(Guid id)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role == Roles.Employee.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _walletServices.Get(id);
            return StatusCode((int)result.Code, result);
        }
        /// <summary>
        ///  Only Employee use
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpGet("account/{accountId}")]
        public IActionResult GetWalletAccount(Guid accountId)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role == Roles.SystemAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _walletServices.GetWalletsAccount(accountId);
            return StatusCode((int)result.Code, result);
        }
        /// <summary>
        /// API test create wallet
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult Post(WalletRequestModel request)
        {
            var result = _walletServices.CreateAsync(request).Result;
            return StatusCode((int)result.Code, result);
        }
        /// <summary>
        /// Update wallet info exclude balance, only System Admin can use
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public IActionResult Put(Guid id, WalletInfoRequestModel request)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.SystemAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _walletServices.UpdateWalletInfoAsync(id, request).Result;
            return StatusCode((int)result.Code, result);
        }
        /// <summary>
        /// update wallet balance, only Enterprise can use
        /// </summary>
        /// <param name="id"></param>
        /// <param name="balance"></param>
        /// <returns></returns>
        [HttpPut("{id}/{balance}")]
        public IActionResult Put(Guid id, double balance)
        {
            var role = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role).Value.ToString();
            if (role != Roles.EnterpriseAdmin.GetDisplayName())
            {
                return StatusCode(401);
            }
            var result = _walletServices.UpdateWalletBalanceAsync(id, balance).Result;
            return StatusCode((int)result.Code, result);
        }
    }
}