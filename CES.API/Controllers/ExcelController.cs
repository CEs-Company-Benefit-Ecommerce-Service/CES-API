using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.Services;
using CES.DataTier.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace CES.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExcelController : ControllerBase
    {
        private readonly IExcelService _excelService;

        public ExcelController(IExcelService excelService)
        {
            _excelService = excelService;
        }

        /// <summary>
        /// Download Excel Template for Employee
        /// </summary>
        /// <returns></returns>
        [HttpGet("account/template")]
        [Consumes("multipart/form-data")]
        public IActionResult UploadFile()
        {
            return _excelService.DownloadEmployeeTemplate();
        }
        
        /// <summary>
        /// Download list business's employee (System, Supplier, Employee can't use)
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Enterprise Admin")]
        [HttpGet("account/download")]
        [Consumes("multipart/form-data")]
        public IActionResult DownloadEmployeeList([FromQuery] DateRangeFilterModel dateRangeFilter)
        {
            return _excelService.DownloadListEmployeeForCompany(dateRangeFilter);
        }

        /// <summary>
        /// Import Employees from template (System, Supplier, Employee can't use)
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [Authorize(Roles = "Enterprise Admin")]
        [HttpPost("account/import")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<List<Account>>> ImportEmployees(IFormFile file)
        {
            var result = await _excelService.ImportEmployeeList(file);
            return Ok(result);
        }

        /// <summary>
        /// Download Excel Template for Product
        /// </summary>
        /// <returns></returns>
        [HttpGet("product/template")]
        [Consumes("multipart/form-data")]
        public IActionResult DownloadProductTemplate()
        {
            return _excelService.DownloadProductTemplate();
        }
        
        /// <summary>
        /// Import Products from template (System, Enterprise, Employee can't use)
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [Authorize(Roles = "Supplier Admin")]
        [HttpPost("product/import")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<List<Account>>> ImportProductList(IFormFile file)
        {
            var result = await _excelService.ImportProductList(file);
            return Ok(result);
        }
    }
}
