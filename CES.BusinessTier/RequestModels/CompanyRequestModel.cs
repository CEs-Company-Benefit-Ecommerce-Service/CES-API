using CES.BusinessTier.ResponseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.RequestModels
{
    public class CompanyRequestModel
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public int? Status { get; set; }
        public string? ImageUrl { get; set; }
        public double? Limits { get; set; }
        public double? Used { get; set; }
        public DateTime? ExpiredDate { get; set; }
    }
}
