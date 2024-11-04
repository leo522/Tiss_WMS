using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TISS_WMS.Models;

namespace TISS_WMS.Controllers
{
    public class WarehousesController : Controller
    {
        private TISS_WMSEntities _db = new TISS_WMSEntities();

        #region 倉庫管理列表
        public ActionResult WarehouseList()
        {
            var warehouses = _db.Warehouses.ToList();
            return View(warehouses);
        }
        #endregion

        #region 新增倉庫
        [HttpGet]
        public ActionResult AddWarehouse()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddWarehouse(Warehouses warehouse)
        {
            if (ModelState.IsValid)
            {
                warehouse.CreatedAt = DateTime.Now;
                _db.Warehouses.Add(warehouse);
                _db.SaveChanges();
                return RedirectToAction("WarehouseList");
            }
            return View(warehouse);
        }
        #endregion

        #region 編輯倉庫
        [HttpGet]
        public ActionResult EditWarehouse(int id)
        {
            var warehouse = _db.Warehouses.Find(id);
            if (warehouse == null)
            {
                return new HttpNotFoundResult();
            }
            return View(warehouse);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditWarehouse(Warehouses warehouse)
        {
            if (ModelState.IsValid)
            {
                warehouse.UpdatedAt = DateTime.Now;
                _db.Entry(warehouse).State = EntityState.Modified;
                _db.SaveChanges();
                return RedirectToAction("WarehouseList");
            }
            return View(warehouse);
        }
        #endregion

        #region 刪除倉庫
        [HttpPost]
        public ActionResult DeleteWarehouse(int id)
        {
            var warehouse = _db.Warehouses.Find(id);
            if (warehouse != null)
            {
                _db.Warehouses.Remove(warehouse);
                _db.SaveChanges();
            }
            return RedirectToAction("WarehouseList");
        }
        #endregion

        #region 單位管理列表
        public ActionResult UnitList()
        {
            var units = _db.Units.ToList();
            return View(units);
        }
        #endregion

        #region 新增單位
        [HttpGet]
        public ActionResult AddUnit()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddUnit(Units unit)
        {
            if (ModelState.IsValid)
            {
                unit.CreatedAt = DateTime.Now;
                _db.Units.Add(unit);
                _db.SaveChanges();
                return RedirectToAction("UnitList");
            }
            return View(unit);
        }
        #endregion

        #region 編輯單位
        [HttpGet]
        public ActionResult EditUnit(int id)
        {
            var unit = _db.Units.Find(id);
            if (unit == null)
            {
                return new HttpNotFoundResult();
            }
            return View(unit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUnit(Units unit)
        {
            if (ModelState.IsValid)
            {
                unit.UpdatedAt = DateTime.Now;
                _db.Entry(unit).State = EntityState.Modified;
                _db.SaveChanges();
                return RedirectToAction("UnitList");
            }
            return View(unit);
        }
        #endregion

        #region 刪除單位
        [HttpPost]
        public ActionResult DeleteUnit(int id)
        {
            var unit = _db.Units.Find(id);
            if (unit != null)
            {
                _db.Units.Remove(unit);
                _db.SaveChanges();
            }
            return RedirectToAction("UnitList");
        }
        #endregion

        #region 類別管理列表
        public ActionResult CategoryList()
        {
            var categories = _db.Categories.ToList();
            return View(categories);
        }
        #endregion

        #region 新增類別
        [HttpGet]
        public ActionResult AddCategory()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddCategory(Categories category)
        {
            if (ModelState.IsValid)
            {
                category.CreatedAt = DateTime.Now;
                _db.Categories.Add(category);
                _db.SaveChanges();
                return RedirectToAction("CategoryList");
            }
            return View(category);
        }
        #endregion

        #region 編輯類別
        [HttpGet]
        public ActionResult EditCategory(int id)
        {
            var category = _db.Categories.Find(id);
            if (category == null)
            {
                return new HttpNotFoundResult();
            }
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCategory(Categories category)
        {
            if (ModelState.IsValid)
            {
                category.UpdatedAt = DateTime.Now;
                _db.Entry(category).State = EntityState.Modified;
                _db.SaveChanges();
                return RedirectToAction("CategoryList");
            }
            return View(category);
        }
        #endregion

        #region 刪除類別
        [HttpPost]
        public ActionResult DeleteCategory(int id)
        {
            var category = _db.Categories.Find(id);
            if (category != null)
            {
                _db.Categories.Remove(category);
                _db.SaveChanges();
            }
            return RedirectToAction("CategoryList");
        }
        #endregion
    }
}