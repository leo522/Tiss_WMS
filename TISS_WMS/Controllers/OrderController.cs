using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TISS_WMS.Models;

namespace TISS_WMS.Controllers
{
    public class OrderController : Controller
    {
        private readonly TISS_WMSEntities _db = new TISS_WMSEntities();

        #region 訂單下單
        [HttpGet]
        public ActionResult CreateOrder()
        {
            ViewBag.Users = _db.Users.Where(u => u.IsActive ?? false).ToList();
            ViewBag.Products = _db.Products.ToList();
            return View(new OrderViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateOrder(OrderViewModel model)
        {
            if (ModelState.IsValid)
            {
                var order = new Orders
                {
                    UserId = model.UserId,
                    OrderDate = DateTime.Now,
                    OrderStatus = "Pending",
                    TotalAmount = model.OrderItems.Sum(i => i.Quantity * i.Price),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _db.Orders.Add(order);
                _db.SaveChanges();

                foreach (var item in model.OrderItems)
                {
                    _db.OrderItems.Add(new OrderItems
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        CreatedAt = DateTime.Now
                    });
                }
                _db.SaveChanges();

                LogAction(order.UserId, "新增訂單", $"訂單ID: {order.OrderId}");

                return RedirectToAction("OrderList");
            }
            return View(model);
        }

        private void LogAction(int userId, string action, string details)
        {
            _db.Logs.Add(new Logs
            {
                UserId = userId,
                UserAction = action,
                Details = details,
                Timestamp = DateTime.Now
            });
            _db.SaveChanges();
        }
        #endregion

        #region 訂單列表及狀態查看
        public ActionResult OrderList()
        {
            var orders = _db.Orders.Include("Users").ToList();

            return View(orders);
        }
        #endregion

        #region 訂單細節
        public ActionResult OrderDetails(int id)
        {
            var order = _db.Orders
                           .Include("Users")
                           .Include("OrderItems")
                           .Include("OrderItems.Products")
                           .FirstOrDefault(o => o.OrderId == id);
            if (order == null) return HttpNotFound();

            return View(order);
        }
        #endregion

        #region 訂單出庫
        [HttpPost]
        public ActionResult ShipOrder(int orderId)
        {
            var order = _db.Orders.Find(orderId);
            if (order != null && order.OrderStatus == "Pending")
            {
                order.OrderStatus = "Shipped";
                _db.OutboundOrders.Add(new OutboundOrders
                {
                    OrderId = orderId,
                    ShippedByUserId = GetCurrentUserId(),
                    WarehouseId = 1, // 假設默認為1號倉庫
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
                _db.SaveChanges();
            }
            return RedirectToAction("OrderList");
        }

        private int GetCurrentUserId()
        {
            // 假設 UserID 已儲存在 Session 中
            return Session["UserID"] != null ? (int)Session["UserID"] : 0;
        }

        #endregion
    }
}