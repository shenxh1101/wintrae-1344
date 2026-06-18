using System;
using System.Collections.Generic;
using System.Linq;
using FinanceReimbursement.Models;

namespace FinanceReimbursement.Rules
{
    public class RuleScheme
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string CompanyId { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string ParentOrgId { get; set; } = string.Empty;

        public string OrgPath { get; set; } = string.Empty;

        public string Version { get; set; } = "1.0";

        public DateTime EffectiveDate { get; set; } = DateTime.Now;

        public DateTime? ExpiryDate { get; set; }

        public bool IsEnabled { get; set; } = true;

        public string DepartmentId { get; set; } = string.Empty;

        public string CityCode { get; set; } = string.Empty;

        public EmployeeLevel? MinLevel { get; set; }

        public EmployeeLevel? MaxLevel { get; set; }

        public int Priority { get; set; }

        public ReimbursementStandard Standard { get; set; } = new ReimbursementStandard();

        public ApprovalRule ApprovalRule { get; set; } = new ApprovalRule();

        public ProjectTypeRiskRule ProjectTypeRiskRule { get; set; } = new ProjectTypeRiskRule();

        public InvoiceAnomalyRule InvoiceAnomalyRule { get; set; } = new InvoiceAnomalyRule();

        public RuleSchemeSnapshot CaptureSnapshot()
        {
            return new RuleSchemeSnapshot
            {
                SchemeId = Id,
                SchemeName = Name,
                Version = Version,
                CompanyId = CompanyId,
                DepartmentId = DepartmentId,
                OrgPath = OrgPath,
                Priority = Priority,
                EffectiveDate = EffectiveDate,
                ExpiryDate = ExpiryDate,
                CapturedAt = DateTime.Now
            };
        }

        public bool Matches(Employee employee, string cityCode = "")
        {
            if (!IsEnabled) return false;
            if (EffectiveDate > DateTime.Now) return false;
            if (ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Now) return false;

            if (!string.IsNullOrWhiteSpace(DepartmentId) &&
                employee.DepartmentId != DepartmentId) return false;

            if (!string.IsNullOrWhiteSpace(CityCode) &&
                !string.IsNullOrWhiteSpace(cityCode) &&
                CityCode != cityCode) return false;

            if (MinLevel.HasValue && employee.Level < MinLevel.Value) return false;
            if (MaxLevel.HasValue && employee.Level > MaxLevel.Value) return false;

            return true;
        }

        public static RuleScheme CreateDefault(string companyId = "DEFAULT",
            string companyName = "默认公司")
        {
            return new RuleScheme
            {
                Id = "DEFAULT",
                Name = "默认报销规则",
                Description = "适用于所有员工的默认报销标准和审批规则",
                CompanyId = companyId,
                CompanyName = companyName,
                OrgPath = $"/{companyId}",
                Version = "1.0",
                IsEnabled = true,
                Priority = 0,
                Standard = ReimbursementStandard.CreateDefault(),
                ApprovalRule = ApprovalRule.CreateDefault(),
                ProjectTypeRiskRule = ProjectTypeRiskRule.CreateDefault(),
                InvoiceAnomalyRule = InvoiceAnomalyRule.CreateDefault()
            };
        }

        public static RuleScheme CreateForDepartment(string deptId, string deptName,
            string companyId = "DEFAULT", string companyName = "默认公司",
            string parentOrgId = "", string orgPath = "")
        {
            var scheme = CreateDefault(companyId, companyName);
            scheme.Id = $"DEPT_{deptId}";
            scheme.Name = $"{deptName}专属报销规则";
            scheme.Description = $"适用于{deptName}的报销标准和审批规则";
            scheme.DepartmentId = deptId;
            scheme.ParentOrgId = parentOrgId;
            scheme.OrgPath = string.IsNullOrWhiteSpace(orgPath) ? $"/{companyId}/{deptId}" : orgPath;
            scheme.Priority = 10;
            return scheme;
        }

        public static RuleScheme CreateForExecutive(string companyId = "DEFAULT",
            string companyName = "默认公司", string orgPath = "")
        {
            var scheme = CreateDefault(companyId, companyName);
            scheme.Id = "EXECUTIVE";
            scheme.Name = "高管专属报销规则";
            scheme.Description = "适用于总监及以上级别的报销标准（更高限额）";
            scheme.MinLevel = EmployeeLevel.Director;
            scheme.OrgPath = string.IsNullOrWhiteSpace(orgPath) ? $"/{companyId}/EXECUTIVE" : orgPath;
            scheme.Priority = 20;

            var std = scheme.Standard;
            foreach (EmployeeLevel level in new[] { EmployeeLevel.Director, EmployeeLevel.VicePresident, EmployeeLevel.President })
            {
                if (std.AccommodationStandard.ContainsKey(level))
                {
                    std.AccommodationStandard[level][CityLevel.Tier1] *= 1.5m;
                    std.AccommodationStandard[level][CityLevel.Tier2] *= 1.5m;
                    std.AccommodationStandard[level][CityLevel.Tier3] *= 1.5m;
                    std.AccommodationStandard[level][CityLevel.Tier4] *= 1.5m;
                }
                if (std.DailySubsidy.ContainsKey(level))
                {
                    std.DailySubsidy[level] = (int)(std.DailySubsidy[level] * 1.3m);
                }
            }

            return scheme;
        }

        public static RuleScheme CreateVersioned(string baseSchemeId, string newVersion,
            DateTime effectiveDate, string companyId = "DEFAULT", string companyName = "默认公司")
        {
            var scheme = CreateDefault(companyId, companyName);
            scheme.Id = $"{baseSchemeId}_V{newVersion}";
            scheme.Name = $"报销规则V{newVersion}";
            scheme.Version = newVersion;
            scheme.EffectiveDate = effectiveDate;
            scheme.Priority = 5;
            return scheme;
        }
    }

    public class RuleSchemeSnapshot
    {
        public string SchemeId { get; set; } = "";
        public string SchemeName { get; set; } = "";
        public string Version { get; set; } = "";
        public string CompanyId { get; set; } = "";
        public string DepartmentId { get; set; } = "";
        public string OrgPath { get; set; } = "";
        public int Priority { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CapturedAt { get; set; }

        public override string ToString()
        {
            return $"[{SchemeId}] {SchemeName} V{Version} (优先级:{Priority}, 生效:{EffectiveDate:yyyy-MM-dd})";
        }
    }

    public class ProjectTypeRiskRule
    {
        public Dictionary<string, ProjectTypeApprovalConfig> ProjectTypeConfigs { get; set; }
            = new Dictionary<string, ProjectTypeApprovalConfig>();

        public bool BudgetRiskAutoEscalate { get; set; } = true;

        public decimal BudgetRiskThreshold { get; set; } = 80m;

        public bool InvoiceAnomalyEscalate { get; set; } = true;

        public int MaxInvoiceErrorsToEscalate { get; set; } = 1;

        public static ProjectTypeRiskRule CreateDefault()
        {
            var rule = new ProjectTypeRiskRule();

            rule.ProjectTypeConfigs["INTERNAL"] = new ProjectTypeApprovalConfig
            {
                ProjectTypeName = "内部项目",
                ExtraApprovalLevel = 0,
                Reason = "内部项目无额外审批要求"
            };

            rule.ProjectTypeConfigs["COMMERCIAL"] = new ProjectTypeApprovalConfig
            {
                ProjectTypeName = "商业项目",
                ExtraApprovalLevel = 0,
                Reason = "商业项目适用标准审批流程"
            };

            rule.ProjectTypeConfigs["GOVERNMENT"] = new ProjectTypeApprovalConfig
            {
                ProjectTypeName = "政府项目",
                ExtraApprovalLevel = 1,
                Reason = "政府项目需额外财务审核（合规要求）",
                ForceFinanceManager = true
            };

            rule.ProjectTypeConfigs["OVERSEAS"] = new ProjectTypeApprovalConfig
            {
                ProjectTypeName = "海外项目",
                ExtraApprovalLevel = 1,
                Reason = "海外项目涉及汇率风险，需分管领导审批",
                ForceSuperiorLeader = true
            };

            rule.ProjectTypeConfigs["RND"] = new ProjectTypeApprovalConfig
            {
                ProjectTypeName = "研发项目",
                ExtraApprovalLevel = 0,
                Reason = "研发项目适用标准审批流程"
            };

            return rule;
        }
    }

    public class ProjectTypeApprovalConfig
    {
        public string ProjectTypeName { get; set; } = string.Empty;

        public int ExtraApprovalLevel { get; set; }

        public string Reason { get; set; } = string.Empty;

        public bool ForceFinanceManager { get; set; }

        public bool ForceSuperiorLeader { get; set; }

        public bool ForceGeneralManager { get; set; }

        public bool ForceCEO { get; set; }
    }

    public class InvoiceAnomalyRule
    {
        public bool DuplicateAutoBlock { get; set; } = true;

        public bool MissingAutoBlock { get; set; } = true;

        public bool UnverifiedVATWarning { get; set; } = true;

        public bool ExpiredInvoiceWarning { get; set; } = true;

        public int InvoiceExpireMonths { get; set; } = 12;

        public bool AmountMismatchWarning { get; set; } = true;

        public decimal AmountMismatchThreshold { get; set; } = 0.95m;

        public static InvoiceAnomalyRule CreateDefault()
        {
            return new InvoiceAnomalyRule();
        }
    }
}
