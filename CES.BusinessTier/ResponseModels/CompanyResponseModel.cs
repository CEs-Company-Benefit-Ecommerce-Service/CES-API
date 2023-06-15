﻿using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class CompanyResponseModel
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public int? Status { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? ImageUrl { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }

    }
    public class CompanyAllInfoResponse
    {
        //public int? Id { get; set; }
        //public string? Name { get; set; }
        //public string? Address { get; set; }
        //public DateTime? UpdatedAt { get; set; }
        //public DateTime? CreatedAt { get; set; }
        //public int? Status { get; set; }
        //public string? ImageUrl { get; set; }
        //public string? ContactPerson { get; set; }
        //public string? Phone { get; set; }

        //public ICollection<AccountResponseModel>? Accounts { get; set; }
        //public ICollection<DebtNote> DebtNotes { get; set; }
        //public ICollection<Receipt> Receipts { get; set; }
    }
}