using System;
using System.Collections.Generic;
using System.Linq;
using FinanceReimbursement.Models;
using FinanceReimbursement.Rules;

namespace FinanceReimbursement.Services
{
    public class BatchProcessService
    {
        private readonly StandardValidationService _standardValidator;
        private readonly InvoiceValidationService _invoiceValidator;
        private readonly SubsidyCalculator _subsidyCalculator;
        private readonly EnhancedApprovalService _approvalService;

        public BatchProcessService(RuleScheme ruleScheme)
        {
            if (ruleScheme == null) ruleScheme = RuleScheme.CreateDefault();
            _standardValidator = new StandardValidationService(ruleScheme.Standard);
            _invoiceValidator = new InvoiceValidationService(
                invoiceRequiredThreshold: ruleScheme.Standard.InvoiceRequiredThreshold,
                requireInvoiceForOver: ruleScheme.Standard.RequireInvoiceForOver);
            _subsidyCalculator = new SubsidyCalculator(ruleScheme.Standard);
            _approvalService = new EnhancedApprovalService();
        }

        public BatchProcessResult ProcessBatch(IEnumerable<ReimbursementForm> forms,
            RuleScheme ruleScheme,
            IEnumerable<string>? usedInvoiceNos = null,
            BudgetOccupationSummary? budgetSummary = null,
            string projectType = "COMMERCIAL")
        {
            var result = new BatchProcessResult();
            if (forms == null) return result;

            var formList = forms.ToList();
            result.TotalCount = formList.Count;

            var globalInvoiceNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (usedInvoiceNos != null)
            {
                foreach (var no in usedInvoiceNos)
                {
                    if (!string.IsNullOrWhiteSpace(no)) globalInvoiceNos.Add(no);
                }
            }

            for (int i = 0; i < formList.Count; i++)
            {
                var form = formList[i];
                var itemResult = ProcessSingleForm(form, ruleScheme,
                    globalInvoiceNos, budgetSummary, projectType);

                result.Items.Add(itemResult);
                result.TotalAmount += form.TotalAmount;
                result.TotalSubsidy += form.SubsidyAmount;
                result.TotalDeduction += form.DeductionAmount;

                if (itemResult.IsValid) result.ValidCount++;
                else result.InvalidCount++;

                if (itemResult.HasWarnings) result.WarningCount++;

                if (itemResult.Validation != null)
                {
                    result.TotalErrors += itemResult.Validation.ErrorCount;
                    result.TotalWarnings += itemResult.Validation.WarningCount;
                }

                foreach (var invNo in form.GetAllInvoiceNos())
                {
                    globalInvoiceNos.Add(invNo);
                }
            }

            result.DepartmentSummary = BuildDepartmentSummary(result.Items);
            result.CategorySummary = BuildCategorySummary(result.Items);
            result.LevelSummary = BuildLevelSummary(result.Items);

            return result;
        }

        private BatchItemResult ProcessSingleForm(ReimbursementForm form,
            RuleScheme ruleScheme,
            HashSet<string> globalInvoiceNos,
            BudgetOccupationSummary? budgetSummary,
            string projectType)
        {
            var item = new BatchItemResult
            {
                FormNo = form.FormNo,
                Title = form.Title,
                ApplicantName = form.Applicant?.Name ?? "",
                DepartmentName = form.Applicant?.DepartmentName ?? "",
                ApplicantLevel = form.Applicant?.Level ?? EmployeeLevel.Junior
            };

            try
            {
                item.Subsidy = _subsidyCalculator.CalculateTotalSubsidy(form);
                item.Deduction = _subsidyCalculator.CalculateMealDeduction(form);
                form.SubsidyAmount = item.Subsidy;
                form.DeductionAmount = item.Deduction;

                var localInvoiceValidator = new InvoiceValidationService(
                    globalInvoiceNos,
                    ruleScheme.Standard.InvoiceRequiredThreshold,
                    ruleScheme.Standard.RequireInvoiceForOver);

                item.Validation = new ValidationResult();
                item.Validation.Merge(_standardValidator.Validate(form));
                item.Validation.Merge(localInvoiceValidator.Validate(form));

                item.ApprovalRecommendation = _approvalService.GetRecommendation(
                    form, ruleScheme, item.Validation, budgetSummary, projectType);

                form.ApprovalNodes = item.ApprovalRecommendation.Nodes;

                item.IsValid = item.Validation.IsValid;
                item.HasWarnings = item.Validation.HasWarnings;

                item.TotalAmount = form.TotalAmount;
                item.ChineseAmount = AmountConverter.ToChineseAmount(form.TotalAmount);

                var exportService = new ExportService();
                item.PrintData = exportService.GeneratePrintData(form, item.Validation);
            }
            catch (Exception ex)
            {
                item.IsValid = false;
                item.ErrorMessage = ex.Message;
            }

            return item;
        }

        private Dictionary<string, BatchDepartmentSummary> BuildDepartmentSummary(
            List<BatchItemResult> items)
        {
            var dict = new Dictionary<string, BatchDepartmentSummary>();

            foreach (var item in items)
            {
                var dept = item.DepartmentName;
                if (!dict.ContainsKey(dept))
                {
                    dict[dept] = new BatchDepartmentSummary { DepartmentName = dept };
                }
                var s = dict[dept];
                s.FormCount++;
                s.TotalAmount += item.TotalAmount;
                s.TotalSubsidy += item.Subsidy;
                if (!item.IsValid) s.InvalidCount++;
                if (item.HasWarnings) s.WarningCount++;
            }

            return dict;
        }

        private Dictionary<ExpenseCategory, decimal> BuildCategorySummary(
            List<BatchItemResult> items)
        {
            var dict = new Dictionary<ExpenseCategory, decimal>();
            return dict;
        }

        private Dictionary<EmployeeLevel, BatchLevelSummary> BuildLevelSummary(
            List<BatchItemResult> items)
        {
            var dict = new Dictionary<EmployeeLevel, BatchLevelSummary>();

            foreach (var item in items)
            {
                var level = item.ApplicantLevel;
                if (!dict.ContainsKey(level))
                {
                    dict[level] = new BatchLevelSummary { Level = level, LevelName = level.GetDescription() };
                }
                var s = dict[level];
                s.FormCount++;
                s.TotalAmount += item.TotalAmount;
            }

            return dict;
        }
    }

    public class BatchProcessResult
    {
        public int TotalCount { get; set; }
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }
        public int WarningCount { get; set; }
        public int TotalErrors { get; set; }
        public int TotalWarnings { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalSubsidy { get; set; }
        public decimal TotalDeduction { get; set; }
        public string TotalAmountChinese => AmountConverter.ToChineseAmount(TotalAmount);

        public List<BatchItemResult> Items { get; set; } = new List<BatchItemResult>();
        public Dictionary<string, BatchDepartmentSummary> DepartmentSummary { get; set; }
            = new Dictionary<string, BatchDepartmentSummary>();
        public Dictionary<ExpenseCategory, decimal> CategorySummary { get; set; }
            = new Dictionary<ExpenseCategory, decimal>();
        public Dictionary<EmployeeLevel, BatchLevelSummary> LevelSummary { get; set; }
            = new Dictionary<EmployeeLevel, BatchLevelSummary>();

        public string GetSummaryText()
        {
            return $"共{TotalCount}张报销单，" +
                   $"通过{ValidCount}张，不通过{InvalidCount}张，" +
                   $"有警告{WarningCount}张；" +
                   $"总金额¥{TotalAmount:N2}（{TotalAmountChinese}），" +
                   $"补贴¥{TotalSubsidy:N2}，扣减¥{TotalDeduction:N2}；" +
                   $"错误{TotalErrors}个，警告{TotalWarnings}个";
        }

        public List<BatchItemResult> GetInvalidItems()
        {
            return Items.Where(i => !i.IsValid).ToList();
        }

        public List<BatchItemResult> GetWarningItems()
        {
            return Items.Where(i => i.IsValid && i.HasWarnings).ToList();
        }

        public List<BatchItemResult> GetCleanItems()
        {
            return Items.Where(i => i.IsValid && !i.HasWarnings).ToList();
        }
    }

    public class BatchItemResult
    {
        public string FormNo { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public EmployeeLevel ApplicantLevel { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Subsidy { get; set; }
        public decimal Deduction { get; set; }
        public string ChineseAmount { get; set; } = string.Empty;
        public bool IsValid { get; set; } = true;
        public bool HasWarnings { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public ValidationResult? Validation { get; set; }
        public ApprovalRecommendation? ApprovalRecommendation { get; set; }
        public PrintData? PrintData { get; set; }
    }

    public class BatchDepartmentSummary
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int FormCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalSubsidy { get; set; }
        public int InvalidCount { get; set; }
        public int WarningCount { get; set; }
    }

    public class BatchLevelSummary
    {
        public EmployeeLevel Level { get; set; }
        public string LevelName { get; set; } = string.Empty;
        public int FormCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
