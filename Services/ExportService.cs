using System;
using System.Collections.Generic;
using System.Text;
using FinanceReimbursement.Models;
using FinanceReimbursement.Utils;

namespace FinanceReimbursement.Services
{
    public class ExportService
    {
        public PrintData GeneratePrintData(ReimbursementForm form,
            ValidationResult? validation = null)
        {
            var data = new PrintData();
            if (form == null) return data;

            data.FormNo = form.FormNo;
            data.Title = form.Title;
            data.CreateDate = form.CreateDate;
            data.SubmitDate = form.SubmitDate ?? form.CreateDate;
            data.ReimbursementType = form.ReimbursementType;
            data.Status = form.Status.GetDescription();

            if (form.Applicant != null)
            {
                data.ApplicantName = form.Applicant.Name;
                data.ApplicantId = form.Applicant.Id;
                data.DepartmentName = form.Applicant.DepartmentName;
                data.DepartmentId = form.Applicant.DepartmentId;
                data.Position = form.Applicant.Position;
                data.Level = form.Applicant.Level.GetDescription();
                data.BankAccount = form.Applicant.BankAccount;
                data.BankName = form.Applicant.BankName;
                data.ContactPhone = form.Applicant.Phone;
            }

            data.SubtotalAmount = form.SubtotalAmount;
            data.SubsidyAmount = form.SubsidyAmount;
            data.DeductionAmount = form.DeductionAmount;
            data.TotalAmount = form.TotalAmount;
            data.TotalAmountChinese = AmountConverter.ToChineseAmount(form.TotalAmount);
            data.Currency = form.Currency;
            data.ProjectName = form.ProjectName;
            data.Remarks = form.Remarks;

            if (form.Trips != null && form.Trips.Count > 0)
            {
                data.TotalDays = form.GetTotalDays();
                foreach (var trip in form.Trips)
                {
                    data.Trips.Add(new PrintTripData
                    {
                        TripNo = trip.TripNo,
                        Destination = trip.Destination,
                        DestinationCity = trip.DestinationCity,
                        CityLevel = trip.CityLevel?.GetDescription() ?? "",
                        DepartureDate = trip.DepartureDate,
                        ReturnDate = trip.ReturnDate,
                        Days = trip.Days,
                        Purpose = trip.Purpose,
                        ProjectName = trip.ProjectName
                    });
                }
            }

            if (form.ExpenseItems != null)
            {
                int seq = 1;
                foreach (var item in form.ExpenseItems)
                {
                    var printItem = new PrintExpenseItem
                    {
                        Sequence = seq,
                        Category = item.Category.GetDescription(),
                        Description = item.Description,
                        ExpenseDate = item.ExpenseDate,
                        Amount = item.Amount,
                        TaxAmount = item.TaxAmount,
                        TotalAmount = item.TotalAmount,
                        TransportationType = item.TransportationType?.GetDescription() ?? "",
                        FromLocation = item.FromLocation,
                        ToLocation = item.ToLocation,
                        ProjectName = item.ProjectName,
                        Remarks = item.Remarks,
                        InvoiceCount = item.Invoices?.Count ?? 0,
                        InvoiceTotal = item.InvoicesTotalAmount()
                    };
                    data.ExpenseItems.Add(printItem);
                    seq++;

                    if (item.Invoices != null)
                    {
                        foreach (var inv in item.Invoices)
                        {
                            data.Invoices.Add(new PrintInvoiceData
                            {
                                ItemSequence = printItem.Sequence,
                                InvoiceNo = inv.InvoiceNo,
                                InvoiceCode = inv.InvoiceCode,
                                InvoiceType = inv.Type.GetDescription(),
                                InvoiceDate = inv.InvoiceDate,
                                Amount = inv.Amount,
                                TaxAmount = inv.TaxAmount,
                                TotalAmount = inv.TotalAmount,
                                SellerName = inv.SellerName,
                                Category = inv.Category?.GetDescription() ?? "",
                                IsVerified = inv.IsVerified
                            });
                        }
                    }
                }
            }

            if (form.ApprovalNodes != null)
            {
                foreach (var node in form.ApprovalNodes)
                {
                    data.ApprovalNodes.Add(new PrintApprovalNode
                    {
                        Order = node.Order,
                        NodeName = node.NodeName,
                        NodeType = node.NodeType.GetDescription(),
                        ApproverName = node.ApproverName,
                        ApproverPosition = node.ApproverPosition
                    });
                }
            }

            if (validation != null)
            {
                data.IsValid = validation.IsValid;
                data.ErrorCount = validation.ErrorCount;
                data.WarningCount = validation.WarningCount;
                foreach (var msg in validation.Messages)
                {
                    data.ValidationMessages.Add(new PrintValidationMessage
                    {
                        Severity = msg.Severity.GetDescription(),
                        Message = msg.Message,
                        Suggestion = msg.Suggestion,
                        Category = msg.Category
                    });
                }
            }
            else
            {
                data.IsValid = true;
            }

            data.QRCodeContent = GenerateQRCodeContent(form);
            data.Watermark = $"{form.Applicant?.Name ?? ""} {DateTime.Now:yyyy-MM-dd} 报销单";

            return data;
        }

        public string GenerateCsv(ReimbursementForm form)
        {
            if (form == null) return "";

            var sb = new StringBuilder();

            sb.AppendLine("基本信息");
            sb.AppendLine($"单号,{form.FormNo}");
            sb.AppendLine($"标题,{CsvEscape(form.Title)}");
            sb.AppendLine($"申请人,{form.Applicant?.Name}");
            sb.AppendLine($"部门,{form.Applicant?.DepartmentName}");
            sb.AppendLine($"日期,{form.CreateDate:yyyy-MM-dd}");
            sb.AppendLine($"类型,{form.ReimbursementType}");
            sb.AppendLine("");

            if (form.Trips != null && form.Trips.Count > 0)
            {
                sb.AppendLine("行程信息");
                sb.AppendLine("序号,目的地,城市级别,出发日期,返回日期,天数,事由");
                int seq = 1;
                foreach (var trip in form.Trips)
                {
                    sb.AppendLine($"{seq},{CsvEscape(trip.Destination)},{trip.CityLevel?.GetDescription()}," +
                                  $"{trip.DepartureDate:yyyy-MM-dd},{trip.ReturnDate:yyyy-MM-dd},{trip.Days},{CsvEscape(trip.Purpose)}");
                    seq++;
                }
                sb.AppendLine("");
            }

            sb.AppendLine("费用明细");
            sb.AppendLine("序号,类别,描述,日期,金额,税额,合计,发票数,发票合计");
            int itemSeq = 1;
            foreach (var item in form.ExpenseItems)
            {
                sb.AppendLine($"{itemSeq},{item.Category.GetDescription()},{CsvEscape(item.Description)}," +
                              $"{item.ExpenseDate:yyyy-MM-dd},{item.Amount:F2},{item.TaxAmount:F2},{item.TotalAmount:F2}," +
                              $"{item.Invoices?.Count ?? 0},{item.InvoicesTotalAmount():F2}");
                itemSeq++;
            }
            sb.AppendLine("");

            sb.AppendLine("金额汇总");
            sb.AppendLine($"费用合计,{form.SubtotalAmount:F2}");
            sb.AppendLine($"补贴,{form.SubsidyAmount:F2}");
            sb.AppendLine($"扣减,{form.DeductionAmount:F2}");
            sb.AppendLine($"报销总额,{form.TotalAmount:F2}");
            sb.AppendLine($"大写金额,{AmountConverter.ToChineseAmount(form.TotalAmount)}");

            return sb.ToString();
        }

        public string GenerateHtmlReport(ReimbursementForm form, ValidationResult? validation = null)
        {
            var data = GeneratePrintData(form, validation);
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head>");
            sb.AppendLine("<meta charset='utf-8'/>");
            sb.AppendLine("<title>费用报销单 - " + data.FormNo + "</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:'Microsoft YaHei',Arial,sans-serif;padding:20px;color:#333}");
            sb.AppendLine(".container{max-width:900px;margin:0 auto;border:1px solid #ddd;padding:30px;background:#fff}");
            sb.AppendLine("h1{text-align:center;border-bottom:2px solid #333;padding-bottom:15px;margin-top:0}");
            sb.AppendLine("h2{color:#2c5282;border-left:4px solid #2c5282;padding-left:10px;margin-top:25px}");
            sb.AppendLine("table{width:100%;border-collapse:collapse;margin:15px 0}");
            sb.AppendLine("th,td{border:1px solid #ddd;padding:10px;text-align:left}");
            sb.AppendLine("th{background:#f7fafc;font-weight:600}");
            sb.AppendLine(".amount{text-align:right;font-weight:600}");
            sb.AppendLine(".total-row{background:#edf2f7;font-weight:700}");
            sb.AppendLine(".chinese-amount{color:#c53030;font-size:16px;font-weight:700}");
            sb.AppendLine(".meta-row{display:flex;justify-content:space-between;padding:5px 0}");
            sb.AppendLine(".meta-label{color:#718096;font-weight:600}");
            sb.AppendLine(".error{color:#c53030;background:#fed7d7;padding:8px 12px;border-radius:4px;margin:5px 0}");
            sb.AppendLine(".warning{color:#b7791f;background:#feebc8;padding:8px 12px;border-radius:4px;margin:5px 0}");
            sb.AppendLine(".info{color:#2b6cb0;background:#bee3f8;padding:8px 12px;border-radius:4px;margin:5px 0}");
            sb.AppendLine(".footer{margin-top:40px;display:flex;justify-content:space-between;border-top:1px dashed #ccc;padding-top:20px}");
            sb.AppendLine(".sign-area{text-align:center;width:200px}");
            sb.AppendLine(".sign-line{border-bottom:1px solid #333;margin:30px 0 5px;height:20px}");
            sb.AppendLine("@media print{body{padding:0}.container{border:none;padding:10px}}");
            sb.AppendLine("</style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine("<div class='container'>");
            sb.AppendLine("<h1>费 用 报 销 单</h1>");

            sb.AppendLine("<div class='meta-row'><span class='meta-label'>单号：</span><span>" + data.FormNo + "</span></div>");
            sb.AppendLine("<div class='meta-row'><span class='meta-label'>标题：</span><span>" + data.Title + "</span></div>");
            sb.AppendLine("<div class='meta-row'><span class='meta-label'>类型：</span><span>" + data.ReimbursementType + "</span></div>");
            sb.AppendLine("<div class='meta-row'><span class='meta-label'>申请人：</span><span>" + data.ApplicantName + " (" + data.DepartmentName + ")</span></div>");
            sb.AppendLine("<div class='meta-row'><span class='meta-label'>职位/级别：</span><span>" + data.Position + " / " + data.Level + "</span></div>");
            sb.AppendLine("<div class='meta-row'><span class='meta-label'>提交日期：</span><span>" + data.SubmitDate.ToString("yyyy-MM-dd") + "</span></div>");

            if (data.Trips.Count > 0)
            {
                sb.AppendLine("<h2>出差行程</h2>");
                sb.AppendLine("<table><thead><tr><th>序号</th><th>目的地</th><th>城市</th><th>出发日期</th><th>返回日期</th><th>天数</th><th>事由</th></tr></thead><tbody>");
                int tSeq = 1;
                foreach (var trip in data.Trips)
                {
                    sb.AppendLine($"<tr><td>{tSeq}</td><td>{trip.Destination}</td><td>{trip.DestinationCity}</td>" +
                                  $"<td>{trip.DepartureDate:yyyy-MM-dd}</td><td>{trip.ReturnDate:yyyy-MM-dd}</td>" +
                                  $"<td>{trip.Days}</td><td>{trip.Purpose}</td></tr>");
                    tSeq++;
                }
                sb.AppendLine("</tbody></table>");
            }

            sb.AppendLine("<h2>费用明细</h2>");
            sb.AppendLine("<table><thead><tr><th>序号</th><th>类别</th><th>描述</th><th>日期</th><th>金额</th><th>税额</th><th>合计</th><th>发票张数</th></tr></thead><tbody>");
            foreach (var item in data.ExpenseItems)
            {
                sb.AppendLine($"<tr><td>{item.Sequence}</td><td>{item.Category}</td><td>{item.Description}</td>" +
                              $"<td>{item.ExpenseDate:yyyy-MM-dd}</td>" +
                              $"<td class='amount'>{item.Amount:N2}</td><td class='amount'>{item.TaxAmount:N2}</td>" +
                              $"<td class='amount'>{item.TotalAmount:N2}</td><td>{item.InvoiceCount}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");

            sb.AppendLine("<h2>金额汇总</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine($"<tr><td style='width:70%'>费用合计</td><td class='amount'>¥{data.SubtotalAmount:N2}</td></tr>");
            sb.AppendLine($"<tr><td>出差补贴（+）</td><td class='amount'>¥{data.SubsidyAmount:N2}</td></tr>");
            sb.AppendLine($"<tr><td>超标扣减（-）</td><td class='amount'>¥{data.DeductionAmount:N2}</td></tr>");
            sb.AppendLine($"<tr class='total-row'><td>报销总额</td><td class='amount' style='font-size:18px'>¥{data.TotalAmount:N2}</td></tr>");
            sb.AppendLine($"<tr><td colspan='2' class='chinese-amount'>大写金额：{data.TotalAmountChinese}</td></tr>");
            sb.AppendLine("</table>");

            if (data.ValidationMessages.Count > 0)
            {
                sb.AppendLine("<h2>校验结果</h2>");
                foreach (var msg in data.ValidationMessages)
                {
                    string cls = msg.Severity == "错误" ? "error" : msg.Severity == "警告" ? "warning" : "info";
                    sb.AppendLine($"<div class='{cls}'>[{msg.Severity}] {msg.Message}");
                    if (!string.IsNullOrWhiteSpace(msg.Suggestion))
                    {
                        sb.AppendLine($"<br/><span style='font-size:12px;color:#666'>建议：{msg.Suggestion}</span>");
                    }
                    sb.AppendLine("</div>");
                }
            }

            sb.AppendLine("<h2>审批流程</h2>");
            sb.AppendLine("<table><thead><tr><th>顺序</th><th>审批节点</th><th>类型</th><th>审批人</th><th>签名</th></tr></thead><tbody>");
            foreach (var node in data.ApprovalNodes)
            {
                sb.AppendLine($"<tr><td>{node.Order}</td><td>{node.NodeName}</td><td>{node.NodeType}</td>" +
                              $"<td>{node.ApproverName}</td><td style='height:40px'>&nbsp;</td></tr>");
            }
            sb.AppendLine("</tbody></table>");

            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<div class='sign-area'><div class='sign-line'></div>申请人签字</div>");
            sb.AppendLine("<div class='sign-area'><div class='sign-line'></div>审核签字</div>");
            sb.AppendLine("<div class='sign-area'><div class='sign-line'></div>财务付款</div>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div></body></html>");
            return sb.ToString();
        }

        private static string GenerateQRCodeContent(ReimbursementForm form)
        {
            return $"REIMBURSEMENT|{form.FormNo}|{form.Applicant?.Id}|{form.TotalAmount:F2}|{form.CreateDate:yyyyMMdd}";
        }

        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }
    }

    public class PrintData
    {
        public string FormNo { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ReimbursementType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public DateTime SubmitDate { get; set; }

        public string ApplicantId { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;

        public decimal SubtotalAmount { get; set; }
        public decimal SubsidyAmount { get; set; }
        public decimal DeductionAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string TotalAmountChinese { get; set; } = string.Empty;
        public string Currency { get; set; } = "CNY";
        public int TotalDays { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;

        public bool IsValid { get; set; } = true;
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }

        public string QRCodeContent { get; set; } = string.Empty;
        public string Watermark { get; set; } = string.Empty;

        public List<PrintTripData> Trips { get; set; } = new List<PrintTripData>();
        public List<PrintExpenseItem> ExpenseItems { get; set; } = new List<PrintExpenseItem>();
        public List<PrintInvoiceData> Invoices { get; set; } = new List<PrintInvoiceData>();
        public List<PrintApprovalNode> ApprovalNodes { get; set; } = new List<PrintApprovalNode>();
        public List<PrintValidationMessage> ValidationMessages { get; set; } = new List<PrintValidationMessage>();
    }

    public class PrintTripData
    {
        public string TripNo { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string DestinationCity { get; set; } = string.Empty;
        public string CityLevel { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public DateTime ReturnDate { get; set; }
        public int Days { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
    }

    public class PrintExpenseItem
    {
        public int Sequence { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ExpenseDate { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string TransportationType { get; set; } = string.Empty;
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal InvoiceTotal { get; set; }
    }

    public class PrintInvoiceData
    {
        public int ItemSequence { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public string InvoiceCode { get; set; } = string.Empty;
        public string InvoiceType { get; set; } = string.Empty;
        public DateTime? InvoiceDate { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
    }

    public class PrintApprovalNode
    {
        public int Order { get; set; }
        public string NodeName { get; set; } = string.Empty;
        public string NodeType { get; set; } = string.Empty;
        public string ApproverName { get; set; } = string.Empty;
        public string ApproverPosition { get; set; } = string.Empty;
    }

    public class PrintValidationMessage
    {
        public string Severity { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
    }
}
