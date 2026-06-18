using System;
using System.Collections.Generic;
using FinanceReimbursement.Models;

namespace FinanceReimbursement.Services
{
    public class InvoiceValidationService
    {
        private readonly HashSet<string> _usedInvoiceNos;
        private readonly decimal _invoiceRequiredThreshold;
        private readonly bool _requireInvoiceForOver;

        public InvoiceValidationService(
            IEnumerable<string>? existingUsedInvoiceNos = null,
            decimal invoiceRequiredThreshold = 50m,
            bool requireInvoiceForOver = true)
        {
            _usedInvoiceNos = new HashSet<string>(existingUsedInvoiceNos ?? new List<string>(),
                StringComparer.OrdinalIgnoreCase);
            _invoiceRequiredThreshold = invoiceRequiredThreshold;
            _requireInvoiceForOver = requireInvoiceForOver;
        }

        public ValidationResult Validate(ReimbursementForm form)
        {
            var result = new ValidationResult();

            if (form == null || form.ExpenseItems == null)
            {
                return result;
            }

            var currentFormInvoiceNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in form.ExpenseItems)
            {
                ValidateItemInvoice(item, result, currentFormInvoiceNos);
            }

            return result;
        }

        private void ValidateItemInvoice(ExpenseItem item, ValidationResult result,
            HashSet<string> currentFormInvoiceNos)
        {
            if (item.IsPersonal) return;

            bool needsInvoice = _requireInvoiceForOver && item.Amount >= _invoiceRequiredThreshold;

            if (needsInvoice && (item.Invoices == null || item.Invoices.Count == 0))
            {
                result.AddError("INVOICE_MISSING",
                    $"费用[{item.Description ?? GetCategoryName(item.Category)}]金额{item.Amount:N2}元超过{_invoiceRequiredThreshold:N2}元，必须提供发票",
                    "发票校验", item.Id, suggestion: "请上传对应发票");
                return;
            }

            if (!needsInvoice && (item.Invoices == null || item.Invoices.Count == 0))
            {
                result.AddInfo("INVOICE_OPTIONAL",
                    $"费用[{item.Description ?? GetCategoryName(item.Category)}]建议提供发票以便税务抵扣",
                    "发票校验", item.Id);
            }

            if (item.Invoices == null || item.Invoices.Count == 0) return;

            foreach (var invoice in item.Invoices)
            {
                ValidateSingleInvoice(invoice, item, result, currentFormInvoiceNos);
            }

            var invoiceTotal = item.InvoicesTotalAmount();
            if (invoiceTotal < item.Amount * 0.95m && !item.IsPersonal)
            {
                result.AddWarning("INVOICE_AMOUNT_INSUFFICIENT",
                    $"费用[{item.Description ?? GetCategoryName(item.Category)}]发票金额合计{invoiceTotal:N2}元低于报销金额{item.Amount:N2}元的95%，差额{item.Amount * 0.95m - invoiceTotal:N2}元",
                    "发票校验", item.Id, item.Amount * 0.95m, invoiceTotal,
                    "请补充发票或调整报销金额");
            }
        }

        private void ValidateSingleInvoice(Invoice invoice, ExpenseItem item, ValidationResult result,
            HashSet<string> currentFormInvoiceNos)
        {
            if (string.IsNullOrWhiteSpace(invoice.InvoiceNo))
            {
                result.AddWarning("INVOICE_NO_EMPTY",
                    $"费用[{item.Description ?? GetCategoryName(item.Category)}]存在发票号码为空的发票",
                    "发票校验", invoice.Id);
            }
            else
            {
                if (_usedInvoiceNos.Contains(invoice.InvoiceNo))
                {
                    result.AddError("INVOICE_DUPLICATE_GLOBAL",
                        $"发票号[{invoice.InvoiceNo}]已在其他报销单中使用，存在重复报销风险",
                        "发票校验", invoice.Id,
                        suggestion: "请核对发票信息，避免重复报销");
                }

                if (currentFormInvoiceNos.Contains(invoice.InvoiceNo))
                {
                    result.AddError("INVOICE_DUPLICATE_LOCAL",
                        $"发票号[{invoice.InvoiceNo}]在当前报销单中重复使用",
                        "发票校验", invoice.Id,
                        suggestion: "请移除重复的发票");
                }

                currentFormInvoiceNos.Add(invoice.InvoiceNo);
            }

            if (invoice.TotalAmount <= 0)
            {
                result.AddWarning("INVOICE_AMOUNT_INVALID",
                    $"发票号[{invoice.InvoiceNo}]金额必须大于0",
                    "发票校验", invoice.Id);
            }

            if (invoice.InvoiceDate.HasValue && invoice.InvoiceDate.Value.Year < DateTime.Now.Year - 1)
            {
                result.AddWarning("INVOICE_EXPIRED",
                    $"发票号[{invoice.InvoiceNo}]开票日期为{invoice.InvoiceDate.Value:yyyy-MM-dd}，可能已超过报销时效",
                    "发票校验", invoice.Id,
                    suggestion: "请确认公司报销时效规定");
            }

            if (!invoice.IsVerified && invoice.Type == InvoiceType.VATSpecial)
            {
                result.AddWarning("INVOICE_NOT_VERIFIED",
                    $"增值税专用发票[{invoice.InvoiceNo}]建议完成发票验真，以确保可抵扣",
                    "发票校验", invoice.Id,
                    suggestion: "请通过税务系统完成发票验真");
            }
        }

        public bool AddUsedInvoices(IEnumerable<string> invoiceNos)
        {
            bool added = false;
            foreach (var no in invoiceNos)
            {
                if (!string.IsNullOrWhiteSpace(no) && _usedInvoiceNos.Add(no))
                {
                    added = true;
                }
            }
            return added;
        }

        private string GetCategoryName(ExpenseCategory category)
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
}
