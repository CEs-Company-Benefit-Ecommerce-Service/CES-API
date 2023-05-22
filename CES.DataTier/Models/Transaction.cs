using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Transaction
    {
        public Guid Id { get; set; }
        public double? Total { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public int? Type { get; set; }
        public Guid OrderId { get; set; }
        public Guid AccountId { get; set; }

        public virtual Account Account { get; set; } = null!;
        public virtual Order Order { get; set; } = null!;
    }
}
