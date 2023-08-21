using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Transaction
    {
        public Transaction()
        {
            Notifications = new HashSet<Notification>();
        }

        public Guid Id { get; set; }
        public double Total { get; set; }
        public string? Description { get; set; }
        public int? Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? SenderId { get; set; }
        public Guid? RecieveId { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? WalletId { get; set; }
        public int? CompanyId { get; set; }
        public Guid? PaymentProviderId { get; set; }
        public string? InvoiceId { get; set; }
        public int? Status { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        public double? LastPaymentTotal { get; set; }

        public virtual Company? Company { get; set; }
        public virtual PaymentProvider? PaymentProvider { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
    }
}
