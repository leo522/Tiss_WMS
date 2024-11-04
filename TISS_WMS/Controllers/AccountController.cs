using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using static TISS_WMS.Models.accountModel;
using System.Web.Security;
using TISS_WMS.Models;

namespace TISS_WMS.Controllers
{
    public class AccountController : Controller
    {
        private TISS_WMSEntities _db = new TISS_WMSEntities(); //資料庫

        #region 註冊帳號
        [HttpGet]
        public ActionResult Register()
        {
            // 假設已有角色表，這裡填充角色下拉選單
            //ViewBag.Roles = new SelectList(_db.Roles, "RoleID", "RoleName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(Users user, string pwd)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // 檢查帳號或郵箱是否已存在
                    if (_db.Users.Any(u => u.UserAccount == user.UserAccount || u.Email == user.Email))
                    {
                        ModelState.AddModelError("", "帳號或郵箱已存在");
                        return View(user);
                    }

                    // 密碼加密
                    user.PasswordHash = HashPassword(pwd);

                    // 設置其他初始屬性
                    user.CreatedAt = DateTime.Now;
                    user.UpdatedAt = DateTime.Now;
                    user.IsActive = true;

                    // 分配預設角色
                    var userRole = _db.Roles.FirstOrDefault(r => r.RoleName == "User");

                    if (userRole != null)
                    {
                        user.RoleID = userRole.RoleID;
                        user.Role = "User";
                    }

                    _db.Users.Add(user);
                    _db.SaveChanges();

                    // 記錄日誌並使用 GetClientIpAddress() 方法
                    LogAction(user.UserID, "註冊帳號", $"註冊新帳號：{user.UserAccount}", GetClientIpAddress());

                    return RedirectToAction("Login");
                }

                // 如果有錯誤返回視圖
                ViewBag.Roles = new SelectList(_db.Roles, "RoleID", "RoleName");
                return View(user);
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var validationError in ex.EntityValidationErrors)
                {
                    foreach (var error in validationError.ValidationErrors)
                    {
                        // 把錯誤的欄位名稱和錯誤訊息加到 ModelState 中
                        ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                    }
                }

                // 填充角色下拉選單 (若前端需要)
                ViewBag.Roles = new SelectList(_db.Roles, "RoleID", "RoleName");

                // 返回包含錯誤訊息的視圖
                return View(user);
            }
        }
        #endregion

        #region 登入
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string userAccount, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(userAccount) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError("", "請輸入帳號和密碼");
                    return View();
                }

                // 加密輸入的密碼
                var hashedPassword = HashPassword(password);

                // 查找符合的使用者
                var user = _db.Users.FirstOrDefault(u => u.UserAccount == userAccount && u.PasswordHash == hashedPassword && u.IsActive == true);

                if (user != null)
                {
                    Session["UserID"] = user.UserID;
                    Session["Role"] = user.Role;
                    user.LastLogin = DateTime.Now;
                    _db.SaveChanges();

                    // 記錄日誌
                    LogAction(user.UserID, "登入", "使用者登入成功", GetClientIpAddress());

                    return RedirectToAction("Main", "Main");
                }

                ModelState.AddModelError("", "登入失敗，帳號或密碼錯誤");
                return View();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        #endregion

        #region 登出
        public ActionResult Logout()
        {
            int? userId = Session["UserID"] as int?;
            if (userId.HasValue)
            {
                // 記錄登出日誌
                LogAction(userId.Value, "登出", "用戶登出系統", GetClientIpAddress());
            }

            Session.Clear();
            return RedirectToAction("Login");
        }
        #endregion

        #region 密碼加密
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
        #endregion

        #region 忘記密碼
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string email)
        {
            var user = _db.Users.FirstOrDefault(u => u.Email == email);
            if (user != null)
            {
                string resetLink = Url.Action("ResetPassword", "Account", new { userId = user.UserID }, Request.Url.Scheme);
                string subject = "重置您的密碼";
                string body = $"點擊此連結重置您的密碼: <a href='{resetLink}'>重置密碼</a>";

                SendEmail(user.Email, subject, body);
                ViewBag.Message = "重置密碼的郵件已發送至您的電子郵件。";
            }
            else
            {
                ViewBag.Message = "該郵箱不存在於系統中。";
            }
            return View();
        }
        #endregion

        #region Log日誌記錄功能
        private void LogAction(int userId, string action, string details, string ipAddress)
        {
            _db.Logs.Add(new Logs
            {
                UserId = userId,
                UserAction = action,
                Details = details,
                Timestamp = DateTime.Now,
                IPAddress = ipAddress ?? Request.ServerVariables["REMOTE_ADDR"]
            });
            _db.SaveChanges();
        }
        #endregion

        #region 查詢客戶端 IP
        private string GetClientIpAddress()
        {
            var ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip))
            {
                ip = Request.UserHostAddress;
            }
            return ip;
        }

        #endregion

        #region 查詢日誌記錄
        public ActionResult LogList()
        {
            var logs = _db.Logs
                          .OrderByDescending(log => log.Timestamp)
                          .Take(100)
                          .ToList(); // 取得最近的 100 筆記錄

            return View(logs);
        }
        #endregion

        #region 人員權限管理
        // 顯示所有使用者和角色
        [HttpGet]
        public ActionResult ManageUsers()
        {
            var users = _db.Users
                .Select(u => new UserViewModel
                {
                    UserID = u.UserID,
                    UserAccount = u.UserAccount,
                    Email = u.Email,
                    AssignedRoles = u.UserRoles.Select(ur => ur.Roles.RoleName).ToList()
                }).ToList();

            // 傳遞可選的角色清單
            ViewBag.Roles = _db.Roles.Select(r => new SelectListItem
            {
                Value = r.RoleID.ToString(),
                Text = r.RoleName
            }).ToList();

            return View(users);
        }
        #endregion

        #region 更新使用者角色
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateRoles(int userId, List<int> roleIds)
        {
            var user = _db.Users.Find(userId);
            if (user == null) return HttpNotFound();

            // 刪除現有角色
            var existingRoles = _db.UserRoles.Where(ur => ur.UserID == userId).ToList();
            _db.UserRoles.RemoveRange(existingRoles);

            // 新增選定的角色
            foreach (var roleId in roleIds)
            {
                _db.UserRoles.Add(new UserRoles
                {
                    UserID = userId,
                    RoleID = roleId
                });
            }

            _db.SaveChanges();

            LogAction(userId, "更新角色", $"更新角色為: {string.Join(",", roleIds)}", Request.ServerVariables["REMOTE_ADDR"]);

            return RedirectToAction("ManageUsers");
        }
        #endregion

        #region 郵件發送
        private void SendEmail(string toEmail, string subject, string body, string attachmentPath = null)
        {
            var fromEmail = "00048@tiss.org.tw";
            var fromPassword = "lctm hhfh bubx lwda"; //應用程式密碼
            var displayName = "運科中心資訊組"; //顯示的發件人名稱

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(fromEmail, fromPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, displayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            foreach (var email in toEmail.Split(',')) //分割以逗號分隔的收件人地址並添加到郵件中
            {
                mailMessage.To.Add(email.Trim());
            }

            if (!string.IsNullOrEmpty(attachmentPath))
            {
                Attachment attachment = new Attachment(attachmentPath);
                mailMessage.Attachments.Add(attachment);
            }

            try
            {
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("郵件發送失敗: " + ex.Message); //處理發送郵件的錯誤
            }
        }
        #endregion
    }
}