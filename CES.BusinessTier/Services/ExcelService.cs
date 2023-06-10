using CES.BusinessTier.RequestModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace CES.BusinessTier.Services;

public interface IExcelService
{
    Task<List<Account>> ImportEmployeeList(IFormFile file);
}

public class ExcelService : IExcelService
{
    private readonly IUnitOfWork _unitOfWork;

    public ExcelService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<List<Account>> ImportEmployeeList(IFormFile file)
    {
        if (file != null && file.Length > 0)
        {
            // var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            //
            // var filePath = Path.Combine("uploads", fileName);
            //
            // var directory = Path.GetDirectoryName(filePath);
            // Directory.CreateDirectory(directory);
            // using (var stream = new FileStream(filePath, FileMode.Create))
            // {
            //     await file.CopyToAsync(stream);
            // }
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(file.OpenReadStream()))
            {
                var worksheet = package.Workbook.Worksheets[0]; // Assuming data is on the first sheet
                var records = new List<Account>();

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {

                    var account = new Account()
                    {
                        Id = Guid.NewGuid(),
                        Name = worksheet.Cells[row, 1].Value?.ToString(),
                        Email = worksheet.Cells[row, 2].Value?.ToString(),
                        Address = worksheet.Cells[row, 3].Value?.ToString(),
                        Phone = worksheet.Cells[row, 4].Value?.ToString(),
                        ImageUrl = worksheet.Cells[row, 5].Value?.ToString(),
                        Status = (int)Status.Active,
                        CreatedAt = TimeUtils.GetCurrentSEATime(),
                        RoleId = (int)Roles.Employee,
                        CompanyId = 1,
                        Password = Authen.HashPassword("DefaultPassword")
                    };

                    records.Add(account);
                }

                await _unitOfWork.Repository<Account>().AddRangeAsync(records);
                await _unitOfWork.CommitAsync();

                return records;
            }
        }
        return new List<Account>();
    }
}