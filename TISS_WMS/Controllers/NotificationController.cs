using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TISS_WMS.Models;

namespace TISS_WMS.Controllers
{
    public class NotificationController : Controller
    {
        private TISS_WMSEntities _db = new TISS_WMSEntities(); //資料庫

        #region 低庫存提醒
        public ActionResult LowStockAlerts()
        {
            var lowStockItems = _db.ProductStock
                .Join(_db.Products, ps => ps.ProductId, p => p.ProductId, (ps, p) => new
                {
                    ps.ProductId,
                    p.ProductName,
                    ps.CurrentStock,
                    ps.ReorderLevel,
                    ps.WarehouseId,
                    p.Barcode
                })
                .Where(item => item.CurrentStock < item.ReorderLevel) // 查找低於限額的商品
                .ToList();

            return View(lowStockItems);
        }
        #endregion

        #region 過期商品提醒
        public ActionResult ExpiryAlerts()
        {
            var nearExpiryItems = _db.ProductStock
                .Join(_db.Products, ps => ps.ProductId, p => p.ProductId, (ps, p) => new
                {
                    p.ProductId,
                    p.ProductName,
                    p.ExpiryDate,
                    ps.WarehouseId,
                    ps.CurrentStock,
                    p.Barcode
                })
                .Where(item => item.ExpiryDate != null && DbFunctions.DiffDays(DateTime.Now, item.ExpiryDate) <= 7) // 接近過期（例如7天內）
                .ToList();

            return View(nearExpiryItems);
        }
        #endregion
    }
}