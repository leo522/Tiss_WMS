﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TISS_WMS.Models;
using ZXing;

namespace TISS_WMS.Controllers
{
    public class ProductsController : Controller
    {
        private TISS_WMSEntities _db = new TISS_WMSEntities(); //資料庫

        #region 新增產品
        //[AuthorizeRole("Admin")]
        public ActionResult ProductCreate()
        {
            return View();
        }

        //[AuthorizeRole("Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken] //防止CSRF攻擊
        public ActionResult ProductCreate([Bind(Include = "ProductName,ProductDescription,Barcode,Unit,StockQuantity,Price,Remark")] Products product)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    product.CreatedAt = DateTime.Now;
                    product.UpdatedAt = DateTime.Now;

                    _db.Products.Add(product);
                    _db.SaveChanges();

                    // 新增操作日誌
                    LogAction(GetCurrentUserId(), "新增產品", $"新增產品：{product.ProductName}");

                    return RedirectToAction("ProductList");
                }

                return View(product);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "新增產品時出現錯誤: " + ex.Message);
                return View(product);
            }
        }
        #endregion

        #region 批量新增產品
        public ActionResult BatchAddProducts()
        {
            return View(new List<Products>()); // 返回空列表，允許批量新增
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // 防止CSRF攻擊
        public ActionResult BatchAddProducts(List<Products> products)
        {
            try
            {
                if (products != null && products.Any())
                {
                    foreach (var product in products)
                    {
                        product.CreatedAt = DateTime.Now;
                        product.UpdatedAt = DateTime.Now;
                        _db.Products.Add(product);

                        // 新增操作日誌
                        LogAction(GetCurrentUserId(), "批量新增產品", $"新增產品：{product.ProductName}");
                    }

                    _db.SaveChanges();
                    return RedirectToAction("ProductList");
                }

                ModelState.AddModelError("", "沒有可新增的產品");
                return View(products);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "批量新增產品時出現錯誤: " + ex.Message);
                return View(products);
            }
        }
        #endregion

        #region FIFO 產品出庫管理
        public ActionResult ProductFIFO()
        {
            // 根據產品建立時間排序，以先進先出順序處理
            var fifoProduct = _db.Products
                .OrderBy(p => p.CreatedAt)
                .FirstOrDefault();

            if (fifoProduct != null)
            {
                // 處理出庫邏輯
                fifoProduct.StockQuantity -= 1; // 減少庫存
                _db.SaveChanges();
            }

            return RedirectToAction("ProductList");
        }
        #endregion

        #region 顯示產品列表
        public ActionResult ProductList()
        {
            var dtos = _db.Products
                  .OrderBy(p => p.CreatedAt) // 按建立日期排序
                  .ToList();
            return View(dtos);
        }
        #endregion

        #region 查詢產品資訊
        [HttpGet]
        public ActionResult GetProductByBarcodeOrQRCode(string code)
        {
            var product = _db.Products.FirstOrDefault(p => p.Barcode == code);

            if (product != null)
            {
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        product.ProductId,
                        product.ProductName,
                        product.ProductDescription,
                        product.Unit,
                        product.StockQuantity,
                        product.Price,
                        product.Remark
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = false, message = "找不到相符的產品資料" }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region 編輯產品
        public ActionResult ProductEdit(int? id)
        {
            try
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                Products product = _db.Products.Find(id);
                if (product == null)
                {
                    return HttpNotFound();
                }

                return View(product);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] //防止CSRF攻擊
        public ActionResult ProductEdit([Bind(Include = "ProductId,ProductName,ProductDescription,Barcode,Unit,StockQuantity,Price,Remark")] Products product)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    product.UpdatedAt = DateTime.Now; // 更新修改時間
                    _db.Entry(product).State = EntityState.Modified; // 設定狀態為已修改
                    _db.SaveChanges(); // 保存變更

                    // 新增操作日誌
                    LogAction(GetCurrentUserId(), "編輯產品", $"編輯產品：{product.ProductName}");

                    return RedirectToAction("ProductList"); // 編輯成功後重定向到產品列表
                }

                return View(product); // 如果資料驗證失敗，重新顯示編輯表單
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 刪除產品
        [HttpPost]
        [ValidateAntiForgeryToken] // 防止 CSRF 攻擊
        [Authorize(Roles = "Admin")] // 僅限於管理員角色刪除產品
        public ActionResult ProductDelete(int id)
        {
            try
            {
                var product = _db.Products.Find(id);
                if (product != null)
                {
                    _db.Products.Remove(product);
                    _db.SaveChanges();

                    // 新增操作日誌
                    LogAction(GetCurrentUserId(), "刪除產品", $"刪除產品ID：{id}");

                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "產品未找到" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "刪除失敗: " + ex.Message });
            }
        }
        #endregion

        #region 生成條碼字串
        public ActionResult GenerateBarcodeString()
        {
            // 使用 GUID 或其他邏輯生成唯一條碼
            string barcode = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 12);

            return Json(new { barcode = barcode }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region 生成條碼圖像

        public ActionResult GenerateBarcodeImage(string barcodeText)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.CODE_128, // 或者其他格式如 EAN_13, CODE_39 等
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = 150,
                    Width = 300
                }
            };

            // 生成條碼圖像
            var result = writer.Write(barcodeText);
            var barcodeBitmap = new Bitmap(result);

            // 將條碼圖像轉換為 MemoryStream，並返回圖片
            using (var stream = new MemoryStream())
            {
                barcodeBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return File(stream.ToArray(), "image/jepg");
            }
        }
        #endregion

        #region 生成 QR 碼圖像
        public ActionResult GenerateQRCode(string barcodeText)
        {
            var qrWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = 300, // 設置 QR 碼高度
                    Width = 300 // 設置 QR 碼寬度
                }
            };

            // 生成 QR 碼圖像
            var result = qrWriter.Write(barcodeText);
            var qrBitmap = new Bitmap(result);

            // 將 QR 碼圖像轉換為 MemoryStream，並返回圖片
            using (var stream = new MemoryStream())
            {
                qrBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return File(stream.ToArray(), "image/png");
            }
        }
        #endregion

        #region 取得當前用戶ID的方法
        private int GetCurrentUserId()
        {
            // 假設Session中保存了用戶ID，可以根據實際情況調整
            return Session["UserId"] != null ? (int)Session["UserId"] : 0;
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
    }
}