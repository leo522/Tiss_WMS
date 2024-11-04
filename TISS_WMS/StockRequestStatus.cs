using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TISS_WMS
{
    public enum StockRequestStatus
    {
        Pending,    // 待審批
        Approved,   // 已批准
        Rejected,   // 已拒絕
        Completed   // 已完成
    }
}