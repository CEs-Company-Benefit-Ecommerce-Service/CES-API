﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Transaction
    {
        public Guid Id { get; set; }
        public double? Total { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? Type { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? WalletId { get; set; }

        public virtual Order? Order { get; set; }
        public virtual Wallet? Wallet { get; set; }
    }
}