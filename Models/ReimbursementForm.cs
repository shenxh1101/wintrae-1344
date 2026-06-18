using System;
using System.Collections.Generic;
using System.Linq;

namespace FinanceReimbursement.Models
{
    public class ReimbursementForm
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string FormNo { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public Employee Applicant { get; set; } = new Employee();

        public ReimbursementStatus Status { get; set; } = ReimbursementStatus.Draft;

        public DateTime CreateDate { get; set; } = DateTime.Now;

        public DateTime? SubmitDate { get; set; }

        public string ReimbursementType { get; set; } = "差旅费";

        public List<Trip> Trips { get; set; } = new List<Trip>();

        public List<ExpenseItem> ExpenseItems { get; set; } = new List<ExpenseItem>();

        public string DepartmentId => Applicant?.DepartmentId ?? string.Empty;

        public string DepartmentName => Applicant?.DepartmentName ?? string.Empty;

        public decimal SubtotalAmount
        {
            get
            {
                decimal total = 0;
                foreach (var item in ExpenseItems)
                {
                    total += item.TotalAmount;
                }
                return total;
            }
        }

        public decimal SubsidyAmount { get; set; }

        public decimal DeductionAmount { get; set; }

        public decimal TotalAmount => SubtotalAmount + SubsidyAmount - DeductionAmount;

        public string Currency { get; set; } = "CNY";

        public string PaymentMethod { get; set; } = "银行转账";

        public string ProjectId { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;

        public List<ApprovalNode> ApprovalNodes { get; set; } = new List<ApprovalNode>();

        public Dictionary<string, decimal> DepartmentAllocation { get; set; } = new Dictionary<string, decimal>();

        public Dictionary<string, decimal> ProjectAllocation { get; set; } = new Dictionary<string, decimal>();

        public int GetTotalDays()
        {
            int days = 0;
            foreach (var trip in Trips)
            {
                days += trip.Days;
            }
            return days;
        }

        public decimal GetTotalByCategory(ExpenseCategory category)
        {
            decimal total = 0;
            foreach (var item in ExpenseItems)
            {
                if (item.Category == category)
                {
                    total += item.TotalAmount;
                }
            }
            return total;
        }

        public int GetInvoiceCount()
        {
            int count = 0;
            foreach (var item in ExpenseItems)
            {
                count += item.Invoices.Count;
            }
            return count;
        }

        public decimal GetTotalInvoiceAmount()
        {
            decimal total = 0;
            foreach (var item in ExpenseItems)
            {
                total += item.InvoicesTotalAmount();
            }
            return total;
        }

        public List<string> GetAllInvoiceNos()
        {
            var nos = new List<string>();
            foreach (var item in ExpenseItems)
            {
                foreach (var inv in item.Invoices)
                {
                    if (!string.IsNullOrWhiteSpace(inv.InvoiceNo))
                    {
                        nos.Add(inv.InvoiceNo);
                    }
                }
            }
            return nos;
        }

        public List<Invoice> GetAllInvoices()
        {
            var invoices = new List<Invoice>();
            foreach (var item in ExpenseItems)
            {
                invoices.AddRange(item.Invoices);
            }
            return invoices;
        }

        public Dictionary<ExpenseCategory, decimal> GetCategorySummary()
        {
            var summary = new Dictionary<ExpenseCategory, decimal>();
            foreach (var item in ExpenseItems)
            {
                if (!summary.ContainsKey(item.Category))
                {
                    summary[item.Category] = 0;
                }
                summary[item.Category] += item.TotalAmount;
            }
            return summary;
        }
    }
}
