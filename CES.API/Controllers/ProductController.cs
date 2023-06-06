using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CES.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }
        
        // GET: api/Product
        [HttpGet]
        public async Task<ActionResult<DynamicResponse<ProductResponseModel>>> GetAllProduct([FromQuery] ProductResponseModel filter, [FromQuery] PagingModel paging)
        {
            return Ok(await _productService.GetAllProductAsync(filter, paging));
        }

        // GET: api/Product/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseViewModel<ProductResponseModel>>> GetProduct(Guid productId, [FromQuery] ProductResponseModel filter)
        {
            return Ok(await _productService.GetProductAsync(productId, filter));
        }

        // POST: api/Product
        [HttpPost]
        public async Task<ActionResult<BaseResponseViewModel<ProductResponseModel>>> CreateProduct([FromBody] ProductRequestModel product)
        {
            return Ok(await _productService.CreateProductAsync(product));
        }

        // PUT: api/Product/5
        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResponseViewModel<ProductResponseModel>>> UpdateProduct(Guid id, [FromBody] ProductUpdateModel productUpdate)
        {
            return Ok(await _productService.UpdateProductAsync(id, productUpdate));
        }

        // DELETE: api/Product/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResponseViewModel<ProductResponseModel>>> DeleteProduct(Guid id)
        {
            return Ok(await _productService.DeleteProductAsync(id));
        }
    }
}