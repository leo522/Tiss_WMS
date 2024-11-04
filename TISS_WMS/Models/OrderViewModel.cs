using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TISS_WMS.Models
{
    public class OrderViewModel
    {
        public int UserId { get; set; } // 訂購人ID
        public decimal TotalAmount { get; set; } // 訂單總金額
        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>(); // 訂單產品列表
    }

    public class OrderItemViewModel
    {
        public int ProductId { get; set; } // 產品ID
        public int Quantity { get; set; } // 數量
        public decimal Price { get; set; } // 單價
    }
}