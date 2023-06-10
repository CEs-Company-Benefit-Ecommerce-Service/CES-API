using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace CES.BusinessTier.Services;

public interface IExcelService
{
    Task<DynamicResponse<Account>> ImportEmployeeList(IFormFile file);
    FileStreamResult DownloadEmployeeTemplate();
    Task<FileStreamResult> DownloadListEmplyeeForCompany();
}

public class ExcelService : IExcelService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _contextAccessor;

    public ExcelService(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor)
    {
        _unitOfWork = unitOfWork;
        _contextAccessor = contextAccessor;
    }
    
    public async Task<DynamicResponse<Account>> ImportEmployeeList(IFormFile file)
    {
        var companyId = _contextAccessor.HttpContext?.User.FindFirst("CompanyId").Value;
        if (file != null && file.Length > 0)
        {
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
                        Status = (int)Status.Active,
                        CreatedAt = TimeUtils.GetCurrentSEATime(),
                        RoleId = (int)Roles.Employee,
                        CompanyId = int.Parse(companyId),
                        Password = Authen.HashPassword("DefaultPassword")
                    };

                    var existAccount = await _unitOfWork.Repository<Account>()
                        .AsQueryable(x => x.Email == account.Email && x.Status == (int)Status.Active)
                        .AnyAsync();
                    if (!existAccount) records.Add(account);
                }

                foreach (var account in records)
                {
                    var wallets = new List<Wallet>()
                    {
                        new Wallet
                        {
                            AccountId = account.Id,
                            Balance = 0,
                            CreatedAt = TimeUtils.GetCurrentSEATime(),
                            Id = Guid.NewGuid(),
                            Name = WalletTypeEnums.FoodWallet.GetDisplayName(),
                            Type = (int)WalletTypeEnums.FoodWallet,
                        },
                        new Wallet
                        {
                            AccountId = account.Id,
                            Balance = 0,
                            CreatedAt = TimeUtils.GetCurrentSEATime(),
                            Id = Guid.NewGuid(),
                            Name = WalletTypeEnums.StationeryWallet.GetDisplayName(),
                            Type = (int)WalletTypeEnums.StationeryWallet,
                        }
                    };
                    account.Wallets = wallets;
                }

                await _unitOfWork.Repository<Account>().AddRangeAsync(records);
                await _unitOfWork.CommitAsync();

                return new DynamicResponse<Account>()
                {
                    Code = StatusCodes.Status200OK,
                    Message = "Ok",
                    Data = records
                };
            }
        }

        return new DynamicResponse<Account>()
        {
            Code = StatusCodes.Status200OK,
            Message = "Ok",
            Data = new List<Account>()
        };
    }

    public FileStreamResult DownloadEmployeeTemplate()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using (ExcelPackage package = new ExcelPackage())
        {
            ExcelWorksheet ws = package.Workbook.Worksheets.Add("Company");
            ws.Cells["A1"].Value = "Name*";
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            
            ws.Cells["B1"].Value = "Email*";
            ws.Cells["B1"].Style.Font.Bold = true;
            ws.Cells["B1"].Style.Font.Size = 16;
            ws.Cells["B1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["B1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            
            ws.Cells["C1"].Value = "Address";
            ws.Cells["C1"].Style.Font.Bold = true;
            ws.Cells["C1"].Style.Font.Size = 16;
            ws.Cells["C1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["C1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            
            ws.Cells["D1"].Value = "Phone";
            ws.Cells["D1"].Style.Font.Bold = true;
            ws.Cells["D1"].Style.Font.Size = 16;
            ws.Cells["D1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["D1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            
            ws.Cells.AutoFitColumns();
            
            var stream = new MemoryStream(package.GetAsByteArray());

            return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = "employee_template.xlsx" // Specify the desired file name
            };
        }
    }

    public Task<FileStreamResult> DownloadListEmplyeeForCompany()
    {
        throw new NotImplementedException();
    }
}