using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class DebtTicketResponseModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public double Total { get; set; }
        public int? Status { get; set; }
        public string? InfoPayment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CompanyId { get; set; }

        public virtual Company Company { get; set; } = null!;
    }
    public class TotalOrderResponse
    {
        public double Total { get; set; }
        public List<Guid> OrderIds { get; set; }
    }
}
