using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TISS_WMS.Models
{
    public class UserViewModel
    {
        public int UserID { get; set; }
        public string UserAccount { get; set; }
        public string Email { get; set; }
        public List<string> AssignedRoles { get; set; }
    }

    public class User
    {
        public int UserID { get; set; }
        public string UserAccount { get; set; }
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; }
        public virtual ICollection<UserRoles> UserRoles { get; set; } // 加入此集合
    }

    public class Role
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; }
        public virtual ICollection<UserRoles> UserRoles { get; set; } // 加入此集合
    }
}