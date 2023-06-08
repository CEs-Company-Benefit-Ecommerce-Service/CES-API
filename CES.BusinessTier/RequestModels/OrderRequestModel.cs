using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.RequestModels
{
    public class OrderRequestModel
    {
        public double? Total { get; set; }
        public string? Note { get; set; }
        public string? Address { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? Status { get; set; }
        public string? Code { get; set; }
        public int? DebtStatus { get; set; }
        //public Guid? AccountId { get; set; }
    }
}
