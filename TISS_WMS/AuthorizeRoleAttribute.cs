using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TISS_WMS
{
    public class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        private readonly string _role;
        public AuthorizeRoleAttribute(string role)
        {
            _role = role;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var role = httpContext.Session["Role"]?.ToString();
            return role == _role;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectResult("~/Account/Login");
        }
    }
}