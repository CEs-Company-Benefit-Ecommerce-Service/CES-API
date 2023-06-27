﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Account
    {
        public Account()
        {
            GroupAccount = new HashSet<GroupAccount>();
            Order = new HashSet<Order>();
        }

        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string Email { get; set; } = null!;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? ImageUrl { get; set; }
        public int? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? WalletId { get; set; }
        public int RoleId { get; set; }
        public int CompanyId { get; set; }
        public string? Password { get; set; }

        public virtual Company Company { get; set; } = null!;
        public virtual Role Role { get; set; } = null!;
        public virtual Wallet? Wallet { get; set; }
        public virtual ICollection<GroupAccount> GroupAccount { get; set; }
        public virtual ICollection<Order> Order { get; set; }
    }
}