using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class PaymentProvider
    {
        public PaymentProvider()
        {
            Transactions = new HashSet<Transaction>();
        }

        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? ImageUrl { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Description { get; set; }
        public int? Status { get; set; }
        public string? Config { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
