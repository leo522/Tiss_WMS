using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TISS_WMS.Models
{
    public class StockViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Barcode { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public int CurrentStock { get; set; }
        public int StockChange { get; set; }
        public string TransactionType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}