using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.ResponseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AutoMapper.QueryableExtensions;
using LAK.Sdk.Core.Utilities;

namespace CES.BusinessTier.Services
{
    public interface IProductService
    {
        Task<DynamicResponse<ProductResponseModel>> GetAllProductAsync(ProductResponseModel filter, PagingModel paging);
        Task<BaseResponseViewModel<ProductResponseModel>> GetProductAsync(Guid productId, ProductResponseModel filter);
        Task<BaseResponseViewModel<ProductResponseModel>> CreateProductAsync(ProductRequestModel product);
        Task<BaseResponseViewModel<ProductResponseModel>> UpdateProductAsync(Guid productId, ProductUpdateModel productUpdate);
        Task<BaseResponseViewModel<ProductResponseModel>> DeleteProductAsync(Guid productId);
    }

    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICategoryService _categoryService;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ICategoryService categoryService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _categoryService = categoryService;
        }
        public async Task<BaseResponseViewModel<ProductResponseModel>> CreateProductAsync(ProductRequestModel product)
        {
            if (product == null)
            {
                throw new ErrorResponse(StatusCodes.Status400BadRequest, (int)ProductErrorEnums.INVALID_PRODUCT, ProductErrorEnums.INVALID_PRODUCT.GetDisplayName());
            }

            if (product.CategoryId != null)
            {
                var haveCate = _categoryService.ValidCategory((int)product.CategoryId).Result;
                if (!haveCate)
                {
                    throw new ErrorResponse(StatusCodes.Status400BadRequest, (int)CategoryErrorEnums.INVALID_CATEGORY, CategoryErrorEnums.INVALID_CATEGORY.GetDisplayName());
                }
            }
            var newProduct = _mapper.Map<Product>(product);
            newProduct.Id = Guid.NewGuid();
            newProduct.Status = (int)Status.Active;
            newProduct.CreatedAt = TimeUtils.GetCurrentSEATime();
            await _unitOfWork.Repository<Product>().InsertAsync(newProduct);
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<ProductResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = _mapper.Map<ProductResponseModel>(newProduct),
            };
        }

        public async Task<BaseResponseViewModel<ProductResponseModel>> DeleteProductAsync(Guid productId)
        {
            var product = await _unitOfWork.Repository<Product>().AsQueryable(x => x.Id == productId && x.Status == (int)Status.Active).FirstOrDefaultAsync();
            if (product == null) throw new ErrorResponse(StatusCodes.Status404NotFound, (int)CategoryErrorEnums.NOT_FOUND_CATEGORY, CategoryErrorEnums.NOT_FOUND_CATEGORY.GetDisplayName());
            product.Status = (int)Status.Inactive;
            await _unitOfWork.Repository<Product>().UpdateDetached(_mapper.Map<Product>(product));
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<ProductResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = _mapper.Map<ProductResponseModel>(product),
            };
        }

        public async Task<DynamicResponse<ProductResponseModel>> GetAllProductAsync(ProductResponseModel filter, PagingModel paging)
        {
            var result = _unitOfWork.Repository<Product>().AsQueryable(x => x.Status == (int)Status.Active)
                .ProjectTo<ProductResponseModel>(_mapper.ConfigurationProvider)
                .DynamicFilter(filter)
                .DynamicSort(paging.Sort, paging.Order)
                .PagingQueryable(paging.Page, paging.Size);
            return new DynamicResponse<ProductResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                MetaData = new PagingMetaData
                {
                    Page = paging.Page,
                    Size = paging.Size,
                    Total = result.Item1
                },
                Data = await result.Item2.ToListAsync(),
            };
        }

        public async Task<BaseResponseViewModel<ProductResponseModel>> GetProductAsync(Guid productId, ProductResponseModel filter)
        {
            var product = await _unitOfWork.Repository<Product>().AsQueryable(x => x.Id == productId && x.Status == (int)Status.Active)
                .ProjectTo<ProductResponseModel>(_mapper.ConfigurationProvider)
                .DynamicFilter(filter)
                .FirstOrDefaultAsync();
            if (product == null) throw new ErrorResponse(StatusCodes.Status404NotFound, (int)ProductErrorEnums.NOT_FOUND_PRODUCT, ProductErrorEnums.NOT_FOUND_PRODUCT.GetDisplayName());
            return new BaseResponseViewModel<ProductResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = product
            };
        }

        public async Task<BaseResponseViewModel<ProductResponseModel>> UpdateProductAsync(Guid productId, ProductUpdateModel productUpdate)
        {
            var product = await _unitOfWork.Repository<Product>().AsQueryable(x => x.Id == productId && x.Status == (int)Status.Active).FirstOrDefaultAsync();
            if (product == null) throw new ErrorResponse(StatusCodes.Status404NotFound, (int)ProductErrorEnums.NOT_FOUND_PRODUCT, ProductErrorEnums.NOT_FOUND_PRODUCT.GetDisplayName());
            product.UpdatedAt = TimeUtils.GetCurrentSEATime();
            await _unitOfWork.Repository<Product>().UpdateDetached(_mapper.Map<ProductUpdateModel, Product>(productUpdate, product));
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<ProductResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = _mapper.Map<ProductResponseModel>(product),
            };
        }
    }
}
