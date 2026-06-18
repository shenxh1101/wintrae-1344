using System;

namespace FinanceReimbursement.Models
{
    public class Budget
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string DepartmentId { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public string ProjectId { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        public ExpenseCategory? Category { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public decimal TotalBudget { get; set; }

        public decimal UsedBudget { get; set; }

        public decimal RemainingBudget => TotalBudget - UsedBudget;

        public decimal UsageRate => TotalBudget > 0 ? UsedBudget / TotalBudget * 100 : 0;

        public bool IsExceeded => UsedBudget > TotalBudget;

        public void UseBudget(decimal amount)
        {
            UsedBudget += amount;
        }

        public bool CanAfford(decimal amount)
        {
            return RemainingBudget >= amount;
        }
    }
}
