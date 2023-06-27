﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Company
    {
        public Company()
        {
            Account = new HashSet<Account>();
            Benefit = new HashSet<Benefit>();
            DebtTicket = new HashSet<DebtTicket>();
            Group = new HashSet<Group>();
        }

        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public int? Status { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? ContactPersonId { get; set; }
        public double? Limits { get; set; }
        public double? Used { get; set; }
        public DateTime? ExpiredDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }

        public virtual ICollection<Account> Account { get; set; }
        public virtual ICollection<Benefit> Benefit { get; set; }
        public virtual ICollection<DebtTicket> DebtTicket { get; set; }
        public virtual ICollection<Group> Group { get; set; }
    }
}