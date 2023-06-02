﻿using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Account
    {
        public Account()
        {
            Orders = new HashSet<Order>();
            ProjectAccounts = new HashSet<ProjectAccount>();
            Wallets = new HashSet<Wallet>();
        }

        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? ImageUrl { get; set; }
        public int? Status { get; set; }
        public int? RoleId { get; set; }
        public int? CompanyId { get; set; }
        public string? Password { get; set; }

        public virtual Company? Company { get; set; }
        public virtual Role? Role { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<ProjectAccount> ProjectAccounts { get; set; }
        public virtual ICollection<Wallet> Wallets { get; set; }
    }
}
