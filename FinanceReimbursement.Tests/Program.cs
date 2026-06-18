using System;
using System.Collections.Generic;
using System.IO;
using FinanceReimbursement;
using FinanceReimbursement.Models;

namespace FinanceReimbursement.Tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== 财务报销类库 - 完整功能演示 ===\n");

            try
            {
                Demo1_BasicWorkflow();
                Demo2_ValidationAndErrors();
                Demo3_BudgetControl();
                Demo4_AllocationAndSummary();
                Demo5_ExportReports();

                Console.WriteLine("\n=== 所有演示完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n发生异常: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        private static void Demo1_BasicWorkflow()
        {
            Console.WriteLine("--- 演示1: 基本报销流程 ---\n");

            var facade = new ReimbursementFacade();

            var employee = new Employee
            {
                Id = "E001",
                Name = "李明",
                DepartmentId = "DEPT_SALE",
                DepartmentName = "销售部",
                Level = EmployeeLevel.Supervisor,
                Position = "销售主管",
                Email = "liming@company.com",
                Phone = "13900139000",
                BankAccount = "6228480000000001",
                BankName = "招商银行",
                SupervisorId = "E002"
            };

            var form = facade.CreateForm(employee,
                "6月上海客户拜访差旅费报销",
                "差旅费",
                "PROJ_A001",
                "华东区客户拓展项目");

            var trip = facade.AddTrip(form,
                "上海浦东新区客户拜访",
                "上海市",
                new DateTime(2026, 6, 10),
                new DateTime(2026, 6, 13),
                CityLevel.Tier1,
                TransportationType.HighSpeedRail,
                TransportationType.HighSpeedRail,
                "拜访华东区3家重要客户，洽谈Q3合作",
                "PROJ_A001",
                "含客户现场演示");

            var transport1 = facade.AddExpense(form,
                ExpenseCategory.Transportation,
                933m, 84.03m,
                "北京南→上海虹桥 高铁二等座",
                new DateTime(2026, 6, 10),
                trip.Id,
                TransportationType.HighSpeedRail,
                "北京", "上海");
            facade.AddInvoice(transport1,
                "INV-TR-00001", InvoiceType.Electronic,
                933m, 84.03m,
                "RAIL001", new DateTime(2026, 6, 10),
                "中国铁路", "",
                "我司全称", "91110000MA000TEST",
                ExpenseCategory.Transportation,
                "高铁票", true, "验真通过");

            var transport2 = facade.AddExpense(form,
                ExpenseCategory.Transportation,
                933m, 84.03m,
                "上海虹桥→北京南 高铁二等座",
                new DateTime(2026, 6, 13),
                trip.Id,
                TransportationType.HighSpeedRail,
                "上海", "北京");
            facade.AddInvoice(transport2,
                "INV-TR-00002", InvoiceType.Electronic,
                933m, 84.03m,
                "RAIL002", new DateTime(2026, 6, 13),
                "中国铁路");

            var hotel = facade.AddExpense(form,
                ExpenseCategory.Accommodation,
                1350m, 81m,
                "上海XX商务酒店 3晚标准间",
                new DateTime(2026, 6, 10),
                trip.Id);
            facade.AddInvoice(hotel,
                "INV-HT-00001", InvoiceType.VATSpecial,
                1350m, 81m,
                "HT001", new DateTime(2026, 6, 13),
                "上海XX酒店管理有限公司",
                "91310000HT001",
                "我司全称", "91110000MA000TEST",
                ExpenseCategory.Accommodation,
                "住宿服务费*3晚", true, "发票真实有效");

            var meal = facade.AddExpense(form,
                ExpenseCategory.Meal,
                320m, 18m,
                "出差期间餐饮",
                new DateTime(2026, 6, 11),
                trip.Id);
            facade.AddInvoice(meal,
                "INV-ME-00001", InvoiceType.Electronic,
                320m, 18m,
                "", new DateTime(2026, 6, 11),
                "上海XX餐饮公司");

            var taxi = facade.AddExpense(form,
                ExpenseCategory.Transportation,
                156m, 0m,
                "市内出租车费",
                new DateTime(2026, 6, 11),
                trip.Id,
                TransportationType.Taxi);
            facade.AddInvoice(taxi,
                "INV-TX-00001", InvoiceType.FixedAmount,
                156m);

            Console.WriteLine($"报销单号: {form.FormNo}");
            Console.WriteLine($"标题: {form.Title}");
            Console.WriteLine($"申请人: {form.Applicant.Name} ({form.Applicant.Level.GetDescription()})");
            Console.WriteLine($"出差天数: {form.GetTotalDays()} 天");
            Console.WriteLine($"费用项目数: {form.ExpenseItems.Count} 项");
            Console.WriteLine($"发票张数: {form.GetInvoiceCount()} 张");
            Console.WriteLine($"费用合计: ¥{form.SubtotalAmount:N2}");

            var subsidy = facade.CalculateSubsidy(form, true);
            Console.WriteLine($"出差补贴: +¥{form.SubsidyAmount:N2}");
            if (form.DeductionAmount > 0)
                Console.WriteLine($"超标扣减: -¥{form.DeductionAmount:N2}");
            Console.WriteLine($"净补贴: ¥{subsidy:N2}");
            Console.WriteLine($"报销总额: ¥{form.TotalAmount:N2}");
            Console.WriteLine($"大写金额: {facade.AmountToChinese(form.TotalAmount)}");

            var breakdown = facade.GetSubsidyBreakdown(form);
            Console.WriteLine($"\n补贴明细: {breakdown.Summary}");
            foreach (var t in breakdown.Trips)
            {
                Console.WriteLine($"  - {t.Description}");
            }
        }

        private static void Demo2_ValidationAndErrors()
        {
            Console.WriteLine("\n--- 演示2: 校验与错误提示 ---\n");

            var usedInvoices = new List<string> { "USED-0001", "USED-0002" };
            var facade = new ReimbursementFacade(usedInvoiceNos: usedInvoices);

            var employee = new Employee
            {
                Id = "E002",
                Name = "王小二",
                DepartmentId = "DEPT_DEV",
                DepartmentName = "研发部",
                Level = EmployeeLevel.Junior,
                Position = "初级工程师"
            };

            var form = facade.CreateForm(employee, "问题报销单测试");

            var trip = facade.AddTrip(form,
                "深圳", "深圳市",
                new DateTime(2026, 6, 1),
                new DateTime(2026, 6, 7),
                CityLevel.Tier1);

            var expensiveHotel = facade.AddExpense(form,
                ExpenseCategory.Accommodation,
                6000m, 0m,
                "超豪华套房",
                new DateTime(2026, 6, 1),
                trip.Id);
            facade.AddInvoice(expensiveHotel,
                "USED-0001",
                InvoiceType.VATGeneral,
                6000m);

            var noInvoiceItem = facade.AddExpense(form,
                ExpenseCategory.Entertainment,
                3000m, 0m,
                "客户招待 - 无发票");

            var duplicateInForm = facade.AddExpense(form,
                ExpenseCategory.Meal,
                200m);
            facade.AddInvoice(duplicateInForm,
                "LOCAL-DUP-1", InvoiceType.Electronic, 150m);
            facade.AddInvoice(duplicateInForm,
                "LOCAL-DUP-1", InvoiceType.Electronic, 50m);

            var insufficient = facade.AddExpense(form,
                ExpenseCategory.Office,
                1000m, 0m,
                "办公用品");
            facade.AddInvoice(insufficient,
                "INS-001", InvoiceType.Electronic, 500m);

            facade.AddExpense(form,
                ExpenseCategory.Entertainment,
                8000m, 0m,
                "大额招待费测试");

            var result = facade.ProcessFull(form);

            Console.WriteLine("校验结果:");
            Console.WriteLine($"  是否通过: {result.Validation!.IsValid}");
            Console.WriteLine($"  错误数量: {result.Validation.ErrorCount}");
            Console.WriteLine($"  警告数量: {result.Validation.WarningCount}");
            Console.WriteLine($"  提示数量: {result.Validation.InfoCount}");

            Console.WriteLine("\n可展示错误信息:");
            foreach (var msg in result.Validation.GetDisplayMessages())
            {
                Console.WriteLine($"  {msg}");
            }

            Console.WriteLine("\n审批流程建议:");
            foreach (var node in result.ApprovalNodes!)
            {
                Console.WriteLine($"  {node.Order}. {node.NodeName} ({node.RuleDescription})");
            }
        }

        private static void Demo3_BudgetControl()
        {
            Console.WriteLine("\n--- 演示3: 预算管理 ---\n");

            var budgets = new List<Budget>
            {
                new Budget
                {
                    DepartmentId = "DEPT_SALE",
                    DepartmentName = "销售部",
                    Category = null,
                    Year = 2026,
                    Month = 6,
                    TotalBudget = 50000m,
                    UsedBudget = 45000m
                },
                new Budget
                {
                    DepartmentId = "DEPT_SALE",
                    DepartmentName = "销售部",
                    Category = ExpenseCategory.Entertainment,
                    Year = 2026,
                    Month = 6,
                    TotalBudget = 10000m,
                    UsedBudget = 2000m
                },
                new Budget
                {
                    DepartmentId = "DEPT_SALE",
                    DepartmentName = "销售部",
                    Category = ExpenseCategory.Transportation,
                    Year = 2026,
                    Month = 6,
                    TotalBudget = 15000m,
                    UsedBudget = 3000m
                }
            };

            var facade = new ReimbursementFacade(budgets: budgets);

            var summary = facade.GetBudgetSummary("DEPT_SALE");
            Console.WriteLine("销售部6月预算概况:");
            Console.WriteLine($"  总预算: ¥{summary.TotalBudget:N2}");
            Console.WriteLine($"  已使用: ¥{summary.UsedBudget:N2} ({summary.UsageRate:F1}%)");
            Console.WriteLine($"  剩余额: ¥{summary.RemainingBudget:N2}");
            foreach (var d in summary.Details)
            {
                Console.WriteLine($"  - {d.CategoryName}: 预算¥{d.TotalBudget:N2}, 已用¥{d.UsedBudget:N2}, 剩余¥{d.RemainingBudget:N2}");
            }

            var employee = new Employee
            {
                Id = "E003",
                Name = "钱多多",
                DepartmentId = "DEPT_SALE",
                DepartmentName = "销售部",
                Level = EmployeeLevel.Manager
            };

            var overForm = facade.CreateForm(employee, "预算超额测试");
            facade.AddExpense(overForm, ExpenseCategory.Entertainment, 9000m);
            facade.AddExpense(overForm, ExpenseCategory.Transportation, 5000m);

            Console.WriteLine("\n申请报销 ¥{0:N2} 后的预算检查:", overForm.TotalAmount);
            var check = facade.CheckBudget(overForm);
            foreach (var msg in check.GetDisplayMessages())
            {
                Console.WriteLine($"  {msg}");
            }
        }

        private static void Demo4_AllocationAndSummary()
        {
            Console.WriteLine("\n--- 演示4: 分摊与汇总分析 ---\n");

            var facade = new ReimbursementFacade();
            var allForms = new List<ReimbursementForm>();

            var e1 = new Employee
            {
                Id = "E101", Name = "销售A",
                DepartmentId = "D01", DepartmentName = "销售一部",
                Level = EmployeeLevel.Junior
            };
            var f1 = facade.CreateForm(e1, "销售A6月报销");
            facade.AddExpense(f1, ExpenseCategory.Transportation, 3000m, projectId: "P1");
            facade.AddExpense(f1, ExpenseCategory.Entertainment, 5000m, projectId: "P1");
            allForms.Add(f1);

            var e2 = new Employee
            {
                Id = "E102", Name = "销售B",
                DepartmentId = "D01", DepartmentName = "销售一部",
                Level = EmployeeLevel.Supervisor
            };
            var f2 = facade.CreateForm(e2, "销售B6月报销");
            facade.AddExpense(f2, ExpenseCategory.Accommodation, 4000m, projectId: "P2");
            facade.AddExpense(f2, ExpenseCategory.Meal, 1000m, projectId: "P2");
            allForms.Add(f2);

            var e3 = new Employee
            {
                Id = "E201", Name = "研发X",
                DepartmentId = "D02", DepartmentName = "研发中心",
                Level = EmployeeLevel.Manager
            };
            var f3 = facade.CreateForm(e3, "研发X6月报销");
            facade.AddExpense(f3, ExpenseCategory.Training, 6000m, projectId: "P3");
            facade.AddExpense(f3, ExpenseCategory.Office, 500m, projectId: "P3");
            allForms.Add(f3);

            var crossForm = facade.CreateForm(e1, "跨部门项目分摊");
            facade.AddExpense(crossForm, ExpenseCategory.Conference, 10000m);
            var deptRatios = new Dictionary<string, decimal>
            {
                { "D01", 50 },
                { "D02", 30 },
                { "D03", 20 }
            };
            var projRatios = new Dictionary<string, decimal>
            {
                { "P1", 40 },
                { "P3", 60 }
            };
            facade.AllocateByDepartment(crossForm, deptRatios);
            facade.AllocateByProject(crossForm, projRatios);
            allForms.Add(crossForm);

            Console.WriteLine("跨部门分摊单 部门分摊结果:");
            foreach (var kv in crossForm.DepartmentAllocation)
            {
                Console.WriteLine($"  部门{kv.Key}: ¥{kv.Value:N2}");
            }
            Console.WriteLine("跨部门分摊单 项目分摊结果:");
            foreach (var kv in crossForm.ProjectAllocation)
            {
                Console.WriteLine($"  项目{kv.Key}: ¥{kv.Value:N2}");
            }

            Console.WriteLine("\n按部门汇总:");
            var deptSummary = facade.SummarizeByDepartment(allForms);
            foreach (var d in deptSummary)
            {
                Console.WriteLine($"  [{d.DepartmentName}] 单据{d.FormCount}张, 合计¥{d.TotalAmount:N2}");
                Console.WriteLine($"    明细: {d.GetCategorySummaryText()}");
            }

            Console.WriteLine("\n按项目汇总:");
            var projSummary = facade.SummarizeByProject(allForms);
            foreach (var p in projSummary)
            {
                Console.WriteLine($"  [项目{p.ProjectId}] 关联{p.FormCount}次, 合计¥{p.TotalAmount:N2}");
            }

            Console.WriteLine("\n按员工汇总:");
            var empSummary = facade.SummarizeByEmployee(allForms);
            foreach (var e in empSummary)
            {
                Console.WriteLine($"  {e.EmployeeName}({e.DepartmentName}) - {e.FormCount}张单据, 合计¥{e.TotalAmount:N2}");
            }
        }

        private static void Demo5_ExportReports()
        {
            Console.WriteLine("\n--- 演示5: 报表导出与打印数据 ---\n");

            var facade = new ReimbursementFacade();
            var employee = new Employee
            {
                Id = "E999",
                Name = "陈大伟",
                DepartmentId = "DEPT_OPS",
                DepartmentName = "运营部",
                Level = EmployeeLevel.Director,
                Position = "运营总监"
            };
            var form = facade.CreateForm(employee,
                "6月运营工作会议费用",
                "会议费",
                "PROJ_OPS_2026",
                "2026年度运营升级");
            facade.AddTrip(form,
                "杭州总部会议", "杭州市",
                new DateTime(2026, 6, 25),
                new DateTime(2026, 6, 26),
                CityLevel.Tier2,
                TransportationType.Airplane,
                TransportationType.Airplane,
                "Q3运营规划会议");
            var conf = facade.AddExpense(form,
                ExpenseCategory.Conference,
                15000m, 900m,
                "杭州会议中心场地+设备租赁");
            facade.AddInvoice(conf, "CONF-20260625",
                InvoiceType.VATSpecial, 15000m, 900m,
                sellerName: "杭州会议中心有限公司");
            var travel = facade.AddExpense(form,
                ExpenseCategory.Transportation,
                4200m, 252m,
                "往返机票");
            facade.AddInvoice(travel, "AIR-20260625",
                InvoiceType.Electronic, 4200m, 252m,
                sellerName: "国航");

            var result = facade.ProcessFull(form);

            Console.WriteLine($"报销单号: {form.FormNo}");
            Console.WriteLine($"审批摘要:\n{result.ApprovalSummary.Substring(0, Math.Min(400, result.ApprovalSummary.Length))}...");

            Console.WriteLine("\n打印数据结构:");
            var pd = result.PrintData!;
            Console.WriteLine($"  单号: {pd.FormNo}");
            Console.WriteLine($"  申请人: {pd.ApplicantName} - {pd.DepartmentName} ({pd.Level})");
            Console.WriteLine($"  费用项: {pd.ExpenseItems.Count}项, 发票: {pd.Invoices.Count}张");
            Console.WriteLine($"  行程: {pd.Trips.Count}个, 总天数: {pd.TotalDays}天");
            Console.WriteLine($"  报销总额: ¥{pd.TotalAmount:N2} = {pd.TotalAmountChinese}");
            Console.WriteLine($"  校验: 有效={pd.IsValid}, 错误={pd.ErrorCount}, 警告={pd.WarningCount}");
            Console.WriteLine($"  审批节点: {pd.ApprovalNodes.Count}个");
            foreach (var node in pd.ApprovalNodes)
            {
                Console.WriteLine($"    {node.Order}. {node.NodeName} [{node.NodeType}]");
            }
            Console.WriteLine($"  二维码内容: {pd.QRCodeContent}");
            Console.WriteLine($"  水印: {pd.Watermark}");

            string csv = facade.GenerateCsv(form);
            string csvFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "报销单导出.csv");
            File.WriteAllText(csvFile, csv);
            Console.WriteLine($"\nCSV报表已导出: {csvFile}");

            string html = facade.GenerateHtmlReport(form);
            string htmlFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "报销单打印.html");
            File.WriteAllText(htmlFile, html);
            Console.WriteLine($"HTML打印报表已导出: {htmlFile}");

            Console.WriteLine("\n审批意见模板（总经理）:");
            Console.WriteLine(facade.GetApprovalCommentTemplate(form, "总经理").Substring(0, 300) + "...");
        }
    }
}
