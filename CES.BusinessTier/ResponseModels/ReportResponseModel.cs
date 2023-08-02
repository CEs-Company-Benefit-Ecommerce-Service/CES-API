using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class ReportResponseModel
    {
    }

    public class ReportEAResponseModel
    {
        public double? Used { get; set; }
        public int? OrderCount { get; set; }
        //public List<KeyValuePair<Guid, double>>? TopEmpUsed { get; set; }
    }

    public class ReportSAResponseModel
    {
        public int? CompanyCount { get; set; }
        public double? TotalRevenue { get; set; }
        public double? TotalCompanyUsed { get; set; }
        public int? EmployeeCount { get; set; }
    }
}
