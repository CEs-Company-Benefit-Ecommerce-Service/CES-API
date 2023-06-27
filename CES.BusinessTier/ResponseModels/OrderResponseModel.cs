using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class OrderResponseModel
    {
        public Guid Id { get; set; }
        public double Total { get; set; }
        public string? Notes { get; set; }
        public string? Address { get; set; }
        public int? Status { get; set; }
        public int? DebtStatus { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<OrderDetailsResponseModel> OrderDetail { get; set; }
        public AccountResponseModel Account { get; set; } = null!;
    }
}
