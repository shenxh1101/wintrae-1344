using System;
using System.Collections.Generic;
using System.Linq;
using FinanceReimbursement.Models;

namespace FinanceReimbursement.Repositories
{
    public interface IBudgetRepository
    {
        Budget? GetBudget(string departmentId, string projectId = "",
            ExpenseCategory? category = null, int? year = null, int? month = null);
        List<Budget> GetBudgets(string departmentId);
        void SaveBudget(Budget budget);
        void SaveBudgets(IEnumerable<Budget> budgets);
        bool TryOccupyBudget(string departmentId, string projectId,
            ExpenseCategory? category, decimal amount, string formNo, out string message);
        void ReleaseBudget(string formNo);
        BudgetOccupationSummary GetOccupationSummary(string departmentId, string projectId = "");
    }

    public class BudgetOccupationSummary
    {
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public decimal TotalBudget { get; set; }
        public decimal TotalOccupied { get; set; }
        public decimal TotalAvailable => TotalBudget - TotalOccupied;
        public List<BudgetCategoryOccupation> CategoryDetails { get; set; } = new List<BudgetCategoryOccupation>();
        public List<BudgetOccupationRecord> RecentOccupations { get; set; } = new List<BudgetOccupationRecord>();
    }

    public class BudgetCategoryOccupation
    {
        public ExpenseCategory? Category { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public decimal Occupied { get; set; }
        public decimal Available => Budget - Occupied;
        public decimal UsageRate => Budget > 0 ? Occupied / Budget * 100 : 0;
    }

    public class BudgetOccupationRecord
    {
        public string FormNo { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime OccupyDate { get; set; }
        public string DepartmentId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public ExpenseCategory? Category { get; set; }
        public bool IsReleased { get; set; }
    }

    public class InMemoryBudgetRepository : IBudgetRepository
    {
        private readonly Dictionary<string, Budget> _budgets;
        private readonly List<BudgetOccupationRecord> _occupations;
        private readonly object _lock = new object();

        public InMemoryBudgetRepository()
        {
            _budgets = new Dictionary<string, Budget>();
            _occupations = new List<BudgetOccupationRecord>();
        }

        public InMemoryBudgetRepository(IEnumerable<Budget> initialBudgets) : this()
        {
            if (initialBudgets == null) return;
            foreach (var b in initialBudgets)
            {
                SaveBudget(b);
            }
        }

        public Budget? GetBudget(string departmentId, string projectId = "",
            ExpenseCategory? category = null, int? year = null, int? month = null)
        {
            var y = year ?? DateTime.Now.Year;
            var m = month ?? DateTime.Now.Month;

            var searchKeys = new List<string>
            {
                MakeKey(departmentId, projectId, category, y, m),
                MakeKey(departmentId, projectId, category, y, null),
                MakeKey(departmentId, projectId, null, y, m),
                MakeKey(departmentId, projectId, null, y, null),
                MakeKey(departmentId, "", category, y, m),
                MakeKey(departmentId, "", category, y, null),
                MakeKey(departmentId, "", null, y, m),
                MakeKey(departmentId, "", null, y, null),
            };

            lock (_lock)
            {
                foreach (var key in searchKeys)
                {
                    if (_budgets.TryGetValue(key, out var budget))
                    {
                        return budget;
                    }
                }
            }
            return null;
        }

        public List<Budget> GetBudgets(string departmentId)
        {
            lock (_lock)
            {
                return _budgets.Values
                    .Where(b => b.DepartmentId == departmentId)
                    .ToList();
            }
        }

        public void SaveBudget(Budget budget)
        {
            if (budget == null) return;
            lock (_lock)
            {
                var key = MakeKey(budget);
                _budgets[key] = budget;
            }
        }

        public void SaveBudgets(IEnumerable<Budget> budgets)
        {
            if (budgets == null) return;
            foreach (var b in budgets) SaveBudget(b);
        }

        public bool TryOccupyBudget(string departmentId, string projectId,
            ExpenseCategory? category, decimal amount, string formNo, out string message)
        {
            lock (_lock)
            {
                var budget = GetBudget(departmentId, projectId, category);
                if (budget == null)
                {
                    message = $"未找到预算配置（部门:{departmentId}, 项目:{projectId}, 类别:{category})";
                    return true;
                }

                var occupied = GetOccupiedAmount(departmentId, projectId, category);
                var available = budget.TotalBudget - occupied;

                if (amount > available)
                {
                    message = $"预算不足：总额{budget.TotalBudget:N2}，已占用{occupied:N2}，" +
                              $"可用{available:N2}，申请{amount:N2}，差额{amount - available:N2}";
                    return false;
                }

                _occupations.Add(new BudgetOccupationRecord
                {
                    FormNo = formNo,
                    Amount = amount,
                    OccupyDate = DateTime.Now,
                    DepartmentId = departmentId,
                    ProjectId = projectId,
                    Category = category,
                    IsReleased = false
                });

                message = $"预算占用成功：{amount:N2}元（可用余额{available - amount:N2}元）";
                return true;
            }
        }

        public void ReleaseBudget(string formNo)
        {
            if (string.IsNullOrWhiteSpace(formNo)) return;
            lock (_lock)
            {
                foreach (var occ in _occupations)
                {
                    if (occ.FormNo == formNo && !occ.IsReleased)
                    {
                        occ.IsReleased = true;
                    }
                }
            }
        }

        public BudgetOccupationSummary GetOccupationSummary(string departmentId, string projectId = "")
        {
            lock (_lock)
            {
                var summary = new BudgetOccupationSummary
                {
                    DepartmentId = departmentId,
                    ProjectId = projectId
                };

                var allBudgets = _budgets.Values
                    .Where(b => b.DepartmentId == departmentId);

                foreach (var budget in allBudgets)
                {
                    summary.TotalBudget += budget.TotalBudget;
                    summary.DepartmentName = budget.DepartmentName;
                }

                var activeOcc = _occupations.Where(o =>
                    o.DepartmentId == departmentId &&
                    !o.IsReleased);

                foreach (var occ in activeOcc)
                {
                    summary.TotalOccupied += occ.Amount;
                }

                foreach (ExpenseCategory cat in Enum.GetValues(typeof(ExpenseCategory)))
                {
                    var catBudget = GetBudget(departmentId, projectId, cat);
                    if (catBudget == null) continue;

                    var catOccupied = _occupations.Where(o =>
                        o.DepartmentId == departmentId &&
                        o.Category == cat &&
                        !o.IsReleased).Sum(o => o.Amount);

                    summary.CategoryDetails.Add(new BudgetCategoryOccupation
                    {
                        Category = cat,
                        CategoryName = cat.GetDescription(),
                        Budget = catBudget.TotalBudget,
                        Occupied = catOccupied
                    });
                }

                summary.RecentOccupations = activeOcc
                    .OrderByDescending(o => o.OccupyDate)
                    .Take(20)
                    .ToList();

                return summary;
            }
        }

        private decimal GetOccupiedAmount(string departmentId, string projectId,
            ExpenseCategory? category)
        {
            return _occupations.Where(o =>
                o.DepartmentId == departmentId &&
                (string.IsNullOrWhiteSpace(projectId) || o.ProjectId == projectId) &&
                (category == null || o.Category == category) &&
                !o.IsReleased).Sum(o => o.Amount);
        }

        private static string MakeKey(Budget budget)
        {
            return MakeKey(budget.DepartmentId, budget.ProjectId,
                budget.Category, budget.Year, budget.Month);
        }

        private static string MakeKey(string deptId, string projectId,
            ExpenseCategory? category, int year, int? month)
        {
            var cat = category.HasValue ? ((int)category.Value).ToString() : "ALL";
            var mon = month.HasValue ? month.Value.ToString("D2") : "ALL";
            var proj = string.IsNullOrWhiteSpace(projectId) ? "ALL" : projectId;
            return $"{deptId}|{proj}|{cat}|{year}|{mon}";
        }
    }
}
