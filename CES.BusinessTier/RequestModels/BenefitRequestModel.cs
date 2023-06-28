using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.RequestModels
{
    public class BenefitRequestModel
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? Type { get; set; }
        public double UnitPrice { get; set; }
        //public int CompanyId { get; set; }
    }

    public class BenefitUpdateModel
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? Type { get; set; }
        public int? Status { get; set; }
    }
}
