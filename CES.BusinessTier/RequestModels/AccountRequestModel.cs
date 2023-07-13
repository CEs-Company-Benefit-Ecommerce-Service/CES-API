using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.RequestModels
{
    public class AccountRequestModel
    {
        public string? Name { get; set; }
        public string Email { get; set; }
        public string? Password { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? ImageUrl { get; set; }
        public int? Role { get; set; }
        public int? CompanyId { get; set; }
        public CompanyRequestModel? Company { get; set; }
    }

    public class ChangePasswordModel
    {
        public string? NewPassword { get; set; }
        public string? OldPassword { get; set; }
    }
}
