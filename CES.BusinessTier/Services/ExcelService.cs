using System.Drawing;
using System.Security.Claims;
using CES.BusinessTier.RequestModels;
using CES.BusinessTier.ResponseModels.BaseResponseModels;
using CES.BusinessTier.UnitOfWork;
using CES.BusinessTier.Utilities;
using CES.DataTier.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Style;
using System.Security.Principal;
namespace CES.BusinessTier.Services;

public interface IExcelService
{
    Task<DynamicResponse<Account>> ImportEmployeeList(IFormFile file);
    FileStreamResult DownloadEmployeeTemplate();
    FileStreamResult DownloadListEmployeeForCompany(DateRangeFilterModel dateRangeFilter);
    FileStreamResult DownloadProductTemplate();
    Task<DynamicResponse<Product>> ImportProductList(IFormFile file);
    Task<DynamicResponse<Account>> TransferBalanceForEmployee(IFormFile file);
    FileStreamResult DownloadListEmployeeByGroupId(Guid id);
    Task<FileStreamResult> ExportOrdersMonthlyByCompany();
}

public class ExcelService : IExcelService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly ILoginServices _loginServices;
    private readonly ITransactionService _transactionService;
    private readonly IAccountServices _accountServices;

    public ExcelService(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor, ILoginServices loginServices, ITransactionService transactionService, IAccountServices accountServices)
    {
        _unitOfWork = unitOfWork;
        _contextAccessor = contextAccessor;
        _loginServices = loginServices;
        _transactionService = transactionService;
        _accountServices = accountServices;
    }

    public async Task<DynamicResponse<Account>> ImportEmployeeList(IFormFile file)
    {
        var companyId = _contextAccessor.HttpContext?.User.FindFirst("CompanyId").Value;
        var company = await _unitOfWork.Repository<Company>()
            .AsQueryable(x => x.Id == Int32.Parse(companyId) && x.Status == (int)Status.Active)
            .FirstOrDefaultAsync();
        if (file != null && file.Length > 0)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(file.OpenReadStream()))
            {
                var worksheet = package.Workbook.Worksheets[0]; // Assuming data is on the first sheet
                var records = new List<Account>();
                var users = new List<Employee>();
                var wallets = new List<Wallet>();
                int enterpriseCompanyId = Int32.Parse(_contextAccessor.HttpContext?.User.FindFirst("CompanyId").Value);

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var account = new Account()
                    {
                        Id = Guid.NewGuid(),
                        Name = worksheet.Cells[row, 1].Value?.ToString(),
                        Email = worksheet.Cells[row, 2].Value?.ToString(),
                        Address = company.Address,
                        Phone = worksheet.Cells[row, 3].Value?.ToString(),
                        Status = (int)Status.Active,
                        CreatedAt = TimeUtils.GetCurrentSEATime(),
                        Role = Roles.Employee.GetDisplayName(),
                        Password = Authen.HashPassword("123456")
                    };

                    var existAccount = await _unitOfWork.Repository<Account>()
                        .AsQueryable(x => x.Email == account.Email && x.Status == (int)Status.Active)
                        .AnyAsync();
                    if (!existAccount)
                    {
                        records.Add(account);
                        var user = new Employee()
                        {
                            Id = Guid.NewGuid(),
                            CompanyId = enterpriseCompanyId,
                            AccountId = account.Id,
                            Status = (int)Status.Active,
                            CreatedAt = TimeUtils.GetCurrentSEATime()
                        };
                        users.Add(user);
                    }
                    
                }

                foreach (var account in records)
                {
                    var wallet = new Wallet()
                    {
                        Id = Guid.NewGuid(),
                        AccountId = account.Id,
                        Balance = 0,
                        CreatedAt = TimeUtils.GetCurrentSEATime(),
                        Name = WalletTypeEnums.GeneralWallet.GetDisplayName(),
                        Status = (int)Status.Active,
                        CreatedBy = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString())
                        
                    };
                    wallets.Add(wallet);
                
                }

                await _unitOfWork.Repository<Account>().AddRangeAsync(records);
                await _unitOfWork.Repository<Employee>().AddRangeAsync(users);
                await _unitOfWork.Repository<Wallet>().AddRangeAsync(wallets);
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

            ws.Cells["C1"].Value = "Phone";
            ws.Cells["C1"].Style.Font.Bold = true;
            ws.Cells["C1"].Style.Font.Size = 16;
            ws.Cells["C1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["C1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

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
        Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
        var enterpriseWallet = _unitOfWork.Repository<Wallet>()
            .AsQueryable(x => x.AccountId == accountLoginId)
            .FirstOrDefault();

        var employees = _unitOfWork.Repository<Account>()
            .AsQueryable(x => x.Status == (int)Status.Active && x.CreatedAt >= from && x.CreatedAt <= to && x.Role == Roles.Employee.GetDisplayName())
            .Include(x => x.Wallets)
            .OrderBy(x => x.CreatedAt)
            .ToList();
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using (ExcelPackage package = new ExcelPackage())
        {
            List<string> listRow1Title = new List<string>()
            {
                "Company:",
                $"{company.Name}",
                "From-To:",
                $"{from} - {to}",
                "Fund:",
                $"{enterpriseWallet.Balance} VND"
            };
            List<string> listRow2Title = new List<string>()
            {
                "Id",
                "Name",
                "Email",
                "Address",
                "Phone",
                "Image Url",
                "Updated At",
                "Created At",
                "Status",
                "Wallet Balance"
            };
            Dictionary<int, int> walletPositions = new Dictionary<int, int>();
            int initialRow = 1; //A
            int initialCol = 1; //1

            ExcelWorksheet ws = package.Workbook.Worksheets.Add($"{company.Name}");

            // Row 1
            for (int i = 0; i < listRow1Title.Count; i++)
            {
                ws.Cells[initialRow, initialCol].Value = listRow1Title[i];
                ws.Cells[initialRow, initialCol].Style.Font.Bold = true;
                ws.Cells[initialRow, initialCol].Style.Font.Size = 16;
                ws.Cells[initialRow, initialCol].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ws.Cells[initialRow, initialCol].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                initialCol++;
            }
            initialRow++;

            //Row 2
            initialCol = 1;
            for (int i = 0; i < listRow2Title.Count; i++)
            {
                ws.Cells[initialRow, initialCol].Value = listRow2Title[i];
                ws.Cells[initialRow, initialCol].Style.Font.Bold = true;
                ws.Cells[initialRow, initialCol].Style.Font.Size = 16;
                ws.Cells[initialRow, initialCol].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ws.Cells[initialRow, initialCol].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                initialCol++;
            }

            // for (int i = 0; i < wallets.Count; i++)
            // {
            //     ws.Cells[initialRow, initialCol].Value = wallets[i] == 1
            //         ? WalletTypeEnums.FoodWallet.GetDisplayName() : wallets[i] == 2
            //             ? WalletTypeEnums.StationeryWallet.GetDisplayName() : WalletTypeEnums.GeneralWallet.GetDisplayName();
            //     ws.Cells[initialRow, initialCol].Style.Font.Bold = true;
            //     ws.Cells[initialRow, initialCol].Style.Font.Size = 16;
            //     ws.Cells[initialRow, initialCol].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            //     ws.Cells[initialRow, initialCol].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            //
            //     walletPositions.Add(wallets[i] == 1 ? (int)WalletTypeEnums.FoodWallet : wallets[i] == 2
            //         ? (int)WalletTypeEnums.StationeryWallet : (int)WalletTypeEnums.GeneralWallet
            //         , initialCol);
            //
            //     initialCol++;
            //     ws.Cells[initialRow, initialCol].Value = wallets[i] == 1
            //         ? "Add " + WalletTypeEnums.FoodWallet.GetDisplayName() + " (+)" : wallets[i] == 2
            //             ? "Add " + WalletTypeEnums.StationeryWallet.GetDisplayName() + " (+)" : "Add " + WalletTypeEnums.GeneralWallet.GetDisplayName() + " (+)";
            //     ws.Cells[initialRow, initialCol].Style.Font.Bold = true;
            //     ws.Cells[initialRow, initialCol].Style.Font.Size = 16;
            //     ws.Cells[initialRow, initialCol].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            //     ws.Cells[initialRow, initialCol].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            //     initialCol++;
            // }

            // Row 3
            initialRow++;
            initialCol = 1;
            for (int i = 0; i < employees.Count; i++)
            {
                // if ((i + 2) % 2 == 0)
                // {
                //     ws.Cells["A2"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                //     ws.Cells["A2"].Style.Fill.BackgroundColor.SetColor(Color.Gainsboro);
                // }

                ws.Cells[initialRow, initialCol].Value = employees[i].Id;
                ++initialCol;
                ws.Cells[initialRow, initialCol].Value = employees[i].Name;
                ++initialCol;
                ws.Cells[initialRow, initialCol].Value = employees[i].Email;
                ++initialCol;
                ws.Cells[initialRow, initialCol].Value = employees[i].Address;
                ++initialCol;
                ws.Cells[initialRow, initialCol].Value = employees[i].Phone;
                ++initialCol;
                ws.Cells[initialRow, initialCol].Value = employees[i].ImageUrl;
                ++initialCol;
                ws.Cells[initialRow, initialCol].Value = employees[i].UpdatedAt.ToString();
                ++initialCol;
                ws.Cells[initialRow, initialCol].Value = employees[i].CreatedAt.ToString();
                ++initialCol;
                ws.Cells[initialRow, initialCol].Value = employees[i].Status == (int)Status.Active ? Status.Active.GetDisplayName() : Status.Inactive.GetDisplayName();
                foreach (var wallet in employees[i].Wallets)
                {
                    ++initialCol;
                    ws.Cells[initialRow, initialCol].Value = wallet.Balance;
                }

                // if (employees[i].Wallets.Count > 0)
                // {
                //     foreach (var wallet in employees[i].Wallets)
                //     {
                //         if (walletPositions.ContainsKey((int)wallet.Type))
                //         {
                //             ws.Cells[initialRow, walletPositions[(int)wallet.Type]].Value = wallet.Balance;
                //         }
                //     }
                // }

                ++initialRow;
                initialCol = 1;
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

            ws.Cells["E1"].Value = "Category Id*";
            ws.Cells["E1"].Style.Font.Bold = true;
            ws.Cells["E1"].Style.Font.Size = 16;
            ws.Cells["E1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["E1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            
            ws.Cells["I1"].Value = "Category:";
            ws.Cells["I1"].Style.Font.Size = 14;
            ws.Cells["I1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["I1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            
            ws.Cells["J1"].Value = "Id";
            ws.Cells["J1"].Style.Font.Bold = true;
            ws.Cells["J1"].Style.Font.Size = 14;
            ws.Cells["J1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["J1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            
            ws.Cells["K1"].Value = "Name";
            ws.Cells["K1"].Style.Font.Bold = true;
            ws.Cells["K1"].Style.Font.Size = 14;
            ws.Cells["K1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["K1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            
            ws.Cells["L1"].Value = "Description";
            ws.Cells["L1"].Style.Font.Bold = true;
            ws.Cells["L1"].Style.Font.Size = 14;
            ws.Cells["L1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells["L1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            for (int i = 0; i < categories.Count; i++)
            {
                ws.Cells[i + 2, 10].Value = categories[i].Id;
                ws.Cells[i + 2, 10].Style.Font.Size = 16;
                ws.Cells[i + 2, 11].Value = categories[i].Name;
                ws.Cells[i + 2, 11].Style.Font.Size = 16;
                ws.Cells[i + 2, 12].Value = categories[i].Description;
                ws.Cells[i + 2, 12].Style.Font.Size = 16;
            }
            ws.Cells.AutoFitColumns();

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
            var accountId = _contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value;
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
                        CategoryId = Int32.Parse(worksheet.Cells[row, 5].Value?.ToString()),
                        Status = (int)Status.Active,
                        CreatedAt = TimeUtils.GetCurrentSEATime(),
                        SupplierId = Guid.Parse(accountId)
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

    public async Task<DynamicResponse<Account>> TransferBalanceForEmployee(IFormFile file)
    {
        //if (file != null && file.Length > 0)
        //{
        //    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        //    using (var package = new ExcelPackage(file.OpenReadStream()))
        //    {
        //        var ws = package.Workbook.Worksheets[0]; // Assuming data is on the first sheet
        //        var wallets = _unitOfWork.Repository<Wallet>().AsQueryable().Select(x => x.Name).Distinct().ToList();
        //        Dictionary<int, string> positions = new Dictionary<int, string>();
        //        var records = new List<Account>();

        //        Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());

        //        int initialRow = 2; //A
        //        int initialCol = 1; //1

        //        // Get excel dictionnary
        //        for (initialCol = 1; initialCol < ws.Dimension.End.Column; initialCol++)
        //        {
        //            positions.Add(initialCol, ws.Cells[initialRow, initialCol].Value.ToString());
        //        }

        //        var enterprise = await _unitOfWork.Repository<Account>()
        //            .AsQueryable(x => x.Id == accountLoginId)
        //            .Include(x => x.Wallet)
        //            .FirstOrDefaultAsync();
        //        double enterpriseGeneralWalletBalance = 0;
        //        double totalTransfer = 0;

        //        // Get enterprise general wallet
        //        foreach (var wallet in enterprise.Wallet)
        //        {
        //            if (wallet.Type == (int)WalletTypeEnums.GeneralWallet)
        //            {
        //                enterpriseGeneralWalletBalance = (double)wallet.Balance;
        //            }
        //        }

        //        // Get total transfer
        //        for (int row = 3; row <= ws.Dimension.End.Row; row++)
        //        {
        //            foreach (var wallet in wallets)
        //            {
        //                if (positions.ContainsValue(wallet))
        //                {
        //                    var position = positions.FirstOrDefault(x => x.Value == wallet).Key + 1;
        //                    totalTransfer += Double.Parse(ws.Cells[row, position].Value?.ToString() ?? "0");
        //                }
        //            }
        //        }

        //        if (totalTransfer > enterpriseGeneralWalletBalance)
        //        {
        //            throw new ErrorResponse(StatusCodes.Status400BadRequest, 4004,
        //                "Total transfer larger than Enterprise Balance");
        //        }

        //        // Add point for employees
        //        for (int row = 3; row <= ws.Dimension.End.Row; row++)
        //        {
        //            var employee = await _unitOfWork.Repository<Account>()
        //                .AsQueryable(x => x.Id == Guid.Parse(ws.Cells[row, 1].Value.ToString() ?? string.Empty))
        //                .Include(x => x.Wallet)
        //                .FirstOrDefaultAsync();

        //            if (employee.Wallet.Count > 0)
        //            {
        //                foreach (var wallet in employee.Wallet)
        //                {
        //                    // todo tạo transaction, + ví employee = - ví enterprise
        //                    if (positions.ContainsValue(wallet.Name))
        //                    {
        //                        var position = positions.FirstOrDefault(x => x.Value == wallet.Name).Key + 1;
        //                        // Update employee balance
        //                        wallet.Balance += Double.Parse(ws.Cells[row, position].Value?.ToString() ?? "0");

        //                        // todo create transaction

        //                        enterpriseGeneralWalletBalance -= Double.Parse(ws.Cells[row, position].Value?.ToString() ?? "0");
        //                    }
        //                }
        //            }
        //            await _unitOfWork.Repository<Account>().UpdateDetached(employee);
        //            records.Add(employee);
        //        }
        //        // Set enterprise general wallet balance
        //        foreach (var wallet in enterprise.Wallet)
        //        {
        //            if (wallet.Type == (int)WalletTypeEnums.GeneralWallet)
        //            {
        //                wallet.Balance = enterpriseGeneralWalletBalance;
        //            }
        //        }
        //        await _unitOfWork.Repository<Account>().UpdateDetached(enterprise);
        //        await _unitOfWork.CommitAsync();

        //        return new DynamicResponse<Account>()
        //        {
        //            Code = StatusCodes.Status200OK,
        //            Message = "Ok",
        //            Data = records
        //        };
        //    }
        //}

        return new DynamicResponse<Account>()
        {
            Code = StatusCodes.Status200OK,
            Message = "Ok",
            Data = new List<Account>()
        };
    }

    public FileStreamResult DownloadListEmployeeByGroupId(Guid id)
    {
        var group = _unitOfWork.Repository<Group>()
            .AsQueryable(x => x.Id == id && x.Status == (int)Status.Active)
            .Include(x => x.Benefit)
            .FirstOrDefault();
        if (group == null) throw new ErrorResponse(StatusCodes.Status404NotFound, 404, "");
        var companyId = _contextAccessor.HttpContext?.User.FindFirst("CompanyId").Value;
        var company = _unitOfWork.Repository<Company>()
            .AsQueryable(x => x.Id == int.Parse(companyId) && x.Status == (int)Status.Active).FirstOrDefault();
        Guid accountLoginId = new Guid(_contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString());
        var enterpriseWallet = _unitOfWork.Repository<Wallet>()
            .AsQueryable(x => x.AccountId == accountLoginId)
            .FirstOrDefault();
        
        var from = TimeUtils.GetLastAndFirstDateInCurrentMonth().Item1;
        var to = TimeUtils.GetLastAndFirstDateInCurrentMonth().Item2;
        from = ((DateTime)from).GetStartOfDate();
        to = ((DateTime)to).GetEndOfDate();
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using (ExcelPackage package = new ExcelPackage())
        {
            List<string> listRow1Title = new List<string>()
            {
                "Company:",
                $"{company.Name}",
                "From-To:",
                $"{from} - {to}",
                "Group Name:",
                // $"{}",
                "Limit:",
                $"{group.Benefit.UnitPrice} VND",
                "Enterprise Balance:",
                $"{enterpriseWallet.Balance} VND"
            };
            List<string> listRow2Title = new List<string>()
            {
                "Id",
                "Name",
                "Email",
                "Address",
                "Phone",
                "Image Url",
                "Updated At",
                "Created At",
                "Status",
                "Wallet Balance"
            };
        }
        throw new NotImplementedException();
    }

    public async Task<FileStreamResult> ExportOrdersMonthlyByCompany()
    {
        var companyId = Int32.Parse(_contextAccessor.HttpContext?.User.FindFirst("CompanyId").Value);
        var currentMonth = TimeUtils.GetLastAndFirstDateInCurrentMonth();
        var company = _unitOfWork.Repository<Company>()
            .AsQueryable(x => x.Id == companyId && x.Status == (int)Status.Active).FirstOrDefault();
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using (ExcelPackage package = new ExcelPackage())
        {
            List<string> listRow1Title = new List<string>()
            {
                "Company:",
                $"{company.Name}",
                "From-To:",
                $"{currentMonth.Item1} - {currentMonth.Item2.GetEndOfDate()}"
            };
            List<string> listRow2Title = new List<string>()
            {
                "Id",
                "Order Code",
                "Employee Id",
                "Employee Name",
                "Total",
                "Status",
                "DebtStatus",
                "Order Details",
                "Product Name",
                "Quantity",
                "Price"
            };

            int initialRow = 1; //A
            int initialCol = 1; //1

            ExcelWorksheet ws = package.Workbook.Worksheets.Add($"{company.Name}");

            // Row 1
            for (int i = 0; i < listRow1Title.Count; i++)
            {
                ws.Cells[initialRow, initialCol].Value = listRow1Title[i];
                ws.Cells[initialRow, initialCol].Style.Font.Bold = true;
                ws.Cells[initialRow, initialCol].Style.Font.Size = 16;
                ws.Cells[initialRow, initialCol].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ws.Cells[initialRow, initialCol].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                initialCol++;
            }
            initialRow++;

            //Row 2
            initialCol = 1;
            for (int i = 0; i < listRow2Title.Count; i++)
            {
                ws.Cells[initialRow, initialCol].Value = listRow2Title[i];
                ws.Cells[initialRow, initialCol].Style.Font.Bold = true;
                ws.Cells[initialRow, initialCol].Style.Font.Size = 16;
                ws.Cells[initialRow, initialCol].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ws.Cells[initialRow, initialCol].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                initialCol++;
            }

            var orders = await _unitOfWork.Repository<Order>().AsQueryable()
            .Where(x => x.CompanyId == companyId && x.CreatedAt >= currentMonth.Item1 && x.CreatedAt <= currentMonth.Item2.GetEndOfDate() && x.DebtStatus == (int)DebtStatusEnums.New)
            .Include(x => x.Employee)
            .ThenInclude(x => x.Account)
            .Include(x => x.OrderDetails)
            .ThenInclude(x => x.Product)
            .ToListAsync();

            // Row 3
            initialRow++;
            initialCol = 1;
            foreach (var order in orders)
            {
                var status = Commons.GetEnumDisplayNameFromValue<OrderStatusEnums>(order.Status);
                var detbStatus = Commons.GetEnumDisplayNameFromValue<DebtStatusEnums>((int)order.DebtStatus);
                ws.Cells[initialRow, initialCol].Value = order.Id;
                ++initialCol;
                ws.Cells[initialRow, initialCol].Value = order.OrderCode;
                ++initialCol;
                ws.Cells[initialRow, initialCol].Value = order.Employee.Id;
                ++initialCol;
                ws.Cells[initialRow, initialCol].Value = order.Employee.Account.Name;
                ++initialCol;
                ws.Cells[initialRow, initialCol].Value = order.Total;
                ++initialCol;
                ws.Cells[initialRow, initialCol].Value = status;
                ++initialCol;
                ws.Cells[initialRow, initialCol].Value = detbStatus;
                initialRow++;
                ++initialCol;
                foreach (var item in order.OrderDetails)
                {
                    var row = initialCol;
                    ws.Cells[initialRow, row].Value = item.Id;
                    ++row;
                    ws.Cells[initialRow, row].Value = item.Product.Name;
                    ++row;
                    ws.Cells[initialRow, row].Value = item.Quantity;
                    ++row;
                    ws.Cells[initialRow, row].Value = item.Price;
                    initialRow++;
                }
                initialRow++;
                initialCol = 1;
            }

            ws.Cells.AutoFitColumns();

            var stream = new MemoryStream(package.GetAsByteArray());

            return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = $"{company.Name}_orders.xlsx" // Specify the desired file name
            };
        }
    }
}