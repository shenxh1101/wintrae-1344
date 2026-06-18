using System;
using System.Collections.Generic;
using FinanceReimbursement.Models;
using FinanceReimbursement.Services;
using FinanceReimbursement.Utils;

namespace FinanceReimbursement
{
    public class ReimbursementFacade
    {
        private readonly StandardValidationService _standardValidator;
        private readonly InvoiceValidationService _invoiceValidator;
        private readonly SubsidyCalculator _subsidyCalculator;
        private readonly BudgetService _budgetService;
        private readonly ApprovalService _approvalService;
        private readonly AllocationService _allocationService;
        private readonly ExportService _exportService;
        private readonly ReimbursementStandard _standard;

        public ReimbursementFacade(
            ReimbursementStandard? standard = null,
            ApprovalRule? approvalRule = null,
            IEnumerable<string>? usedInvoiceNos = null,
            IEnumerable<Budget>? budgets = null)
        {
            _standard = standard ?? ReimbursementStandard.CreateDefault();

            _standardValidator = new StandardValidationService(_standard);
            _invoiceValidator = new InvoiceValidationService(usedInvoiceNos,
                _standard.InvoiceRequiredThreshold, _standard.RequireInvoiceForOver);
            _subsidyCalculator = new SubsidyCalculator(_standard);
            _budgetService = budgets != null ? new BudgetService(budgets) : new BudgetService();
            _approvalService = new ApprovalService(approvalRule);
            _allocationService = new AllocationService();
            _exportService = new ExportService();
        }

        public ReimbursementForm CreateForm(Employee applicant, string title = "",
            string reimbursementType = "差旅费", string projectId = "", string projectName = "")
        {
            var form = new ReimbursementForm
            {
                FormNo = GenerateFormNo(),
                Title = title,
                Applicant = applicant ?? new Employee(),
                ReimbursementType = reimbursementType,
                ProjectId = projectId,
                ProjectName = projectName,
                Status = ReimbursementStatus.Draft,
                CreateDate = DateTime.Now
            };
            return form;
        }

        public Trip AddTrip(ReimbursementForm form, string destination, string destinationCity,
            DateTime departureDate, DateTime returnDate,
            CityLevel? cityLevel = null,
            TransportationType? transportTo = null,
            TransportationType? transportBack = null,
            string purpose = "", string projectId = "", string remarks = "")
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var trip = new Trip
            {
                TripNo = $"T{DateTime.Now:yyyyMMddHHmmssfff}",
                Destination = destination,
                DestinationCity = destinationCity,
                CityLevel = cityLevel,
                DepartureDate = departureDate,
                ReturnDate = returnDate,
                TransportationTo = transportTo,
                TransportationBack = transportBack,
                Purpose = purpose,
                ProjectId = projectId,
                Remarks = remarks
            };

            form.Trips.Add(trip);
            return trip;
        }

        public ExpenseItem AddExpense(ReimbursementForm form, ExpenseCategory category,
            decimal amount, decimal taxAmount = 0, string description = "",
            DateTime? expenseDate = null, string tripId = "",
            TransportationType? transportType = null,
            string fromLocation = "", string toLocation = "",
            string projectId = "", string projectName = "",
            string departmentId = "", bool isPersonal = false,
            string remarks = "")
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            var item = new ExpenseItem
            {
                Category = category,
                Description = description,
                ExpenseDate = expenseDate ?? DateTime.Now,
                Amount = amount,
                TaxAmount = taxAmount,
                Currency = "CNY",
                TripId = tripId,
                TransportationType = transportType,
                FromLocation = fromLocation,
                ToLocation = toLocation,
                ProjectId = projectId,
                ProjectName = projectName,
                DepartmentId = string.IsNullOrWhiteSpace(departmentId) ? form.DepartmentId : departmentId,
                IsPersonal = isPersonal,
                Remarks = remarks
            };

            form.ExpenseItems.Add(item);
            return item;
        }

        public Invoice AddInvoice(ExpenseItem item, string invoiceNo, InvoiceType type,
            decimal amount, decimal taxAmount = 0, string invoiceCode = "",
            DateTime? invoiceDate = null, string sellerName = "",
            string sellerTaxId = "", string buyerName = "",
            string buyerTaxId = "", ExpenseCategory? category = null,
            string contentDesc = "", bool isVerified = false,
            string verifyResult = "", string imageUrl = "", string pdfUrl = "")
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var invoice = new Invoice
            {
                InvoiceNo = invoiceNo,
                InvoiceCode = invoiceCode,
                Type = type,
                InvoiceDate = invoiceDate,
                Amount = amount,
                TaxAmount = taxAmount,
                SellerName = sellerName,
                SellerTaxId = sellerTaxId,
                BuyerName = buyerName,
                BuyerTaxId = buyerTaxId,
                Category = category,
                ContentDescription = contentDesc,
                IsVerified = isVerified,
                VerifyResult = verifyResult,
                ImageUrl = imageUrl,
                PdfUrl = pdfUrl,
                ExpenseItemId = item.Id
            };

            item.Invoices.Add(invoice);
            return invoice;
        }

        public decimal CalculateSubsidy(ReimbursementForm form, bool autoApply = true)
        {
            var subsidy = _subsidyCalculator.CalculateTotalSubsidy(form);
            var deduction = _subsidyCalculator.CalculateMealDeduction(form);

            if (autoApply)
            {
                form.SubsidyAmount = subsidy;
                form.DeductionAmount = deduction;
            }

            return subsidy - deduction;
        }

        public SubsidyBreakdown GetSubsidyBreakdown(ReimbursementForm form)
        {
            return _subsidyCalculator.GetSubsidyBreakdown(form);
        }

        public ValidationResult ValidateAll(ReimbursementForm form)
        {
            var result = new ValidationResult();

            result.Merge(_standardValidator.Validate(form));
            result.Merge(_invoiceValidator.Validate(form));
            result.Merge(_budgetService.CheckBudget(form));

            return result;
        }

        public ValidationResult ValidateStandard(ReimbursementForm form)
        {
            return _standardValidator.Validate(form);
        }

        public ValidationResult ValidateInvoices(ReimbursementForm form)
        {
            return _invoiceValidator.Validate(form);
        }

        public ValidationResult CheckBudget(ReimbursementForm form)
        {
            return _budgetService.CheckBudget(form);
        }

        public void AddBudget(Budget budget)
        {
            _budgetService.AddBudget(budget);
        }

        public void UseBudget(ReimbursementForm form)
        {
            _budgetService.UseBudget(form);
        }

        public BudgetSummary GetBudgetSummary(string departmentId, string projectId = "")
        {
            return _budgetService.GetBudgetSummary(departmentId, projectId);
        }

        public List<ApprovalNode> GetApprovalNodes(ReimbursementForm form)
        {
            var nodes = _approvalService.GetApprovalNodes(form);
            form.ApprovalNodes = nodes;
            return nodes;
        }

        public string GenerateApprovalSummary(ReimbursementForm form, ValidationResult? validation = null)
        {
            if (validation == null)
            {
                validation = ValidateAll(form);
            }
            return _approvalService.GetApprovalSummary(form, validation);
        }

        public string GetApprovalCommentTemplate(ReimbursementForm form, string approverRole = "")
        {
            return _approvalService.GenerateApprovalCommentTemplate(form, approverRole);
        }

        public Dictionary<string, decimal> AllocateByDepartment(ReimbursementForm form,
            Dictionary<string, decimal>? ratios = null)
        {
            return _allocationService.AllocateByDepartment(form, ratios);
        }

        public Dictionary<string, decimal> AllocateByProject(ReimbursementForm form,
            Dictionary<string, decimal>? ratios = null)
        {
            return _allocationService.AllocateByProject(form, ratios);
        }

        public List<DepartmentSummaryItem> SummarizeByDepartment(IEnumerable<ReimbursementForm> forms)
        {
            return _allocationService.SummarizeByDepartment(forms);
        }

        public List<ProjectSummaryItem> SummarizeByProject(IEnumerable<ReimbursementForm> forms)
        {
            return _allocationService.SummarizeByProject(forms);
        }

        public List<EmployeeSummaryItem> SummarizeByEmployee(IEnumerable<ReimbursementForm> forms,
            DateTime? startDate = null, DateTime? endDate = null)
        {
            return _allocationService.SummarizeByEmployee(forms, startDate, endDate);
        }

        public string AmountToChinese(decimal amount)
        {
            return AmountConverter.ToChineseAmount(amount);
        }

        public string AmountToEnglish(decimal amount)
        {
            return AmountConverter.ToEnglishAmount(amount);
        }

        public string FormatAmount(decimal amount, string currency = "CNY")
        {
            return AmountConverter.FormatAmount(amount, currency);
        }

        public PrintData GeneratePrintData(ReimbursementForm form, ValidationResult? validation = null)
        {
            if (validation == null)
            {
                validation = ValidateAll(form);
            }
            if (form.ApprovalNodes == null || form.ApprovalNodes.Count == 0)
            {
                form.ApprovalNodes = GetApprovalNodes(form);
            }
            return _exportService.GeneratePrintData(form, validation);
        }

        public string GenerateCsv(ReimbursementForm form)
        {
            return _exportService.GenerateCsv(form);
        }

        public string GenerateHtmlReport(ReimbursementForm form, ValidationResult? validation = null)
        {
            if (validation == null)
            {
                validation = ValidateAll(form);
            }
            if (form.ApprovalNodes == null || form.ApprovalNodes.Count == 0)
            {
                form.ApprovalNodes = GetApprovalNodes(form);
            }
            return _exportService.GenerateHtmlReport(form, validation);
        }

        public ReimbursementStandard GetCurrentStandard()
        {
            return _standard;
        }

        public void RegisterUsedInvoices(IEnumerable<string> invoiceNos)
        {
            _invoiceValidator.AddUsedInvoices(invoiceNos);
        }

        public ProcessResult ProcessFull(ReimbursementForm form, bool autoApplySubsidy = true)
        {
            var result = new ProcessResult();

            if (autoApplySubsidy)
            {
                result.NetSubsidy = CalculateSubsidy(form, true);
                result.SubsidyBreakdown = GetSubsidyBreakdown(form);
            }

            result.Validation = ValidateAll(form);

            result.ApprovalNodes = GetApprovalNodes(form);

            result.ApprovalSummary = GenerateApprovalSummary(form, result.Validation);

            result.PrintData = GeneratePrintData(form, result.Validation);

            result.ChineseAmount = AmountToChinese(form.TotalAmount);

            result.DepartmentAllocation = AllocateByDepartment(form);
            result.ProjectAllocation = AllocateByProject(form);

            return result;
        }

        private static string GenerateFormNo()
        {
            return $"BX{DateTime.Now:yyyyMMddHHmmssfff}";
        }
    }

    public class ProcessResult
    {
        public decimal NetSubsidy { get; set; }
        public SubsidyBreakdown? SubsidyBreakdown { get; set; }
        public ValidationResult? Validation { get; set; }
        public List<ApprovalNode>? ApprovalNodes { get; set; }
        public string ApprovalSummary { get; set; } = string.Empty;
        public PrintData? PrintData { get; set; }
        public string ChineseAmount { get; set; } = string.Empty;
        public Dictionary<string, decimal> DepartmentAllocation { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> ProjectAllocation { get; set; } = new Dictionary<string, decimal>();
    }
}
