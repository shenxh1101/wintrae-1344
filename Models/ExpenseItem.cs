using System;
using System.Collections.Generic;

namespace FinanceReimbursement.Models
{
    public class ExpenseItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public ExpenseCategory Category { get; set; }

        public string Description { get; set; } = string.Empty;

        public DateTime ExpenseDate { get; set; }

        public decimal Amount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TotalAmount => Amount + TaxAmount;

        public string Currency { get; set; } = "CNY";

        public decimal? ExchangeRate { get; set; }

        public decimal? CNYAmount { get; set; }

        public TransportationType? TransportationType { get; set; }

        public string FromLocation { get; set; } = string.Empty;

        public string ToLocation { get; set; } = string.Empty;

        public string TripId { get; set; } = string.Empty;

        public string ProjectId { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        public string DepartmentId { get; set; } = string.Empty;

        public bool IsPersonal { get; set; }

        public string Remarks { get; set; } = string.Empty;

        public List<Invoice> Invoices { get; set; } = new List<Invoice>();

        public decimal InvoicesTotalAmount()
        {
            decimal total = 0;
            foreach (var inv in Invoices)
            {
                total += inv.TotalAmount;
            }
            return total;
        }

        public bool HasInvoice()
        {
            return Invoices.Count > 0;
        }

        public bool IsInvoiceSufficient()
        {
            if (IsPersonal) return true;
            if (!HasInvoice()) return false;
            return InvoicesTotalAmount() >= Amount * 0.95m;
        }
    }
}
