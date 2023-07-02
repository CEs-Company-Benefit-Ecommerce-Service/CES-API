using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Supplier
    {
        public Supplier()
        {
            Products = new HashSet<Product>();
        }

        public Guid Id { get; set; }
        public string? SupplierName { get; set; }
        public Guid AccountId { get; set; }
        public string? SupplierAddress { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual Account Account { get; set; } = null!;
        public virtual ICollection<Product> Products { get; set; }
    }
}
