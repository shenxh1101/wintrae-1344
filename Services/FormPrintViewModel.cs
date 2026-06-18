using System;
using System.Collections.Generic;
using FinanceReimbursement.Models;
using FinanceReimbursement.Utils;

namespace FinanceReimbursement.Services
{
    public class FormPrintViewModel
    {
        public string FormNo { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public PrintHeaderSection Header { get; set; } = new PrintHeaderSection();
        public List<PrintTripRow> Trips { get; set; } = new List<PrintTripRow>();
        public PrintExpenseTable ExpenseTable { get; set; } = new PrintExpenseTable();
        public PrintInvoiceTable InvoiceTable { get; set; } = new PrintInvoiceTable();
        public PrintAmountSection Amount { get; set; } = new PrintAmountSection();
        public PrintAllocationSection Allocation { get; set; } = new PrintAllocationSection();
        public PrintValidationSection Validation { get; set; } = new PrintValidationSection();
        public PrintApprovalSection Approval { get; set; } = new PrintApprovalSection();
        public PrintFooterSection Footer { get; set; } = new PrintFooterSection();
    }

    public class PrintHeaderSection
    {
        public string FormNo { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ReimbursementType { get; set; } = string.Empty;
        public string CreateDate { get; set; } = string.Empty;
        public string SubmitDate { get; set; } = string.Empty;

        public string ApplicantId { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
    }

    public class PrintTripRow
    {
        public int Sequence { get; set; }
        public string Destination { get; set; } = string.Empty;
        public string CityLevel { get; set; } = string.Empty;
        public string DepartureDate { get; set; } = string.Empty;
        public string ReturnDate { get; set; } = string.Empty;
        public int Days { get; set; }
        public string TransportationTo { get; set; } = string.Empty;
        public string TransportationBack { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
    }

    public class PrintExpenseTable
    {
        public List<PrintExpenseRow> Rows { get; set; } = new List<PrintExpenseRow>();
        public decimal SubtotalAmount { get; set; }
        public string SubtotalAmountText => $"¥{SubtotalAmount:N2}";
        public int TotalRows => Rows.Count;
        public List<PrintCategorySubtotal> CategorySubtotals { get; set; }
            = new List<PrintCategorySubtotal>();
    }

    public class PrintExpenseRow
    {
        public int Sequence { get; set; }
        public string Category { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ExpenseDate { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string TaxAmount { get; set; } = string.Empty;
        public string TotalAmount { get; set; } = string.Empty;
        public string Currency { get; set; } = "CNY";
        public string TransportationType { get; set; } = string.Empty;
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public string InvoiceTotal { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public bool IsOverStandard { get; set; }
        public string OverStandardNote { get; set; } = string.Empty;
    }

    public class PrintCategorySubtotal
    {
        public string Category { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public int Count { get; set; }
        public string TotalAmount { get; set; } = string.Empty;
    }

    public class PrintInvoiceTable
    {
        public List<PrintInvoiceRow> Rows { get; set; } = new List<PrintInvoiceRow>();
        public int TotalCount => Rows.Count;
        public string TotalAmount { get; set; } = string.Empty;
        public string TotalTaxAmount { get; set; } = string.Empty;
        public int VatDeductibleCount { get; set; }
        public string VatDeductibleAmount { get; set; } = string.Empty;
    }

    public class PrintInvoiceRow
    {
        public int ItemSequence { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public string InvoiceCode { get; set; } = string.Empty;
        public string InvoiceType { get; set; } = string.Empty;
        public string InvoiceTypeCode { get; set; } = string.Empty;
        public string InvoiceDate { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string TaxAmount { get; set; } = string.Empty;
        public string TotalAmount { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public string VerifyStatus { get; set; } = string.Empty;
        public bool IsVatDeductible { get; set; }
    }

    public class PrintAmountSection
    {
        public string SubtotalAmount { get; set; } = string.Empty;
        public string SubsidyAmount { get; set; } = string.Empty;
        public string DeductionAmount { get; set; } = string.Empty;
        public string TotalAmount { get; set; } = string.Empty;
        public string TotalAmountChinese { get; set; } = string.Empty;
        public string Currency { get; set; } = "CNY";
        public List<PrintSubsidyDetail> SubsidyDetails { get; set; }
            = new List<PrintSubsidyDetail>();
    }

    public class PrintSubsidyDetail
    {
        public string Destination { get; set; } = string.Empty;
        public string CityLevel { get; set; } = string.Empty;
        public int Days { get; set; }
        public string DailySubsidy { get; set; } = string.Empty;
        public string TotalSubsidy { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class PrintAllocationSection
    {
        public List<PrintAllocationItem> DepartmentAllocation { get; set; }
            = new List<PrintAllocationItem>();
        public List<PrintAllocationItem> ProjectAllocation { get; set; }
            = new List<PrintAllocationItem>();
    }

    public class PrintAllocationItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Ratio { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
    }

    public class PrintValidationSection
    {
        public bool IsValid { get; set; } = true;
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }
        public string SummaryText { get; set; } = "✅ 校验通过";
        public List<PrintValidationItem> Items { get; set; }
            = new List<PrintValidationItem>();
        public List<PrintValidationItem> Errors { get; set; }
            = new List<PrintValidationItem>();
        public List<PrintValidationItem> Warnings { get; set; }
            = new List<PrintValidationItem>();
    }

    public class PrintValidationItem
    {
        public string Severity { get; set; } = string.Empty;
        public string SeverityIcon { get; set; } = string.Empty;
        public string SeverityColor { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public string ActualValue { get; set; } = string.Empty;
    }

    public class PrintApprovalSection
    {
        public string RiskLevel { get; set; } = string.Empty;
        public string RiskLevelColor { get; set; } = string.Empty;
        public int TotalNodes { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<PrintApprovalNodeItem> Nodes { get; set; }
            = new List<PrintApprovalNodeItem>();
        public List<PrintApprovalReasonItem> Reasons { get; set; }
            = new List<PrintApprovalReasonItem>();
    }

    public class PrintApprovalNodeItem
    {
        public int Order { get; set; }
        public string NodeName { get; set; } = string.Empty;
        public string NodeType { get; set; } = string.Empty;
        public string RuleDescription { get; set; } = string.Empty;
        public string ApproverName { get; set; } = string.Empty;
        public string ApproverPosition { get; set; } = string.Empty;
        public string SignArea { get; set; } = string.Empty;
    }

    public class PrintApprovalReasonItem
    {
        public string Factor { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }

    public class PrintFooterSection
    {
        public string QRCodeContent { get; set; } = string.Empty;
        public string Watermark { get; set; } = string.Empty;
        public string PrintDate { get; set; } = string.Empty;
        public List<PrintSignArea> SignAreas { get; set; } = new List<PrintSignArea>();
    }

    public class PrintSignArea
    {
        public string Label { get; set; } = string.Empty;
        public string SignLine { get; set; } = "________________________";
    }

    public static class FormPrintViewModelBuilder
    {
        public static FormPrintViewModel Build(ReimbursementForm form,
            ValidationResult? validation = null,
            ApprovalRecommendation? recommendation = null)
        {
            var vm = new FormPrintViewModel();
            if (form == null) return vm;

            vm.FormNo = form.FormNo;
            vm.Title = form.Title;
            vm.Status = form.Status.GetDescription();

            var h = vm.Header;
            h.FormNo = form.FormNo;
            h.Title = form.Title;
            h.ReimbursementType = form.ReimbursementType;
            h.CreateDate = form.CreateDate.ToString("yyyy-MM-dd HH:mm");
            h.SubmitDate = form.SubmitDate?.ToString("yyyy-MM-dd HH:mm") ?? "-";
            if (form.Applicant != null)
            {
                h.ApplicantId = form.Applicant.Id;
                h.ApplicantName = form.Applicant.Name;
                h.DepartmentName = form.Applicant.DepartmentName;
                h.Position = form.Applicant.Position;
                h.Level = form.Applicant.Level.GetDescription();
                h.BankAccount = form.Applicant.BankAccount;
                h.BankName = form.Applicant.BankName;
                h.ContactPhone = form.Applicant.Phone;
            }
            h.ProjectName = form.ProjectName;

            if (form.Trips != null)
            {
                int tSeq = 1;
                foreach (var trip in form.Trips)
                {
                    vm.Trips.Add(new PrintTripRow
                    {
                        Sequence = tSeq++,
                        Destination = trip.Destination,
                        CityLevel = trip.CityLevel?.GetDescription() ?? "",
                        DepartureDate = trip.DepartureDate.ToString("yyyy-MM-dd"),
                        ReturnDate = trip.ReturnDate.ToString("yyyy-MM-dd"),
                        Days = trip.Days,
                        TransportationTo = trip.TransportationTo?.GetDescription() ?? "",
                        TransportationBack = trip.TransportationBack?.GetDescription() ?? "",
                        Purpose = trip.Purpose,
                        ProjectName = trip.ProjectName
                    });
                }
            }

            var catSubtotals = new Dictionary<ExpenseCategory, PrintCategorySubtotal>();
            int eSeq = 1;
            decimal invoiceTotalAmt = 0;
            decimal invoiceTotalTax = 0;
            int vatDeductibleCount = 0;
            decimal vatDeductibleAmount = 0;

            if (form.ExpenseItems != null)
            {
                foreach (var item in form.ExpenseItems)
                {
                    var row = new PrintExpenseRow
                    {
                        Sequence = eSeq,
                        Category = item.Category.GetDescription(),
                        CategoryCode = ((int)item.Category).ToString(),
                        Description = item.Description,
                        ExpenseDate = item.ExpenseDate.ToString("yyyy-MM-dd"),
                        Amount = $"¥{item.Amount:N2}",
                        TaxAmount = $"¥{item.TaxAmount:N2}",
                        TotalAmount = $"¥{item.TotalAmount:N2}",
                        TransportationType = item.TransportationType?.GetDescription() ?? "",
                        FromLocation = item.FromLocation,
                        ToLocation = item.ToLocation,
                        ProjectName = item.ProjectName,
                        InvoiceCount = item.Invoices?.Count ?? 0,
                        InvoiceTotal = $"¥{item.InvoicesTotalAmount():N2}",
                        Remarks = item.Remarks
                    };
                    vm.ExpenseTable.Rows.Add(row);

                    if (!catSubtotals.ContainsKey(item.Category))
                    {
                        catSubtotals[item.Category] = new PrintCategorySubtotal
                        {
                            Category = item.Category.GetDescription(),
                            CategoryCode = ((int)item.Category).ToString()
                        };
                    }
                    catSubtotals[item.Category].Count++;
                    catSubtotals[item.Category].TotalAmount = $"¥{(decimal.Parse(catSubtotals[item.Category].TotalAmount.TrimStart('¥').Replace(",", "")) + item.TotalAmount):N2}";

                    if (item.Invoices != null)
                    {
                        foreach (var inv in item.Invoices)
                        {
                            invoiceTotalAmt += inv.Amount;
                            invoiceTotalTax += inv.TaxAmount;

                            var invRow = new PrintInvoiceRow
                            {
                                ItemSequence = eSeq,
                                InvoiceNo = inv.InvoiceNo,
                                InvoiceCode = inv.InvoiceCode,
                                InvoiceType = inv.Type.GetDescription(),
                                InvoiceTypeCode = ((int)inv.Type).ToString(),
                                InvoiceDate = inv.InvoiceDate?.ToString("yyyy-MM-dd") ?? "",
                                Amount = $"¥{inv.Amount:N2}",
                                TaxAmount = $"¥{inv.TaxAmount:N2}",
                                TotalAmount = $"¥{inv.TotalAmount:N2}",
                                SellerName = inv.SellerName,
                                Category = inv.Category?.GetDescription() ?? "",
                                IsVerified = inv.IsVerified,
                                VerifyStatus = inv.IsVerified ? "已验真" : "未验真",
                                IsVatDeductible = inv.HasValidTaxDeduction()
                            };
                            vm.InvoiceTable.Rows.Add(invRow);

                            if (inv.HasValidTaxDeduction())
                            {
                                vatDeductibleCount++;
                                vatDeductibleAmount += inv.TaxAmount;
                            }
                        }
                    }

                    eSeq++;
                }
            }

            vm.ExpenseTable.SubtotalAmount = form.SubtotalAmount;
            vm.ExpenseTable.CategorySubtotals = new List<PrintCategorySubtotal>(catSubtotals.Values);
            vm.InvoiceTable.TotalAmount = $"¥{invoiceTotalAmt:N2}";
            vm.InvoiceTable.TotalTaxAmount = $"¥{invoiceTotalTax:N2}";
            vm.InvoiceTable.VatDeductibleCount = vatDeductibleCount;
            vm.InvoiceTable.VatDeductibleAmount = $"¥{vatDeductibleAmount:N2}";

            var amt = vm.Amount;
            amt.SubtotalAmount = $"¥{form.SubtotalAmount:N2}";
            amt.SubsidyAmount = $"+¥{form.SubsidyAmount:N2}";
            amt.DeductionAmount = $"-¥{form.DeductionAmount:N2}";
            amt.TotalAmount = $"¥{form.TotalAmount:N2}";
            amt.TotalAmountChinese = AmountConverter.ToChineseAmount(form.TotalAmount);

            if (form.ApprovalNodes != null)
            {
                foreach (var node in form.ApprovalNodes)
                {
                    vm.Approval.Nodes.Add(new PrintApprovalNodeItem
                    {
                        Order = node.Order,
                        NodeName = node.NodeName,
                        NodeType = node.NodeType.GetDescription(),
                        RuleDescription = node.RuleDescription,
                        ApproverName = node.ApproverName,
                        ApproverPosition = node.ApproverPosition
                    });
                }
            }

            if (validation != null)
            {
                var v = vm.Validation;
                v.IsValid = validation.IsValid;
                v.ErrorCount = validation.ErrorCount;
                v.WarningCount = validation.WarningCount;
                v.InfoCount = validation.InfoCount;
                v.SummaryText = validation.IsValid
                    ? $"✅ 校验通过" + (validation.HasWarnings ? $"（{validation.WarningCount}个警告）" : "")
                    : $"❌ 不通过（{validation.ErrorCount}个错误）";

                foreach (var msg in validation.Messages)
                {
                    var vi = new PrintValidationItem
                    {
                        Severity = msg.SeverityText,
                        SeverityIcon = msg.SeverityIcon,
                        SeverityColor = msg.Severity == ValidationSeverity.Error ? "#dc2626" :
                                        msg.Severity == ValidationSeverity.Warning ? "#d97706" : "#2563eb",
                        Code = msg.Code,
                        Category = msg.Category,
                        Message = msg.Message,
                        Suggestion = msg.Suggestion,
                        ExpectedValue = msg.ExpectedValue.HasValue ? $"¥{msg.ExpectedValue.Value:N2}" : "",
                        ActualValue = msg.ActualValue.HasValue ? $"¥{msg.ActualValue.Value:N2}" : ""
                    };
                    v.Items.Add(vi);
                    if (msg.Severity == ValidationSeverity.Error) v.Errors.Add(vi);
                    else if (msg.Severity == ValidationSeverity.Warning) v.Warnings.Add(vi);
                }
            }

            if (recommendation != null)
            {
                vm.Approval.RiskLevel = recommendation.RiskLevel;
                vm.Approval.RiskLevelColor = recommendation.RiskLevel.Contains("高") ? "#dc2626" :
                                              recommendation.RiskLevel.Contains("中") ? "#d97706" : "#16a34a";
                vm.Approval.TotalNodes = recommendation.TotalNodes;
                vm.Approval.Summary = recommendation.Summary;

                vm.Approval.Reasons.Clear();
                foreach (var reason in recommendation.Reasons)
                {
                    vm.Approval.Reasons.Add(new PrintApprovalReasonItem
                    {
                        Factor = reason.Factor,
                        Description = reason.Description,
                        Impact = reason.Impact
                    });
                }
            }

            if (form.DepartmentAllocation != null)
            {
                foreach (var kv in form.DepartmentAllocation)
                {
                    vm.Allocation.DepartmentAllocation.Add(new PrintAllocationItem
                    {
                        Id = kv.Key,
                        Amount = $"¥{kv.Value:N2}"
                    });
                }
            }

            if (form.ProjectAllocation != null)
            {
                foreach (var kv in form.ProjectAllocation)
                {
                    vm.Allocation.ProjectAllocation.Add(new PrintAllocationItem
                    {
                        Id = kv.Key,
                        Amount = $"¥{kv.Value:N2}"
                    });
                }
            }

            vm.Footer.QRCodeContent = $"REIMBURSEMENT|{form.FormNo}|{form.Applicant?.Id}|{form.TotalAmount:F2}|{form.CreateDate:yyyyMMdd}";
            vm.Footer.Watermark = $"{form.Applicant?.Name ?? ""} {DateTime.Now:yyyy-MM-dd} 报销单";
            vm.Footer.PrintDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            vm.Footer.SignAreas = new List<PrintSignArea>
            {
                new PrintSignArea { Label = "申请人签字" },
                new PrintSignArea { Label = "审核签字" },
                new PrintSignArea { Label = "财务付款" }
            };

            return vm;
        }
    }
}
