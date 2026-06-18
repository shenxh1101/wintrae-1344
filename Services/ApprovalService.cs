using System;
using System.Collections.Generic;
using FinanceReimbursement.Models;
using FinanceReimbursement.Utils;

namespace FinanceReimbursement.Services
{
    public class ApprovalService
    {
        private readonly ApprovalRule _approvalRule;

        public ApprovalService(ApprovalRule? approvalRule = null)
        {
            _approvalRule = approvalRule ?? ApprovalRule.CreateDefault();
        }

        public List<ApprovalNode> GetApprovalNodes(ReimbursementForm form)
        {
            var nodes = new List<ApprovalNode>();
            if (form == null) return nodes;

            var amount = form.TotalAmount;
            var level = form.Applicant?.Level ?? EmployeeLevel.Junior;
            var deptId = form.Applicant?.DepartmentId ?? "";

            var matchedRule = FindThresholdRule(amount);

            int order = 1;
            if (matchedRule != null)
            {
                foreach (var nodeType in matchedRule.NodeTypes)
                {
                    var node = CreateApprovalNode(nodeType, order, amount, level, deptId, matchedRule);
                    if (node != null)
                    {
                        nodes.Add(node);
                        order++;
                    }
                }
            }

            nodes.Sort((a, b) => a.Order.CompareTo(b.Order));
            return nodes;
        }

        public string GetApprovalSummary(ReimbursementForm form, ValidationResult? validation = null)
        {
            if (form == null) return "";

            var parts = new List<string>();

            parts.Add($"【报销单审批摘要】");
            parts.Add($"单号: {form.FormNo}");
            parts.Add($"类型: {form.ReimbursementType}");
            parts.Add($"申请人: {form.Applicant?.Name}({form.Applicant?.DepartmentName}) - {(form.Applicant?.Level ?? EmployeeLevel.Junior).GetDescription()}");
            parts.Add($"报销事由: {form.Title}");

            if (form.Trips != null && form.Trips.Count > 0)
            {
                parts.Add($"出差行程: {form.Trips.Count}个");
                foreach (var trip in form.Trips)
                {
                    parts.Add($"  - {trip.Destination}({trip.DepartureDate:yyyy-MM-dd} ~ {trip.ReturnDate:yyyy-MM-dd}, {trip.Days}天)");
                }
            }

            var categorySummary = form.GetCategorySummary();
            parts.Add($"费用明细: {form.ExpenseItems?.Count ?? 0}项, {form.GetInvoiceCount()}张发票");
            foreach (var kv in categorySummary)
            {
                parts.Add($"  - {kv.Key.GetDescription()}: {kv.Value:N2}元");
            }

            parts.Add($"费用合计: {form.SubtotalAmount:N2}元");
            parts.Add($"补贴: +{form.SubsidyAmount:N2}元");
            if (form.DeductionAmount > 0)
            {
                parts.Add($"扣减: -{form.DeductionAmount:N2}元");
            }
            parts.Add($"报销总额: {form.TotalAmount:N2}元");
            parts.Add($"大写金额: {AmountConverter.ToChineseAmount(form.TotalAmount)}");

            if (!string.IsNullOrWhiteSpace(form.ProjectName))
            {
                parts.Add($"关联项目: {form.ProjectName}");
            }

            var nodes = form.ApprovalNodes ?? GetApprovalNodes(form);
            parts.Add($"审批流程: {nodes.Count}个节点");
            foreach (var node in nodes)
            {
                parts.Add($"  {node.Order}. {node.NodeName}({node.NodeType.GetDescription()})");
            }

            if (validation != null && !validation.IsValid)
            {
                parts.Add($"校验结果: 不通过({validation.ErrorCount}个错误, {validation.WarningCount}个警告)");
                foreach (var msg in validation.GetErrors())
                {
                    parts.Add($"  [错误] {msg.Message}");
                }
            }
            else if (validation != null && validation.HasWarnings)
            {
                parts.Add($"校验结果: 通过但有警告({validation.WarningCount}个警告)");
                foreach (var msg in validation.GetWarnings())
                {
                    parts.Add($"  [警告] {msg.Message}");
                }
            }
            else
            {
                parts.Add($"校验结果: 全部通过");
            }

            return string.Join(Environment.NewLine, parts);
        }

        public string GenerateApprovalCommentTemplate(ReimbursementForm form, string approverRole)
        {
            if (form == null) return "";

            string suggestion;
            if (form.TotalAmount >= 50000)
            {
                suggestion = "金额较大，建议重点核查费用真实性及预算执行情况。";
            }
            else if (form.TotalAmount >= 10000)
            {
                suggestion = "金额较大，请核查费用明细是否合规合理。";
            }
            else if (form.TotalAmount >= 2000)
            {
                suggestion = "请确认费用属实并符合部门预算。";
            }
            else
            {
                suggestion = "常规报销，请确认。";
            }

            return $@"
审批模板 - {approverRole}
================================
报销单号: {form.FormNo}
申请人: {form.Applicant?.Name}
部门: {form.Applicant?.DepartmentName}
报销金额: ¥{form.TotalAmount:N2}

建议意见:
{suggestion}

可选操作:
☐ 同意 - 费用合规，予以通过
☐ 退回 - 存在问题，请补充材料或修改
☐ 拒绝 - 不符合报销规定

审批意见:
______________________________________________
";
        }

        private ApprovalThresholdRule? FindThresholdRule(decimal amount)
        {
            foreach (var rule in _approvalRule.ThresholdRules)
            {
                if (amount >= rule.MinAmount && amount < rule.MaxAmount)
                {
                    return rule;
                }
            }
            return _approvalRule.ThresholdRules.Count > 0
                ? _approvalRule.ThresholdRules[_approvalRule.ThresholdRules.Count - 1]
                : null;
        }

        private ApprovalNode? CreateApprovalNode(ApprovalNodeType nodeType, int order,
            decimal amount, EmployeeLevel applicantLevel, string deptId,
            ApprovalThresholdRule rule)
        {
            var node = new ApprovalNode
            {
                Order = order,
                NodeType = nodeType,
                DepartmentId = deptId,
                AmountThreshold = rule.MaxAmount,
                IsRequired = true
            };

            switch (nodeType)
            {
                case ApprovalNodeType.DepartmentHead:
                    node.NodeName = "部门主管审批";
                    node.RuleDescription = "部门负责人审核业务真实性";
                    break;

                case ApprovalNodeType.SuperiorLeader:
                    node.NodeName = "分管领导审批";
                    node.RuleDescription = $"金额≥{rule.MinAmount:N0}元需分管领导审批";
                    break;

                case ApprovalNodeType.FinanceReview:
                    node.NodeName = "财务审核";
                    node.RuleDescription = "财务核对票据合规性与费用标准";
                    break;

                case ApprovalNodeType.FinanceManager:
                    node.NodeName = "财务经理审批";
                    node.RuleDescription = $"金额≥{rule.MinAmount:N0}元需财务经理审批";
                    break;

                case ApprovalNodeType.GeneralManager:
                    node.NodeName = "总经理审批";
                    node.RuleDescription = $"金额≥{rule.MinAmount:N0}元需总经理审批";
                    break;

                case ApprovalNodeType.CEO:
                    node.NodeName = "总裁审批";
                    node.RuleDescription = $"金额≥{rule.MinAmount:N0}元需总裁审批";
                    break;

                default:
                    node.NodeName = nodeType.GetDescription();
                    break;
            }

            return node;
        }
    }
}
