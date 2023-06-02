using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Order
    {
        public Order()
        {
            DebtNotes = new HashSet<DebtNote>();
            OrderDetails = new HashSet<OrderDetail>();
            Transactions = new HashSet<Transaction>();
        }

        public Guid Id { get; set; }
        public double? Total { get; set; }
        public string? Note { get; set; }
        public string? Address { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? Status { get; set; }
        public string? Code { get; set; }
        public int? DebtStatus { get; set; }
        public Guid? AccountId { get; set; }

        public virtual Account? Account { get; set; }
        public virtual ICollection<DebtNote> DebtNotes { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
