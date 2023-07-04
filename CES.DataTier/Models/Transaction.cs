using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Transaction
    {
        public Guid Id { get; set; }
        public double Total { get; set; }
        public string? Description { get; set; }
        public int? Type { get; set; }
        public DateTime? CreatedAt { get; set; }
        public Guid? SenderId { get; set; }
        public Guid? RecieveId { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? WalletId { get; set; }
        public int? CompanyId { get; set; }
    }
}
