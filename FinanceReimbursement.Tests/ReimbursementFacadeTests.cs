using System;
using System.Collections.Generic;
using Xunit;
using FinanceReimbursement;
using FinanceReimbursement.Models;

namespace FinanceReimbursement.Tests
{
    public class ReimbursementFacadeTests
    {
        private ReimbursementFacade CreateFacade()
        {
            return new ReimbursementFacade();
        }

        private Employee CreateTestEmployee(EmployeeLevel level = EmployeeLevel.Manager)
        {
            return new Employee
            {
                Id = "EMP001",
                Name = "张三",
                DepartmentId = "DEP001",
                DepartmentName = "技术部",
                Level = level,
                Position = "开发经理",
                Email = "zhangsan@example.com",
                Phone = "13800138000",
                BankAccount = "6222021234567890",
                BankName = "工商银行",
                SupervisorId = "EMP002"
            };
        }

        [Fact]
        public void CreateForm_ShouldSetBasicProperties()
        {
            var facade = CreateFacade();
            var emp = CreateTestEmployee();

            var form = facade.CreateForm(emp, "北京出差报销", "差旅费", "PROJ001", "智慧城市项目");

            Assert.NotNull(form);
            Assert.StartsWith("BX", form.FormNo);
            Assert.Equal("北京出差报销", form.Title);
            Assert.Equal("张三", form.Applicant.Name);
            Assert.Equal("技术部", form.DepartmentName);
            Assert.Equal(ReimbursementStatus.Draft, form.Status);
            Assert.Equal("PROJ001", form.ProjectId);
        }

        [Fact]
        public void AddTrip_ShouldAddCorrectly()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee());
            var departure = new DateTime(2026, 6, 1);
            var returnDate = new DateTime(2026, 6, 5);

            var trip = facade.AddTrip(form, "北京", "北京市",
                departure, returnDate,
                CityLevel.Tier1,
                TransportationType.Airplane,
                TransportationType.Airplane,
                "项目现场调研", "PROJ001", "重要客户拜访");

            Assert.Single(form.Trips);
            Assert.Equal("北京", trip.Destination);
            Assert.Equal(5, trip.Days);
            Assert.Equal(CityLevel.Tier1, trip.CityLevel);
        }

        [Fact]
        public void AddExpense_ShouldAddCorrectly()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee());
            var trip = facade.AddTrip(form, "北京", "北京市",
                new DateTime(2026, 6, 1), new DateTime(2026, 6, 5), CityLevel.Tier1);

            var item = facade.AddExpense(form,
                ExpenseCategory.Accommodation,
                2800m, 0m,
                "北京XX酒店住宿4晚",
                new DateTime(2026, 6, 1),
                trip.Id,
                projectName: "智慧城市项目");

            Assert.Single(form.ExpenseItems);
            Assert.Equal(2800m, item.Amount);
            Assert.Equal(ExpenseCategory.Accommodation, item.Category);
            Assert.Equal(trip.Id, item.TripId);
            Assert.Equal(2800m, form.SubtotalAmount);
        }

        [Fact]
        public void AddInvoice_ShouldAssociateWithExpense()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee());
            var item = facade.AddExpense(form, ExpenseCategory.Transportation, 1200m, 108m);

            var invoice = facade.AddInvoice(item,
                "INV2026060001",
                InvoiceType.VATSpecial,
                1200m, 108m,
                "CODE001",
                new DateTime(2026, 6, 1),
                "XX航空公司",
                "TAX0011223344",
                "我司名称",
                "TAX9988776655",
                ExpenseCategory.Transportation,
                "机票",
                true,
                "验真通过");

            Assert.Single(item.Invoices);
            Assert.Equal("INV2026060001", invoice.InvoiceNo);
            Assert.Equal(1308m, invoice.TotalAmount);
            Assert.True(invoice.HasValidTaxDeduction());
        }

        [Fact]
        public void CalculateSubsidy_ShouldCalculateCorrectly()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee(EmployeeLevel.Manager));

            facade.AddTrip(form, "北京", "北京市",
                new DateTime(2026, 6, 1), new DateTime(2026, 6, 5),
                CityLevel.Tier1);

            var subsidy = facade.CalculateSubsidy(form, true);

            Assert.True(subsidy > 0);
            Assert.Equal(subsidy, form.SubsidyAmount - form.DeductionAmount);
        }

        [Fact]
        public void GetSubsidyBreakdown_ShouldReturnDetails()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee(EmployeeLevel.Junior));

            facade.AddTrip(form, "上海", "上海市",
                new DateTime(2026, 6, 10), new DateTime(2026, 6, 12),
                CityLevel.Tier1);

            var breakdown = facade.GetSubsidyBreakdown(form);

            Assert.NotNull(breakdown);
            Assert.Equal(3, breakdown.TotalDays);
            Assert.Single(breakdown.Trips);
            Assert.True(breakdown.TotalAmount > 0);
        }

        [Fact]
        public void ValidateInvoices_ShouldDetectMissingInvoice()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee());

            facade.AddExpense(form, ExpenseCategory.Accommodation, 1000m, 0m, "无票测试");

            var validation = facade.ValidateInvoices(form);

            Assert.False(validation.IsValid);
            Assert.True(validation.ErrorCount > 0);
            Assert.Contains(validation.GetErrors(), m => m.Code == "INVOICE_MISSING");
        }

        [Fact]
        public void ValidateInvoices_ShouldDetectDuplicateInSameForm()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee());

            var item1 = facade.AddExpense(form, ExpenseCategory.Meal, 300m);
            var item2 = facade.AddExpense(form, ExpenseCategory.Meal, 400m);

            facade.AddInvoice(item1, "DUP001", InvoiceType.Electronic, 300m);
            facade.AddInvoice(item2, "DUP001", InvoiceType.Electronic, 400m);

            var validation = facade.ValidateInvoices(form);

            Assert.False(validation.IsValid);
            Assert.Contains(validation.GetErrors(), m => m.Code == "INVOICE_DUPLICATE_LOCAL");
        }

        [Fact]
        public void ValidateInvoices_ShouldDetectDuplicateInGlobal()
        {
            var used = new List<string> { "GLOBAL001" };
            var facade = new ReimbursementFacade(usedInvoiceNos: used);
            var form = facade.CreateForm(CreateTestEmployee());

            var item = facade.AddExpense(form, ExpenseCategory.Office, 500m);
            facade.AddInvoice(item, "GLOBAL001", InvoiceType.VATGeneral, 500m);

            var validation = facade.ValidateInvoices(form);

            Assert.False(validation.IsValid);
            Assert.Contains(validation.GetErrors(), m => m.Code == "INVOICE_DUPLICATE_GLOBAL");
        }

        [Fact]
        public void ValidateStandard_ShouldDetectAccommodationOverLimit()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee(EmployeeLevel.Junior));
            var trip = facade.AddTrip(form, "北京", "北京市",
                new DateTime(2026, 6, 1), new DateTime(2026, 6, 5),
                CityLevel.Tier1);

            facade.AddExpense(form, ExpenseCategory.Accommodation,
                5000m, 0m, "豪华酒店", tripId: trip.Id);

            var validation = facade.ValidateStandard(form);

            Assert.True(validation.HasWarnings);
            Assert.Contains(validation.GetWarnings(), m => m.Code == "ACCOMMODATION_OVER_LIMIT");
        }

        [Fact]
        public void BudgetCheck_ShouldDetectExceeded()
        {
            var budgets = new List<Budget>
            {
                new Budget
                {
                    DepartmentId = "DEP001",
                    DepartmentName = "技术部",
                    TotalBudget = 10000m,
                    UsedBudget = 9000m,
                    Year = 2026
                }
            };
            var facade = new ReimbursementFacade(budgets: budgets);
            var form = facade.CreateForm(CreateTestEmployee());
            facade.AddExpense(form, ExpenseCategory.Meal, 5000m);

            var validation = facade.CheckBudget(form);

            Assert.False(validation.IsValid);
            Assert.Contains(validation.GetErrors(), m => m.Code == "TOTAL_BUDGET_EXCEEDED");
        }

        [Fact]
        public void ApprovalNodes_ShouldIncreaseWithAmount()
        {
            var facade = CreateFacade();

            var smallForm = facade.CreateForm(CreateTestEmployee());
            facade.AddExpense(smallForm, ExpenseCategory.Office, 500m);
            smallForm.SubsidyAmount = 0;
            var smallNodes = facade.GetApprovalNodes(smallForm);

            var largeForm = facade.CreateForm(CreateTestEmployee());
            facade.AddExpense(largeForm, ExpenseCategory.Entertainment, 80000m);
            largeForm.SubsidyAmount = 0;
            var largeNodes = facade.GetApprovalNodes(largeForm);

            Assert.True(largeNodes.Count > smallNodes.Count);
            Assert.Contains(largeNodes, n => n.NodeType == ApprovalNodeType.CEO);
        }

        [Fact]
        public void AllocateByDepartment_CustomRatios()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee());
            facade.AddExpense(form, ExpenseCategory.Transportation, 1000m);

            var ratios = new Dictionary<string, decimal>
            {
                { "DEP001", 60 },
                { "DEP002", 40 }
            };

            var alloc = facade.AllocateByDepartment(form, ratios);

            Assert.Equal(2, alloc.Count);
            Assert.Equal(600m, alloc["DEP001"]);
            Assert.Equal(400m, alloc["DEP002"]);
        }

        [Fact]
        public void AllocateByProject_AutoFromExpenseItems()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee());
            facade.AddExpense(form, ExpenseCategory.Transportation, 1000m,
                projectId: "P1", projectName: "项目A");
            facade.AddExpense(form, ExpenseCategory.Meal, 500m,
                projectId: "P2", projectName: "项目B");

            var alloc = facade.AllocateByProject(form);

            Assert.Equal(2, alloc.Count);
            Assert.Equal(1000m, alloc["P1"]);
            Assert.Equal(500m, alloc["P2"]);
        }

        [Fact]
        public void GenerateApprovalSummary_ShouldContainKeyInfo()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee(), "测试报销单");
            facade.AddTrip(form, "北京", "北京市",
                new DateTime(2026, 6, 1), new DateTime(2026, 6, 3), CityLevel.Tier1);
            facade.AddExpense(form, ExpenseCategory.Accommodation, 1000m);

            var summary = facade.GenerateApprovalSummary(form);

            Assert.Contains("测试报销单", summary);
            Assert.Contains("张三", summary);
            Assert.Contains("北京", summary);
            Assert.Contains("审批流程", summary);
        }

        [Fact]
        public void AmountToChinese_Integration()
        {
            var facade = CreateFacade();
            var result = facade.AmountToChinese(12345.67m);
            Assert.Equal("壹万贰仟叁佰肆拾伍元陆角柒分", result);
        }

        [Fact]
        public void ProcessFull_ShouldReturnAllResults()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee(), "完整流程测试");
            var trip = facade.AddTrip(form, "上海", "上海市",
                new DateTime(2026, 6, 15), new DateTime(2026, 6, 17),
                CityLevel.Tier1);
            var item = facade.AddExpense(form, ExpenseCategory.Accommodation,
                1500m, 0m, tripId: trip.Id);
            facade.AddInvoice(item, "FULL001", InvoiceType.Electronic, 1500m);

            var result = facade.ProcessFull(form);

            Assert.NotNull(result);
            Assert.NotNull(result.Validation);
            Assert.NotNull(result.ApprovalNodes);
            Assert.NotEmpty(result.ApprovalSummary);
            Assert.NotNull(result.PrintData);
            Assert.NotEmpty(result.ChineseAmount);
            Assert.NotEmpty(result.DepartmentAllocation);
        }

        [Fact]
        public void GeneratePrintData_ShouldHaveAllSections()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee(), "打印测试");
            var trip = facade.AddTrip(form, "广州", "广州市",
                new DateTime(2026, 6, 20), new DateTime(2026, 6, 22),
                CityLevel.Tier1);
            var item = facade.AddExpense(form, ExpenseCategory.Transportation,
                800m, 72m, tripId: trip.Id);
            facade.AddInvoice(item, "PRINT001", InvoiceType.VATSpecial,
                800m, 72m, sellerName: "XX航司");

            var data = facade.GeneratePrintData(form);

            Assert.Equal("打印测试", data.Title);
            Assert.Single(data.Trips);
            Assert.Single(data.ExpenseItems);
            Assert.Single(data.Invoices);
            Assert.Contains("元整", data.TotalAmountChinese);
        }

        [Fact]
        public void GenerateHtmlReport_ShouldBeValidHtml()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee(), "HTML报告测试");
            facade.AddExpense(form, ExpenseCategory.Office, 300m);

            var html = facade.GenerateHtmlReport(form);

            Assert.StartsWith("<!DOCTYPE html>", html);
            Assert.Contains("</html>", html);
            Assert.Contains("HTML报告测试", html);
        }

        [Fact]
        public void GenerateCsv_ShouldContainHeaders()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee(), "CSV测试");
            facade.AddExpense(form, ExpenseCategory.Meal, 200m);

            var csv = facade.GenerateCsv(form);

            Assert.Contains("基本信息", csv);
            Assert.Contains("费用明细", csv);
            Assert.Contains("金额汇总", csv);
        }

        [Fact]
        public void SummarizeByDepartment_AggregatesMultipleForms()
        {
            var facade = CreateFacade();
            var forms = new List<ReimbursementForm>();

            var emp1 = CreateTestEmployee();
            emp1.DepartmentId = "D1";
            emp1.DepartmentName = "销售部";
            var f1 = facade.CreateForm(emp1);
            facade.AddExpense(f1, ExpenseCategory.Entertainment, 2000m);
            forms.Add(f1);

            var emp2 = CreateTestEmployee();
            emp2.DepartmentId = "D1";
            emp2.DepartmentName = "销售部";
            var f2 = facade.CreateForm(emp2);
            facade.AddExpense(f2, ExpenseCategory.Transportation, 1000m);
            forms.Add(f2);

            var emp3 = CreateTestEmployee();
            emp3.DepartmentId = "D2";
            emp3.DepartmentName = "研发部";
            var f3 = facade.CreateForm(emp3);
            facade.AddExpense(f3, ExpenseCategory.Office, 500m);
            forms.Add(f3);

            var summary = facade.SummarizeByDepartment(forms);

            Assert.Equal(2, summary.Count);
            var dept1 = summary.Find(s => s.DepartmentId == "D1");
            Assert.NotNull(dept1);
            Assert.Equal(2, dept1.FormCount);
            Assert.Equal(3000m, dept1.TotalAmount);
        }

        [Fact]
        public void GetApprovalCommentTemplate_ShouldMatchRole()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee(), "模板测试");
            facade.AddExpense(form, ExpenseCategory.Training, 60000m);

            var template = facade.GetApprovalCommentTemplate(form, "总经理");

            Assert.Contains("总经理", template);
            Assert.Contains("可选操作", template);
        }

        [Fact]
        public void FormComputedProperties_SumCorrectly()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee());

            facade.AddExpense(form, ExpenseCategory.Meal, 100m, 6m);
            facade.AddExpense(form, ExpenseCategory.Transportation, 200m, 18m);

            Assert.Equal(324m, form.SubtotalAmount);
            Assert.Equal(2, form.ExpenseItems.Count);

            var categorySummary = form.GetCategorySummary();
            Assert.Equal(106m, categorySummary[ExpenseCategory.Meal]);
            Assert.Equal(218m, categorySummary[ExpenseCategory.Transportation]);
        }

        [Fact]
        public void RegisterUsedInvoices_ShouldPersist()
        {
            var facade = CreateFacade();
            var form = facade.CreateForm(CreateTestEmployee());
            var item = facade.AddExpense(form, ExpenseCategory.Office, 200m);
            facade.AddInvoice(item, "REG001", InvoiceType.Electronic, 200m);

            facade.RegisterUsedInvoices(new[] { "REG001" });

            var form2 = facade.CreateForm(CreateTestEmployee());
            var item2 = facade.AddExpense(form2, ExpenseCategory.Office, 200m);
            facade.AddInvoice(item2, "REG001", InvoiceType.Electronic, 200m);

            var validation = facade.ValidateInvoices(form2);
            Assert.Contains(validation.GetErrors(), m => m.Code == "INVOICE_DUPLICATE_GLOBAL");
        }
    }
}
