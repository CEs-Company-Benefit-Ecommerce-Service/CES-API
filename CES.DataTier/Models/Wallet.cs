﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Wallet
    {
        public Wallet()
        {
            Account = new HashSet<Account>();
        }

        public Guid Id { get; set; }
        public string? Name { get; set; }
        public int? Status { get; set; }
        public double? Balance { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }

        public virtual ICollection<Account> Account { get; set; }
    }
}