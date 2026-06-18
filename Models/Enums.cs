using System;
using System.ComponentModel;

namespace FinanceReimbursement.Models
{
    public enum EmployeeLevel
    {
        [Description("普通员工")]
        Junior = 1,

        [Description("主管")]
        Supervisor = 2,

        [Description("经理")]
        Manager = 3,

        [Description("总监")]
        Director = 4,

        [Description("副总裁")]
        VicePresident = 5,

        [Description("总裁")]
        President = 6
    }

    public enum CityLevel
    {
        [Description("一线城市")]
        Tier1 = 1,

        [Description("二线城市")]
        Tier2 = 2,

        [Description("三线城市")]
        Tier3 = 3,

        [Description("四线及以下")]
        Tier4 = 4
    }

    public enum TransportationType
    {
        [Description("飞机")]
        Airplane = 1,

        [Description("高铁/动车")]
        HighSpeedRail = 2,

        [Description("火车")]
        Train = 3,

        [Description("汽车")]
        Bus = 4,

        [Description("出租车")]
        Taxi = 5,

        [Description("地铁")]
        Subway = 6,

        [Description("自驾")]
        SelfDriving = 7,

        [Description("轮船")]
        Ship = 8
    }

    public enum ExpenseCategory
    {
        [Description("交通费")]
        Transportation = 1,

        [Description("住宿费")]
        Accommodation = 2,

        [Description("餐饮费")]
        Meal = 3,

        [Description("通讯费")]
        Communication = 4,

        [Description("办公费")]
        Office = 5,

        [Description("招待费")]
        Entertainment = 6,

        [Description("会议费")]
        Conference = 7,

        [Description("培训费")]
        Training = 8,

        [Description("其他")]
        Other = 99
    }

    public enum InvoiceType
    {
        [Description("增值税专用发票")]
        VATSpecial = 1,

        [Description("增值税普通发票")]
        VATGeneral = 2,

        [Description("电子发票")]
        Electronic = 3,

        [Description("定额发票")]
        FixedAmount = 4,

        [Description("收据")]
        Receipt = 5,

        [Description("国际发票")]
        International = 6
    }

    public enum ReimbursementStatus
    {
        [Description("草稿")]
        Draft = 0,

        [Description("待审批")]
        Pending = 1,

        [Description("审批中")]
        Approving = 2,

        [Description("已通过")]
        Approved = 3,

        [Description("已拒绝")]
        Rejected = 4,

        [Description("已退回")]
        Returned = 5,

        [Description("已付款")]
        Paid = 6
    }

    public enum ApprovalNodeType
    {
        [Description("部门主管")]
        DepartmentHead = 1,

        [Description("财务审核")]
        FinanceReview = 2,

        [Description("财务经理")]
        FinanceManager = 3,

        [Description("分管领导")]
        SuperiorLeader = 4,

        [Description("总经理")]
        GeneralManager = 5,

        [Description("总裁")]
        CEO = 6
    }

    public enum ValidationSeverity
    {
        [Description("信息")]
        Info = 0,

        [Description("警告")]
        Warning = 1,

        [Description("错误")]
        Error = 2
    }
}
