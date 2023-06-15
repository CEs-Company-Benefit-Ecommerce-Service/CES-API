﻿using CES.DataTier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.ResponseModels
{
    public class OrderDetailsResponseModel
    {
        public double? Price { get; set; }
        public int? Quantity { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Notes { get; set; }
        public Guid? ProductId { get; set; }
        public Guid? OrderId { get; set; }

        //public virtual Order? Order { get; set; }
        public ProductResponseModel? Product { get; set; }
    }
}