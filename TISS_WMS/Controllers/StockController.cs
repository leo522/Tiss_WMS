using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TISS_WMS.Models;
using OfficeOpenXml;

namespace TISS_WMS.Controllers
{
    public class StockController : Controller
    {
        private TISS_WMSEntities _db = new TISS_WMSEntities(); //資料庫

        #region 查看庫存清單
        public ActionResult StockList()
        {
            try
            {
                // 顯示所有庫存，包含產品名稱、倉庫名稱和當前庫存
                var stockList = from ps in _db.ProductStock
                                join p in _db.Products on ps.ProductId equals p.ProductId
                                join w in _db.Warehouses on ps.WarehouseId equals w.WarehouseId
                                select new StockViewModel
                                {
                                    ProductId = p.ProductId,
                                    ProductName = p.ProductName,
                                    Barcode = p.Barcode,
                                    WarehouseId = w.WarehouseId,
                                    WarehouseName = w.WarehouseName,
                                    CurrentStock = ps.CurrentStock,
                                    StockChange = ps.StockChange,
                                    TransactionType = ps.TransactionType,
                                    CreatedAt = ps.CreatedAt
                                };

                return View(stockList.ToList());
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        #endregion

        #region 新增庫存（入庫）
        public ActionResult AddStock()
        {
            try
            {
                ViewBag.Products = new SelectList(_db.Products, "ProductId", "ProductName");
                ViewBag.Warehouses = new SelectList(_db.Warehouses, "WarehouseId", "WarehouseName");
                return View();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddStock([Bind(Include = "ProductId, WarehouseId, StockChange, TransactionType")] ProductStock stock)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // 找到產品當前庫存，然後更新庫存
                    var currentStock = _db.ProductStock
                                        .Where(ps => ps.ProductId == stock.ProductId && ps.WarehouseId == stock.WarehouseId)
                                        .OrderByDescending(ps => ps.CreatedAt)
                                        .FirstOrDefault()?.CurrentStock ?? 0;

                    stock.CurrentStock = currentStock + stock.StockChange;
                    stock.CreatedAt = DateTime.Now;

                    _db.ProductStock.Add(stock);
                    _db.SaveChanges();

                    // 新增操作日誌
                    LogAction(GetCurrentUserId(), "新增庫存", $"產品ID: {stock.ProductId}，數量變更: {stock.StockChange}");

                    return RedirectToAction("StockList");
                }

                return View(stock);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "新增庫存時出現錯誤: " + ex.Message);
                return View(stock);
            }
        }
        #endregion

        #region 批量更新庫存
        [HttpGet]
        public ActionResult BatchUpdateStock()
        {
            var stockList = from ps in _db.ProductStock
                            join p in _db.Products on ps.ProductId equals p.ProductId
                            join w in _db.Warehouses on ps.WarehouseId equals w.WarehouseId
                            select new StockViewModel
                            {
                                ProductId = p.ProductId,
                                ProductName = p.ProductName,
                                Barcode = p.Barcode,
                                WarehouseId = w.WarehouseId,
                                WarehouseName = w.WarehouseName,
                                CurrentStock = ps.CurrentStock,
                                StockChange = 0 // 初始的變動量設為 0
                            };

            return View(stockList.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BatchUpdateStock(List<StockViewModel> stockUpdates)
        {
            try
            {
                foreach (var update in stockUpdates)
                {
                    var stockRecord = _db.ProductStock
                        .Where(ps => ps.ProductId == update.ProductId && ps.WarehouseId == update.WarehouseId)
                        .OrderByDescending(ps => ps.CreatedAt)
                        .FirstOrDefault();

                    if (stockRecord != null)
                    {
                        stockRecord.CurrentStock += update.StockChange;
                        _db.Entry(stockRecord).State = EntityState.Modified;

                        // 記錄日誌
                        LogAction(GetCurrentUserId(), "批量更新庫存", $"產品ID: {update.ProductId}，更新數量: {update.StockChange}");
                    }
                }

                _db.SaveChanges();
                return RedirectToAction("StockList");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "批量更新庫存時出現錯誤: " + ex.Message);
                return View(stockUpdates);
            }
        }
        #endregion

        #region 編輯庫存
        public ActionResult EditStock(int? id)
        {
            try
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                var stock = _db.ProductStock.Find(id);
                if (stock == null)
                {
                    return HttpNotFound();
                }

                ViewBag.Products = new SelectList(_db.Products, "ProductId", "ProductName", stock.ProductId);
                ViewBag.Warehouses = new SelectList(_db.Warehouses, "WarehouseId", "WarehouseName", stock.WarehouseId);

                return View(stock);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditStock([Bind(Include = "ProductStockId, ProductId, WarehouseId, StockChange, CurrentStock, TransactionType")] ProductStock stock)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _db.Entry(stock).State = EntityState.Modified;
                    _db.SaveChanges();

                    // 新增操作日誌
                    LogAction(GetCurrentUserId(), "編輯庫存", $"產品ID: {stock.ProductId}，新數量: {stock.CurrentStock}");

                    return RedirectToAction("StockList");
                }

                ViewBag.Products = new SelectList(_db.Products, "ProductId", "ProductName", stock.ProductId);
                ViewBag.Warehouses = new SelectList(_db.Warehouses, "WarehouseId", "WarehouseName", stock.WarehouseId);

                return View(stock);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 刪除庫存
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteStock(int id)
        {
            try
            {
                var stock = _db.ProductStock.Find(id);
                if (stock != null)
                {
                    _db.ProductStock.Remove(stock);
                    _db.SaveChanges();

                    // 新增操作日誌
                    LogAction(GetCurrentUserId(), "刪除庫存", $"庫存ID: {id}");

                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "庫存商品未找到" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "刪除庫存商品時出現錯誤: " + ex.Message });
            }
        }
        #endregion

        #region 查詢庫存
        public ActionResult SearchStock(string keyword)
        {
            try
            {
                var stockList = from ps in _db.ProductStock
                                join p in _db.Products on ps.ProductId equals p.ProductId
                                join w in _db.Warehouses on ps.WarehouseId equals w.WarehouseId
                                where p.ProductName.Contains(keyword) || p.Barcode.Contains(keyword)
                                select new StockViewModel
                                {
                                    ProductId = p.ProductId,
                                    ProductName = p.ProductName,
                                    Barcode = p.Barcode,
                                    WarehouseId = w.WarehouseId,
                                    WarehouseName = w.WarehouseName,
                                    CurrentStock = ps.CurrentStock,
                                    StockChange = ps.StockChange,
                                    TransactionType = ps.TransactionType,
                                    CreatedAt = ps.CreatedAt
                                };

                return View("StockList", stockList.ToList());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 顯示庫存盤點頁面
        [HttpGet]
        public ActionResult StockTaking()
        {
            try
            {
                // 獲取庫存資料並傳遞到視圖
                var stockList = _db.ProductStock
                                   .Join(_db.Products, ps => ps.ProductId, p => p.ProductId, (ps, p) => new StockViewModel
                                   {
                                       ProductId = p.ProductId,
                                       ProductName = p.ProductName,
                                       Barcode = p.Barcode,
                                       WarehouseId = ps.WarehouseId,
                                       WarehouseName = _db.Warehouses.FirstOrDefault(w => w.WarehouseId == ps.WarehouseId).WarehouseName,
                                       CurrentStock = ps.CurrentStock
                                   }).ToList();

                return View(stockList);  // 顯示庫存盤點頁面
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 提交庫存變更
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StockTaking(Dictionary<int, int> NewStock)
        {
            try
            {
                foreach (var stockItem in NewStock)
                {
                    var stock = _db.ProductStock.FirstOrDefault(ps => ps.ProductId == stockItem.Key);
                    if (stock != null)
                    {
                        stock.CurrentStock = stockItem.Value;
                        _db.Entry(stock).State = EntityState.Modified;
                    }
                }

                _db.SaveChanges();
                return RedirectToAction("StockList");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "庫存盤點時發生錯誤: " + ex.Message);
                return View();
            }
        }
        #endregion

        #region 歷史庫存查詢頁面
        public ActionResult HistoryStockList()
        {
            return View();
        }
        #endregion

        #region 查詢指定日期的庫存
        [HttpPost]
        public ActionResult GetStockSnapshot(DateTime date)
        {
            try
            {
                // 查詢指定日期的庫存快照
                var stockSnapshot = from ps in _db.ProductStock
                                    join p in _db.Products on ps.ProductId equals p.ProductId
                                    join w in _db.Warehouses on ps.WarehouseId equals w.WarehouseId
                                    where DbFunctions.TruncateTime(ps.CreatedAt) == date.Date
                                    select new StockViewModel
                                    {
                                        ProductId = p.ProductId,
                                        ProductName = p.ProductName,
                                        Barcode = p.Barcode,
                                        WarehouseId = w.WarehouseId,
                                        WarehouseName = w.WarehouseName,
                                        CurrentStock = ps.CurrentStock,
                                        StockChange = ps.StockChange,
                                        TransactionType = ps.TransactionType,
                                        CreatedAt = ps.CreatedAt
                                    };

                return View("_HistoryStockResults", stockSnapshot.ToList());
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "查詢庫存快照時出現錯誤: " + ex.Message);
                return View("HistoryStockList");
            }
        }
        #endregion

        #region 生成和導出庫存報表

        public ActionResult ExportStockReport(string format)
        {
            try
            {
                var stockData = from ps in _db.ProductStock
                                join p in _db.Products on ps.ProductId equals p.ProductId
                                join w in _db.Warehouses on ps.WarehouseId equals w.WarehouseId
                                select new
                                {
                                    p.ProductName,
                                    p.Barcode,
                                    Warehouse = w.WarehouseName,
                                    ps.CurrentStock,
                                    ps.CreatedAt
                                };

                // 將數據導出為 CSV 或 Excel 格式
                if (format == "csv")
                {
                    var csvData = "產品名稱, 條碼, 倉庫, 當前庫存, 創建日期\n";
                    foreach (var item in stockData)
                    {
                        csvData += $"{item.ProductName}, {item.Barcode}, {item.Warehouse}, {item.CurrentStock}, {item.CreatedAt}\n";
                    }
                    var bytes = System.Text.Encoding.UTF8.GetBytes(csvData);
                    return File(bytes, "text/csv", "StockReport.csv");
                }
                else if (format == "excel")
                {
                    // 使用 ExcelPackage 導出數據
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("StockReport");
                        worksheet.Cells.LoadFromCollection(stockData, true);
                        var excelBytes = package.GetAsByteArray();
                        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StockReport.xlsx");
                    }
                }

                return RedirectToAction("StockList");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region 進出貨報表功能
        public ActionResult ExportInOutReport(string format)
        {
            try
            {
                var inOutData = from ps in _db.ProductStock
                                join p in _db.Products on ps.ProductId equals p.ProductId
                                join w in _db.Warehouses on ps.WarehouseId equals w.WarehouseId
                                select new
                                {
                                    p.ProductName,
                                    p.Barcode,
                                    Warehouse = w.WarehouseName,
                                    ps.StockChange,
                                    ps.TransactionType,
                                    ps.CreatedAt
                                };

                if (format == "csv")
                {
                    var csvData = "產品名稱, 條碼, 倉庫, 庫存變動, 交易類型, 日期\n";
                    foreach (var item in inOutData)
                    {
                        csvData += $"{item.ProductName}, {item.Barcode}, {item.Warehouse}, {item.StockChange}, {item.TransactionType}, {item.CreatedAt}\n";
                    }
                    var bytes = System.Text.Encoding.UTF8.GetBytes(csvData);
                    return File(bytes, "text/csv", "InOutReport.csv");
                }
                else if (format == "excel")
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("InOutReport");
                        worksheet.Cells.LoadFromCollection(inOutData, true);
                        var excelBytes = package.GetAsByteArray();
                        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InOutReport.xlsx");
                    }
                }

                return RedirectToAction("StockList");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 庫存盤點報表
        public ActionResult ExportStockTakingReport(string format)
        {
            try
            {
                var stockTakingData = from ps in _db.ProductStock
                                      join p in _db.Products on ps.ProductId equals p.ProductId
                                      join w in _db.Warehouses on ps.WarehouseId equals w.WarehouseId
                                      where ps.CurrentStock != ps.StockChange  // 篩選出庫存異常的產品
                                      select new
                                      {
                                          p.ProductName,
                                          p.Barcode,
                                          Warehouse = w.WarehouseName,
                                          ps.CurrentStock,
                                          ExpectedStock = ps.CurrentStock + ps.StockChange,
                                          Discrepancy = ps.StockChange,
                                          ps.CreatedAt
                                      };

                if (format == "csv")
                {
                    var csvData = "產品名稱, 條碼, 倉庫, 當前庫存, 理論庫存, 庫存差異, 日期\n";
                    foreach (var item in stockTakingData)
                    {
                        csvData += $"{item.ProductName}, {item.Barcode}, {item.Warehouse}, {item.CurrentStock}, {item.ExpectedStock}, {item.Discrepancy}, {item.CreatedAt}\n";
                    }
                    var bytes = System.Text.Encoding.UTF8.GetBytes(csvData);
                    return File(bytes, "text/csv", "StockTakingReport.csv");
                }
                else if (format == "excel")
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("StockTakingReport");
                        worksheet.Cells.LoadFromCollection(stockTakingData, true);
                        var excelBytes = package.GetAsByteArray();
                        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StockTakingReport.xlsx");
                    }
                }

                return RedirectToAction("StockList");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Log操作紀錄
        private void LogAction(int userId, string userAction, string details)
        {
            _db.Logs.Add(new Logs
            {
                UserId = userId,
                UserAction = userAction,
                Details = details,
                Timestamp = DateTime.Now
            });
            _db.SaveChanges();
        }
        #endregion

        #region 取得當前用戶ID

        private int GetCurrentUserId()
        {
            return Session["UserId"] != null ? (int)Session["UserId"] : 0;
        }
        #endregion
    }
}