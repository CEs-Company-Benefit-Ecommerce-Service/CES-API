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
        public AccountResponseModel()
        {
            Wallets = new HashSet<WalletResponseModel>();
        }

        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Password { get; set; }
        public string? Address { get; set; }
        public string? ImageUrl { get; set; }
        public int? Role { get; set; }
        public int? Status { get; set; }
        public int? CompanyId { get; set; }
        public DateTime? ExpiredDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool? IsReceived { get; set; }

        public ICollection<WalletResponseModel> Wallets { get; set; }
        public ICollection<UserResponseModel> Suppliers { get; set; }
        public ICollection<UserResponseModel> Employees { get; set; }
        public ICollection<UserResponseModel> Enterprises { get; set; }
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
        public int? Role { get; set; }
        public int? CompanyId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class AccountResponseWithCompany : AccountResponseModel
    {
        public CompanyResponseModel? Company { get; set; }
    }
}
