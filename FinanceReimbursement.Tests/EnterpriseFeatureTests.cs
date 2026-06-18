using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FinanceReimbursement;
using FinanceReimbursement.Adapters;
using FinanceReimbursement.Models;
using FinanceReimbursement.Repositories;
using FinanceReimbursement.Rules;
using FinanceReimbursement.Services;

namespace FinanceReimbursement.Tests
{
    public class EnterpriseFeatureTests
    {
        [Fact]
        public void InMemoryInvoiceRepository_TrackAndQuery()
        {
            var repo = new InMemoryInvoiceRepository();
            Assert.False(repo.IsInvoiceUsed("INV001"));

            repo.MarkInvoicesAsUsed(new[] { "INV001", "INV002" }, "BX001", "D001");
            Assert.True(repo.IsInvoiceUsed("INV001"));
            Assert.True(repo.IsInvoiceUsed("INV002"));
            Assert.Equal(2, repo.GetUsedInvoiceCount());
            Assert.Equal(2, repo.GetUsedInvoiceCount("D001"));
            Assert.Equal(0, repo.GetUsedInvoiceCount("D999"));

            repo.UnmarkInvoices(new[] { "INV001" });
            Assert.False(repo.IsInvoiceUsed("INV001"));
            Assert.Equal(1, repo.GetUsedInvoiceCount());
        }

        [Fact]
        public void InMemoryBudgetRepository_OccupyAndRelease()
        {
            var repo = new InMemoryBudgetRepository(new[]
            {
                new Budget { DepartmentId = "D1", DepartmentName = "技术部", Year = 2026, TotalBudget = 10000m, UsedBudget = 0 }
            });

            Assert.True(repo.TryOccupyBudget("D1", "", null, 5000m, "BX001", out var msg1));
            Assert.Contains("成功", msg1);

            Assert.False(repo.TryOccupyBudget("D1", "", null, 6000m, "BX002", out var msg2));
            Assert.Contains("不足", msg2);

            repo.ReleaseBudget("BX001");
            Assert.True(repo.TryOccupyBudget("D1", "", null, 6000m, "BX003", out var msg3));
        }

        [Fact]
        public void BudgetOccupationSummary_ShowsDetails()
        {
            var repo = new InMemoryBudgetRepository(new[]
            {
                new Budget { DepartmentId = "D1", DepartmentName = "技术部", Year = 2026, TotalBudget = 10000m, UsedBudget = 0 }
            });
            repo.TryOccupyBudget("D1", "", null, 3000m, "BX001", out _);

            var summary = repo.GetOccupationSummary("D1");
            Assert.Equal("技术部", summary.DepartmentName);
            Assert.Equal(10000m, summary.TotalBudget);
            Assert.Equal(3000m, summary.TotalOccupied);
            Assert.Equal(7000m, summary.TotalAvailable);
        }

        [Fact]
        public void RuleScheme_MatchesCorrectly()
        {
            var defaultScheme = RuleScheme.CreateDefault();
            var deptScheme = RuleScheme.CreateForDepartment("D001", "销售部");
            var execScheme = RuleScheme.CreateForExecutive();

            var junior = new Employee { Id = "1", DepartmentId = "D001", Level = EmployeeLevel.Junior };
            var manager = new Employee { Id = "2", DepartmentId = "D002", Level = EmployeeLevel.Manager };
            var director = new Employee { Id = "3", DepartmentId = "D003", Level = EmployeeLevel.Director };

            Assert.True(deptScheme.Matches(junior));
            Assert.False(deptScheme.Matches(manager));

            Assert.True(execScheme.Matches(director));
            Assert.False(execScheme.Matches(junior));
        }

        [Fact]
        public void RuleScheme_OrgPathAndParentSet()
        {
            var dept = RuleScheme.CreateForDepartment("D001", "销售部", "ACME", "ACME集团",
                parentOrgId: "ACME", orgPath: "/ACME/D001");
            Assert.Equal("ACME", dept.ParentOrgId);
            Assert.Equal("/ACME/D001", dept.OrgPath);

            var exec = RuleScheme.CreateForExecutive("ACME", "ACME集团", "/ACME/EXEC");
            Assert.Equal("/ACME/EXEC", exec.OrgPath);
        }

        [Fact]
        public void RuleScheme_EffectiveDateFiltering()
        {
            var futureScheme = RuleScheme.CreateDefault();
            futureScheme.Id = "FUTURE";
            futureScheme.EffectiveDate = DateTime.Now.AddDays(30);

            var emp = new Employee { Id = "1", DepartmentId = "D001", Level = EmployeeLevel.Junior };
            Assert.False(futureScheme.Matches(emp));

            var expiredScheme = RuleScheme.CreateDefault();
            expiredScheme.Id = "EXPIRED";
            expiredScheme.EffectiveDate = DateTime.Now.AddDays(-60);
            expiredScheme.ExpiryDate = DateTime.Now.AddDays(-1);
            Assert.False(expiredScheme.Matches(emp));
        }

        [Fact]
        public void RuleSchemeSnapshot_CapturedCorrectly()
        {
            var scheme = RuleScheme.CreateForDepartment("D001", "销售部", "ACME", "ACME集团",
                orgPath: "/ACME/D001");
            scheme.Version = "2.1";

            var snapshot = scheme.CaptureSnapshot();
            Assert.Equal("DEPT_D001", snapshot.SchemeId);
            Assert.Equal("2.1", snapshot.Version);
            Assert.Equal("ACME", snapshot.CompanyId);
            Assert.Equal("D001", snapshot.DepartmentId);
            Assert.Equal("/ACME/D001", snapshot.OrgPath);
            Assert.True(snapshot.CapturedAt <= DateTime.Now);
        }

        [Fact]
        public void RuleSchemeManager_ResolvesPriority()
        {
            var manager = new RuleSchemeManager();
            var deptScheme = RuleScheme.CreateForDepartment("D001", "销售部");
            manager.AddScheme(deptScheme);
            manager.SetActiveScheme("D001", deptScheme.Id);

            var emp = new Employee { Id = "1", DepartmentId = "D001", Level = EmployeeLevel.Junior };
            var resolved = manager.ResolveRule(emp);

            Assert.Equal(deptScheme.Id, resolved.Id);
        }

        [Fact]
        public void RuleSchemeManager_GetEffectiveSchemes()
        {
            var manager = new RuleSchemeManager();
            var v1 = RuleScheme.CreateDefault();
            v1.Id = "V1";
            v1.EffectiveDate = DateTime.Now.AddDays(-10);

            var v2 = RuleScheme.CreateVersioned("TEST", "2.0", DateTime.Now.AddDays(30));
            manager.AddScheme(v2);

            var effective = manager.GetEffectiveSchemes();
            Assert.DoesNotContain(effective, s => s.Id == "ACME_V2.0");
        }

        [Fact]
        public void RuleSchemeManager_CaptureAndRecordSnapshot()
        {
            var manager = new RuleSchemeManager();
            var emp = new Employee { Id = "E1", DepartmentId = "D001", Level = EmployeeLevel.Manager };

            var snapshot = manager.CaptureResolvedSnapshot(emp);
            Assert.NotEmpty(snapshot.SchemeId);

            manager.RecordFormRule("BX-TEST-001", snapshot);
            var history = manager.GetSnapshots("BX-TEST-001");
            Assert.Single(history);
            Assert.Equal(snapshot.SchemeId, history[0].SchemeId);
        }

        [Fact]
        public void RuleSchemeManager_SwitchScheme()
        {
            var manager = new RuleSchemeManager();
            var scheme1 = RuleScheme.CreateDefault();
            scheme1.Id = "V1";
            scheme1.Name = "规则V1";
            var scheme2 = RuleScheme.CreateDefault();
            scheme2.Id = "V2";
            scheme2.Name = "规则V2";

            manager.AddScheme(scheme1);
            manager.AddScheme(scheme2);
            manager.SetActiveScheme("D001", "V1");
            Assert.Equal("V1", manager.GetActiveSchemeId("D001"));

            manager.SetActiveScheme("D001", "V2");
            Assert.Equal("V2", manager.GetActiveSchemeId("D001"));
        }

        [Fact]
        public void Facade_WithRepositories_IntegratesCorrectly()
        {
            var invoiceRepo = new InMemoryInvoiceRepository();
            var budgetRepo = new InMemoryBudgetRepository(new[]
            {
                new Budget { DepartmentId = "D001", DepartmentName = "测试部", Year = 2026, TotalBudget = 50000m, UsedBudget = 0 }
            });
            var ruleProvider = new RuleSchemeManager();

            var facade = new ReimbursementFacade(ruleProvider, invoiceRepo, budgetRepo);
            Assert.NotNull(facade.InvoiceRepository);
            Assert.NotNull(facade.BudgetRepository);

            var emp = new Employee { Id = "E1", Name = "测试", DepartmentId = "D001", DepartmentName = "测试部", Level = EmployeeLevel.Manager };
            var form = facade.CreateForm(emp, "仓储集成测试");
            var item = facade.AddExpense(form, ExpenseCategory.Office, 1000m, 60m, "办公用品");
            facade.AddInvoice(item, "INT-INV-001", InvoiceType.Electronic, 1000m, 60m);

            var result = facade.ProcessFull(form);
            Assert.NotNull(result);

            Assert.True(facade.TryOccupyBudget(form, out string msg));
            facade.CommitInvoices(form);

            Assert.True(invoiceRepo.IsInvoiceUsed("INT-INV-001"));
            Assert.Equal(1, invoiceRepo.GetUsedInvoiceCount());
        }

        [Fact]
        public void Facade_RollbackInvoices_ReleasesInvoiceOccupation()
        {
            var invoiceRepo = new InMemoryInvoiceRepository();
            var budgetRepo = new InMemoryBudgetRepository(new[]
            {
                new Budget { DepartmentId = "D001", DepartmentName = "测试部", Year = 2026, TotalBudget = 50000m, UsedBudget = 0 }
            });
            var facade = new ReimbursementFacade(new RuleSchemeManager(), invoiceRepo, budgetRepo);

            var emp = new Employee { Id = "E1", Name = "退回测试", DepartmentId = "D001", DepartmentName = "测试部", Level = EmployeeLevel.Manager };
            var form = facade.CreateForm(emp, "退回流程测试");
            var item = facade.AddExpense(form, ExpenseCategory.Office, 2000m, 120m, "办公设备");
            facade.AddInvoice(item, "RB-INV-001", InvoiceType.VATGeneral, 2000m, 120m);

            facade.CommitInvoices(form);
            Assert.True(invoiceRepo.IsInvoiceUsed("RB-INV-001"));

            facade.RollbackInvoices(form);
            Assert.False(invoiceRepo.IsInvoiceUsed("RB-INV-001"));
        }

        [Fact]
        public void Facade_SubmitReturnResubmit_BudgetAndInvoiceConsistent()
        {
            var invoiceRepo = new InMemoryInvoiceRepository();
            var budgetRepo = new InMemoryBudgetRepository(new[]
            {
                new Budget { DepartmentId = "D001", DepartmentName = "测试部", Year = 2026, TotalBudget = 10000m, UsedBudget = 0 }
            });
            var facade = new ReimbursementFacade(new RuleSchemeManager(), invoiceRepo, budgetRepo);

            var emp = new Employee { Id = "E1", Name = "完整流程", DepartmentId = "D001", DepartmentName = "测试部", Level = EmployeeLevel.Supervisor };
            var form = facade.CreateForm(emp, "完整流程测试");
            var item = facade.AddExpense(form, ExpenseCategory.Accommodation, 3000m, 180m, "酒店");
            facade.AddInvoice(item, "FULL-001", InvoiceType.VATSpecial, 3000m, 180m, isVerified: true);

            facade.ProcessFull(form);
            facade.TryOccupyBudget(form, out _);
            facade.CommitInvoices(form);

            var bs1 = facade.GetBudgetOccupation("D001");
            Assert.True(bs1.TotalOccupied > 0);
            Assert.True(invoiceRepo.IsInvoiceUsed("FULL-001"));

            facade.ReleaseBudget(form.FormNo);
            facade.RollbackInvoices(form);

            var bs2 = facade.GetBudgetOccupation("D001");
            Assert.Equal(0m, bs2.TotalOccupied);
            Assert.False(invoiceRepo.IsInvoiceUsed("FULL-001"));

            facade.TryOccupyBudget(form, out _);
            facade.CommitInvoices(form);

            var bs3 = facade.GetBudgetOccupation("D001");
            Assert.True(bs3.TotalOccupied > 0);
            Assert.True(invoiceRepo.IsInvoiceUsed("FULL-001"));
        }

        [Fact]
        public void EnhancedApproval_MultipleFactors()
        {
            var facade = new ReimbursementFacade();
            var emp = new Employee { Id = "E1", Name = "审批测试", DepartmentId = "D001", Level = EmployeeLevel.Manager };
            var form = facade.CreateForm(emp, "多因子审批测试");
            facade.AddExpense(form, ExpenseCategory.Entertainment, 80000m);

            var ruleScheme = RuleScheme.CreateDefault();
            var validation = facade.ValidateAll(form);
            var budgetSummary = new BudgetOccupationSummary
            {
                DepartmentId = "D001",
                TotalBudget = 100000m,
                TotalOccupied = 85000m
            };

            var recommendation = facade.GetEnhancedApproval(form, ruleScheme, validation, budgetSummary, "GOVERNMENT");

            Assert.NotNull(recommendation);
            Assert.True(recommendation.Nodes.Count >= 3);
            Assert.True(recommendation.Reasons.Count >= 2);
            Assert.NotEmpty(recommendation.Summary);
        }

        [Fact]
        public void BatchProcess_MultipleForms()
        {
            var facade = new ReimbursementFacade();
            var forms = new List<ReimbursementForm>();

            for (int i = 0; i < 5; i++)
            {
                var emp = new Employee
                {
                    Id = $"E{i}",
                    Name = $"员工{i}",
                    DepartmentId = i < 3 ? "D001" : "D002",
                    DepartmentName = i < 3 ? "销售部" : "研发部",
                    Level = EmployeeLevel.Junior
                };
                var f = facade.CreateForm(emp, $"批量测试{i}");
                facade.AddExpense(f, ExpenseCategory.Transportation, (i + 1) * 500m);
                forms.Add(f);
            }

            var rule = RuleScheme.CreateDefault();
            var result = facade.ProcessBatch(forms, rule);

            Assert.Equal(5, result.TotalCount);
            Assert.True(result.TotalAmount > 0);
            Assert.NotEmpty(result.DepartmentSummary);
            Assert.NotEmpty(result.LevelSummary);
            Assert.NotEmpty(result.TotalAmountChinese);
        }

        [Fact]
        public void BatchProcess_GetInvalidAndWarningItems()
        {
            var facade = new ReimbursementFacade();
            var forms = new List<ReimbursementForm>();

            var emp1 = new Employee { Id = "E1", Name = "好员工", DepartmentId = "D1", DepartmentName = "部1", Level = EmployeeLevel.Junior };
            var f1 = facade.CreateForm(emp1, "正常单");
            var item1 = facade.AddExpense(f1, ExpenseCategory.Office, 100m);
            facade.AddInvoice(item1, "GOOD-001", InvoiceType.Electronic, 100m);
            forms.Add(f1);

            var emp2 = new Employee { Id = "E2", Name = "问题员工", DepartmentId = "D2", DepartmentName = "部2", Level = EmployeeLevel.Junior };
            var f2 = facade.CreateForm(emp2, "问题单");
            facade.AddExpense(f2, ExpenseCategory.Entertainment, 5000m);
            forms.Add(f2);

            var result = facade.ProcessBatch(forms, RuleScheme.CreateDefault());

            Assert.True(result.InvalidCount >= 1);
            Assert.NotEmpty(result.GetInvalidItems());
        }

        [Fact]
        public void PrintViewModel_AllSectionsPopulated()
        {
            var facade = new ReimbursementFacade();
            var emp = new Employee { Id = "E1", Name = "视图模型测试", DepartmentId = "D1", DepartmentName = "技术部", Level = EmployeeLevel.Manager };
            var form = facade.CreateForm(emp, "视图模型测试");
            var trip = facade.AddTrip(form, "上海", "上海市",
                new DateTime(2026, 6, 1), new DateTime(2026, 6, 3), CityLevel.Tier1);
            var hotel = facade.AddExpense(form, ExpenseCategory.Accommodation, 1500m, 90m, "酒店", tripId: trip.Id);
            facade.AddInvoice(hotel, "VM-HT-001", InvoiceType.VATSpecial, 1500m, 90m, sellerName: "酒店", isVerified: true);

            var rule = facade.ResolveRule(emp);
            var result = facade.ProcessFullEnhanced(form, rule);

            var vm = result.PrintViewModel;
            Assert.NotNull(vm);
            Assert.Equal(form.FormNo, vm.Header.FormNo);
            Assert.Equal("视图模型测试", vm.Header.ApplicantName);
            Assert.Single(vm.Trips);
            Assert.True(vm.ExpenseTable.TotalRows >= 1);
            Assert.True(vm.InvoiceTable.TotalCount >= 1);
            Assert.NotEmpty(vm.Amount.TotalAmount);
            Assert.NotEmpty(vm.Amount.TotalAmountChinese);
            Assert.True(vm.Approval.TotalNodes >= 2);
            Assert.NotEmpty(vm.Validation.SummaryText);
            Assert.NotNull(vm.Footer);
        }

        [Fact]
        public void PrintViewModel_ValidationItemsHaveColors()
        {
            var facade = new ReimbursementFacade();
            var form = facade.CreateForm(new Employee { Id = "E1", Name = "色", DepartmentId = "D1", Level = EmployeeLevel.Junior });
            facade.AddExpense(form, ExpenseCategory.Meal, 800m);

            var validation = facade.ValidateAll(form);
            var vm = FormPrintViewModelBuilder.Build(form, validation);

            Assert.NotEmpty(vm.Validation.Items);
            Assert.All(vm.Validation.Items, item =>
            {
                Assert.NotEmpty(item.SeverityColor);
                Assert.NotEmpty(item.SeverityIcon);
            });
        }

        [Fact]
        public void ApprovalRecommendation_RiskLevel_DeterminedByFactors()
        {
            var facade = new ReimbursementFacade();
            var form = facade.CreateForm(new Employee { Id = "E1", Name = "风险", DepartmentId = "D1", Level = EmployeeLevel.Junior });
            facade.AddExpense(form, ExpenseCategory.Entertainment, 3000m);

            var validation = facade.ValidateAll(form);
            var budgetSummary = new BudgetOccupationSummary { TotalBudget = 10000m, TotalOccupied = 9000m };

            var recommendation = facade.GetEnhancedApproval(form, RuleScheme.CreateDefault(), validation, budgetSummary, "COMMERCIAL");

            Assert.NotEmpty(recommendation.RiskLevel);
            Assert.Contains(recommendation.RiskLevel, new[] { "🔴 高风险", "🟡 中风险", "🟢 低风险" });
        }

        [Fact]
        public void Facade_ResolveRule_WithoutProvider_ReturnsDefault()
        {
            var facade = new ReimbursementFacade();
            var rule = facade.ResolveRule(new Employee { Id = "E1", Level = EmployeeLevel.Junior });
            Assert.Equal("DEFAULT", rule.Id);
        }

        [Fact]
        public void Facade_SwitchRule_WithProvider()
        {
            var provider = new RuleSchemeManager();
            var custom = RuleScheme.CreateDefault();
            custom.Id = "CUSTOM_V2";
            custom.Name = "自定义规则V2";
            provider.AddScheme(custom);

            var facade = new ReimbursementFacade(provider);
            facade.SwitchRule("D001", "CUSTOM_V2");

            Assert.Equal("CUSTOM_V2", facade.RuleProvider?.GetActiveSchemeId("D001"));
        }

        [Fact]
        public void JsonAdapter_ExportAndImport()
        {
            var facade = new ReimbursementFacade();
            var emp = new Employee { Id = "E1", Name = "导出测试", DepartmentId = "D1", DepartmentName = "测试部", Level = EmployeeLevel.Manager };
            var form = facade.CreateForm(emp, "JSON导出测试");
            var item = facade.AddExpense(form, ExpenseCategory.Office, 1000m, 60m, "办公用品");
            facade.AddInvoice(item, "JSON-001", InvoiceType.Electronic, 1000m, 60m, sellerName: "文具店");

            var json = JsonAdapter.ExportForm(form);
            Assert.NotEmpty(json);
            Assert.Contains("JSON导出测试", json);
            Assert.Contains("JSON-001", json);
            Assert.Contains("导出测试", json);

            var importData = new Dictionary<string, object>
            {
                ["formNo"] = "IMP-001",
                ["title"] = "导入测试",
                ["applicant"] = new Dictionary<string, object>
                {
                    ["id"] = "E2",
                    ["name"] = "导入员工",
                    ["departmentId"] = "D2",
                    ["departmentName"] = "研发部",
                    ["level"] = "主管"
                },
                ["expenseItems"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["category"] = "交通费",
                        ["description"] = "打车",
                        ["amount"] = "200",
                        ["taxAmount"] = "12"
                    }
                }
            };

            var imported = JsonAdapter.ImportFromDictionary(importData);
            Assert.Equal("IMP-001", imported.FormNo);
            Assert.Equal("导入员工", imported.Applicant?.Name);
            Assert.Equal(EmployeeLevel.Supervisor, imported.Applicant?.Level);
            Assert.Single(imported.ExpenseItems);
            Assert.Equal(200m, imported.ExpenseItems[0].Amount);
        }

        [Fact]
        public void JsonAdapter_ImportFromTable()
        {
            var rows = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    ["formNo"] = "TBL-001",
                    ["费用类别"] = "住宿费",
                    ["描述"] = "酒店",
                    ["金额"] = "800",
                    ["税额"] = "48",
                    ["发票号"] = "TBL-INV-001"
                },
                new Dictionary<string, string>
                {
                    ["formNo"] = "TBL-001",
                    ["费用类别"] = "交通费",
                    ["描述"] = "地铁",
                    ["金额"] = "100",
                    ["税额"] = "6"
                }
            };

            var result = JsonAdapter.ImportFromTable(rows, applicant: new Employee { Id = "E1", Name = "表", DepartmentId = "D1", Level = EmployeeLevel.Junior });
            Assert.Single(result);

            var formData = result[0];
            Assert.Equal("TBL-001", formData["formNo"]);
        }

        [Fact]
        public void ResultPackageBuilder_BuildsCorrectly()
        {
            var facade = new ReimbursementFacade();
            var forms = new List<ReimbursementForm>();
            var results = new List<EnhancedProcessResult>();
            var rules = new List<RuleScheme>();

            for (int i = 0; i < 3; i++)
            {
                var emp = new Employee { Id = $"E{i}", Name = $"P{i}", DepartmentId = "D1", DepartmentName = "部", Level = EmployeeLevel.Junior };
                var f = facade.CreateForm(emp, $"结果包{i}");
                facade.AddExpense(f, ExpenseCategory.Office, (i + 1) * 500m);
                forms.Add(f);

                var r = facade.ResolveRule(emp);
                results.Add(facade.ProcessFullEnhanced(f, r));
                rules.Add(r);
            }

            var pkg = ResultPackageBuilder.Build(results, forms, rules);

            Assert.Equal(3, pkg.Summary.TotalCount);
            Assert.NotEmpty(pkg.PackageId);
            Assert.NotEmpty(pkg.Records);
            Assert.All(pkg.Records, r =>
            {
                Assert.NotEmpty(r.FormNo);
                Assert.NotEmpty(r.AppliedRuleSchemeId);
                Assert.NotEmpty(r.AppliedRuleSchemeVersion);
            });

            var json = pkg.ExportJson();
            Assert.NotEmpty(json);
            Assert.Contains("packageId", json);
        }

        [Fact]
        public void AuditTextGenerator_GeneratesReadableText()
        {
            var facade = new ReimbursementFacade();
            var emp = new Employee { Id = "E1", Name = "审计", DepartmentId = "D001", DepartmentName = "销售部", Level = EmployeeLevel.Manager };
            var form = facade.CreateForm(emp, "审计文本测试");
            facade.AddExpense(form, ExpenseCategory.Entertainment, 12000m, 720m, "客户招待");

            var rule = facade.ResolveRule(emp);
            var result = facade.ProcessFullEnhanced(form, rule, projectType: "GOVERNMENT");

            var budgetSummary = new BudgetOccupationSummary
            {
                DepartmentId = "D001",
                DepartmentName = "销售部",
                TotalBudget = 50000m,
                TotalOccupied = 40000m
            };

            var snapshot = rule.CaptureSnapshot();

            var auditText = AuditTextGenerator.Generate(form,
                result.ApprovalRecommendation,
                result.Validation,
                budgetSummary,
                projectType: "GOVERNMENT",
                ruleSnapshot: snapshot);

            Assert.NotEmpty(auditText);
            Assert.Contains("审计", auditText);
            Assert.Contains("审批流程判定", auditText);
            Assert.Contains("风险等级", auditText);
            Assert.Contains("预算状况", auditText);
            Assert.Contains(snapshot.SchemeId, auditText);
        }

        [Fact]
        public void AuditTextGenerator_WithoutBudgetOrRule_NoCrash()
        {
            var facade = new ReimbursementFacade();
            var emp = new Employee { Id = "E1", Name = "简单", DepartmentId = "D1", Level = EmployeeLevel.Junior };
            var form = facade.CreateForm(emp, "简单审计");
            facade.AddExpense(form, ExpenseCategory.Office, 500m);

            var rule = facade.ResolveRule(emp);
            var result = facade.ProcessFullEnhanced(form, rule);

            var auditText = AuditTextGenerator.Generate(form, result.ApprovalRecommendation, result.Validation);
            Assert.NotEmpty(auditText);
            Assert.Contains("审批流程判定", auditText);
        }
    }
}
