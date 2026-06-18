using System;
using System.Collections.Generic;
using FinanceReimbursement.Models;

namespace FinanceReimbursement.Services
{
    public class BudgetService
    {
        private readonly Dictionary<string, Budget> _budgets;

        public BudgetService()
        {
            _budgets = new Dictionary<string, Budget>();
        }

        public BudgetService(IEnumerable<Budget> budgets) : this()
        {
            if (budgets == null) return;
            foreach (var budget in budgets)
            {
                var key = GetBudgetKey(budget);
                _budgets[key] = budget;
            }
        }

        public void AddBudget(Budget budget)
        {
            if (budget == null) return;
            var key = GetBudgetKey(budget);
            _budgets[key] = budget;
        }

        public void AddBudgets(IEnumerable<Budget> budgets)
        {
            if (budgets == null) return;
            foreach (var budget in budgets)
            {
                AddBudget(budget);
            }
        }

        public Budget? GetBudget(string departmentId, string projectId = "",
            ExpenseCategory? category = null, int? year = null, int? month = null)
        {
            var y = year ?? DateTime.Now.Year;
            var m = month ?? DateTime.Now.Month;

            var searchKeys = new List<string>
            {
                GetKey(departmentId, projectId, category, y, m),
                GetKey(departmentId, projectId, category, y, null),
                GetKey(departmentId, projectId, null, y, m),
                GetKey(departmentId, projectId, null, y, null),
                GetKey(departmentId, "", category, y, m),
                GetKey(departmentId, "", category, y, null),
                GetKey(departmentId, "", null, y, m),
                GetKey(departmentId, "", null, y, null),
            };

            foreach (var key in searchKeys)
            {
                if (_budgets.TryGetValue(key, out var budget))
                {
                    return budget;
                }
            }

            return null;
        }

        public ValidationResult CheckBudget(ReimbursementForm form)
        {
            var result = new ValidationResult();

            if (form == null) return result;

            var categorySummary = form.GetCategorySummary();
            string deptId = form.DepartmentId;
            string projectId = form.ProjectId;

            if (string.IsNullOrWhiteSpace(deptId))
            {
                result.AddWarning("DEPARTMENT_EMPTY", "未指定部门，无法进行预算校验", "预算校验");
            }

            decimal totalExceed = 0;
            Dictionary<string, BudgetCheckDetail> details = new Dictionary<string, BudgetCheckDetail>();

            foreach (var kv in categorySummary)
            {
                var category = kv.Key;
                var amount = kv.Value;

                var budget = GetBudget(deptId, projectId, category);

                if (budget == null)
                {
                    result.AddInfo("BUDGET_NOT_FOUND",
                        $"[{GetCategoryName(category)}]未配置预算，跳过检查",
                        "预算校验");
                    continue;
                }

                if (!budget.CanAfford(amount))
                {
                    var exceed = amount - budget.RemainingBudget;
                    totalExceed += exceed;

                    result.AddError("BUDGET_EXCEEDED",
                        $"[{GetCategoryName(category)}]预算不足，预算{budget.TotalBudget:N2}元，已用{budget.UsedBudget:N2}元，剩余{budget.RemainingBudget:N2}元，本次申请{amount:N2}元，超额{exceed:N2}元",
                        "预算校验", expected: budget.RemainingBudget, actual: amount,
                        suggestion: "请申请预算追加或拆分到下个周期");
                }
                else if (budget.UsageRate >= 80)
                {
                    result.AddWarning("BUDGET_ALERT",
                        $"[{GetCategoryName(category)}]预算使用率已达{budget.UsageRate:F1}%，剩余{budget.RemainingBudget:N2}元，请节约使用",
                        "预算校验");
                }
            }

            var overallBudget = GetBudget(deptId, projectId);
            if (overallBudget != null)
            {
                if (!overallBudget.CanAfford(form.TotalAmount))
                {
                    var exceed = form.TotalAmount - overallBudget.RemainingBudget;
                    result.AddError("TOTAL_BUDGET_EXCEEDED",
                        $"部门总预算不足，总预算{overallBudget.TotalBudget:N2}元，已用{overallBudget.UsedBudget:N2}元，剩余{overallBudget.RemainingBudget:N2}元，本次申请{form.TotalAmount:N2}元，超额{exceed:N2}元",
                        "预算校验", overallBudget.RemainingBudget, form.TotalAmount,
                        "请申请预算追加");
                }
            }

            return result;
        }

        public void UseBudget(ReimbursementForm form)
        {
            if (form == null) return;

            var categorySummary = form.GetCategorySummary();
            string deptId = form.DepartmentId;
            string projectId = form.ProjectId;

            foreach (var kv in categorySummary)
            {
                var budget = GetBudget(deptId, projectId, kv.Key);
                budget?.UseBudget(kv.Value);
            }

            var overallBudget = GetBudget(deptId, projectId);
            overallBudget?.UseBudget(form.TotalAmount);
        }

        public BudgetSummary GetBudgetSummary(string departmentId, string projectId = "")
        {
            var summary = new BudgetSummary
            {
                DepartmentId = departmentId,
                ProjectId = projectId,
                Details = new List<BudgetDetailItem>()
            };

            int year = DateTime.Now.Year;

            foreach (ExpenseCategory category in Enum.GetValues(typeof(ExpenseCategory)))
            {
                var budget = GetBudget(departmentId, projectId, category, year);
                if (budget != null)
                {
                    summary.TotalBudget += budget.TotalBudget;
                    summary.UsedBudget += budget.UsedBudget;
                    summary.Details.Add(new BudgetDetailItem
                    {
                        Category = category,
                        CategoryName = GetCategoryName(category),
                        TotalBudget = budget.TotalBudget,
                        UsedBudget = budget.UsedBudget,
                        RemainingBudget = budget.RemainingBudget,
                        UsageRate = budget.UsageRate
                    });
                }
            }

            summary.RemainingBudget = summary.TotalBudget - summary.UsedBudget;
            summary.UsageRate = summary.TotalBudget > 0 ? summary.UsedBudget / summary.TotalBudget * 100 : 0;

            return summary;
        }

        private static string GetBudgetKey(Budget budget)
        {
            return GetKey(budget.DepartmentId, budget.ProjectId,
                budget.Category, budget.Year, budget.Month);
        }

        private static string GetKey(string deptId, string projectId,
            ExpenseCategory? category, int year, int? month)
        {
            var cat = category.HasValue ? ((int)category.Value).ToString() : "ALL";
            var mon = month.HasValue ? month.Value.ToString("D2") : "ALL";
            var proj = string.IsNullOrWhiteSpace(projectId) ? "ALL" : projectId;
            return $"{deptId}|{proj}|{cat}|{year}|{mon}";
        }

        private static string GetCategoryName(ExpenseCategory category)
        {
            switch (category)
            {
                case ExpenseCategory.Transportation: return "交通费";
                case ExpenseCategory.Accommodation: return "住宿费";
                case ExpenseCategory.Meal: return "餐饮费";
                case ExpenseCategory.Communication: return "通讯费";
                case ExpenseCategory.Office: return "办公费";
                case ExpenseCategory.Entertainment: return "招待费";
                case ExpenseCategory.Conference: return "会议费";
                case ExpenseCategory.Training: return "培训费";
                default: return "其他费用";
            }
        }
    }

    public class BudgetSummary
    {
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public decimal TotalBudget { get; set; }
        public decimal UsedBudget { get; set; }
        public decimal RemainingBudget { get; set; }
        public decimal UsageRate { get; set; }
        public List<BudgetDetailItem> Details { get; set; } = new List<BudgetDetailItem>();
    }

    public class BudgetDetailItem
    {
        public ExpenseCategory Category { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalBudget { get; set; }
        public decimal UsedBudget { get; set; }
        public decimal RemainingBudget { get; set; }
        public decimal UsageRate { get; set; }
    }

    public class BudgetCheckDetail
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalBudget { get; set; }
        public decimal UsedBudget { get; set; }
        public decimal AppliedAmount { get; set; }
        public decimal ExceedAmount { get; set; }
    }
}
