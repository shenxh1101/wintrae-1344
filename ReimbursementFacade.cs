using System;
using System.Collections.Generic;
using FinanceReimbursement.Models;
using FinanceReimbursement.Repositories;
using FinanceReimbursement.Rules;
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
        private readonly EnhancedApprovalService _enhancedApprovalService;
        private readonly AllocationService _allocationService;
        private readonly ExportService _exportService;
        private readonly ReimbursementStandard _standard;
        private readonly IRuleProvider? _ruleProvider;
        private readonly IInvoiceRepository? _invoiceRepository;
        private readonly IBudgetRepository? _budgetRepository;

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
            _enhancedApprovalService = new EnhancedApprovalService();
            _allocationService = new AllocationService();
            _exportService = new ExportService();
        }

        public ReimbursementFacade(
            IRuleProvider ruleProvider,
            IInvoiceRepository? invoiceRepository = null,
            IBudgetRepository? budgetRepository = null)
        {
            _ruleProvider = ruleProvider ?? new RuleSchemeManager();
            var defaultScheme = _ruleProvider.GetRuleScheme(
                _ruleProvider.GetActiveSchemeId()) ?? RuleScheme.CreateDefault();
            _standard = defaultScheme.Standard;

            _standardValidator = new StandardValidationService(_standard);
            _invoiceValidator = new InvoiceValidationService(
                invoiceRepository?.GetUsedInvoiceNos(),
                _standard.InvoiceRequiredThreshold, _standard.RequireInvoiceForOver);
            _subsidyCalculator = new SubsidyCalculator(_standard);
            _budgetService = new BudgetService();
            _approvalService = new ApprovalService(defaultScheme.ApprovalRule);
            _enhancedApprovalService = new EnhancedApprovalService();
            _allocationService = new AllocationService();
            _exportService = new ExportService();
            _invoiceRepository = invoiceRepository;
            _budgetRepository = budgetRepository;
        }

        #region === 创建与编辑 ===

        public ReimbursementForm CreateForm(Employee applicant, string title = "",
            string reimbursementType = "差旅费", string projectId = "", string projectName = "")
        {
            return new ReimbursementForm
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
            string departmentId = "", bool isPersonal = false, string remarks = "")
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

        #endregion

        #region === 补贴计算 ===

        public decimal CalculateSubsidy(ReimbursementForm form, bool autoApply = true)
        {
            var subsidy = _subsidyCalculator.CalculateTotalSubsidy(form);
            var deduction = _subsidyCalculator.CalculateMealDeduction(form);
            if (autoApply) { form.SubsidyAmount = subsidy; form.DeductionAmount = deduction; }
            return Math.Round(subsidy - deduction, 2);
        }

        public SubsidyBreakdown GetSubsidyBreakdown(ReimbursementForm form)
        {
            return _subsidyCalculator.GetSubsidyBreakdown(form);
        }

        #endregion

        #region === 规则校验 ===

        public ValidationResult ValidateAll(ReimbursementForm form)
        {
            var result = new ValidationResult();
            result.Merge(_standardValidator.Validate(form));
            result.Merge(_invoiceValidator.Validate(form));
            result.Merge(_budgetService.CheckBudget(form));
            return result;
        }

        public ValidationResult ValidateStandard(ReimbursementForm form) => _standardValidator.Validate(form);
        public ValidationResult ValidateInvoices(ReimbursementForm form) => _invoiceValidator.Validate(form);
        public ValidationResult CheckBudget(ReimbursementForm form) => _budgetService.CheckBudget(form);

        #endregion

        #region === 预算管理 ===

        public void AddBudget(Budget budget) => _budgetService.AddBudget(budget);
        public void UseBudget(ReimbursementForm form) => _budgetService.UseBudget(form);
        public BudgetSummary GetBudgetSummary(string departmentId, string projectId = "")
            => _budgetService.GetBudgetSummary(departmentId, projectId);

        #endregion

        #region === 审批（原版兼容） ===

        public List<ApprovalNode> GetApprovalNodes(ReimbursementForm form)
        {
            var nodes = _approvalService.GetApprovalNodes(form);
            form.ApprovalNodes = nodes;
            return nodes;
        }

        public string GenerateApprovalSummary(ReimbursementForm form, ValidationResult? validation = null)
        {
            validation ??= ValidateAll(form);
            return _approvalService.GetApprovalSummary(form, validation);
        }

        public string GetApprovalCommentTemplate(ReimbursementForm form, string approverRole = "")
            => _approvalService.GenerateApprovalCommentTemplate(form, approverRole);

        #endregion

        #region === 增强审批（多因子推荐 + 原因解释） ===

        public ApprovalRecommendation GetEnhancedApproval(ReimbursementForm form,
            RuleScheme ruleScheme, ValidationResult? validation = null,
            BudgetOccupationSummary? budgetSummary = null, string projectType = "COMMERCIAL")
        {
            validation ??= ValidateAll(form);
            var recommendation = _enhancedApprovalService.GetRecommendation(
                form, ruleScheme, validation, budgetSummary, projectType);
            form.ApprovalNodes = recommendation.Nodes;
            return recommendation;
        }

        public string GenerateEnhancedSummary(ReimbursementForm form,
            ApprovalRecommendation recommendation, ValidationResult? validation = null)
        {
            return _enhancedApprovalService.GenerateEnhancedSummary(form, recommendation, validation);
        }

        #endregion

        #region === 分摊与汇总 ===

        public Dictionary<string, decimal> AllocateByDepartment(ReimbursementForm form,
            Dictionary<string, decimal>? ratios = null)
            => _allocationService.AllocateByDepartment(form, ratios);

        public Dictionary<string, decimal> AllocateByProject(ReimbursementForm form,
            Dictionary<string, decimal>? ratios = null)
            => _allocationService.AllocateByProject(form, ratios);

        public List<DepartmentSummaryItem> SummarizeByDepartment(IEnumerable<ReimbursementForm> forms)
            => _allocationService.SummarizeByDepartment(forms);

        public List<ProjectSummaryItem> SummarizeByProject(IEnumerable<ReimbursementForm> forms)
            => _allocationService.SummarizeByProject(forms);

        public List<EmployeeSummaryItem> SummarizeByEmployee(IEnumerable<ReimbursementForm> forms,
            DateTime? startDate = null, DateTime? endDate = null)
            => _allocationService.SummarizeByEmployee(forms, startDate, endDate);

        #endregion

        #region === 金额与导出 ===

        public string AmountToChinese(decimal amount) => AmountConverter.ToChineseAmount(amount);
        public string AmountToEnglish(decimal amount) => AmountConverter.ToEnglishAmount(amount);
        public string FormatAmount(decimal amount, string currency = "CNY") => AmountConverter.FormatAmount(amount, currency);

        public PrintData GeneratePrintData(ReimbursementForm form, ValidationResult? validation = null)
        {
            validation ??= ValidateAll(form);
            if (form.ApprovalNodes == null || form.ApprovalNodes.Count == 0)
                form.ApprovalNodes = GetApprovalNodes(form);
            return _exportService.GeneratePrintData(form, validation);
        }

        public FormPrintViewModel GeneratePrintViewModel(ReimbursementForm form,
            ValidationResult? validation = null,
            ApprovalRecommendation? recommendation = null)
        {
            validation ??= ValidateAll(form);
            return FormPrintViewModelBuilder.Build(form, validation, recommendation);
        }

        public string GenerateCsv(ReimbursementForm form) => _exportService.GenerateCsv(form);

        public string GenerateHtmlReport(ReimbursementForm form, ValidationResult? validation = null)
        {
            validation ??= ValidateAll(form);
            if (form.ApprovalNodes == null || form.ApprovalNodes.Count == 0)
                form.ApprovalNodes = GetApprovalNodes(form);
            return _exportService.GenerateHtmlReport(form, validation);
        }

        #endregion

        #region === 仓储接口 ===

        public IInvoiceRepository? InvoiceRepository => _invoiceRepository;
        public IBudgetRepository? BudgetRepository => _budgetRepository;

        public void CommitInvoices(ReimbursementForm form)
        {
            if (_invoiceRepository == null || form == null) return;
            var nos = form.GetAllInvoiceNos();
            _invoiceRepository.MarkInvoicesAsUsed(nos, form.FormNo, form.DepartmentId);
        }

        public bool TryOccupyBudget(ReimbursementForm form, out string message)
        {
            message = "";
            if (_budgetRepository == null || form == null) return true;
            return _budgetRepository.TryOccupyBudget(
                form.DepartmentId, form.ProjectId, null,
                form.TotalAmount, form.FormNo, out message);
        }

        public void ReleaseBudget(string formNo)
        {
            _budgetRepository?.ReleaseBudget(formNo);
        }

        public BudgetOccupationSummary GetBudgetOccupation(string departmentId, string projectId = "")
        {
            if (_budgetRepository != null)
                return _budgetRepository.GetOccupationSummary(departmentId, projectId);
            return new BudgetOccupationSummary { DepartmentId = departmentId };
        }

        #endregion

        #region === 规则方案 ===

        public IRuleProvider? RuleProvider => _ruleProvider;

        public RuleScheme ResolveRule(Employee employee, string cityCode = "")
        {
            if (_ruleProvider != null)
                return _ruleProvider.ResolveRule(employee, cityCode);
            return RuleScheme.CreateDefault();
        }

        public void SwitchRule(string departmentId, string schemeId)
        {
            _ruleProvider?.SetActiveScheme(departmentId, schemeId);
        }

        public List<RuleScheme> GetAllRuleSchemes() => _ruleProvider?.GetAllSchemes() ?? new List<RuleScheme>();

        public void AddRuleScheme(RuleScheme scheme) => _ruleProvider?.AddScheme(scheme);

        #endregion

        #region === 批量处理 ===

        public BatchProcessResult ProcessBatch(IEnumerable<ReimbursementForm> forms,
            RuleScheme ruleScheme,
            IEnumerable<string>? usedInvoiceNos = null,
            BudgetOccupationSummary? budgetSummary = null,
            string projectType = "COMMERCIAL")
        {
            var batchService = new BatchProcessService(ruleScheme);
            return batchService.ProcessBatch(forms, ruleScheme, usedInvoiceNos, budgetSummary, projectType);
        }

        #endregion

        #region === 其他 ===

        public ReimbursementStandard GetCurrentStandard() => _standard;

        public void RegisterUsedInvoices(IEnumerable<string> invoiceNos)
            => _invoiceValidator.AddUsedInvoices(invoiceNos);

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

        public EnhancedProcessResult ProcessFullEnhanced(ReimbursementForm form,
            RuleScheme ruleScheme, string projectType = "COMMERCIAL",
            bool autoApplySubsidy = true)
        {
            var result = new EnhancedProcessResult();
            if (autoApplySubsidy)
            {
                result.NetSubsidy = CalculateSubsidy(form, true);
                result.SubsidyBreakdown = GetSubsidyBreakdown(form);
            }
            result.Validation = ValidateAll(form);

            BudgetOccupationSummary? budgetSummary = null;
            if (_budgetRepository != null)
                budgetSummary = _budgetRepository.GetOccupationSummary(form.DepartmentId, form.ProjectId);

            result.ApprovalRecommendation = GetEnhancedApproval(
                form, ruleScheme, result.Validation, budgetSummary, projectType);
            result.ApprovalSummary = GenerateEnhancedSummary(form, result.ApprovalRecommendation, result.Validation);
            result.PrintData = GeneratePrintData(form, result.Validation);
            result.PrintViewModel = GeneratePrintViewModel(form, result.Validation, result.ApprovalRecommendation);
            result.ChineseAmount = AmountToChinese(form.TotalAmount);
            result.DepartmentAllocation = AllocateByDepartment(form);
            result.ProjectAllocation = AllocateByProject(form);
            return result;
        }

        private static string GenerateFormNo() => $"BX{DateTime.Now:yyyyMMddHHmmssfff}";
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

    public class EnhancedProcessResult : ProcessResult
    {
        public ApprovalRecommendation? ApprovalRecommendation { get; set; }
        public FormPrintViewModel? PrintViewModel { get; set; }
    }
}
