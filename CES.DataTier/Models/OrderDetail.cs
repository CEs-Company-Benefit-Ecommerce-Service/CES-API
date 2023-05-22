using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class OrderDetail
    {
        public Guid Id { get; set; }
        public double? Price { get; set; }
        public DateTime? CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public string? Note { get; set; }
        public Guid? RecieverId { get; set; }
        public Guid? SenderId { get; set; }
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }

        public virtual Order Order { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
