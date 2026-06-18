using System;
using System.Collections.Generic;
using System.Text;
using FinanceReimbursement.Models;
using FinanceReimbursement.Rules;
using FinanceReimbursement.Services;
using FinanceReimbursement.Utils;

namespace FinanceReimbursement.Adapters
{
    public class ResultPackage
    {
        public string PackageId { get; set; } = Guid.NewGuid().ToString();
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public string GeneratorVersion { get; set; } = "3.0";

        public List<FormProcessRecord> Records { get; set; } = new List<FormProcessRecord>();
        public PackageSummary Summary { get; set; } = new PackageSummary();

        public string ExportJson()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            JsonAdapter.AppendKeyValue(sb, "packageId", PackageId);
            JsonAdapter.AppendKeyValue(sb, "generatedAt", GeneratedAt.ToString("yyyy-MM-ddTHH:mm:ss"));
            JsonAdapter.AppendKeyValue(sb, "generatorVersion", GeneratorVersion, true);

            sb.AppendLine("  \"records\": [");
            for (int i = 0; i < Records.Count; i++)
            {
                var r = Records[i];
                sb.AppendLine("    {");
                JsonAdapter.AppendKeyValue(sb, "formNo", r.FormNo, 3);
                JsonAdapter.AppendKeyValue(sb, "title", r.Title, 3);
                JsonAdapter.AppendKeyValue(sb, "applicantId", r.ApplicantId, 3);
                JsonAdapter.AppendKeyValue(sb, "applicantName", r.ApplicantName, 3);
                JsonAdapter.AppendKeyValue(sb, "departmentId", r.DepartmentId, 3);
                JsonAdapter.AppendKeyValue(sb, "departmentName", r.DepartmentName, 3);
                JsonAdapter.AppendKeyValue(sb, "totalAmount", r.TotalAmount.ToString("F2"), 3);
                JsonAdapter.AppendKeyValue(sb, "totalAmountChinese", r.TotalAmountChinese, 3);
                JsonAdapter.AppendKeyValue(sb, "isValid", r.IsValid.ToString().ToLower(), 3);
                JsonAdapter.AppendKeyValue(sb, "ruleSchemeId", r.AppliedRuleSchemeId, 3);
                JsonAdapter.AppendKeyValue(sb, "ruleSchemeVersion", r.AppliedRuleSchemeVersion, 3);
                JsonAdapter.AppendKeyValue(sb, "approvalRiskLevel", r.ApprovalRiskLevel, 3);
                JsonAdapter.AppendKeyValue(sb, "approvalSummary", r.ApprovalAuditText, 3, true);
                sb.AppendLine("    }" + (i < Records.Count - 1 ? "," : ""));
            }
            sb.AppendLine("  ],");

            sb.AppendLine("  \"summary\": {");
            JsonAdapter.AppendKeyValue(sb, "totalCount", Summary.TotalCount.ToString(), 2);
            JsonAdapter.AppendKeyValue(sb, "validCount", Summary.ValidCount.ToString(), 2);
            JsonAdapter.AppendKeyValue(sb, "invalidCount", Summary.InvalidCount.ToString(), 2);
            JsonAdapter.AppendKeyValue(sb, "totalAmount", Summary.TotalAmount.ToString("F2"), 2);
            JsonAdapter.AppendKeyValue(sb, "totalAmountChinese", Summary.TotalAmountChinese, 2, true);
            sb.AppendLine("  }");

            sb.Append("}");
            return sb.ToString();
        }
    }

    public class FormProcessRecord
    {
        public string FormNo { get; set; } = "";
        public string Title { get; set; } = "";
        public string ApplicantId { get; set; } = "";
        public string ApplicantName { get; set; } = "";
        public string DepartmentId { get; set; } = "";
        public string DepartmentName { get; set; } = ""
;
        public decimal TotalAmount { get; set; }
        public string TotalAmountChinese { get; set; } = "";
        public bool IsValid { get; set; }
        public string AppliedRuleSchemeId { get; set; } = "";
        public string AppliedRuleSchemeVersion { get; set; } = "";
        public string ApprovalRiskLevel { get; set; } = "";
        public string ApprovalAuditText { get; set; } = "";
    }

    public class PackageSummary
    {
        public int TotalCount { get; set; }
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string TotalAmountChinese { get; set; } = "";
    }

    public static class ResultPackageBuilder
    {
        public static ResultPackage Build(List<EnhancedProcessResult> results, List<ReimbursementForm> forms, List<RuleScheme>? appliedRules = null)
        {
            var pkg = new ResultPackage();

            for (int i = 0; i < forms.Count; i++)
            {
                var form = forms[i];
                var result = i < results.Count ? results[i] : null;
                var rule = (appliedRules != null && i < appliedRules.Count) ? appliedRules[i] : null;

                var record = new FormProcessRecord
                {
                    FormNo = form.FormNo,
                    Title = form.Title,
                    ApplicantId = form.Applicant?.Id ?? "",
                    ApplicantName = form.Applicant?.Name ?? "",
                    DepartmentId = form.Applicant?.DepartmentId ?? "",
                    DepartmentName = form.Applicant?.DepartmentName ?? "",
                    TotalAmount = form.TotalAmount,
                    TotalAmountChinese = result?.ChineseAmount ?? AmountConverter.ToChineseAmount(form.TotalAmount),
                    IsValid = result?.Validation?.IsValid ?? false,
                    AppliedRuleSchemeId = rule?.Id ?? "",
                    AppliedRuleSchemeVersion = rule?.Version ?? "",
                    ApprovalRiskLevel = result?.ApprovalRecommendation?.RiskLevel ?? "",
                    ApprovalAuditText = result?.ApprovalRecommendation?.AuditText ?? ""
                };

                pkg.Records.Add(record);
            }

            pkg.Summary = new PackageSummary
            {
                TotalCount = forms.Count,
                ValidCount = pkg.Records.FindAll(r => r.IsValid).Count,
                InvalidCount = pkg.Records.FindAll(r => !r.IsValid).Count,
                TotalAmount = pkg.Records.Count > 0 ? pkg.Records[0].TotalAmount : 0,
                TotalAmountChinese = ""
            };

            decimal total = 0;
            foreach (var r in pkg.Records) total += r.TotalAmount;
            pkg.Summary.TotalAmount = total;
            pkg.Summary.TotalAmountChinese = AmountConverter.ToChineseAmount(total);

            return pkg;
        }

        public static ResultPackage BuildFromBatch(BatchProcessResult batchResult, List<ReimbursementForm> forms, List<RuleScheme>? appliedRules = null)
        {
            var pkg = new ResultPackage();

            for (int i = 0; i < batchResult.Items.Count; i++)
            {
                var item = batchResult.Items[i];
                var rule = (appliedRules != null && i < appliedRules.Count) ? appliedRules[i] : null;

                var record = new FormProcessRecord
                {
                    FormNo = item.FormNo,
                    Title = item.Title,
                    ApplicantId = item.ApplicantId,
                    ApplicantName = item.ApplicantName,
                    DepartmentId = item.DepartmentId,
                    DepartmentName = item.DepartmentName,
                    TotalAmount = item.TotalAmount,
                    TotalAmountChinese = item.ChineseAmount,
                    IsValid = item.IsValid,
                    AppliedRuleSchemeId = rule?.Id ?? "",
                    AppliedRuleSchemeVersion = rule?.Version ?? "",
                    ApprovalRiskLevel = item.ApprovalRecommendation?.RiskLevel ?? "",
                    ApprovalAuditText = item.ApprovalRecommendation?.AuditText ?? ""
                };

                pkg.Records.Add(record);
            }

            pkg.Summary = new PackageSummary
            {
                TotalCount = batchResult.TotalCount,
                ValidCount = batchResult.ValidCount,
                InvalidCount = batchResult.InvalidCount,
                TotalAmount = batchResult.TotalAmount,
                TotalAmountChinese = batchResult.TotalAmountChinese
            };

            return pkg;
        }
    }
}
