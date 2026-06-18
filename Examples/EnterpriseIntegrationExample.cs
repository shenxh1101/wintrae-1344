using System;
using System.Collections.Generic;
using FinanceReimbursement;
using FinanceReimbursement.Models;
using FinanceReimbursement.Repositories;
using FinanceReimbursement.Rules;

namespace FinanceReimbursement.Examples
{
    public static class EnterpriseIntegrationExample
    {
        public static void Run()
        {
            Console.WriteLine("=== 企业级报销类库 - 完整接入演示 ===\n");

            // ========== 1. 初始化：仓储 + 规则方案 ==========
            Console.WriteLine("【步骤1】初始化仓储与规则方案\n");

            var invoiceRepo = new InMemoryInvoiceRepository(new[]
            {
                new UsedInvoiceRecord { InvoiceNo = "HIST-001", FormNo = "BX-OLD-001", DepartmentId = "D001" },
                new UsedInvoiceRecord { InvoiceNo = "HIST-002", FormNo = "BX-OLD-002", DepartmentId = "D001" }
            });

            var budgetRepo = new InMemoryBudgetRepository(new[]
            {
                new Budget { DepartmentId = "D001", DepartmentName = "销售部", Year = 2026, TotalBudget = 50000m, UsedBudget = 0 },
                new Budget { DepartmentId = "D002", DepartmentName = "研发部", Year = 2026, TotalBudget = 80000m, UsedBudget = 0 }
            });

            var ruleManager = new RuleSchemeManager();
            ruleManager.AddScheme(RuleScheme.CreateDefault("ACME", "ACME集团"));
            var salesScheme = RuleScheme.CreateForDepartment("D001", "销售部", "ACME", "ACME集团");
            salesScheme.ProjectTypeRiskRule.ProjectTypeConfigs["COMMERCIAL"] = new ProjectTypeApprovalConfig
            {
                ProjectTypeName = "商业项目",
                ExtraApprovalLevel = 1,
                Reason = "销售部商业项目需财务经理额外审核",
                ForceFinanceManager = true
            };
            ruleManager.AddScheme(salesScheme);
            ruleManager.SetActiveScheme("D001", salesScheme.Id);

            var facade = new ReimbursementFacade(ruleManager, invoiceRepo, budgetRepo);

            Console.WriteLine($"  发票仓储: 已有 {invoiceRepo.GetUsedInvoiceCount()} 张历史发票");
            Console.WriteLine($"  规则方案: {facade.GetAllRuleSchemes().Count} 套");
            foreach (var s in facade.GetAllRuleSchemes())
                Console.WriteLine($"    - [{s.Id}] {s.Name} (优先级:{s.Priority})");
            Console.WriteLine();

            // ========== 2. 创建报销单 ==========
            Console.WriteLine("【步骤2】创建报销单并填写内容\n");

            var emp = new Employee
            {
                Id = "E001", Name = "李明",
                DepartmentId = "D001", DepartmentName = "销售部",
                Level = EmployeeLevel.Manager, Position = "销售经理"
            };
            var form = facade.CreateForm(emp, "上海客户拜访差旅报销", "差旅费", "P-SH-01", "华东拓展项目");
            var trip = facade.AddTrip(form, "上海浦东", "上海市",
                new DateTime(2026, 6, 15), new DateTime(2026, 6, 17),
                CityLevel.Tier1, TransportationType.HighSpeedRail, TransportationType.HighSpeedRail,
                "拜访3家客户", "P-SH-01");

            var hotel = facade.AddExpense(form, ExpenseCategory.Accommodation,
                1800m, 108m, "上海XX酒店2晚", new DateTime(2026, 6, 15), trip.Id, projectId: "P-SH-01");
            facade.AddInvoice(hotel, "ENT-HT-001", InvoiceType.VATSpecial,
                1800m, 108m, sellerName: "上海XX酒店", isVerified: true);

            var train = facade.AddExpense(form, ExpenseCategory.Transportation,
                1100m, 99m, "高铁往返", new DateTime(2026, 6, 15), trip.Id,
                TransportationType.HighSpeedRail, "北京", "上海", projectId: "P-SH-01");
            facade.AddInvoice(train, "ENT-TR-001", InvoiceType.Electronic,
                1100m, 99m, sellerName: "中国铁路");

            facade.AddExpense(form, ExpenseCategory.Meal,
                500m, 30m, "出差餐饮", new DateTime(2026, 6, 16), trip.Id);

            Console.WriteLine($"  报销单: {form.FormNo}");
            Console.WriteLine($"  费用合计: ¥{form.SubtotalAmount:N2}");
            Console.WriteLine();

            // ========== 3. 自动解析规则方案 ==========
            Console.WriteLine("【步骤3】自动解析适用规则方案\n");

            var rule = facade.ResolveRule(emp);
            Console.WriteLine($"  适配方案: [{rule.Id}] {rule.Name}");
            Console.WriteLine($"  住宿标准(经理/一线): ¥{rule.Standard.AccommodationStandard[EmployeeLevel.Manager][CityLevel.Tier1]}/晚");
            Console.WriteLine();

            // ========== 4. 计算补贴 + 校验 + 增强审批 ==========
            Console.WriteLine("【步骤4】增强模式：补贴+校验+多因子审批\n");

            var result = facade.ProcessFullEnhanced(form, rule, projectType: "COMMERCIAL");

            Console.WriteLine($"  补贴: ¥{form.SubsidyAmount:N2}，扣减: ¥{form.DeductionAmount:N2}");
            Console.WriteLine($"  总额: ¥{form.TotalAmount:N2} = {result.ChineseAmount}");
            Console.WriteLine();
            Console.WriteLine($"  校验: {(result.Validation?.IsValid == true ? "✅ 通过" : "❌ 不通过")}");
            if (result.Validation != null && result.Validation.Messages.Count > 0)
            {
                foreach (var m in result.Validation.GetDisplayMessages())
                    Console.WriteLine($"    {m}");
            }
            Console.WriteLine();
            Console.WriteLine($"  审批风险等级: {result.ApprovalRecommendation?.RiskLevel}");
            Console.WriteLine($"  审批节点 ({result.ApprovalRecommendation?.TotalNodes}个):");
            if (result.ApprovalRecommendation != null)
            {
                foreach (var n in result.ApprovalRecommendation.Nodes)
                    Console.WriteLine($"    {n.Order}. {n.NodeName} — {n.RuleDescription}");
                Console.WriteLine("  审批理由:");
                foreach (var r in result.ApprovalRecommendation.Reasons)
                    Console.WriteLine($"    [{r.Factor}] {r.Description} → {r.Impact}");
            }
            Console.WriteLine();

            // ========== 5. 仓储操作：预算占用 + 发票提交 ==========
            Console.WriteLine("【步骤5】仓储操作：预算占用与发票提交\n");

            if (facade.TryOccupyBudget(form, out string budgetMsg))
            {
                Console.WriteLine($"  ✅ 预算占用成功: {budgetMsg}");
            }
            else
            {
                Console.WriteLine($"  ❌ 预算占用失败: {budgetMsg}");
            }

            facade.CommitInvoices(form);
            Console.WriteLine($"  发票已提交: {form.GetInvoiceCount()} 张");
            Console.WriteLine($"  发票仓储当前记录: {invoiceRepo.GetUsedInvoiceCount()} 张");
            Console.WriteLine();

            var budgetSummary = facade.GetBudgetOccupation("D001");
            Console.WriteLine($"  销售部预算概况: 总额 ¥{budgetSummary.TotalBudget:N2}，已占用 ¥{budgetSummary.TotalOccupied:N2}，可用 ¥{budgetSummary.TotalAvailable:N2}");
            Console.WriteLine();

            // ========== 6. 前端视图模型 ==========
            Console.WriteLine("【步骤6】前端视图模型（可直接序列化为JSON给前端）\n");

            var vm = result.PrintViewModel;
            Console.WriteLine($"  表头: {vm.Header.FormNo} / {vm.Header.ApplicantName} ({vm.Header.DepartmentName})");
            Console.WriteLine($"  费用行数: {vm.ExpenseTable.TotalRows}");
            Console.WriteLine($"  发票行数: {vm.InvoiceTable.TotalCount}");
            Console.WriteLine($"  金额: {vm.Amount.TotalAmount} = {vm.Amount.TotalAmountChinese}");
            Console.WriteLine($"  校验: {vm.Validation.SummaryText}");
            Console.WriteLine($"  审批风险: {vm.Approval.RiskLevel} ({vm.Approval.TotalNodes}个节点)");
            Console.WriteLine($"  审批理由: {vm.Approval.Reasons.Count}条");
            foreach (var r in vm.Approval.Reasons)
                Console.WriteLine($"    [{r.Factor}] {r.Impact}");
            Console.WriteLine();

            // ========== 7. 批量处理 ==========
            Console.WriteLine("【步骤7】批量处理多张报销单\n");

            var batchForms = new List<ReimbursementForm> { form };

            var emp2 = new Employee
            {
                Id = "E002", Name = "王芳",
                DepartmentId = "D002", DepartmentName = "研发部",
                Level = EmployeeLevel.Supervisor
            };
            var form2 = facade.CreateForm(emp2, "深圳技术交流差旅报销");
            var trip2 = facade.AddTrip(form2, "深圳", "深圳市",
                new DateTime(2026, 6, 20), new DateTime(2026, 6, 22), CityLevel.Tier1);
            var hotel2 = facade.AddExpense(form2, ExpenseCategory.Accommodation,
                1200m, 72m, "酒店2晚", tripId: trip2.Id);
            facade.AddInvoice(hotel2, "BATCH-HT-001", InvoiceType.VATSpecial,
                1200m, 72m, sellerName: "深圳酒店", isVerified: true);
            facade.AddExpense(form2, ExpenseCategory.Transportation,
                1500m, 0m, "机票", tripId: trip2.Id);
            batchForms.Add(form2);

            var emp3 = new Employee
            {
                Id = "E003", Name = "赵六",
                DepartmentId = "D001", DepartmentName = "销售部",
                Level = EmployeeLevel.Junior
            };
            var form3 = facade.CreateForm(emp3, "北京会议报销");
            facade.AddExpense(form3, ExpenseCategory.Conference,
                6000m, 360m, "会议场地费");
            batchForms.Add(form3);

            var batchResult = facade.ProcessBatch(batchForms, rule,
                usedInvoiceNos: invoiceRepo.GetUsedInvoiceNos(),
                budgetSummary: budgetSummary,
                projectType: "COMMERCIAL");

            Console.WriteLine($"  批量处理: 共{batchResult.TotalCount}张");
            Console.WriteLine($"  {batchResult.GetSummaryText()}");
            Console.WriteLine();

            Console.WriteLine("  各单结果:");
            foreach (var bi in batchResult.Items)
            {
                var status = bi.IsValid ? "✅" : "❌";
                Console.WriteLine($"    {status} {bi.FormNo} - {bi.ApplicantName} " +
                                  $"¥{bi.TotalAmount:N2} {bi.ChineseAmount}");
            }
            Console.WriteLine();

            Console.WriteLine("  部门汇总:");
            foreach (var kv in batchResult.DepartmentSummary)
            {
                Console.WriteLine($"    {kv.Key}: {kv.Value.FormCount}张, 合计¥{kv.Value.TotalAmount:N2}, 不通过{kv.Value.InvalidCount}张");
            }

            Console.WriteLine("\n=== 企业级接入演示完毕 ===");
        }
    }
}
