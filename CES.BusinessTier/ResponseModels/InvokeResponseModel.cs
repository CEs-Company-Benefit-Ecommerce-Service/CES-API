using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class InvokeResponseModel
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public double Total { get; set; }
        public int? Status { get; set; }
        public string? ImageUrl { get; set; } = null!;
        public int? DebtId { get; set; }

        public DebtTicketResponseModel? Debt { get; set; } = null!;
    }
}
