using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Account
    {
        public Account()
        {
            Employees = new HashSet<Employee>();
            Enterprises = new HashSet<Enterprise>();
            Notifications = new HashSet<Notification>();
            Suppliers = new HashSet<Supplier>();
            Wallets = new HashSet<Wallet>();
        }

        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Password { get; set; }
        public string? Address { get; set; }
        public string? ImageUrl { get; set; }
        public string? Role { get; set; }
        public int? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? FcmToken { get; set; }
        public string? RefreshToken { get; set; }

        public virtual ICollection<Employee> Employees { get; set; }
        public virtual ICollection<Enterprise> Enterprises { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<Supplier> Suppliers { get; set; }
        public virtual ICollection<Wallet> Wallets { get; set; }
    }
}
