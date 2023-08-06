using CES.BusinessTier.RequestModels;
using CES.BusinessTier.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CES.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportServices _reportServices;

        public ReportController(IReportServices reportServices)
        {
            _reportServices = reportServices;
        }

        [HttpGet("ea")]
        public async Task<ActionResult> GetReportForEA([FromQuery] ReportRequestModel request)
        {
            var result = await _reportServices.GetReportForEA(request);
            return StatusCode((int)result.Code, result);
        }

        [HttpGet("sa")]
        public async Task<ActionResult> GetReportForSA([FromQuery] ReportRequestModel request)
        {
            var result = await _reportServices.GetReportForSA(request);
            return StatusCode((int)result.Code, result);
        }
    }
}
