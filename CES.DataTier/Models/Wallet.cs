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
            Transaction = new HashSet<Transaction>();
            WalletTransaction = new HashSet<WalletTransaction>();
        }

        public Guid Id { get; set; }
        public string? Name { get; set; }
        public int? Type { get; set; }
        public double? Balance { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }
        public Guid? AccountId { get; set; }
        public double? Limit { get; set; }

        public virtual Account? Account { get; set; }
        public virtual ICollection<Transaction> Transaction { get; set; }
        public virtual ICollection<WalletTransaction> WalletTransaction { get; set; }
    }
}