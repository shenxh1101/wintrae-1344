using System;
using System.Collections.Generic;
using FinanceReimbursement.Models;
using FinanceReimbursement.Utils;

namespace FinanceReimbursement.Services
{
    public class AllocationService
    {
        public Dictionary<string, decimal> AllocateByDepartment(ReimbursementForm form,
            Dictionary<string, decimal>? ratios = null)
        {
            var result = new Dictionary<string, decimal>();
            if (form == null) return result;

            string mainDeptId = form.Applicant?.DepartmentId ?? "";
            string mainDeptName = form.Applicant?.DepartmentName ?? "";

            if (ratios == null || ratios.Count == 0)
            {
                result[mainDeptId] = Math.Round(form.TotalAmount, 2);
                form.DepartmentAllocation = result;
                return result;
            }

            decimal totalRatio = 0;
            foreach (var kv in ratios)
            {
                if (kv.Value < 0)
                {
                    throw new ArgumentException($"部门分摊比例不能为负数: {kv.Key}");
                }
                totalRatio += kv.Value;
            }

            if (totalRatio == 0)
            {
                throw new ArgumentException("部门分摊比例之和不能为0");
            }

            decimal remaining = form.TotalAmount;
            int i = 0;
            int count = ratios.Count;

            foreach (var kv in ratios)
            {
                decimal amount;
                if (i == count - 1)
                {
                    amount = Math.Round(remaining, 2);
                }
                else
                {
                    amount = Math.Round(form.TotalAmount * (kv.Value / totalRatio), 2);
                    remaining -= amount;
                }

                result[kv.Key] = amount;
                i++;
            }

            form.DepartmentAllocation = result;
            return result;
        }

        public Dictionary<string, decimal> AllocateByProject(ReimbursementForm form,
            Dictionary<string, decimal>? ratios = null)
        {
            var result = new Dictionary<string, decimal>();
            if (form == null) return result;

            if (ratios == null || ratios.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(form.ProjectId))
                {
                    result[form.ProjectId] = Math.Round(form.TotalAmount, 2);
                }
                else if (form.ExpenseItems != null)
                {
                    var itemProjects = new Dictionary<string, decimal>();
                    foreach (var item in form.ExpenseItems)
                    {
                        var pid = string.IsNullOrWhiteSpace(item.ProjectId)
                            ? "__default__" : item.ProjectId;
                        if (!itemProjects.ContainsKey(pid))
                        {
                            itemProjects[pid] = 0;
                        }
                        itemProjects[pid] += item.TotalAmount;
                    }

                    foreach (var kv in itemProjects)
                    {
                        result[kv.Key] = Math.Round(kv.Value, 2);
                    }
                }
                else
                {
                    result["__default__"] = Math.Round(form.TotalAmount, 2);
                }

                form.ProjectAllocation = result;
                return result;
            }

            decimal totalRatio = 0;
            foreach (var kv in ratios)
            {
                if (kv.Value < 0)
                {
                    throw new ArgumentException($"项目分摊比例不能为负数: {kv.Key}");
                }
                totalRatio += kv.Value;
            }

            if (totalRatio == 0)
            {
                throw new ArgumentException("项目分摊比例之和不能为0");
            }

            decimal remaining = form.TotalAmount;
            int i = 0;
            int count = ratios.Count;

            foreach (var kv in ratios)
            {
                decimal amount;
                if (i == count - 1)
                {
                    amount = Math.Round(remaining, 2);
                }
                else
                {
                    amount = Math.Round(form.TotalAmount * (kv.Value / totalRatio), 2);
                    remaining -= amount;
                }

                result[kv.Key] = amount;
                i++;
            }

            form.ProjectAllocation = result;
            return result;
        }

        public List<DepartmentSummaryItem> SummarizeByDepartment(IEnumerable<ReimbursementForm> forms)
        {
            var summary = new Dictionary<string, DepartmentSummaryItem>();

            if (forms == null) return new List<DepartmentSummaryItem>();

            foreach (var form in forms)
            {
                if (form == null) continue;

                string deptId = form.Applicant?.DepartmentId ?? "";
                string deptName = form.Applicant?.DepartmentName ?? "未分配部门";

                if (!summary.ContainsKey(deptId))
                {
                    summary[deptId] = new DepartmentSummaryItem
                    {
                        DepartmentId = deptId,
                        DepartmentName = deptName,
                        FormCount = 0,
                        TotalAmount = 0,
                        CategoryBreakdown = new Dictionary<ExpenseCategory, decimal>()
                    };
                }

                summary[deptId].FormCount++;
                summary[deptId].TotalAmount += form.TotalAmount;

                var categorySummary = form.GetCategorySummary();
                foreach (var kv in categorySummary)
                {
                    if (!summary[deptId].CategoryBreakdown.ContainsKey(kv.Key))
                    {
                        summary[deptId].CategoryBreakdown[kv.Key] = 0;
                    }
                    summary[deptId].CategoryBreakdown[kv.Key] += kv.Value;
                }
            }

            var result = new List<DepartmentSummaryItem>(summary.Values);
            result.Sort((a, b) => b.TotalAmount.CompareTo(a.TotalAmount));
            return result;
        }

        public List<ProjectSummaryItem> SummarizeByProject(IEnumerable<ReimbursementForm> forms)
        {
            var summary = new Dictionary<string, ProjectSummaryItem>();

            if (forms == null) return new List<ProjectSummaryItem>();

            foreach (var form in forms)
            {
                if (form == null) continue;

                if (form.ProjectAllocation != null && form.ProjectAllocation.Count > 0)
                {
                    foreach (var kv in form.ProjectAllocation)
                    {
                        var pid = kv.Key;
                        if (!summary.ContainsKey(pid))
                        {
                            summary[pid] = new ProjectSummaryItem
                            {
                                ProjectId = pid,
                                FormCount = 0,
                                TotalAmount = 0,
                                CategoryBreakdown = new Dictionary<ExpenseCategory, decimal>()
                            };
                        }
                        summary[pid].TotalAmount += kv.Value;
                        summary[pid].FormCount++;
                    }
                }
                else
                {
                    string pid = string.IsNullOrWhiteSpace(form.ProjectId) ? "__default__" : form.ProjectId;
                    if (!summary.ContainsKey(pid))
                    {
                        summary[pid] = new ProjectSummaryItem
                        {
                            ProjectId = pid,
                            ProjectName = form.ProjectName,
                            FormCount = 0,
                            TotalAmount = 0,
                            CategoryBreakdown = new Dictionary<ExpenseCategory, decimal>()
                        };
                    }
                    summary[pid].FormCount++;
                    summary[pid].TotalAmount += form.TotalAmount;

                    var categorySummary = form.GetCategorySummary();
                    foreach (var kv in categorySummary)
                    {
                        if (!summary[pid].CategoryBreakdown.ContainsKey(kv.Key))
                        {
                            summary[pid].CategoryBreakdown[kv.Key] = 0;
                        }
                        summary[pid].CategoryBreakdown[kv.Key] += kv.Value;
                    }
                }
            }

            var result = new List<ProjectSummaryItem>(summary.Values);
            result.Sort((a, b) => b.TotalAmount.CompareTo(a.TotalAmount));
            return result;
        }

        public List<EmployeeSummaryItem> SummarizeByEmployee(IEnumerable<ReimbursementForm> forms,
            DateTime? startDate = null, DateTime? endDate = null)
        {
            var summary = new Dictionary<string, EmployeeSummaryItem>();

            if (forms == null) return new List<EmployeeSummaryItem>();

            foreach (var form in forms)
            {
                if (form == null || form.Applicant == null) continue;
                if (startDate.HasValue && form.CreateDate < startDate.Value) continue;
                if (endDate.HasValue && form.CreateDate > endDate.Value) continue;

                string empId = form.Applicant.Id;
                if (!summary.ContainsKey(empId))
                {
                    summary[empId] = new EmployeeSummaryItem
                    {
                        EmployeeId = empId,
                        EmployeeName = form.Applicant.Name,
                        DepartmentId = form.Applicant.DepartmentId,
                        DepartmentName = form.Applicant.DepartmentName,
                        Level = form.Applicant.Level,
                        FormCount = 0,
                        TotalAmount = 0,
                        TotalDays = 0
                    };
                }

                summary[empId].FormCount++;
                summary[empId].TotalAmount += form.TotalAmount;
                summary[empId].TotalDays += form.GetTotalDays();
            }

            var result = new List<EmployeeSummaryItem>(summary.Values);
            result.Sort((a, b) => b.TotalAmount.CompareTo(a.TotalAmount));
            return result;
        }
    }

    public class DepartmentSummaryItem
    {
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int FormCount { get; set; }
        public decimal TotalAmount { get; set; }
        public Dictionary<ExpenseCategory, decimal> CategoryBreakdown { get; set; }
            = new Dictionary<ExpenseCategory, decimal>();

        public string GetCategorySummaryText()
        {
            var parts = new List<string>();
            foreach (var kv in CategoryBreakdown)
            {
                parts.Add($"{kv.Key.GetDescription()}: {kv.Value:N2}");
            }
            return string.Join(", ", parts);
        }
    }

    public class ProjectSummaryItem
    {
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public int FormCount { get; set; }
        public decimal TotalAmount { get; set; }
        public Dictionary<ExpenseCategory, decimal> CategoryBreakdown { get; set; }
            = new Dictionary<ExpenseCategory, decimal>();
    }

    public class EmployeeSummaryItem
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public EmployeeLevel Level { get; set; }
        public int FormCount { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalDays { get; set; }
    }
}
