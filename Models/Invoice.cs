using System;

namespace FinanceReimbursement.Models
{
    public class Invoice
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string InvoiceNo { get; set; } = string.Empty;

        public string InvoiceCode { get; set; } = string.Empty;

        public InvoiceType Type { get; set; }

        public DateTime? InvoiceDate { get; set; }

        public decimal Amount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TotalAmount => Amount + TaxAmount;

        public string SellerName { get; set; } = string.Empty;

        public string SellerTaxId { get; set; } = string.Empty;

        public string BuyerName { get; set; } = string.Empty;

        public string BuyerTaxId { get; set; } = string.Empty;

        public ExpenseCategory? Category { get; set; }

        public string ContentDescription { get; set; } = string.Empty;

        public bool IsVerified { get; set; }

        public DateTime? VerifyDate { get; set; }

        public string VerifyResult { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public string PdfUrl { get; set; } = string.Empty;

        public string ExpenseItemId { get; set; } = string.Empty;

        public bool IsElectronic()
        {
            return Type == InvoiceType.Electronic;
        }

        public bool HasValidTaxDeduction()
        {
            return Type == InvoiceType.VATSpecial && IsVerified && TaxAmount > 0;
        }
    }
}
