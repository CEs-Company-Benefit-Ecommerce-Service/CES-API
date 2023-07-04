using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class DebtTicket
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
}
