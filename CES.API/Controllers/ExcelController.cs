using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.Services;
using CES.DataTier.Models;
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
        
        // GET: api/Excel
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Excel/5
        [HttpGet("{id}", Name = "Get")]
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Excel
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try
            {
                if (file != null && file.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

                    var filePath = Path.Combine("uploads", fileName);

                    var directory = Path.GetDirectoryName(filePath);
                    Directory.CreateDirectory(directory);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (ExcelPackage package = new ExcelPackage())
                    {
                        // var worksheet = package.Workbook.Worksheets[0];
                        // var records = new List<Person>();
                        // for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                        // {
                        //     var name = worksheet.Cells[row, 1].Value?.ToString();
                        //     var age = int.Parse(worksheet.Cells[row, 2].Value?.ToString() ?? "0");
                        //     var sex = worksheet.Cells[row, 3].Value?.ToString();
                        //     var newPerson = new Person()
                        //     {
                        //         Name = name,
                        //         Age = age,
                        //         Sex = sex
                        //     };
                        //     records.Add(newPerson);
                        // }
                        ExcelWorksheet ws = package.Workbook.Worksheets.Add("Company");
                        ws.Cells["A1"].Value = "Company Name:";
                        ws.Cells["A1"].Style.Font.Bold = true;
                        ws.Cells["A1"].Style.Font.Size = 16;
                        ws.Cells["A1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        
                        // Auto-fit columns for better readability
                        ws.Cells.AutoFitColumns();

                        // var memoryStream = new MemoryStream();
                        // package.SaveAs(memoryStream);
                        //
                        // var response = new HttpResponseMessage(HttpStatusCode.OK);
                        // response.Content = new ByteArrayContent(memoryStream.ToArray());
                        // response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                        // {
                        //     FileName = "data.xlsx" // Specify the desired file name
                        // };
                        // response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                        //
                        // return Ok(response);
                        
                        var stream = new MemoryStream(package.GetAsByteArray());

                        return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                        {
                            FileDownloadName = "data.xlsx" // Specify the desired file name
                        };
                    }
                }

                return BadRequest("No file was upload");
            }
            catch (Exception e)
            {
                return BadRequest($"No file was upload {e}");
            }
        }

        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<List<Account>>> ImportEmplyees(IFormFile file)
        {
            var result = await _excelService.ImportEmployeeList(file);
            return Ok(result);
        }

        // PUT: api/Excel/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/Excel/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
