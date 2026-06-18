using System;
using System.Collections.Generic;
using System.Text;
using FinanceReimbursement.Models;
using FinanceReimbursement.Rules;
using FinanceReimbursement.Utils;

namespace FinanceReimbursement.Services
{
    public class EnhancedApprovalService
    {
        public ApprovalRecommendation GetRecommendation(ReimbursementForm form,
            RuleScheme ruleScheme,
            ValidationResult? validation = null,
            BudgetOccupationSummary? budgetSummary = null,
            string projectType = "COMMERCIAL")
        {
            var recommendation = new ApprovalRecommendation();
            if (form == null) return recommendation;

            var amount = form.TotalAmount;
            var level = form.Applicant?.Level ?? EmployeeLevel.Junior;
            var deptId = form.Applicant?.DepartmentId ?? "";

            var amountRule = FindThresholdRule(amount, ruleScheme.ApprovalRule);
            var projectRule = ruleScheme.ProjectTypeRiskRule;
            var invoiceRule = ruleScheme.InvoiceAnomalyRule;

            var baseNodes = new List<ApprovalNode>();
            var reasons = new List<ApprovalReason>();

            if (amountRule != null)
            {
                int order = 1;
                foreach (var nodeType in amountRule.NodeTypes)
                {
                    var node = CreateNode(nodeType, order, amount, amountRule);
                    if (node != null)
                    {
                        baseNodes.Add(node);
                        order++;
                    }
                }

                reasons.Add(new ApprovalReason
                {
                    Factor = "金额阈值",
                    Description = $"报销金额 ¥{amount:N2}，" +
                                  $"适用阈值 [{amountRule.MinAmount:N0} ~ {FormatMaxAmount(amountRule.MaxAmount)}] 元",
                    Impact = $"基础审批流 {baseNodes.Count} 个节点"
                });
            }

            if (!string.IsNullOrWhiteSpace(projectType) &&
                projectRule.ProjectTypeConfigs.ContainsKey(projectType))
            {
                var config = projectRule.ProjectTypeConfigs[projectType];
                if (config.ExtraApprovalLevel > 0 || config.ForceFinanceManager ||
                    config.ForceSuperiorLeader || config.ForceGeneralManager || config.ForceCEO)
                {
                    ApplyProjectTypeOverride(baseNodes, config, reasons);
                }
                else
                {
                    reasons.Add(new ApprovalReason
                    {
                        Factor = "项目类型",
                        Description = $"项目类型为[{config.ProjectTypeName}]，{config.Reason}",
                        Impact = "无额外审批要求"
                    });
                }
            }

            if (projectRule.BudgetRiskAutoEscalate && budgetSummary != null)
            {
                CheckBudgetRisk(baseNodes, budgetSummary, projectRule, reasons);
            }

            if (validation != null)
            {
                CheckInvoiceAnomaly(baseNodes, validation, invoiceRule, projectRule, reasons);
            }

            for (int i = 0; i < baseNodes.Count; i++)
            {
                baseNodes[i].Order = i + 1;
            }
            baseNodes.Sort((a, b) => a.Order.CompareTo(b.Order));

            recommendation.Nodes = baseNodes;
            recommendation.Reasons = reasons;
            recommendation.Summary = BuildReasonSummary(reasons);
            recommendation.TotalNodes = baseNodes.Count;
            recommendation.RiskLevel = DetermineRiskLevel(reasons);

            return recommendation;
        }

        public string GenerateEnhancedSummary(ReimbursementForm form,
            ApprovalRecommendation recommendation,
            ValidationResult? validation = null)
        {
            if (form == null) return "";

            var sb = new StringBuilder();

            sb.AppendLine("【报销单审批摘要】");
            sb.AppendLine($"单号: {form.FormNo}");
            sb.AppendLine($"标题: {form.Title}");
            sb.AppendLine($"类型: {form.ReimbursementType}");
            sb.AppendLine($"申请人: {form.Applicant?.Name}({form.Applicant?.DepartmentName}) - {(form.Applicant?.Level ?? EmployeeLevel.Junior).GetDescription()}");
            sb.AppendLine($"报销事由: {form.Title}");

            if (form.Trips != null && form.Trips.Count > 0)
            {
                sb.AppendLine($"出差行程: {form.Trips.Count}个");
                foreach (var trip in form.Trips)
                {
                    sb.AppendLine($"  - {trip.Destination}({trip.DepartureDate:yyyy-MM-dd} ~ {trip.ReturnDate:yyyy-MM-dd}, {trip.Days}天)");
                }
            }

            var categorySummary = form.GetCategorySummary();
            sb.AppendLine($"费用明细: {form.ExpenseItems?.Count ?? 0}项, {form.GetInvoiceCount()}张发票");
            foreach (var kv in categorySummary)
            {
                sb.AppendLine($"  - {kv.Key.GetDescription()}: ¥{kv.Value:N2}");
            }

            sb.AppendLine($"费用合计: ¥{form.SubtotalAmount:N2}");
            sb.AppendLine($"补贴: +¥{form.SubsidyAmount:N2}");
            if (form.DeductionAmount > 0)
                sb.AppendLine($"扣减: -¥{form.DeductionAmount:N2}");
            sb.AppendLine($"报销总额: ¥{form.TotalAmount:N2}");
            sb.AppendLine($"大写金额: {AmountConverter.ToChineseAmount(form.TotalAmount)}");

            if (!string.IsNullOrWhiteSpace(form.ProjectName))
                sb.AppendLine($"关联项目: {form.ProjectName}");

            sb.AppendLine();
            sb.AppendLine($"审批风险等级: {recommendation.RiskLevel}");
            sb.AppendLine($"审批流程: {recommendation.TotalNodes}个节点");
            foreach (var node in recommendation.Nodes)
            {
                sb.AppendLine($"  {node.Order}. {node.NodeName} — {node.RuleDescription}");
            }

            sb.AppendLine();
            sb.AppendLine("审批理由:");
            foreach (var reason in recommendation.Reasons)
            {
                sb.AppendLine($"  [{reason.Factor}] {reason.Description}");
                sb.AppendLine($"    影响: {reason.Impact}");
            }

            if (validation != null)
            {
                sb.AppendLine();
                if (validation.IsValid)
                {
                    sb.AppendLine($"校验结果: ✅ 通过" + (validation.HasWarnings ? $"（{validation.WarningCount}个警告）" : ""));
                }
                else
                {
                    sb.AppendLine($"校验结果: ❌ 不通过（{validation.ErrorCount}个错误, {validation.WarningCount}个警告）");
                }
                foreach (var msg in validation.GetErrors())
                {
                    sb.AppendLine($"  ❌ {msg.Message}");
                }
                foreach (var msg in validation.GetWarnings())
                {
                    sb.AppendLine($"  ⚠️ {msg.Message}");
                }
            }

            return sb.ToString();
        }

        private void ApplyProjectTypeOverride(List<ApprovalNode> nodes,
            ProjectTypeApprovalConfig config, List<ApprovalReason> reasons)
        {
            bool added = false;

            if (config.ForceFinanceManager &&
                !nodes.Exists(n => n.NodeType == ApprovalNodeType.FinanceManager))
            {
                nodes.Add(CreateNode(ApprovalNodeType.FinanceManager, 99,
                    "项目类型要求", config.Reason));
                added = true;
            }

            if (config.ForceSuperiorLeader &&
                !nodes.Exists(n => n.NodeType == ApprovalNodeType.SuperiorLeader))
            {
                nodes.Add(CreateNode(ApprovalNodeType.SuperiorLeader, 99,
                    "项目类型要求", config.Reason));
                added = true;
            }

            if (config.ForceGeneralManager &&
                !nodes.Exists(n => n.NodeType == ApprovalNodeType.GeneralManager))
            {
                nodes.Add(CreateNode(ApprovalNodeType.GeneralManager, 99,
                    "项目类型要求", config.Reason));
                added = true;
            }

            if (config.ForceCEO &&
                !nodes.Exists(n => n.NodeType == ApprovalNodeType.CEO))
            {
                nodes.Add(CreateNode(ApprovalNodeType.CEO, 99,
                    "项目类型要求", config.Reason));
                added = true;
            }

            if (added)
            {
                reasons.Add(new ApprovalReason
                {
                    Factor = "项目类型",
                    Description = $"项目类型为[{config.ProjectTypeName}]，{config.Reason}",
                    Impact = $"需追加审批节点（{config.ExtraApprovalLevel}级提升）"
                });
            }
        }

        private void CheckBudgetRisk(List<ApprovalNode> nodes,
            BudgetOccupationSummary budgetSummary,
            ProjectTypeRiskRule rule, List<ApprovalReason> reasons)
        {
            decimal maxUsageRate = 0;
            string worstCategory = "总预算";

            if (budgetSummary.TotalBudget > 0)
            {
                maxUsageRate = budgetSummary.TotalOccupied / budgetSummary.TotalBudget * 100;
            }

            foreach (var cat in budgetSummary.CategoryDetails)
            {
                if (cat.UsageRate > maxUsageRate)
                {
                    maxUsageRate = cat.UsageRate;
                    worstCategory = cat.CategoryName;
                }
            }

            if (maxUsageRate >= rule.BudgetRiskThreshold)
            {
                if (!nodes.Exists(n => n.NodeType == ApprovalNodeType.FinanceManager))
                {
                    nodes.Add(CreateNode(ApprovalNodeType.FinanceManager, 99,
                        "预算风险", $"{worstCategory}使用率达{maxUsageRate:F1}%，需财务经理审批"));
                }

                reasons.Add(new ApprovalReason
                {
                    Factor = "预算风险",
                    Description = $"{worstCategory}使用率已达 {maxUsageRate:F1}%，超过预警阈值 {rule.BudgetRiskThreshold}%",
                    Impact = "自动追加财务经理审批节点"
                });
            }
            else if (maxUsageRate > 0)
            {
                reasons.Add(new ApprovalReason
                {
                    Factor = "预算风险",
                    Description = $"预算使用率 {maxUsageRate:F1}%，未超过预警阈值",
                    Impact = "无需追加审批"
                });
            }
        }

        private void CheckInvoiceAnomaly(List<ApprovalNode> nodes,
            ValidationResult validation, InvoiceAnomalyRule invoiceRule,
            ProjectTypeRiskRule projectRule, List<ApprovalReason> reasons)
        {
            int invoiceErrors = validation.ErrorCount +
                validation.GetErrors().FindAll(m =>
                    m.Code.StartsWith("INVOICE")).Count;

            bool hasDuplicates = validation.GetErrors().Exists(m =>
                m.Code == "INVOICE_DUPLICATE_LOCAL" || m.Code == "INVOICE_DUPLICATE_GLOBAL");
            bool hasMissing = validation.GetErrors().Exists(m =>
                m.Code == "INVOICE_MISSING");
            bool hasAmountMismatch = validation.GetWarnings().Exists(m =>
                m.Code == "INVOICE_AMOUNT_INSUFFICIENT");
            bool hasUnverified = validation.GetWarnings().Exists(m =>
                m.Code == "INVOICE_NOT_VERIFIED");

            var anomalies = new List<string>();
            if (hasDuplicates) anomalies.Add("重复发票");
            if (hasMissing) anomalies.Add("缺票");
            if (hasAmountMismatch) anomalies.Add("发票金额不足");
            if (hasUnverified) anomalies.Add("发票未验真");

            if (anomalies.Count > 0 && projectRule.InvoiceAnomalyEscalate)
            {
                if (!nodes.Exists(n => n.NodeType == ApprovalNodeType.FinanceReview))
                {
                    nodes.Insert(0, CreateNode(ApprovalNodeType.FinanceReview, 1,
                        "发票异常", $"存在{string.Join("、", anomalies)}问题，需财务重点审核"));
                }

                reasons.Add(new ApprovalReason
                {
                    Factor = "发票异常",
                    Description = $"发现发票问题：{string.Join("、", anomalies)}",
                    Impact = "自动追加/前置财务审核节点，需重点核查票据"
                });
            }
            else if (anomalies.Count == 0)
            {
                reasons.Add(new ApprovalReason
                {
                    Factor = "发票校验",
                    Description = "发票校验无异常",
                    Impact = "无额外审批要求"
                });
            }
        }

        private ApprovalThresholdRule? FindThresholdRule(decimal amount, ApprovalRule approvalRule)
        {
            foreach (var rule in approvalRule.ThresholdRules)
            {
                if (amount >= rule.MinAmount && amount < rule.MaxAmount)
                    return rule;
            }
            return approvalRule.ThresholdRules.Count > 0
                ? approvalRule.ThresholdRules[approvalRule.ThresholdRules.Count - 1]
                : null;
        }

        private ApprovalNode? CreateNode(ApprovalNodeType nodeType, int order,
            object context, string description)
        {
            var node = new ApprovalNode
            {
                Order = order,
                NodeType = nodeType,
                RuleDescription = description,
                IsRequired = true
            };

            switch (nodeType)
            {
                case ApprovalNodeType.DepartmentHead:
                    node.NodeName = "部门主管审批";
                    break;
                case ApprovalNodeType.SuperiorLeader:
                    node.NodeName = "分管领导审批";
                    break;
                case ApprovalNodeType.FinanceReview:
                    node.NodeName = "财务审核";
                    break;
                case ApprovalNodeType.FinanceManager:
                    node.NodeName = "财务经理审批";
                    break;
                case ApprovalNodeType.GeneralManager:
                    node.NodeName = "总经理审批";
                    break;
                case ApprovalNodeType.CEO:
                    node.NodeName = "总裁审批";
                    break;
                default:
                    node.NodeName = nodeType.GetDescription();
                    break;
            }

            return node;
        }

        private string FormatMaxAmount(decimal max)
        {
            return max == decimal.MaxValue ? "∞" : max.ToString("N0");
        }

        private string BuildReasonSummary(List<ApprovalReason> reasons)
        {
            if (reasons.Count == 0) return "标准审批流程";

            var parts = new List<string>();
            foreach (var r in reasons)
            {
                parts.Add($"{r.Factor}: {r.Impact}");
            }
            return string.Join("; ", parts);
        }

        private string DetermineRiskLevel(List<ApprovalReason> reasons)
        {
            int highRisk = 0;
            int mediumRisk = 0;

            foreach (var r in reasons)
            {
                if (r.Factor == "发票异常" || r.Factor == "预算风险")
                    highRisk++;
                else if (r.Factor == "项目类型" && r.Impact.Contains("追加"))
                    mediumRisk++;
            }

            if (highRisk > 0) return "🔴 高风险";
            if (mediumRisk > 0) return "🟡 中风险";
            return "🟢 低风险";
        }
    }

    public class ApprovalRecommendation
    {
        public List<ApprovalNode> Nodes { get; set; } = new List<ApprovalNode>();
        public List<ApprovalReason> Reasons { get; set; } = new List<ApprovalReason>();
        public string Summary { get; set; } = string.Empty;
        public int TotalNodes { get; set; }
        public string RiskLevel { get; set; } = "🟢 低风险";
    }

    public class ApprovalReason
    {
        public string Factor { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
    }
}
