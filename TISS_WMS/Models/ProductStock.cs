//------------------------------------------------------------------------------
// <auto-generated>
//     這個程式碼是由範本產生。
//
//     對這個檔案進行手動變更可能導致您的應用程式產生未預期的行為。
//     如果重新產生程式碼，將會覆寫對這個檔案的手動變更。
// </auto-generated>
//------------------------------------------------------------------------------

namespace TISS_WMS.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class ProductStock
    {
        public int ProductStockId { get; set; }
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int StockChange { get; set; }
        public int CurrentStock { get; set; }
        public string TransactionType { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public Nullable<int> ReorderLevel { get; set; }
    
        public virtual Products Products { get; set; }
        public virtual Warehouses Warehouses { get; set; }
    }
}
