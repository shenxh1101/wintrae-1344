using System;
using System.Collections.Generic;
using FinanceReimbursement.Models;
using FinanceReimbursement.Services;
using FinanceReimbursement.Utils;

namespace FinanceReimbursement
{
    /// <summary>
    /// 财务报销处理门面类 - 业务系统统一入口。
    /// 提供创建报销单、添加行程/发票、计算补贴、规则校验、预算检查、
    /// 审批摘要、部门汇总、项目分摊、金额大小写转换、打印导出等能力。
    /// </summary>
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

        /// <summary>
        /// 初始化报销处理门面。
        /// 所有参数均为可选，不传则使用内置企业级默认配置。
        /// </summary>
        /// <param name="standard">报销标准（员工级别×城市级别的住宿/交通/餐饮标准）</param>
        /// <param name="approvalRule">审批金额阈值及节点规则</param>
        /// <param name="usedInvoiceNos">历史已报销发票号集合，用于重复发票校验</param>
        /// <param name="budgets">部门/项目/分类预算集合</param>
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

        #region === 创建与编辑报销单 ===

        /// <summary>
        /// 创建一张新的报销单草稿。
        /// </summary>
        /// <param name="applicant">申请人信息（员工ID、姓名、部门、级别等）</param>
        /// <param name="title">报销单标题，如"6月北京出差报销</param>
        /// <param name="reimbursementType">报销类型：差旅费/招待费/办公费等</param>
        /// <param name="projectId">关联项目编号</param>
        /// <param name="projectName">关联项目名称</param>
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

        /// <summary>
        /// 向报销单添加一条出差行程。
        /// </summary>
        /// <param name="form">目标报销单</param>
        /// <param name="destination">目的地，如"北京朝阳区"</param>
        /// <param name="destinationCity">目的地城市，如"北京市"</param>
        /// <param name="departureDate">出发日期</param>
        /// <param name="returnDate">返回日期</param>
        /// <param name="cityLevel">城市级别（一线/二线/三线/四线），影响住宿和补贴标准</param>
        /// <param name="transportTo">去程交通方式</param>
        /// <param name="transportBack">返程交通方式</param>
        /// <param name="purpose">出差事由</param>
        /// <param name="projectId">关联项目编号</param>
        /// <param name="remarks">备注</param>
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

        /// <summary>
        /// 向报销单添加一条费用明细。
        /// </summary>
        /// <param name="form">目标报销单</param>
        /// <param name="category">费用类别：交通/住宿/餐饮/招待等</param>
        /// <param name="amount">不含税金额</param>
        /// <param name="taxAmount">税额</param>
        /// <param name="description">费用描述</param>
        /// <param name="expenseDate">费用发生日期</param>
        /// <param name="tripId">关联行程ID（差旅费必填，其他可空</param>
        /// <param name="transportType">交通方式（交通费必填）</param>
        /// <param name="fromLocation">出发地</param>
        /// <param name="toLocation">到达地</param>
        /// <param name="projectId">项目编号（用于项目分摊）</param>
        /// <param name="projectName">项目名称</param>
        /// <param name="departmentId">部门编号（留空取报销人部门）</param>
        /// <param name="isPersonal">是否个人费用（个人费用无需发票）</param>
        /// <param name="remarks">备注</param>
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

        /// <summary>
        /// 向费用明细下添加一张发票。
        /// </summary>
        /// <param name="item">目标费用明细</param>
        /// <param name="invoiceNo">发票号码（查重主键）</param>
        /// <param name="type">发票类型：专票/普票/电子/定额等</param>
        /// <param name="amount">不含税金额</param>
        /// <param name="taxAmount">税额</param>
        /// <param name="invoiceCode">发票代码</param>
        /// <param name="invoiceDate">开票日期</param>
        /// <param name="sellerName">销售方名称</param>
        /// <param name="sellerTaxId">销售方税号</param>
        /// <param name="buyerName">购买方名称</param>
        /// <param name="buyerTaxId">购买方税号</param>
        /// <param name="category">发票对应的费用类别</param>
        /// <param name="contentDesc">发票内容摘要</param>
        /// <param name="isVerified">是否已完成验真（专票建议验真）</param>
        /// <param name="verifyResult">验真结果说明</param>
        /// <param name="imageUrl">发票影像URL</param>
        /// <param name="pdfUrl">PDF文件URL</param>
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

        /// <summary>
        /// 按员工级别×城市级别×出差天数计算出差补贴，
        /// 可选将结果自动写入报销单的 SubsidyAmount 和 DeductionAmount。
        /// </summary>
        /// <param name="form">报销单</param>
        /// <param name="form">autoApply 是否将计算结果写回报销单</param>
        /// <returns>净补贴（补贴减扣减）</returns>
        public decimal CalculateSubsidy(ReimbursementForm form, bool autoApply = true)
        {
            var subsidy = _subsidyCalculator.CalculateTotalSubsidy(form);
            var deduction = _subsidyCalculator.CalculateMealDeduction(form);

            if (autoApply)
            {
                form.SubsidyAmount = subsidy;
                form.DeductionAmount = deduction;
            }

            return Math.Round(subsidy - deduction, 2);
        }

        /// <summary>
        /// 获取补贴的逐行程明细（含每日标准、天数、金额，适合前端展示）。
        /// </summary>
        public SubsidyBreakdown GetSubsidyBreakdown(ReimbursementForm form)
        {
            return _subsidyCalculator.GetSubsidyBreakdown(form);
        }

        #endregion

        #region === 规则校验 ===

        /// <summary>
        /// 执行全部校验（费用标准+发票合规+预算检查）。
        /// 返回结果可直接展示给员工查看。
        /// </summary>
        public ValidationResult ValidateAll(ReimbursementForm form)
        {
            var result = new ValidationResult();

            result.Merge(_standardValidator.Validate(form));
            result.Merge(_invoiceValidator.Validate(form));
            result.Merge(_budgetService.CheckBudget(form));

            return result;
        }

        /// <summary>
        /// 仅校验费用标准（住宿超标、餐饮超标、交通超标、行程时长等）。
        /// </summary>
        public ValidationResult ValidateStandard(ReimbursementForm form)
        {
            return _standardValidator.Validate(form);
        }

        /// <summary>
        /// 仅校验发票（缺票、重复、金额不足、时效、验真等）。
        /// </summary>
        public ValidationResult ValidateInvoices(ReimbursementForm form)
        {
            return _invoiceValidator.Validate(form);
        }

        /// <summary>
        /// 仅检查预算（部门/项目/分类预算是否充足）。
        /// </summary>
        public ValidationResult CheckBudget(ReimbursementForm form)
        {
            return _budgetService.CheckBudget(form);
        }

        #endregion

        #region === 预算管理 ===

        /// <summary>
        /// 登记一张预算配置（部门或项目或分类）。
        /// </summary>
        public void AddBudget(Budget budget)
        {
            _budgetService.AddBudget(budget);
        }

        /// <summary>
        /// 报销通过后从预算中扣减金额。
        /// </summary>
        public void UseBudget(ReimbursementForm form)
        {
            _budgetService.UseBudget(form);
        }

        /// <summary>
        /// 获取部门/项目预算执行概况（含各分类明细）。
        /// </summary>
        public BudgetSummary GetBudgetSummary(string departmentId, string projectId = "")
        {
            return _budgetService.GetBudgetSummary(departmentId, projectId);
        }

        #endregion

        #region === 审批相关 ===

        /// <summary>
        /// 根据报销金额与申请人级别，推荐审批节点列表（自动写入报销单）。
        /// </summary>
        public List<ApprovalNode> GetApprovalNodes(ReimbursementForm form)
        {
            var nodes = _approvalService.GetApprovalNodes(form);
            form.ApprovalNodes = nodes;
            return nodes;
        }

        /// <summary>
        /// 生成完整的审批摘要文本，包含：
        /// 人员/金额/行程/费用分类/审批流程/校验结果，适合审批人查看。
        /// </summary>
        public string GenerateApprovalSummary(ReimbursementForm form, ValidationResult? validation = null)
        {
            validation ??= ValidateAll(form);
            return _approvalService.GetApprovalSummary(form, validation);
        }

        /// <summary>
        /// 生成审批意见模板（按审批节点角色）。
        /// </summary>
        public string GetApprovalCommentTemplate(ReimbursementForm form, string approverRole = "")
        {
            return _approvalService.GenerateApprovalCommentTemplate(form, approverRole);
        }

        #endregion

        #region === 分摊与汇总 ===

        /// <summary>
        /// 按部门分摊报销金额。
        /// ratios 传 null 则全部计入报销人部门；
        /// 传比例则按比例精确分摊，最后一项承担尾差。
        /// </summary>
        /// <param name="form">报销单</param>
        /// <param name="ratios">部门ID→比例（比例之和不必等于任意数值，内部按比例归一化</param>
        public Dictionary<string, decimal> AllocateByDepartment(ReimbursementForm form,
            Dictionary<string, decimal>? ratios = null)
        {
            return _allocationService.AllocateByDepartment(form, ratios);
        }

        /// <summary>
        /// 按项目分摊报销金额。
        /// ratios 传 null 则按费用明细上的 ProjectId 自动归集；
        /// 传比例则按比例精确分摊。
        /// </summary>
        public Dictionary<string, decimal> AllocateByProject(ReimbursementForm form,
            Dictionary<string, decimal>? ratios = null)
        {
            return _allocationService.AllocateByProject(form, ratios);
        }

        /// <summary>
        /// 按部门维度汇总多张报销单（含各分类金额）。
        /// </summary>
        public List<DepartmentSummaryItem> SummarizeByDepartment(IEnumerable<ReimbursementForm> forms)
        {
            return _allocationService.SummarizeByDepartment(forms);
        }

        /// <summary>
        /// 按项目维度汇总多张报销单。
        /// </summary>
        public List<ProjectSummaryItem> SummarizeByProject(IEnumerable<ReimbursementForm> forms)
        {
            return _allocationService.SummarizeByProject(forms);
        }

        /// <summary>
        /// 按员工维度汇总多张报销单，可限定时间范围。
        /// </summary>
        public List<EmployeeSummaryItem> SummarizeByEmployee(IEnumerable<ReimbursementForm> forms,
            DateTime? startDate = null, DateTime? endDate = null)
        {
            return _allocationService.SummarizeByEmployee(forms, startDate, endDate);
        }

        #endregion

        #region === 金额与导出 ===

        /// <summary>
        /// 金额转中文大写（如 壹万贰仟叁佰肆拾伍元陆角柒分）。
        /// </summary>
        public string AmountToChinese(decimal amount)
        {
            return AmountConverter.ToChineseAmount(amount);
        }

        /// <summary>
        /// 金额转英文大写（如 Twelve Thousand Three Hundred Forty Five Dollars）。
        /// </summary>
        public string AmountToEnglish(decimal amount)
        {
            return AmountConverter.ToEnglishAmount(amount);
        }

        /// <summary>
        /// 金额按币种格式化（如 ¥12,345.67）。
        /// </summary>
        public string FormatAmount(decimal amount, string currency = "CNY")
        {
            return AmountConverter.FormatAmount(amount, currency);
        }

        /// <summary>
        /// 生成结构化打印数据（含行程/费用/发票/审批节点/校验结果，
        /// 可直接映射到打印模板或前端页面）。
        /// </summary>
        public PrintData GeneratePrintData(ReimbursementForm form, ValidationResult? validation = null)
        {
            validation ??= ValidateAll(form);
            if (form.ApprovalNodes == null || form.ApprovalNodes.Count == 0)
            {
                form.ApprovalNodes = GetApprovalNodes(form);
            }
            return _exportService.GeneratePrintData(form, validation);
        }

        /// <summary>
        /// 导出 CSV 格式报表内容。
        /// </summary>
        public string GenerateCsv(ReimbursementForm form)
        {
            return _exportService.GenerateCsv(form);
        }

        /// <summary>
        /// 生成完整的 HTML 打印报表（内嵌 CSS，可直接浏览器打印）。
        /// </summary>
        public string GenerateHtmlReport(ReimbursementForm form, ValidationResult? validation = null)
        {
            validation ??= ValidateAll(form);
            if (form.ApprovalNodes == null || form.ApprovalNodes.Count == 0)
            {
                form.ApprovalNodes = GetApprovalNodes(form);
            }
            return _exportService.GenerateHtmlReport(form, validation);
        }

        #endregion

        #region === 其他辅助 ===

        /// <summary>
        /// 获取当前生效的报销标准配置（可查看各级别各城市标准）。
        /// </summary>
        public ReimbursementStandard GetCurrentStandard()
        {
            return _standard;
        }

        /// <summary>
        /// 向查重库追加已使用发票号（报销通过后调用，避免后续重复报销）。
        /// </summary>
        public void RegisterUsedInvoices(IEnumerable<string> invoiceNos)
        {
            _invoiceValidator.AddUsedInvoices(invoiceNos);
        }

        /// <summary>
        /// 一键式完成：计算补贴 + 全量校验 + 生成审批节点 +
        /// 生成审批摘要 + 生成打印数据 + 金额中文 + 自动分摊。
        /// 适合业务系统在提交报销前一次性调用。
        /// </summary>
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

    /// <summary>
    /// ProcessFull 的聚合返回结果。
    /// </summary>
    public class ProcessResult
    {
        /// <summary>净补贴金额（补贴减扣减）</summary>
        public decimal NetSubsidy { get; set; }

        /// <summary>补贴逐行程明细</summary>
        public SubsidyBreakdown? SubsidyBreakdown { get; set; }

        /// <summary>全量校验结果（超标/缺票/预算）</summary>
        public ValidationResult? Validation { get; set; }

        /// <summary>推荐审批节点列表</summary>
        public List<ApprovalNode>? ApprovalNodes { get; set; }

        /// <summary>完整审批摘要文本</summary>
        public string ApprovalSummary { get; set; } = string.Empty;

        /// <summary>打印所需结构化数据</summary>
        public PrintData? PrintData { get; set; }

        /// <summary>报销总额中文大写</summary>
        public string ChineseAmount { get; set; } = string.Empty;

        /// <summary>部门分摊结果</summary>
        public Dictionary<string, decimal> DepartmentAllocation { get; set; } = new Dictionary<string, decimal>();

        /// <summary>项目分摊结果</summary>
        public Dictionary<string, decimal> ProjectAllocation { get; set; } = new Dictionary<string, decimal>();
    }
}
