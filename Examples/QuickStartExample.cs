/* ================================================================
 * 财务报销类库 - 极简接入示例
 * 演示：从建单 → 加行程 → 加费用/发票 → 计算补贴
 *       → 校验规则 → 生成审批摘要 → 生成打印数据
 * ================================================================
 *
 * 业务系统接入只需 4 步：
 *   1. new ReimbursementFacade() 初始化（可传入公司自定义标准、预算、历史发票号）
 *   2. 用 CreateForm / AddTrip / AddExpense / AddInvoice 填写内容
 *   3. 调用 ValidateAll() 或 ProcessFull() 得到校验结果
 *   4. 取 ChineseAmount / ApprovalSummary / PrintData / HtmlReport 用于展示和打印
 *
 * ================================================================ */

using System;
using System.Collections.Generic;
using FinanceReimbursement;
using FinanceReimbursement.Models;

namespace FinanceReimbursement.Examples
{
    public static class QuickStartExample
    {
        public static void Run()
        {
            Console.WriteLine("=== 财务报销类库 - 快速接入示例 ===\n");

            // （可选）准备预算数据和历史已用发票号
            var budgets = new List<Budget>
            {
                new Budget
                {
                    DepartmentId = "D001",
                    DepartmentName = "销售部",
                    Year = 2026,
                    TotalBudget = 50000m,
                    UsedBudget = 5000m
                }
            };
            var usedInvoices = new List<string> { "OLD-0001", "OLD-0002" };

            // 第 1 步：初始化门面（不传参数则使用默认企业级标准）
            var facade = new ReimbursementFacade(
                standard: null,
                approvalRule: null,
                usedInvoiceNos: usedInvoices,
                budgets: budgets);

            // 第 2 步：准备申请人
            var employee = new Employee
            {
                Id = "E001",
                Name = "张伟",
                DepartmentId = "D001",
                DepartmentName = "销售部",
                Level = EmployeeLevel.Manager,
                Position = "销售经理",
                BankAccount = "6222021234567890",
                BankName = "工商银行"
            };

            // 第 3 步：创建报销单
            var form = facade.CreateForm(
                applicant: employee,
                title: "6月上海客户拜访差旅费",
                reimbursementType: "差旅费",
                projectId: "P-SH-2026",
                projectName: "上海华东区拓展项目");

            Console.WriteLine($"[1] 已创建报销单：{form.FormNo} / {form.Title}");
            Console.WriteLine($"    申请人：{form.Applicant.Name}（{form.Applicant.DepartmentName}）");
            Console.WriteLine();

            // 第 4 步：添加行程
            var trip = facade.AddTrip(
                form: form,
                destination: "上海浦东新区",
                destinationCity: "上海市",
                departureDate: new DateTime(2026, 6, 15),
                returnDate: new DateTime(2026, 6, 17),
                cityLevel: CityLevel.Tier1,
                transportTo: TransportationType.HighSpeedRail,
                transportBack: TransportationType.HighSpeedRail,
                purpose: "拜访3家客户洽谈Q3合作",
                projectId: "P-SH-2026");

            Console.WriteLine($"[2] 已添加行程：{trip.Destination}，{trip.Days} 天");
            Console.WriteLine();

            // 第 5 步：添加费用明细 + 发票
            // 5a. 往返高铁
            var train = facade.AddExpense(form,
                category: ExpenseCategory.Transportation,
                amount: 1100m, taxAmount: 99m,
                description: "北京↔上海 高铁二等座往返",
                expenseDate: new DateTime(2026, 6, 15),
                tripId: trip.Id,
                transportType: TransportationType.HighSpeedRail,
                fromLocation: "北京", toLocation: "上海",
                projectId: "P-SH-2026");
            facade.AddInvoice(train,
                invoiceNo: "INV-RAIL-20260615",
                type: InvoiceType.Electronic,
                amount: 1100m, taxAmount: 99m,
                sellerName: "中国铁路",
                invoiceDate: new DateTime(2026, 6, 15));

            // 5b. 酒店住宿 2 晚
            var hotel = facade.AddExpense(form,
                category: ExpenseCategory.Accommodation,
                amount: 1400m, taxAmount: 84m,
                description: "上海XX商务酒店 标准间2晚",
                expenseDate: new DateTime(2026, 6, 15),
                tripId: trip.Id,
                projectId: "P-SH-2026");
            facade.AddInvoice(hotel,
                invoiceNo: "INV-HOTEL-20260615",
                type: InvoiceType.VATSpecial,
                amount: 1400m, taxAmount: 84m,
                sellerName: "上海XX酒店有限公司",
                invoiceDate: new DateTime(2026, 6, 17),
                isVerified: true, verifyResult: "发票验真通过");

            // 5c. 餐饮
            var meal = facade.AddExpense(form,
                category: ExpenseCategory.Meal,
                amount: 450m, taxAmount: 27m,
                description: "出差期间工作餐",
                expenseDate: new DateTime(2026, 6, 16),
                tripId: trip.Id,
                projectId: "P-SH-2026");
            facade.AddInvoice(meal,
                invoiceNo: "INV-MEAL-20260616",
                type: InvoiceType.Electronic,
                amount: 450m, taxAmount: 27m,
                sellerName: "上海XX餐饮管理有限公司");

            // 5d. 市内出租车
            var taxi = facade.AddExpense(form,
                category: ExpenseCategory.Transportation,
                amount: 180m, taxAmount: 0m,
                description: "市内出租车",
                expenseDate: new DateTime(2026, 6, 16),
                tripId: trip.Id,
                transportType: TransportationType.Taxi);
            facade.AddInvoice(taxi,
                invoiceNo: "INV-TAXI-001",
                type: InvoiceType.FixedAmount,
                amount: 180m);

            Console.WriteLine($"[3] 已添加 {form.ExpenseItems.Count} 项费用，{form.GetInvoiceCount()} 张发票");
            Console.WriteLine($"    费用合计：¥{form.SubtotalAmount:N2}");
            Console.WriteLine();

            // 第 6 步：计算出差补贴（自动写回报销单）
            var netSubsidy = facade.CalculateSubsidy(form, autoApply: true);
            Console.WriteLine($"[4] 出差补贴：+¥{form.SubsidyAmount:N2}，超标扣减：-¥{form.DeductionAmount:N2}，净补贴：¥{netSubsidy:N2}");
            Console.WriteLine($"    报销总额：¥{form.TotalAmount:N2} → {facade.AmountToChinese(form.TotalAmount)}");
            Console.WriteLine();

            // 第 7 步：执行校验（员工可直接看到友好提示）
            var validation = facade.ValidateAll(form);

            Console.WriteLine("[5] 规则校验结果：");
            Console.WriteLine(validation.GetDisplayText());
            Console.WriteLine();

            if (!validation.IsValid)
            {
                // 有错误时可单独展示
                Console.WriteLine("  需员工修正的错误：");
                foreach (var msg in validation.GetDisplayMessages())
                {
                    Console.WriteLine($"    {msg}");
                }
            }

            // 第 8 步：生成审批节点建议
            var nodes = facade.GetApprovalNodes(form);
            Console.WriteLine("[6] 建议审批节点：");
            foreach (var n in nodes)
            {
                Console.WriteLine($"    {n.Order}. {n.NodeName} — {n.RuleDescription}");
            }
            Console.WriteLine();

            // 第 9 步：生成审批摘要（审批人查看）
            var summary = facade.GenerateApprovalSummary(form, validation);
            Console.WriteLine("[7] 审批摘要：");
            Console.WriteLine(summary);
            Console.WriteLine();

            // 第 10 步：按部门/项目分摊
            var deptAlloc = facade.AllocateByDepartment(form);
            var projAlloc = facade.AllocateByProject(form);
            Console.WriteLine("[8] 费用分摊：");
            foreach (var kv in deptAlloc)
                Console.WriteLine($"    部门 {kv.Key}：¥{kv.Value:N2}");
            foreach (var kv in projAlloc)
                Console.WriteLine($"    项目 {kv.Key}：¥{kv.Value:N2}");
            Console.WriteLine();

            // 第 11 步：生成打印数据（可映射到打印模板）
            var printData = facade.GeneratePrintData(form, validation);
            Console.WriteLine("[9] 打印数据就绪：");
            Console.WriteLine($"    单号：{printData.FormNo}");
            Console.WriteLine($"    总额：¥{printData.TotalAmount:N2} = {printData.TotalAmountChinese}");
            Console.WriteLine($"    行程：{printData.Trips.Count} 个，费用：{printData.ExpenseItems.Count} 项，发票：{printData.Invoices.Count} 张");
            Console.WriteLine($"    审批节点：{printData.ApprovalNodes.Count} 个");
            Console.WriteLine($"    校验：有效={printData.IsValid}，错误={printData.ErrorCount}，警告={printData.WarningCount}");
            Console.WriteLine();

            // 第 12 步：一键导出 HTML 打印报表（可直接浏览器打开/打印）
            var html = facade.GenerateHtmlReport(form, validation);
            Console.WriteLine($"[10] 已生成 HTML 报表，长度 {html.Length} 字符");

            var csv = facade.GenerateCsv(form);
            Console.WriteLine($"     已生成 CSV 报表，长度 {csv.Length} 字符");

            // （推荐）若想一步到位，直接调用 ProcessFull：
            // var full = facade.ProcessFull(form);
            // full.Validation / full.ApprovalSummary / full.PrintData / full.ChineseAmount ...

            Console.WriteLine("\n=== 示例执行完毕 ===");
        }
    }
}
