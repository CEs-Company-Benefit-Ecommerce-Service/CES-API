using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class AccountResponseModel
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; } = null!;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? ImageUrl { get; set; }
        public int? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? WalletId { get; set; }
        public int? RoleId { get; set; }
        public int? CompanyId { get; set; }

        public CompanyResponseModel? Company { get; set; }
        public WalletResponseModel? Wallet { get; set; }
    }
    public class AccountAllResponseModel
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? ImageUrl { get; set; }
        public int? Status { get; set; }
        public int? RoleId { get; set; }
        public int? CompanyId { get; set; }

    }
}
