using System;
using System.Collections.Generic;
using FinanceReimbursement;
using FinanceReimbursement.Adapters;
using FinanceReimbursement.Models;
using FinanceReimbursement.Repositories;
using FinanceReimbursement.Rules;
using FinanceReimbursement.Services;

namespace FinanceReimbursement.Examples
{
    public static class EnterpriseIntegrationExample
    {
        public static void Run()
        {
            Console.WriteLine("=== 企业级报销类库 - 完整接入演示 ===\n");

            RunInitAndBasicFlow();
            RunSubmitReturnResubmitFlow();
            RunImportExportFlow();
            RunRuleVersionAndAuditFlow();
        }

        private static void RunInitAndBasicFlow()
        {
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine("  Part 1: 初始化 + 基础流程");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            var invoiceRepo = new InMemoryInvoiceRepository(new[]
            {
                new UsedInvoiceRecord { InvoiceNo = "HIST-001", FormNo = "BX-OLD-001", DepartmentId = "D001" }
            });

            var budgetRepo = new InMemoryBudgetRepository(new[]
            {
                new Budget { DepartmentId = "D001", DepartmentName = "销售部", Year = 2026, TotalBudget = 50000m, UsedBudget = 0 },
                new Budget { DepartmentId = "D002", DepartmentName = "研发部", Year = 2026, TotalBudget = 80000m, UsedBudget = 0 }
            });

            var ruleManager = new RuleSchemeManager();
            ruleManager.AddScheme(RuleScheme.CreateDefault("ACME", "ACME集团"));
            var salesScheme = RuleScheme.CreateForDepartment("D001", "销售部", "ACME", "ACME集团",
                parentOrgId: "ACME", orgPath: "/ACME/D001");
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

            Console.WriteLine($"  发票仓储: {invoiceRepo.GetUsedInvoiceCount()} 张历史发票");
            Console.WriteLine($"  规则方案: {facade.GetAllRuleSchemes().Count} 套");
            foreach (var s in facade.GetAllRuleSchemes())
                Console.WriteLine($"    [{s.Id}] {s.Name} V{s.Version} (优先级:{s.Priority}, 路径:{s.OrgPath})");
            Console.WriteLine();

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

            var rule = facade.ResolveRule(emp);
            var result = facade.ProcessFullEnhanced(form, rule, projectType: "COMMERCIAL");

            Console.WriteLine($"  报销单: {form.FormNo}");
            Console.WriteLine($"  总额: ¥{form.TotalAmount:N2} = {result.ChineseAmount}");
            Console.WriteLine($"  校验: {(result.Validation?.IsValid == true ? "✅" : "❌")}");
            Console.WriteLine($"  审批风险: {result.ApprovalRecommendation?.RiskLevel}, {result.ApprovalRecommendation?.TotalNodes}个节点");
            Console.WriteLine();
        }

        private static void RunSubmitReturnResubmitFlow()
        {
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine("  Part 2: 提交 → 退回 → 释放 → 重新提交");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            var invoiceRepo = new InMemoryInvoiceRepository();
            var budgetRepo = new InMemoryBudgetRepository(new[]
            {
                new Budget { DepartmentId = "D001", DepartmentName = "销售部", Year = 2026, TotalBudget = 10000m, UsedBudget = 0 }
            });
            var facade = new ReimbursementFacade(new RuleSchemeManager(), invoiceRepo, budgetRepo);

            var emp = new Employee
            {
                Id = "E100", Name = "张三",
                DepartmentId = "D001", DepartmentName = "销售部",
                Level = EmployeeLevel.Supervisor
            };

            // ---- 第一次提交 ----
            Console.WriteLine("▶ 第一次提交:");
            var form = facade.CreateForm(emp, "深圳出差报销");
            var trip = facade.AddTrip(form, "深圳", "深圳市",
                new DateTime(2026, 7, 1), new DateTime(2026, 7, 3), CityLevel.Tier1);
            var hotel = facade.AddExpense(form, ExpenseCategory.Accommodation,
                2400m, 144m, "酒店2晚", tripId: trip.Id);
            facade.AddInvoice(hotel, "FLOW-HT-001", InvoiceType.VATSpecial,
                2400m, 144m, sellerName: "深圳酒店", isVerified: true);
            facade.AddExpense(form, ExpenseCategory.Transportation,
                2000m, 0m, "机票", tripId: trip.Id);

            var rule = facade.ResolveRule(emp);
            var result = facade.ProcessFullEnhanced(form, rule);

            Console.WriteLine($"    金额: ¥{form.TotalAmount:N2}");

            bool budgetOk = facade.TryOccupyBudget(form, out string budgetMsg);
            Console.WriteLine($"    预算占用: {(budgetOk ? "✅ " + budgetMsg : "❌ " + budgetMsg)}");

            facade.CommitInvoices(form);
            Console.WriteLine($"    发票已提交: 发票仓储共 {invoiceRepo.GetUsedInvoiceCount()} 张");
            Console.WriteLine($"    发票 FLOW-HT-001 已占用: {invoiceRepo.IsInvoiceUsed("FLOW-HT-001")}");

            var bs1 = facade.GetBudgetOccupation("D001");
            Console.WriteLine($"    销售部预算: 总额 ¥{bs1.TotalBudget:N2}, 已占用 ¥{bs1.TotalOccupied:N2}, 可用 ¥{bs1.TotalAvailable:N2}");
            Console.WriteLine();

            // ---- 退回 ----
            Console.WriteLine("▶ 审批退回（需修改发票）:");
            facade.ReleaseBudget(form.FormNo);
            Console.WriteLine($"    预算已释放");

            facade.RollbackInvoices(form);
            Console.WriteLine($"    发票已回退: FLOW-HT-001 已占用={invoiceRepo.IsInvoiceUsed("FLOW-HT-001")}");

            var bs2 = facade.GetBudgetOccupation("D001");
            Console.WriteLine($"    销售部预算: 总额 ¥{bs2.TotalBudget:N2}, 已占用 ¥{bs2.TotalOccupied:N2}, 可用 ¥{bs2.TotalAvailable:N2}");
            Console.WriteLine();

            // ---- 修改后重新提交 ----
            Console.WriteLine("▶ 修改后重新提交:");
            form.ExpenseItems[0].Invoices[0].Amount = 2400m;
            form.ExpenseItems[0].Invoices[0].IsVerified = true;

            var result2 = facade.ProcessFullEnhanced(form, rule);

            bool budgetOk2 = facade.TryOccupyBudget(form, out string budgetMsg2);
            Console.WriteLine($"    预算占用: {(budgetOk2 ? "✅ " + budgetMsg2 : "❌ " + budgetMsg2)}");

            facade.CommitInvoices(form);
            Console.WriteLine($"    发票已重新提交: FLOW-HT-001 已占用={invoiceRepo.IsInvoiceUsed("FLOW-HT-001")}");

            var bs3 = facade.GetBudgetOccupation("D001");
            Console.WriteLine($"    销售部预算: 总额 ¥{bs3.TotalBudget:N2}, 已占用 ¥{bs3.TotalOccupied:N2}, 可用 ¥{bs3.TotalAvailable:N2}");
            Console.WriteLine();

            Console.WriteLine($"    ✅ 发票占用和预算占用在退回后均正确释放，重新提交后正确恢复");
            Console.WriteLine();
        }

        private static void RunImportExportFlow()
        {
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine("  Part 3: 导入导出适配能力");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            var facade = new ReimbursementFacade();

            // ---- JSON 导出 ----
            Console.WriteLine("▶ 单据JSON导出:");
            var emp = new Employee { Id = "E200", Name = "测试员工", DepartmentId = "D002", DepartmentName = "研发部", Level = EmployeeLevel.Senior };
            var form = facade.CreateForm(emp, "导入导出测试");
            facade.AddExpense(form, ExpenseCategory.Office, 3000m, 180m, "办公用品");

            var json = JsonAdapter.ExportForm(form);
            Console.WriteLine($"    导出JSON长度: {json.Length} 字符");
            Console.WriteLine($"    前100字: {json.Substring(0, Math.Min(100, json.Length))}...");
            Console.WriteLine();

            // ---- 字典导入 ----
            Console.WriteLine("▶ 从字典导入报销单:");
            var importData = new Dictionary<string, object>
            {
                ["formNo"] = "IMP-001",
                ["title"] = "外部系统导入报销单",
                ["reimbursementType"] = "差旅费",
                ["applicant"] = new Dictionary<string, object>
                {
                    ["id"] = "E300",
                    ["name"] = "导入员工",
                    ["departmentId"] = "D003",
                    ["departmentName"] = "市场部",
                    ["level"] = "主管"
                },
                ["expenseItems"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["category"] = "交通费",
                        ["description"] = "出租车",
                        ["amount"] = "500",
                        ["taxAmount"] = "30",
                        ["invoices"] = new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object>
                            {
                                ["invoiceNo"] = "IMP-INV-001",
                                ["type"] = "电子发票",
                                ["amount"] = "500",
                                ["taxAmount"] = "30",
                                ["sellerName"] = "滴滴出行"
                            }
                        }
                    },
                    new Dictionary<string, object>
                    {
                        ["category"] = "住宿费",
                        ["description"] = "酒店",
                        ["amount"] = "1200",
                        ["taxAmount"] = "72"
                    }
                }
            };

            var importedForm = JsonAdapter.ImportFromDictionary(importData);
            Console.WriteLine($"    导入单号: {importedForm.FormNo}");
            Console.WriteLine($"    申请人: {importedForm.Applicant?.Name} ({importedForm.Applicant?.Level.GetDescription()})");
            Console.WriteLine($"    费用项: {importedForm.ExpenseItems.Count}项");
            Console.WriteLine($"    有发票项: {importedForm.ExpenseItems.FindAll(e => e.Invoices.Count > 0).Count}项");
            Console.WriteLine();

            // ---- 表格导入 ----
            Console.WriteLine("▶ 从表格结构导入（Excel/CSV映射）:");
            var tableRows = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    ["formNo"] = "TBL-001",
                    ["费用类别"] = "交通费",
                    ["描述"] = "地铁公交",
                    ["金额"] = "200",
                    ["税额"] = "12",
                    ["发票号"] = "TBL-INV-001",
                    ["发票类型"] = "电子发票",
                    ["开票方"] = "北京地铁"
                },
                new Dictionary<string, string>
                {
                    ["formNo"] = "TBL-001",
                    ["费用类别"] = "餐饮费",
                    ["描述"] = "工作餐",
                    ["金额"] = "150",
                    ["税额"] = "9"
                }
            };

            var importedDicts = JsonAdapter.ImportFromTable(tableRows,
                applicant: new Employee { Id = "E400", Name = "表格员工", DepartmentId = "D001", DepartmentName = "销售部", Level = EmployeeLevel.Junior });
            Console.WriteLine($"    表格行: {tableRows.Count}行 → 报销单: {importedDicts.Count}张");
            Console.WriteLine();

            // ---- 统一结果包导出 ----
            Console.WriteLine("▶ 统一结果包导出:");
            var processedForms = new List<ReimbursementForm> { form, importedForm };
            var results = new List<EnhancedProcessResult>();
            var appliedRules = new List<RuleScheme>();
            foreach (var f in processedForms)
            {
                var r = facade.ResolveRule(f.Applicant);
                results.Add(facade.ProcessFullEnhanced(f, r));
                appliedRules.Add(r);
            }

            var pkg = ResultPackageBuilder.Build(results, processedForms, appliedRules);
            Console.WriteLine($"    结果包ID: {pkg.PackageId}");
            Console.WriteLine($"    处理记录: {pkg.Records.Count}条");
            Console.WriteLine($"    通过: {pkg.Summary.ValidCount}, 不通过: {pkg.Summary.InvalidCount}");
            Console.WriteLine($"    总金额: ¥{pkg.Summary.TotalAmount:N2} = {pkg.Summary.TotalAmountChinese}");

            foreach (var rec in pkg.Records)
            {
                Console.WriteLine($"    [{rec.FormNo}] {rec.ApplicantName} ¥{rec.TotalAmount:N2} " +
                                  $"规则:{rec.AppliedRuleSchemeId} V{rec.AppliedRuleSchemeVersion} " +
                                  $"风险:{rec.ApprovalRiskLevel}");
            }

            var pkgJson = pkg.ExportJson();
            Console.WriteLine($"    导出JSON长度: {pkgJson.Length} 字符");
            Console.WriteLine();
        }

        private static void RunRuleVersionAndAuditFlow()
        {
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine("  Part 4: 规则版本追溯 + 审计留档文本");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            var ruleManager = new RuleSchemeManager();
            ruleManager.AddScheme(RuleScheme.CreateDefault("ACME", "ACME集团"));

            var v2 = RuleScheme.CreateVersioned("ACME", "2.0",
                new DateTime(2026, 7, 1), "ACME", "ACME集团");
            v2.Description = "2026年7月起生效的新报销标准";
            ruleManager.AddScheme(v2);

            var deptRule = RuleScheme.CreateForDepartment("D001", "销售部", "ACME", "ACME集团",
                parentOrgId: "ACME", orgPath: "/ACME/D001");
            ruleManager.AddScheme(deptRule);
            ruleManager.SetActiveScheme("D001", deptRule.Id);

            Console.WriteLine("▶ 当前生效规则:");
            foreach (var s in ruleManager.GetEffectiveSchemes())
                Console.WriteLine($"    [{s.Id}] {s.Name} V{s.Version} 生效:{s.EffectiveDate:yyyy-MM-dd} 优先级:{s.Priority}");
            Console.WriteLine();

            // ---- 规则快照追溯 ----
            Console.WriteLine("▶ 规则快照追溯:");
            var emp = new Employee { Id = "E500", Name = "审计员工", DepartmentId = "D001", DepartmentName = "销售部", Level = EmployeeLevel.Manager };
            var snapshot = ruleManager.CaptureResolvedSnapshot(emp);
            Console.WriteLine($"    快照: {snapshot}");
            Console.WriteLine($"    方案ID: {snapshot.SchemeId}, 版本: {snapshot.Version}");
            Console.WriteLine($"    组织路径: {snapshot.OrgPath}, 抓取时间: {snapshot.CapturedAt:yyyy-MM-dd HH:mm:ss}");

            ruleManager.RecordFormRule("BX-AUDIT-001", snapshot);
            var history = ruleManager.GetSnapshots("BX-AUDIT-001");
            Console.WriteLine($"    报销单 BX-AUDIT-001 关联规则快照: {history.Count}条");
            Console.WriteLine();

            // ---- 审计留档文本 ----
            Console.WriteLine("▶ 审计留档文本:");
            var facade = new ReimbursementFacade(ruleManager);
            var form = facade.CreateForm(emp, "审计留档测试报销单");
            facade.AddExpense(form, ExpenseCategory.Entertainment, 15000m, 900m, "客户招待");
            var rule = facade.ResolveRule(emp);
            var result = facade.ProcessFullEnhanced(form, rule, projectType: "GOVERNMENT");

            var budgetSummary = new BudgetOccupationSummary
            {
                DepartmentId = "D001",
                DepartmentName = "销售部",
                TotalBudget = 50000m,
                TotalOccupied = 42000m
            };

            var auditText = AuditTextGenerator.Generate(form,
                result.ApprovalRecommendation,
                result.Validation,
                budgetSummary,
                projectType: "GOVERNMENT",
                ruleSnapshot: snapshot);

            Console.WriteLine("    ┌──────────────────────────────────────────────");
            foreach (var line in auditText.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    Console.WriteLine($"    │ {line.TrimEnd()}");
            }
            Console.WriteLine("    └──────────────────────────────────────────────");
            Console.WriteLine();
        }
    }
}
