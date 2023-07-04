using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class BankTransaction
    {
        public Guid Id { get; set; }
        public double? Total { get; set; }
        public double? CurrentTotal { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? Description { get; set; }
        public string? TransactionCode { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CompanyId { get; set; }

        public virtual Company? Company { get; set; }
    }
}
