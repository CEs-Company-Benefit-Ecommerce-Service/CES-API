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
        public Guid? Id { get; set; }
        public double? Total { get; set; }
        public string? Notes { get; set; }
        public string? Address { get; set; }
        public int? Status { get; set; }
        public int? DebtStatus { get; set; }
        public string? OrderCode { get; set; }
        public string? CompanyName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? EmployeeId { get; set; }
        public int? CompanyId { get; set; }
        public ICollection<OrderDetailsResponseModel>? OrderDetails { get; set; }
        public UserResponseModel? Employee { get; set; } = null!;
    }

    public class ListOrderToPaymentResponse
    {
        public double? Total { get; set; }
        public List<OrderToPaymentResponse> Orders { get; set; }
    }

    public class OrderToPaymentResponse
    {
        public Guid? Id { get; set; }
        public double? Total { get; set; }
        public string? Notes { get; set; }
        public string? Address { get; set; }
        public int? Status { get; set; }
        public int? DebtStatus { get; set; }
        public string? OrderCode { get; set; }
        public string? CompanyName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? EmployeeId { get; set; }
        public int? CompanyId { get; set; }
        public ICollection<OrderDetailsResponseModel>? OrderDetails { get; set; }
        public UserResponseModel? Employee { get; set; }
        public string? EmployeeName { get; set; }
    }
    public class FilterFromTo
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
