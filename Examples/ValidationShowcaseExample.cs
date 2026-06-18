/* ================================================================
 * 场景演示：各类校验错误（超标 / 缺票 / 重复发票 / 预算不足）
 * 展示如何把校验结果以用户友好的形式展示给员工。
 * ================================================================ */

using System;
using System.Collections.Generic;
using FinanceReimbursement;
using FinanceReimbursement.Models;

namespace FinanceReimbursement.Examples
{
    public static class ValidationShowcaseExample
    {
        public static void Run()
        {
            Console.WriteLine("=== 报销规则校验 - 错误展示演示 ===\n");

            // 准备：已有历史发票号 + 紧张的预算
            var usedInvoices = new List<string> { "DUP-GLOBAL-001" };
            var budgets = new List<Budget>
            {
                new Budget
                {
                    DepartmentId = "D002",
                    DepartmentName = "研发部",
                    Year = 2026,
                    Category = ExpenseCategory.Entertainment,
                    TotalBudget = 1000m,
                    UsedBudget = 900m
                }
            };

            var facade = new ReimbursementFacade(
                usedInvoiceNos: usedInvoices,
                budgets: budgets);

            // 构造一个"问题多多"的报销单
            var emp = new Employee
            {
                Id = "E999",
                Name = "王小二",
                DepartmentId = "D002",
                DepartmentName = "研发部",
                Level = EmployeeLevel.Junior
            };
            var form = facade.CreateForm(emp, "问题报销单 - 演示各种校验错误");

            // 一线+普通员工住超豪华酒店 → 住宿超标
            var trip = facade.AddTrip(form, "深圳", "深圳市",
                new DateTime(2026, 6, 1), new DateTime(2026, 6, 5),
                CityLevel.Tier1);
            var hotel = facade.AddExpense(form, ExpenseCategory.Accommodation,
                8000m, 0m, "超豪华套房", tripId: trip.Id);
            facade.AddInvoice(hotel, "HT-8888", InvoiceType.VATGeneral, 8000m);

            // 缺票：大额招待无发票
            facade.AddExpense(form, ExpenseCategory.Entertainment,
                3000m, 0m, "客户招待 - 忘了要发票");

            // 本单内重复发票号
            var m1 = facade.AddExpense(form, ExpenseCategory.Meal, 100m);
            var m2 = facade.AddExpense(form, ExpenseCategory.Meal, 200m);
            facade.AddInvoice(m1, "DUP-LOCAL-001", InvoiceType.Electronic, 100m);
            facade.AddInvoice(m2, "DUP-LOCAL-001", InvoiceType.Electronic, 200m);

            // 历史已报销发票号
            var old = facade.AddExpense(form, ExpenseCategory.Office, 500m);
            facade.AddInvoice(old, "DUP-GLOBAL-001", InvoiceType.Electronic, 500m);

            // 发票金额不足
            var ins = facade.AddExpense(form, ExpenseCategory.Office, 1000m, 0m, "办公采购");
            facade.AddInvoice(ins, "INS-001", InvoiceType.Electronic, 400m);

            Console.WriteLine("报销单构造完成（含住宿超标、缺票、重复发票、发票不足、预算不足）\n");

            // 执行校验
            var result = facade.ValidateAll(form);

            // 展示方式 1：总览文本
            Console.WriteLine("--- 展示方式1：总览文本 ---");
            Console.WriteLine(result.GetDisplayText());
            Console.WriteLine();

            // 展示方式 2：按类别分组
            Console.WriteLine("--- 展示方式2：按校验类别分组 ---");
            foreach (var kv in result.GetMessagesByCategory())
            {
                Console.WriteLine($"  【{kv.Key}】共 {kv.Value.Count} 条");
                foreach (var msg in kv.Value)
                {
                    Console.WriteLine($"    {msg.ToDisplayString()}");
                }
            }
            Console.WriteLine();

            // 展示方式 3：UI 绑定用的结构化项
            Console.WriteLine("--- 展示方式3：UI层结构化数据（可直接绑定前端） ---");
            foreach (var item in result.GetDisplayItems())
            {
                Console.WriteLine($"  {item.SeverityIcon} [{item.SeverityText}] {item.Message}");
                if (!string.IsNullOrWhiteSpace(item.Suggestion))
                    Console.WriteLine($"      建议：{item.Suggestion}");
            }
            Console.WriteLine();

            // 展示方式 4：仅错误列表
            if (!result.IsValid)
            {
                Console.WriteLine("--- 仅展示需修正的错误 ---");
                foreach (var err in result.GetErrors())
                {
                    Console.WriteLine($"  ❌ {err.Message}");
                    if (!string.IsNullOrWhiteSpace(err.Suggestion))
                        Console.WriteLine($"     建议：{err.Suggestion}");
                }
            }

            Console.WriteLine("\n=== 校验展示演示完毕 ===");
        }
    }
}
