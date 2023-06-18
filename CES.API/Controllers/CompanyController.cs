using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CES.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyServices _companyServices;
        private readonly IHttpContextAccessor _contextAccessor;

        public CompanyController(IHttpContextAccessor accessor, ICompanyServices companyServices)
        {
            _companyServices = companyServices;
            _contextAccessor = accessor;
        }

        [HttpGet]
        public async Task<ActionResult> Gets([FromQuery] CompanyResponseModel filter, [FromQuery] PagingModel paging)
        {
            var result = await _companyServices.Gets(filter, paging);
            return StatusCode((int)result.Code, result);
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var result = await _companyServices.GetById(id);
            return StatusCode((int)result.Code, result);
        }

        //[HttpGet("all-info")]
        //public async Task<ActionResult> GetAllInfo([FromQuery] CompanyAllInfoResponse filter, [FromQuery] PagingModel paging)
        //{
        //    var result = _companyServices.GetsAllInfo(filter, paging).Result;
        //    return StatusCode((int)result.Code, result);
        //}
        /// <summary>
        /// For System Admin
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "System Admin")]
        public async Task<ActionResult> Post([FromBody] CompanyRequestModel request)
        {
            var result = await _companyServices.CreateNew(request);
            return StatusCode((int)result.Code, result);
        }
        /// <summary>
        /// For System Admin
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "System Admin")]
        public async Task<ActionResult> Put(int id, [FromBody] CompanyRequestModel request)
        {
            var result = await _companyServices.Update(id, request);
            return StatusCode((int)result.Code, result);
        }
    }
}
