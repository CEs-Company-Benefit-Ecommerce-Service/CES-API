using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Product
    {
        public Product()
        {
            Discounts = new HashSet<Discount>();
            OrderDetails = new HashSet<OrderDetail>();
        }

        public Guid Id { get; set; }
        public string? Name { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        public double? UnitPrice { get; set; }
        public double? PreDiscount { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CategoryId { get; set; }
        public Guid? SupplierId { get; set; }

        public virtual Category Category { get; set; } = null!;
        public virtual Supplier? Supplier { get; set; }
        public virtual ICollection<Discount> Discounts { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
