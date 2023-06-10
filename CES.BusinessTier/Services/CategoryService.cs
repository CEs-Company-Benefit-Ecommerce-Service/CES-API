using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.ResponseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CES.BusinessTier.UnitOfWork;
using CES.DataTier.Models;
using AutoMapper.QueryableExtensions;
using LAK.Sdk.Core.Utilities;
using Microsoft.EntityFrameworkCore;
using CES.BusinessTier.Utilities;
using Microsoft.AspNetCore.Http;
using CES.BusinessTier.RequestModels;
using System.Security.Principal;

namespace CES.BusinessTier.Services
{
    public interface ICategoryService
    {
        Task<DynamicResponse<CategoryResponseModel>> GetAllCategoryAsync(CategoryResponseModel filter, PagingModel paging);
        Task<BaseResponseViewModel<CategoryResponseModel>> GetCategoryAsync(int categoryId, CategoryResponseModel filter);
        Task<BaseResponseViewModel<CategoryResponseModel>> CreateCategoryAsync(CategoryRequestModel category);
        Task<BaseResponseViewModel<CategoryResponseModel>> UpdateCategoryAsync(int categoryId, CategoryUpdateModel categoryUpdate);
        Task<BaseResponseViewModel<CategoryResponseModel>> DeleteCategoryAsync(int categoryId);
        Task<bool> ValidCategory(int id);
    }

    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BaseResponseViewModel<CategoryResponseModel>> CreateCategoryAsync(CategoryRequestModel category)
        {
            if (category == null)
            {
                throw new ErrorResponse(StatusCodes.Status400BadRequest, (int)CategoryErrorEnums.INVALID_CATEGORY, CategoryErrorEnums.INVALID_CATEGORY.GetDisplayName());
            }
            var newCategory = _mapper.Map<Category>(category);
            newCategory.Status = (int)Status.Active;
            newCategory.CreatedAt = TimeUtils.GetCurrentSEATime();
            await _unitOfWork.Repository<Category>().InsertAsync(newCategory);
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<CategoryResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = _mapper.Map<CategoryResponseModel>(newCategory),
            };
        }

        public async Task<BaseResponseViewModel<CategoryResponseModel>> DeleteCategoryAsync(int categoryId)
        {
            var category = await _unitOfWork.Repository<Category>().AsQueryable(x => x.Id == categoryId && x.Status == (int)Status.Active).FirstOrDefaultAsync();
            if (category == null) throw new ErrorResponse(StatusCodes.Status404NotFound, (int)CategoryErrorEnums.NOT_FOUND_CATEGORY, CategoryErrorEnums.NOT_FOUND_CATEGORY.GetDisplayName());
            category.Status = (int)Status.Inactive;
            await _unitOfWork.Repository<Category>().UpdateDetached(_mapper.Map<Category>(category));
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<CategoryResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = _mapper.Map<CategoryResponseModel>(category),
            };
        }

        public async Task<bool> ValidCategory(int id)
        {
            var category = await _unitOfWork.Repository<Category>().AsQueryable(x => x.Id == id && x.Status == (int)Status.Active)
                .FirstOrDefaultAsync();
            if (category == null)
            {
                return false;
            }

            return true;
        }

        public async Task<DynamicResponse<CategoryResponseModel>> GetAllCategoryAsync(CategoryResponseModel filter, PagingModel paging)
        {
            var result = _unitOfWork.Repository<Category>().AsQueryable(x => x.Status == (int)Status.Active)
                .ProjectTo<CategoryResponseModel>(_mapper.ConfigurationProvider)
                .DynamicFilter(filter)
                .DynamicSort(paging.Sort, paging.Order)
                .PagingQueryable(paging.Page, paging.Size);
            return new DynamicResponse<CategoryResponseModel>
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

        public async Task<BaseResponseViewModel<CategoryResponseModel>> GetCategoryAsync(int categoryId, CategoryResponseModel filter)
        {
            var category = await _unitOfWork.Repository<Category>().AsQueryable(x => x.Id == categoryId && x.Status == (int)Status.Active)
                .ProjectTo<CategoryResponseModel>(_mapper.ConfigurationProvider)
                .DynamicFilter(filter)
                .FirstOrDefaultAsync();
            if (category == null) throw new ErrorResponse(StatusCodes.Status404NotFound, (int)CategoryErrorEnums.NOT_FOUND_CATEGORY, CategoryErrorEnums.NOT_FOUND_CATEGORY.GetDisplayName());
            return new BaseResponseViewModel<CategoryResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = category
            };
        }

        public async Task<BaseResponseViewModel<CategoryResponseModel>> UpdateCategoryAsync(int categoryId, CategoryUpdateModel categoryUpdate)
        {
            var category = await _unitOfWork.Repository<Category>().AsQueryable(x => x.Id == categoryId && x.Status == (int)Status.Active).FirstOrDefaultAsync();
            if (category == null) throw new ErrorResponse(StatusCodes.Status404NotFound, (int)CategoryErrorEnums.NOT_FOUND_CATEGORY, CategoryErrorEnums.NOT_FOUND_CATEGORY.GetDisplayName());
            category.UpdatedAt = TimeUtils.GetCurrentSEATime();
            await _unitOfWork.Repository<Category>().UpdateDetached(_mapper.Map<CategoryUpdateModel, Category>(categoryUpdate, category));
            await _unitOfWork.CommitAsync();
            return new BaseResponseViewModel<CategoryResponseModel>
            {
                Code = StatusCodes.Status200OK,
                Message = "OK",
                Data = _mapper.Map<CategoryResponseModel>(category),
            };
        }
    }
}
