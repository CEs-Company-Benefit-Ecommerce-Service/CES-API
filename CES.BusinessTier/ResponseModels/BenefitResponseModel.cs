using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class BenefitResponseModel
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? Type { get; set; }
        public double? UnitPrice { get; set; }
        public int? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? CompanyId { get; set; }
        public double? TotalReceive { get; set; }
        public double? EstimateTotal { get; set; }

        public CompanyResponseModel? Company { get; set; }
        public ICollection<GroupResponseModel>? Groups { get; set; }
    }
}
