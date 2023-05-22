using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Wallet
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public int? Type { get; set; }
        public decimal? Balance { get; set; }
        public DateTime? CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public Guid AccountId { get; set; }

        public virtual Account Account { get; set; } = null!;
    }
}
