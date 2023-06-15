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
    FileStreamResult DownloadListEmployeeForCompany(DateRangeFilterModel dateRangeFilter);
    FileStreamResult DownloadProductTemplate();
    Task<DynamicResponse<Product>> ImportProductList(IFormFile file);
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

    public FileStreamResult DownloadListEmployeeForCompany(DateRangeFilterModel dateRangeFilter)
    {
        #region check date range

        var from = dateRangeFilter?.From;
        var to = dateRangeFilter?.To;
        if (from == null && to == null)
        {
            from = TimeUtils.GetLastAndFirstDateInCurrentMonth().Item1;
            to = TimeUtils.GetLastAndFirstDateInCurrentMonth().Item2;
        }

        from ??= TimeUtils.GetCurrentDate();
        to ??= TimeUtils.GetCurrentDate();

        if (DateTime.Compare((DateTime)from, (DateTime)to) > 0)
        {
            throw new ErrorResponse(StatusCodes.Status400BadRequest, 4001, "Invalid day!");
        }

        from = ((DateTime)from).GetStartOfDate();
        to = ((DateTime)to).GetEndOfDate();

        #endregion
        var companyId = _contextAccessor.HttpContext?.User.FindFirst("CompanyId").Value;
        var company = _unitOfWork.Repository<Company>()
            .AsQueryable(x => x.Id == int.Parse(companyId) && x.Status == (int)Status.Active).FirstOrDefault();
        var employees = _unitOfWork.Repository<Account>()
            .AsQueryable(x => x.RoleId == (int)Roles.Employee && x.CompanyId == int.Parse(companyId) && x.Status == (int)Status.Active && x.CreatedAt >= from && x.CreatedAt <= to)
            .OrderBy(x => x.CreatedAt)
            .ToList();
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using (ExcelPackage package = new ExcelPackage())
        {
            var date = DateTime.Now;
            ExcelWorksheet ws = package.Workbook.Worksheets.Add($"{company.Name}");
            ws.Column(7).Style.Numberformat.Format = "DD-MM-YYYY";
            ws.Cells["A1"].Value = "Company:";
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["B1"].Value = company.Name;
            ws.Cells["B1"].Style.Font.Size = 16;
            ws.Cells["B1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["B1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["D1"].Value = "From-To:";
            ws.Cells["D1"].Style.Font.Size = 16;
            ws.Cells["D1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["D1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["E1"].Value = $"{from}-{to}";
            ws.Cells["E1"].Style.Font.Size = 16;
            ws.Cells["E1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["E1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2"].Value = "Name";
            ws.Cells["A2"].Style.Font.Bold = true;
            ws.Cells["A2"].Style.Font.Size = 16;
            ws.Cells["A2"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["B2"].Value = "Email";
            ws.Cells["B2"].Style.Font.Bold = true;
            ws.Cells["B2"].Style.Font.Size = 16;
            ws.Cells["B2"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["B2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["C2"].Value = "Address";
            ws.Cells["C2"].Style.Font.Bold = true;
            ws.Cells["C2"].Style.Font.Size = 16;
            ws.Cells["C2"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["C2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["D2"].Value = "Phone";
            ws.Cells["D2"].Style.Font.Bold = true;
            ws.Cells["D2"].Style.Font.Size = 16;
            ws.Cells["D2"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["D2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["E2"].Value = "Image Url";
            ws.Cells["E2"].Style.Font.Bold = true;
            ws.Cells["E2"].Style.Font.Size = 16;
            ws.Cells["E2"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["E2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["F2"].Value = "Updated At";
            ws.Cells["F2"].Style.Font.Bold = true;
            ws.Cells["F2"].Style.Font.Size = 16;
            ws.Cells["F2"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["F2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["G2"].Value = "Created At";
            ws.Cells["G2"].Style.Font.Bold = true;
            ws.Cells["G2"].Style.Font.Size = 16;
            ws.Cells["G2"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["G2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["H2"].Value = "Status";
            ws.Cells["H2"].Style.Font.Bold = true;
            ws.Cells["H2"].Style.Font.Size = 16;
            ws.Cells["H2"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["H2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            for (int i = 0; i < employees.Count; i++)
            {
                ws.Cells[i + 3, 1].Value = employees[i].Name;
                ws.Cells[i + 3, 2].Value = employees[i].Email;
                ws.Cells[i + 3, 3].Value = employees[i].Address;
                ws.Cells[i + 3, 4].Value = employees[i].Phone;
                ws.Cells[i + 3, 5].Value = employees[i].ImageUrl;
                ws.Cells[i + 3, 6].Value = employees[i].UpdatedAt.ToString();
                ws.Cells[i + 3, 7].Value = employees[i].CreatedAt.ToString();
                ws.Cells[i + 3, 8].Value = employees[i].Status == (int)Status.Active ? Status.Active.GetDisplayName() : Status.Inactive.GetDisplayName();
            }

            ws.Cells.AutoFitColumns();

            var stream = new MemoryStream(package.GetAsByteArray());

            return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = $"{company.Name}_employees.xlsx" // Specify the desired file name
            };
        }
    }

    public FileStreamResult DownloadProductTemplate()
    {
        var categories = _unitOfWork.Repository<Category>().AsQueryable(x => x.Status == (int)Status.Active).ToList();
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using (ExcelPackage package = new ExcelPackage())
        {
            ExcelWorksheet ws = package.Workbook.Worksheets.Add("Products");
            ws.Cells["A1"].Value = "Name*";
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["B1"].Value = "Price*";
            ws.Cells["B1"].Style.Font.Bold = true;
            ws.Cells["B1"].Style.Font.Size = 16;
            ws.Cells["B1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["B1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["C1"].Value = "Quantity*";
            ws.Cells["C1"].Style.Font.Bold = true;
            ws.Cells["C1"].Style.Font.Size = 16;
            ws.Cells["C1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["C1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["D1"].Value = "Description";
            ws.Cells["D1"].Style.Font.Bold = true;
            ws.Cells["D1"].Style.Font.Size = 16;
            ws.Cells["D1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["D1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["E1"].Value = "Service Duration";
            ws.Cells["E1"].Style.Font.Bold = true;
            ws.Cells["E1"].Style.Font.Size = 16;
            ws.Cells["E1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["E1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["F1"].Value = "Type*";
            ws.Cells["F1"].Style.Font.Bold = true;
            ws.Cells["F1"].Style.Font.Size = 16;
            ws.Cells["F1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["F1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["G1"].Value = "Category Id*";
            ws.Cells["G1"].Style.Font.Bold = true;
            ws.Cells["G1"].Style.Font.Size = 16;
            ws.Cells["G1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["G1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ExcelWorksheet wsCate = package.Workbook.Worksheets.Add("Categories");
            wsCate.Cells["A1"].Value = "Id";
            wsCate.Cells["A1"].Style.Font.Bold = true;
            wsCate.Cells["A1"].Style.Font.Size = 16;
            wsCate.Cells["A1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            wsCate.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            wsCate.Cells["B1"].Value = "Name";
            wsCate.Cells["B1"].Style.Font.Bold = true;
            wsCate.Cells["B1"].Style.Font.Size = 16;
            wsCate.Cells["B1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            wsCate.Cells["B1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            wsCate.Cells["C1"].Value = "Description";
            wsCate.Cells["C1"].Style.Font.Bold = true;
            wsCate.Cells["C1"].Style.Font.Size = 16;
            wsCate.Cells["C1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            wsCate.Cells["C1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            for (int i = 0; i < categories.Count; i++)
            {
                wsCate.Cells[i + 2, 1].Value = categories[i].Id;
                wsCate.Cells[i + 2, 2].Value = categories[i].Name;
                wsCate.Cells[i + 2, 3].Value = categories[i].Description;
            }
            ws.Cells.AutoFitColumns();
            wsCate.Cells.AutoFitColumns();

            var stream = new MemoryStream(package.GetAsByteArray());

            return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = "product_template.xlsx" // Specify the desired file name
            };
        }
    }

    public async Task<DynamicResponse<Product>> ImportProductList(IFormFile file)
    {
        if (file != null && file.Length > 0)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(file.OpenReadStream()))
            {
                var worksheet = package.Workbook.Worksheets[0]; // Assuming data is on the first sheet
                var records = new List<Product>();

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {

                    var product = new Product()
                    {
                        Id = Guid.NewGuid(),
                        Name = worksheet.Cells[row, 1].Value?.ToString(),
                        Price = Double.Parse(worksheet.Cells[row, 2].Value?.ToString()),
                        Quantity = Int32.Parse(worksheet.Cells[row, 3].Value?.ToString()),
                        Description = worksheet.Cells[row, 4].Value?.ToString(),
                        ServiceDuration = worksheet.Cells[row, 5].Value?.ToString(),
                        Type = Int32.Parse(worksheet.Cells[row, 6].Value?.ToString()),
                        CategoryId = Int32.Parse(worksheet.Cells[row, 7].Value?.ToString()),
                        Status = (int)Status.Active,
                        CreatedAt = TimeUtils.GetCurrentSEATime()
                    };

                    records.Add(product);
                }

                await _unitOfWork.Repository<Product>().AddRangeAsync(records);
                await _unitOfWork.CommitAsync();

                return new DynamicResponse<Product>()
                {
                    Code = StatusCodes.Status200OK,
                    Message = "Ok",
                    Data = records
                };
            }
        }

        return new DynamicResponse<Product>()
        {
            Code = StatusCodes.Status200OK,
            Message = "Ok",
            Data = new List<Product>()
        };
    }
}