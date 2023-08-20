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
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        private readonly IHttpContextAccessor _contextAccessor;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper, ICategoryService categoryService, IHttpContextAccessor contextAccessor)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _categoryService = categoryService;
            _contextAccessor = contextAccessor;
        }
        public async Task<BaseResponseViewModel<ProductResponseModel>> CreateProductAsync(ProductRequestModel product)
        {
            #region Validate amount
            if (!Commons.ValidateAmount(product.Price) || !Commons.ValidateAmount(product.Quantity))
            {
                throw new ErrorResponse(StatusCodes.Status400BadRequest, 400, "Số tiền không hợp lệ");
            }
            #endregion

            Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
            var supplier = await _unitOfWork.Repository<Supplier>().AsQueryable(x => x.AccountId == accountLoginId).FirstOrDefaultAsync();
            if (supplier == null)
            {
                throw new ErrorResponse(StatusCodes.Status400BadRequest, (int)ProductErrorEnums.INVALID_PRODUCT, AccountErrorEnums.NOT_HAVE_PERMISSION.GetDisplayName());
            }
            if (product == null)
            {
                throw new ErrorResponse(StatusCodes.Status400BadRequest, 035, ProductErrorEnums.INVALID_PRODUCT.GetDisplayName());
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
            newProduct.SupplierId = supplier.Id;
            await _unitOfWork.Repository<Product>().InsertAsync(newProduct);
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<ProductResponseModel>
            {
                Code = StatusCodes.Status200OK,
                SystemCode = "036",
                Message = "OK",
                Data = _mapper.Map<ProductResponseModel>(newProduct),
            };
        }

        public async Task<BaseResponseViewModel<ProductResponseModel>> DeleteProductAsync(Guid productId)
        {
            var product = await _unitOfWork.Repository<Product>().AsQueryable(x => x.Id == productId && x.Status == (int)Status.Active).FirstOrDefaultAsync();
            if (product == null) throw new ErrorResponse(StatusCodes.Status404NotFound, 038, CategoryErrorEnums.NOT_FOUND_CATEGORY.GetDisplayName());
            product.Status = (int)Status.Inactive;
            await _unitOfWork.Repository<Product>().UpdateDetached(_mapper.Map<Product>(product));
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<ProductResponseModel>
            {
                Code = StatusCodes.Status200OK,
                SystemCode = "037",
                Message = "OK",
                Data = _mapper.Map<ProductResponseModel>(product),
            };
        }

        public async Task<DynamicResponse<ProductResponseModel>> GetAllProductAsync(ProductResponseModel filter, PagingModel paging)
        {
            try
            {
                var result = _unitOfWork.Repository<Product>().AsQueryable(x => x.Status == (int)Status.Active)
               .ProjectTo<ProductResponseModel>(_mapper.ConfigurationProvider)
               .DynamicFilter(filter)
               .DynamicSort(paging.Sort, paging.Order)
               .PagingQueryable(paging.Page, paging.Size);

                var products = await result.Item2.ToListAsync();
                foreach (var product in products)
                {
                    var discount = await _unitOfWork.Repository<Discount>()
                        .AsQueryable(x =>
                            x.ProductId == product.Id && x.Status == (int)Status.Active &&
                            x.ExpiredDate >= TimeUtils.GetCurrentSEATime())
                        .FirstOrDefaultAsync();
                    if (discount != null && product.PreDiscount == null)
                    {
                        var entityProduct = await _unitOfWork.Repository<Product>()
                            .AsQueryable(x => x.Status == (int)Status.Active && x.Id == product.Id)
                            .FirstOrDefaultAsync();
                        entityProduct.PreDiscount = product.Price;
                        entityProduct.Price -= (double)discount.Amount;
                        product.PreDiscount = entityProduct.PreDiscount;
                        product.Price = entityProduct.Price;
                        await _unitOfWork.Repository<Product>().UpdateDetached(entityProduct);
                    }
                    else if(discount == null && product.PreDiscount != null)
                    {
                        var entityProduct = await _unitOfWork.Repository<Product>()
                            .AsQueryable(x => x.Status == (int)Status.Active && x.Id == product.Id)
                            .FirstOrDefaultAsync();
                        entityProduct.Price = (double)entityProduct.PreDiscount;
                        entityProduct.PreDiscount = null;
                        product.PreDiscount = entityProduct.PreDiscount;
                        product.Price = entityProduct.Price;
                        await _unitOfWork.Repository<Product>().UpdateDetached(entityProduct);
                    } else if (discount != null && product.PreDiscount != null)
                    {
                        var entityProduct = await _unitOfWork.Repository<Product>()
                            .AsQueryable(x => x.Status == (int)Status.Active && x.Id == product.Id)
                            .FirstOrDefaultAsync();
                        entityProduct.Price = (double)entityProduct.PreDiscount - (double)discount.Amount;
                        product.PreDiscount = entityProduct.PreDiscount;
                        product.Price = entityProduct.Price;
                        await _unitOfWork.Repository<Product>().UpdateDetached(entityProduct);
                    }
                }

                await _unitOfWork.CommitAsync();

                return new DynamicResponse<ProductResponseModel>
                {
                    Code = StatusCodes.Status200OK,
                    SystemCode = "000",
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
            catch (Exception ex)
            {
                return new DynamicResponse<ProductResponseModel>
                {
                    Code = StatusCodes.Status400BadRequest,
                    SystemCode = "038",
                    Message = ex.Message,
                    MetaData = new PagingMetaData
                    {
                        Page = paging.Page,
                        Size = paging.Size,
                        Total = 0
                    }
                };
            }
           
           
        }

        public async Task<BaseResponseViewModel<ProductResponseModel>> GetProductAsync(Guid productId, ProductResponseModel filter)
        {
            var product = await _unitOfWork.Repository<Product>().AsQueryable(x => x.Id == productId && x.Status == (int)Status.Active)
                .ProjectTo<ProductResponseModel>(_mapper.ConfigurationProvider)
                .DynamicFilter(filter)
                .FirstOrDefaultAsync();
            if (product == null) throw new ErrorResponse(StatusCodes.Status404NotFound, (int)ProductErrorEnums.NOT_FOUND_PRODUCT, ProductErrorEnums.NOT_FOUND_PRODUCT.GetDisplayName());
            
            var discount = await _unitOfWork.Repository<Discount>()
                .AsQueryable(x =>
                    x.ProductId == product.Id && x.Status == (int)Status.Active &&
                    x.ExpiredDate >= TimeUtils.GetCurrentSEATime())
                .FirstOrDefaultAsync();
            if (discount != null && product.PreDiscount == null)
            {
                var entityProduct = await _unitOfWork.Repository<Product>()
                    .AsQueryable(x => x.Status == (int)Status.Active && x.Id == product.Id)
                    .FirstOrDefaultAsync();
                entityProduct.PreDiscount = product.Price;
                entityProduct.Price -= (double)discount.Amount;
                product.PreDiscount = entityProduct.PreDiscount;
                product.Price = entityProduct.Price;
                await _unitOfWork.Repository<Product>().UpdateDetached(entityProduct);
            }
            else if(discount == null && product.PreDiscount != null)
            {
                var entityProduct = await _unitOfWork.Repository<Product>()
                    .AsQueryable(x => x.Status == (int)Status.Active && x.Id == product.Id)
                    .FirstOrDefaultAsync();
                entityProduct.Price = (double)entityProduct.PreDiscount;
                entityProduct.PreDiscount = null;
                product.PreDiscount = entityProduct.PreDiscount;
                product.Price = entityProduct.Price;
                await _unitOfWork.Repository<Product>().UpdateDetached(entityProduct);
            } else if (discount != null && product.PreDiscount != null)
            {
                var entityProduct = await _unitOfWork.Repository<Product>()
                    .AsQueryable(x => x.Status == (int)Status.Active && x.Id == product.Id)
                    .FirstOrDefaultAsync();
                entityProduct.Price = (double)entityProduct.PreDiscount - (double)discount.Amount;
                product.PreDiscount = entityProduct.PreDiscount;
                product.Price = entityProduct.Price;
                await _unitOfWork.Repository<Product>().UpdateDetached(entityProduct);
            }
            
            return new BaseResponseViewModel<ProductResponseModel>
            {
                Code = StatusCodes.Status200OK,
                SystemCode = "000",
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
                SystemCode = "039",
                Message = "OK",
                Data = _mapper.Map<ProductResponseModel>(product),
            };
        }
    }
}
