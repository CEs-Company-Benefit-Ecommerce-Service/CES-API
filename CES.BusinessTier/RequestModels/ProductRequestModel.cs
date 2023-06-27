﻿using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.RequestModels
{
    public class ProductRequestModel
    {
        public double Price { get; set; }
        public int Quantity { get; set; }
        public string? Name { get; set; }
        public int? Status { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
    }
}
