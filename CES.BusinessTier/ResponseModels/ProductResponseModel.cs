﻿using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class ProductResponseModel
    {
        public Guid Id { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        public string? Name { get; set; }
        public int? Status { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ImageUrl { get; set; }

        public CategoryResponseModel? Category { get; set; }

    }
}
