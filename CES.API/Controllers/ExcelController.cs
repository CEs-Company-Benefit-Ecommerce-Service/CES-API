using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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
        /// Download Excel Template for Employee (Supplier, Employee can't use)
        /// </summary>
        /// <returns></returns>
        [HttpGet("account/template")]
        [Consumes("multipart/form-data")]
        public IActionResult UploadFile()
        {
            return _excelService.DownloadEmployeeTemplate();
        }

        /// <summary>
        /// Import Employees from template (Supplier, Employee can't use)
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [Authorize(Roles = "Enterprise Admin, System Admin")]
        [HttpPost("account/import")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<List<Account>>> ImportEmployees(IFormFile file)
        {
            var result = await _excelService.ImportEmployeeList(file);
            return Ok(result);
        }
    }
}
