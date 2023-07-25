using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Notification
    {
        public Guid Id { get; set; }
        public string? Description { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? TransactionId { get; set; }
        public Guid? AccountId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Title { get; set; }
        public bool? IsRead { get; set; }

        public virtual Account? Account { get; set; }
        public virtual Order? Order { get; set; }
        public virtual Transaction? Transaction { get; set; }
    }
}
