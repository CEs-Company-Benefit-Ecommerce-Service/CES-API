﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.RequestModels
{
    public class OrderRequestModel
    {
        public double Total { get; set; }
        public string? Notes { get; set; }
        public string? Address { get; set; }
    }
}
