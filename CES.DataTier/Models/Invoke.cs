﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Invoke
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public double Total { get; set; }
        public int? Status { get; set; }
        public string ImageUrl { get; set; } = null!;
        public int DebtId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual DebtTicket Debt { get; set; } = null!;
    }
}