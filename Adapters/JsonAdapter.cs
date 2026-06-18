using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using FinanceReimbursement.Models;
using FinanceReimbursement.Services;
using FinanceReimbursement.Utils;

namespace FinanceReimbursement.Adapters
{
    public static class JsonAdapter
    {
        public static string ExportForm(ReimbursementForm form)
        {
            if (form == null) return "{}";
            var sb = new StringBuilder();
            sb.AppendLine("{");
            AppendKeyValue(sb, "formNo", form.FormNo);
            AppendKeyValue(sb, "title", form.Title);
            AppendKeyValue(sb, "reimbursementType", form.ReimbursementType);
            AppendKeyValue(sb, "status", form.Status.GetDescription());
            AppendKeyValue(sb, "createDate", form.CreateDate.ToString("yyyy-MM-ddTHH:mm:ss"));
            AppendKeyValue(sb, "currency", form.Currency);
            AppendKeyValue(sb, "projectId", form.ProjectId);
            AppendKeyValue(sb, "projectName", form.ProjectName);
            AppendKeyValue(sb, "remarks", form.Remarks);

            sb.AppendLine("  \"applicant\": {");
            if (form.Applicant != null)
            {
                AppendKeyValue(sb, "id", form.Applicant.Id, 2);
                AppendKeyValue(sb, "name", form.Applicant.Name, 2);
                AppendKeyValue(sb, "departmentId", form.Applicant.DepartmentId, 2);
                AppendKeyValue(sb, "departmentName", form.Applicant.DepartmentName, 2);
                AppendKeyValue(sb, "level", form.Applicant.Level.GetDescription(), 2, true);
            }
            sb.AppendLine("  },");

            sb.AppendLine("  \"trips\": [");
            for (int i = 0; i < form.Trips.Count; i++)
            {
                var t = form.Trips[i];
                sb.AppendLine("    {");
                AppendKeyValue(sb, "id", t.Id, 3);
                AppendKeyValue(sb, "destination", t.Destination, 3);
                AppendKeyValue(sb, "destinationCity", t.DestinationCity, 3);
                AppendKeyValue(sb, "cityLevel", t.CityLevel?.GetDescription() ?? "", 3);
                AppendKeyValue(sb, "departureDate", t.DepartureDate.ToString("yyyy-MM-dd"), 3);
                AppendKeyValue(sb, "returnDate", t.ReturnDate.ToString("yyyy-MM-dd"), 3);
                AppendKeyValue(sb, "days", t.Days.ToString(), 3, true);
                AppendKeyValue(sb, "transportTo", t.TransportationTo?.GetDescription() ?? "", 3);
                AppendKeyValue(sb, "transportBack", t.TransportationBack?.GetDescription() ?? "", 3);
                AppendKeyValue(sb, "purpose", t.Purpose, 3, true);
                sb.AppendLine("    }" + (i < form.Trips.Count - 1 ? "," : ""));
            }
            sb.AppendLine("  ],");

            sb.AppendLine("  \"expenseItems\": [");
            for (int i = 0; i < form.ExpenseItems.Count; i++)
            {
                var e = form.ExpenseItems[i];
                sb.AppendLine("    {");
                AppendKeyValue(sb, "id", e.Id, 3);
                AppendKeyValue(sb, "category", e.Category.GetDescription(), 3);
                AppendKeyValue(sb, "categoryCode", ((int)e.Category).ToString(), 3);
                AppendKeyValue(sb, "description", e.Description, 3);
                AppendKeyValue(sb, "expenseDate", e.ExpenseDate.ToString("yyyy-MM-dd"), 3);
                AppendKeyValue(sb, "amount", e.Amount.ToString("F2"), 3);
                AppendKeyValue(sb, "taxAmount", e.TaxAmount.ToString("F2"), 3);
                AppendKeyValue(sb, "totalAmount", e.TotalAmount.ToString("F2"), 3);
                AppendKeyValue(sb, "tripId", e.TripId, 3);
                AppendKeyValue(sb, "projectId", e.ProjectId, 3);
                AppendKeyValue(sb, "transportationType", e.TransportationType?.GetDescription() ?? "", 3);

                sb.AppendLine("      \"invoices\": [");
                for (int j = 0; j < e.Invoices.Count; j++)
                {
                    var inv = e.Invoices[j];
                    sb.AppendLine("        {");
                    AppendKeyValue(sb, "invoiceNo", inv.InvoiceNo, 4);
                    AppendKeyValue(sb, "invoiceCode", inv.InvoiceCode, 4);
                    AppendKeyValue(sb, "type", inv.Type.GetDescription(), 4);
                    AppendKeyValue(sb, "typeCode", ((int)inv.Type).ToString(), 4);
                    AppendKeyValue(sb, "invoiceDate", inv.InvoiceDate?.ToString("yyyy-MM-dd") ?? "", 4);
                    AppendKeyValue(sb, "amount", inv.Amount.ToString("F2"), 4);
                    AppendKeyValue(sb, "taxAmount", inv.TaxAmount.ToString("F2"), 4);
                    AppendKeyValue(sb, "totalAmount", inv.TotalAmount.ToString("F2"), 4);
                    AppendKeyValue(sb, "sellerName", inv.SellerName, 4);
                    AppendKeyValue(sb, "isVerified", inv.IsVerified.ToString().ToLower(), 4, true);
                    sb.AppendLine("        }" + (j < e.Invoices.Count - 1 ? "," : ""));
                }
                sb.AppendLine("      ]" + (i < form.ExpenseItems.Count - 1 ? "," : ""));
                sb.AppendLine("    }" + (i < form.ExpenseItems.Count - 1 ? "," : ""));
            }
            sb.AppendLine("  ],");

            AppendKeyValue(sb, "subtotalAmount", form.SubtotalAmount.ToString("F2"));
            AppendKeyValue(sb, "subsidyAmount", form.SubsidyAmount.ToString("F2"));
            AppendKeyValue(sb, "deductionAmount", form.DeductionAmount.ToString("F2"));
            AppendKeyValue(sb, "totalAmount", form.TotalAmount.ToString("F2"));
            AppendKeyValue(sb, "totalAmountChinese", AmountConverter.ToChineseAmount(form.TotalAmount), true);

            sb.Append("}");
            return sb.ToString();
        }

        public static string ExportResultPackage(BatchProcessResult batchResult)
        {
            if (batchResult == null) return "{}";
            var sb = new StringBuilder();
            sb.AppendLine("{");
            AppendKeyValue(sb, "totalCount", batchResult.TotalCount.ToString());
            AppendKeyValue(sb, "validCount", batchResult.ValidCount.ToString());
            AppendKeyValue(sb, "invalidCount", batchResult.InvalidCount.ToString());
            AppendKeyValue(sb, "totalAmount", batchResult.TotalAmount.ToString("F2"));
            AppendKeyValue(sb, "totalAmountChinese", batchResult.TotalAmountChinese);
            AppendKeyValue(sb, "totalErrors", batchResult.TotalErrors.ToString());
            AppendKeyValue(sb, "totalWarnings", batchResult.TotalWarnings.ToString(), true);

            sb.AppendLine("  \"items\": [");
            for (int i = 0; i < batchResult.Items.Count; i++)
            {
                var item = batchResult.Items[i];
                sb.AppendLine("    {");
                AppendKeyValue(sb, "formNo", item.FormNo, 3);
                AppendKeyValue(sb, "title", item.Title, 3);
                AppendKeyValue(sb, "applicantName", item.ApplicantName, 3);
                AppendKeyValue(sb, "departmentName", item.DepartmentName, 3);
                AppendKeyValue(sb, "totalAmount", item.TotalAmount.ToString("F2"), 3);
                AppendKeyValue(sb, "chineseAmount", item.ChineseAmount, 3);
                AppendKeyValue(sb, "isValid", item.IsValid.ToString().ToLower(), 3);
                AppendKeyValue(sb, "hasWarnings", item.HasWarnings.ToString().ToLower(), 3, true);
                sb.AppendLine("    }" + (i < batchResult.Items.Count - 1 ? "," : ""));
            }
            sb.AppendLine("  ]");

            sb.Append("}");
            return sb.ToString();
        }

        public static string ExportEnhancedResult(EnhancedProcessResult result)
        {
            if (result == null) return "{}";
            var sb = new StringBuilder();
            sb.AppendLine("{");
            AppendKeyValue(sb, "chineseAmount", result.ChineseAmount);
            AppendKeyValue(sb, "netSubsidy", result.NetSubsidy.ToString("F2"));

            if (result.Validation != null)
            {
                AppendKeyValue(sb, "isValid", result.Validation.IsValid.ToString().ToLower());
                AppendKeyValue(sb, "errorCount", result.Validation.ErrorCount.ToString());
                AppendKeyValue(sb, "warningCount", result.Validation.WarningCount.ToString(), true);
            }

            if (result.ApprovalRecommendation != null)
            {
                sb.AppendLine("  \"approval\": {");
                var rec = result.ApprovalRecommendation;
                AppendKeyValue(sb, "riskLevel", rec.RiskLevel, 2);
                AppendKeyValue(sb, "totalNodes", rec.TotalNodes.ToString(), 2);
                AppendKeyValue(sb, "summary", rec.Summary, 2, true);
                sb.AppendLine("  }");
            }

            sb.Append("}");
            return sb.ToString();
        }

        public static ReimbursementForm ImportFromDictionary(Dictionary<string, object> data)
        {
            var form = new ReimbursementForm();
            if (data == null) return form;

            form.FormNo = GetString(data, "formNo") ?? GenerateFormNo();
            form.Title = GetString(data, "title") ?? "";
            form.ReimbursementType = GetString(data, "reimbursementType") ?? "差旅费";
            form.ProjectId = GetString(data, "projectId") ?? "";
            form.ProjectName = GetString(data, "projectName") ?? "";
            form.Remarks = GetString(data, "remarks") ?? "";
            form.Currency = GetString(data, "currency") ?? "CNY";
            form.Status = ReimbursementStatus.Draft;
            form.CreateDate = DateTime.Now;

            if (data.TryGetValue("applicant", out var appObj) && appObj is Dictionary<string, object> app)
            {
                form.Applicant = new Employee
                {
                    Id = GetString(app, "id") ?? "",
                    Name = GetString(app, "name") ?? "",
                    DepartmentId = GetString(app, "departmentId") ?? "",
                    DepartmentName = GetString(app, "departmentName") ?? "",
                    Level = ParseEnum<EmployeeLevel>(GetString(app, "level")),
                    Position = GetString(app, "position") ?? ""
                };
            }

            if (data.TryGetValue("trips", out var tripsObj) && tripsObj is List<Dictionary<string, object>> trips)
            {
                foreach (var t in trips)
                {
                    form.Trips.Add(new Trip
                    {
                        Id = GetString(t, "id") ?? Guid.NewGuid().ToString(),
                        Destination = GetString(t, "destination") ?? "",
                        DestinationCity = GetString(t, "destinationCity") ?? "",
                        CityLevel = ParseEnum<CityLevel>(GetString(t, "cityLevel")),
                        DepartureDate = ParseDate(GetString(t, "departureDate")),
                        ReturnDate = ParseDate(GetString(t, "returnDate")),
                        TransportationTo = ParseEnum<TransportationType>(GetString(t, "transportTo")),
                        TransportationBack = ParseEnum<TransportationType>(GetString(t, "transportBack")),
                        Purpose = GetString(t, "purpose") ?? "",
                        ProjectId = GetString(t, "projectId") ?? ""
                    });
                }
            }

            if (data.TryGetValue("expenseItems", out var expObj) && expObj is List<Dictionary<string, object>> exps)
            {
                foreach (var e in exps)
                {
                    var item = new ExpenseItem
                    {
                        Id = GetString(e, "id") ?? Guid.NewGuid().ToString(),
                        Category = ParseEnum<ExpenseCategory>(GetString(e, "category")),
                        Description = GetString(e, "description") ?? "",
                        ExpenseDate = ParseDate(GetString(e, "expenseDate")),
                        Amount = GetDecimal(e, "amount"),
                        TaxAmount = GetDecimal(e, "taxAmount"),
                        TripId = GetString(e, "tripId") ?? "",
                        ProjectId = GetString(e, "projectId") ?? "",
                        TransportationType = ParseEnum<TransportationType>(GetString(e, "transportationType"))
                    };

                    if (e.TryGetValue("invoices", out var invObj) && invObj is List<Dictionary<string, object>> invs)
                    {
                        foreach (var inv in invs)
                        {
                            item.Invoices.Add(new Invoice
                            {
                                InvoiceNo = GetString(inv, "invoiceNo") ?? "",
                                InvoiceCode = GetString(inv, "invoiceCode") ?? "",
                                Type = ParseEnum<InvoiceType>(GetString(inv, "type")),
                                InvoiceDate = ParseNullableDate(GetString(inv, "invoiceDate")),
                                Amount = GetDecimal(inv, "amount"),
                                TaxAmount = GetDecimal(inv, "taxAmount"),
                                SellerName = GetString(inv, "sellerName") ?? "",
                                IsVerified = GetBool(inv, "isVerified")
                            });
                        }
                    }

                    form.ExpenseItems.Add(item);
                }
            }

            return form;
        }

        public static List<Dictionary<string, object>> ImportFromTable(List<Dictionary<string, string>> rows,
            string formNo = "", string title = "", Employee? applicant = null)
        {
            var grouped = new Dictionary<string, List<Dictionary<string, string>>>();

            foreach (var row in rows)
            {
                var key = GetString(row, "formNo");
                if (string.IsNullOrWhiteSpace(key)) key = formNo;
                if (!grouped.ContainsKey(key)) grouped[key] = new List<Dictionary<string, string>>();
                grouped[key].Add(row);
            }

            var result = new List<Dictionary<string, object>>();
            foreach (var kv in grouped)
            {
                var formData = new Dictionary<string, object>();
                formData["formNo"] = kv.Key;
                formData["title"] = title;
                formData["reimbursementType"] = "差旅费";

                if (applicant != null)
                {
                    var appData = new Dictionary<string, object>();
                    appData["id"] = applicant.Id;
                    appData["name"] = applicant.Name;
                    appData["departmentId"] = applicant.DepartmentId;
                    appData["departmentName"] = applicant.DepartmentName;
                    appData["level"] = applicant.Level.GetDescription();
                    formData["applicant"] = appData;
                }

                var expItems = new List<Dictionary<string, object>>();
                foreach (var row in kv.Value)
                {
                    var exp = new Dictionary<string, object>();
                    exp["category"] = GetString(row, "category") ?? GetString(row, "费用类别") ?? "其他";
                    exp["description"] = GetString(row, "description") ?? GetString(row, "描述") ?? "";
                    exp["expenseDate"] = GetString(row, "expenseDate") ?? GetString(row, "日期") ?? DateTime.Now.ToString("yyyy-MM-dd");
                    exp["amount"] = GetString(row, "amount") ?? GetString(row, "金额") ?? "0";
                    exp["taxAmount"] = GetString(row, "taxAmount") ?? GetString(row, "税额") ?? "0";
                    exp["tripId"] = GetString(row, "tripId") ?? "";
                    exp["projectId"] = GetString(row, "projectId") ?? GetString(row, "项目编号") ?? "";

                    var invNo = GetString(row, "invoiceNo") ?? GetString(row, "发票号") ?? "";
                    if (!string.IsNullOrWhiteSpace(invNo))
                    {
                        var inv = new Dictionary<string, object>();
                        inv["invoiceNo"] = invNo;
                        inv["type"] = GetString(row, "invoiceType") ?? GetString(row, "发票类型") ?? "电子发票";
                        inv["amount"] = exp["amount"];
                        inv["taxAmount"] = exp["taxAmount"];
                        inv["sellerName"] = GetString(row, "sellerName") ?? GetString(row, "开票方") ?? "";
                        inv["isVerified"] = "false";
                        exp["invoices"] = new List<Dictionary<string, object>> { inv };
                    }
                    expItems.Add(exp);
                }
                formData["expenseItems"] = expItems;
                result.Add(formData);
            }

            return result;
        }

        private static string? GetString(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != null) return val.ToString();
            return null;
        }

        private static string? GetString(Dictionary<string, string> dict, string key)
        {
            if (dict.TryGetValue(key, out var val) && val != null) return val;
            return null;
        }

        private static decimal GetDecimal(Dictionary<string, object> dict, string key)
        {
            var s = GetString(dict, key);
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
            return 0;
        }

        private static bool GetBool(Dictionary<string, object> dict, string key)
        {
            var s = GetString(dict, key);
            return s?.ToLower() == "true";
        }

        private static DateTime ParseDate(string? s)
        {
            if (DateTime.TryParse(s, out var d)) return d;
            return DateTime.Now;
        }

        private static DateTime? ParseNullableDate(string? s)
        {
            if (DateTime.TryParse(s, out var d)) return d;
            return null;
        }

        private static T ParseEnum<T>(string? s) where T : struct
        {
            if (string.IsNullOrWhiteSpace(s)) return default;
            if (Enum.TryParse<T>(s, true, out var v)) return v;
            foreach (T val in Enum.GetValues(typeof(T)))
            {
                if (val.GetDescription() == s) return val;
            }
            return default;
        }

        private static string GenerateFormNo() => $"BX{DateTime.Now:yyyyMMddHHmmssfff}";

        private static void AppendKeyValue(StringBuilder sb, string key, string value, int indent = 1, bool isLast = false)
        {
            var pad = new string(' ', indent * 2);
            var escaped = (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
            sb.AppendLine($"{pad}\"{key}\": \"{escaped}\"{(isLast ? "" : ",")}");
        }
    }
}
