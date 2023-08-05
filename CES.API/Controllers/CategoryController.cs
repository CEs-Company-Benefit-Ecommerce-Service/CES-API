using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.Services;
using Microsoft.AspNetCore.Mvc;
using CES.BusinessTier.RequestModels;
using CES.DataTier.Models;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CES.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        
        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }
        // GET: api/<CategoryController>
        [SwaggerOperation(summary: "Get categories", description: "Sort: {Parameter}, Order: asc || desc")]
        [HttpGet]
        [Authorize(Roles = "System Admin, Supplier Admin, Employee")]
        public async Task<ActionResult<DynamicResponse<CategoryResponseModel>>> GetAllCategory([FromQuery] CategoryResponseModel filter, [FromQuery] PagingModel paging)
        {
            return Ok(await _categoryService.GetAllCategoryAsync(filter, paging));
        }

        // GET api/<CategoryController>/5
        [HttpGet("{id}")]
        [Authorize(Roles = "System Admin, Supplier Admin, Employee")]
        public async Task<ActionResult<BaseResponseViewModel<CategoryResponseModel>>> GetCategoryById(int id, [FromQuery] CategoryResponseModel filter)
        {
            return Ok(await _categoryService.GetCategoryAsync(id, filter));
        }

        // POST api/<CategoryController>
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<CategoryResponseModel>>> PostCategory([FromBody] CategoryRequestModel category)
        {
            return Ok(await _categoryService.CreateCategoryAsync(category));
        }

        // PUT api/<CategoryController>/5
        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResponseViewModel<CategoryResponseModel>>> PutCategory(int id, [FromBody] CategoryUpdateModel category)
        {
            return Ok(await _categoryService.UpdateCategoryAsync(id, category));
        }

        // DELETE api/<CategoryController>/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResponseViewModel<CategoryResponseModel>>> Delete(int id)
        {
            return Ok(await _categoryService.DeleteCategoryAsync(id));
        }
    }
}
