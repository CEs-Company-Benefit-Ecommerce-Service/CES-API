﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;

namespace CES.DataTier.Models
{
    public partial class Project
    {
        public Project()
        {
            ProjectAccount = new HashSet<ProjectAccount>();
        }

        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? Status { get; set; }
        public string? ImageUrl { get; set; }
        public int? CompanyId { get; set; }

        public virtual ICollection<ProjectAccount> ProjectAccount { get; set; }
    }
}