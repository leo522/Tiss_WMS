using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TISS_WMS.Models;

namespace TISS_WMS.Controllers
{
    public class MainController : Controller
    {
        private TISS_WMSEntities _db = new TISS_WMSEntities();

        #region 首頁
        public ActionResult Main()
        {
            // 庫存概覽數據
            ViewBag.TotalStockQuantity = _db.ProductStock.Select(ps => (int?)ps.CurrentStock).Sum() ?? 0;
            ViewBag.TotalProductCount = _db.Products.Count();
            ViewBag.TotalStockValue = _db.ProductStock
            .Select(ps => (decimal?)(ps.CurrentStock * ps.Products.Price))
            .Sum() ?? 0;

            // 低庫存提醒
            ViewBag.LowStockItems = _db.Products
                .Where(p => p.StockQuantity < p.MinStockLevel)
                .Select(p => new { p.ProductName, p.StockQuantity, p.ProductId })
                .ToList();

            // 即將到期提醒
            // 先從資料庫中提取所有產品，並在本地過濾即將到期的產品
            var products = _db.Products.ToList(); // 先載入所有產品
            ViewBag.ExpiringSoonItems = products
                .Where(p => p.ExpiryDate.HasValue && p.ExpiryDate.Value <= DateTime.Now.AddDays(30))
                .ToList();

            // 最近庫存變動
            ViewBag.RecentStockChanges = _db.ProductStock
                .OrderByDescending(ps => ps.CreatedAt)
                .Take(10)
                .Select(ps => new { ps.Products.ProductName, ps.StockChange, ps.TransactionType, ps.CreatedAt })
                .ToList();

            // 新訂單通知（顯示狀態為"Pending"的最近5筆訂單）
            ViewBag.NewOrders = _db.Orders
                .Where(o => o.OrderStatus == "Pending")
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new
                {
                    o.OrderId,
                    o.Users.UserAccount,
                    o.TotalAmount,
                    o.OrderDate
                })
                .ToList();

            return View();
        }
        #endregion
    }
}