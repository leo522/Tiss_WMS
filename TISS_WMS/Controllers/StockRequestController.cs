using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TISS_WMS.Models;

namespace TISS_WMS.Controllers
{
    public class StockRequestController : Controller
    {
        private TISS_WMSEntities _db = new TISS_WMSEntities(); //資料庫

        #region 申請入庫/出庫
        public ActionResult CreateRequest()
        {
            ViewBag.Products = new SelectList(_db.Products, "ProductId", "ProductName");
            ViewBag.Warehouses = new SelectList(_db.Warehouses, "WarehouseId", "WarehouseName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRequest([Bind(Include = "ProductId, WarehouseId, Quantity, RequestType")] StockRequests request)
        {
            request.RequestDate = DateTime.Now;
            request.Status = StockRequestStatus.Pending.ToString();
            request.RequestedBy = /* 當前登入用戶ID */ 1;

            _db.StockRequests.Add(request);
            _db.SaveChanges();

            return RedirectToAction("RequestList");
        }
        #endregion

        #region 查看申請列表
        public ActionResult RequestList()
        {
            var requests = _db.StockRequests.Include("Product").Include("Warehouse").ToList();
            return View(requests);
        }
        #endregion

        #region  審批入庫/出庫申請
        public ActionResult ApproveRequest(int id)
        {
            var request = _db.StockRequests.Find(id);
            if (request == null) return HttpNotFound();

            request.Status = StockRequestStatus.Approved.ToString();
            request.ApprovedBy = /* 當前登入管理員ID */ 1;
            request.ApprovalDate = DateTime.Now;

            _db.SaveChanges();
            return RedirectToAction("RequestList");
        }

        public ActionResult RejectRequest(int id)
        {
            var request = _db.StockRequests.Find(id);
            if (request == null) return HttpNotFound();

            request.Status = StockRequestStatus.Rejected.ToString();
            _db.SaveChanges();
            return RedirectToAction("RequestList");
        }
        #endregion

        #region 確認出庫/入庫並更新庫存
        public ActionResult ConfirmRequest(int id)
        {
            var request = _db.StockRequests.Find(id);
            if (request == null || request.Status != StockRequestStatus.Approved.ToString()) return HttpNotFound();

            // 根據RequestType確定入庫或出庫
            var productStock = _db.ProductStock.FirstOrDefault(ps => ps.ProductId == request.ProductId && ps.WarehouseId == request.WarehouseId);
            if (request.RequestType == "In")
            {
                productStock.CurrentStock += request.Quantity;
            }
            else if (request.RequestType == "Out" && productStock.CurrentStock >= request.Quantity)
            {
                productStock.CurrentStock -= request.Quantity;
            }

            request.Status = StockRequestStatus.Completed.ToString();
            _db.SaveChanges();

            return RedirectToAction("RequestList");
        }
        #endregion
    }
}