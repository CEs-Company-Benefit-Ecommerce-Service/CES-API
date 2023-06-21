using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Receipt
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
        public Guid? DebtId { get; set; }

        public virtual Company? Company { get; set; }
        public virtual DebtNotes? DebtNotes { get; set; }
    }
}
