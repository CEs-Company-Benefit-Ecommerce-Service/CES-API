using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class ReceiptResponseModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public double? Total { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? Status { get; set; }
        public string? ImageUrl { get; set; }
        public string? PaymentCode { get; set; }
        public int? CompanyId { get; set; }
    }
}
