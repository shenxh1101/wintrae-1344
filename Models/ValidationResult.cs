using System;
using System.Collections.Generic;
using System.Text;
using FinanceReimbursement.Utils;

namespace FinanceReimbursement.Models
{
    public class ValidationMessage
    {
        public string Code { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;

        public string Category { get; set; } = string.Empty;

        public string RelatedItemId { get; set; } = string.Empty;

        public string RelatedItemType { get; set; } = string.Empty;

        public decimal? ExpectedValue { get; set; }

        public decimal? ActualValue { get; set; }

        public string Suggestion { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string SeverityText => Severity.GetDescription();

        public string SeverityIcon
        {
            get
            {
                switch (Severity)
                {
                    case ValidationSeverity.Error: return "❌";
                    case ValidationSeverity.Warning: return "⚠️";
                    default: return "ℹ️";
                }
            }
        }

        public string ToDisplayString()
        {
            var sb = new StringBuilder();
            sb.Append($"{SeverityIcon} [{SeverityText}] {Message}");
            if (ExpectedValue.HasValue && ActualValue.HasValue)
            {
                sb.Append($" (标准: {ExpectedValue.Value:N2}元, 实际: {ActualValue.Value:N2}元)");
            }
            if (!string.IsNullOrWhiteSpace(Suggestion))
            {
                sb.Append($" → 建议: {Suggestion}");
            }
            return sb.ToString();
        }
    }

    public class ValidationResult
    {
        public List<ValidationMessage> Messages { get; set; } = new List<ValidationMessage>();

        public bool IsValid
        {
            get
            {
                foreach (var msg in Messages)
                {
                    if (msg.Severity == ValidationSeverity.Error)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public bool HasWarnings
        {
            get
            {
                foreach (var msg in Messages)
                {
                    if (msg.Severity == ValidationSeverity.Warning)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public int ErrorCount
        {
            get
            {
                int count = 0;
                foreach (var msg in Messages)
                {
                    if (msg.Severity == ValidationSeverity.Error) count++;
                }
                return count;
            }
        }

        public int WarningCount
        {
            get
            {
                int count = 0;
                foreach (var msg in Messages)
                {
                    if (msg.Severity == ValidationSeverity.Warning) count++;
                }
                return count;
            }
        }

        public int InfoCount
        {
            get
            {
                int count = 0;
                foreach (var msg in Messages)
                {
                    if (msg.Severity == ValidationSeverity.Info) count++;
                }
                return count;
            }
        }

        public void AddError(string code, string message, string category = "", string relatedItemId = "",
            decimal? expected = null, decimal? actual = null, string suggestion = "")
        {
            Messages.Add(new ValidationMessage
            {
                Code = code,
                Message = message,
                Severity = ValidationSeverity.Error,
                Category = category,
                RelatedItemId = relatedItemId,
                ExpectedValue = expected,
                ActualValue = actual,
                Suggestion = suggestion
            });
        }

        public void AddWarning(string code, string message, string category = "", string relatedItemId = "",
            decimal? expected = null, decimal? actual = null, string suggestion = "")
        {
            Messages.Add(new ValidationMessage
            {
                Code = code,
                Message = message,
                Severity = ValidationSeverity.Warning,
                Category = category,
                RelatedItemId = relatedItemId,
                ExpectedValue = expected,
                ActualValue = actual,
                Suggestion = suggestion
            });
        }

        public void AddInfo(string code, string message, string category = "", string relatedItemId = "",
            string suggestion = "")
        {
            Messages.Add(new ValidationMessage
            {
                Code = code,
                Message = message,
                Severity = ValidationSeverity.Info,
                Category = category,
                RelatedItemId = relatedItemId,
                Suggestion = suggestion
            });
        }

        public List<ValidationMessage> GetErrors()
        {
            var result = new List<ValidationMessage>();
            foreach (var msg in Messages)
            {
                if (msg.Severity == ValidationSeverity.Error) result.Add(msg);
            }
            return result;
        }

        public List<ValidationMessage> GetWarnings()
        {
            var result = new List<ValidationMessage>();
            foreach (var msg in Messages)
            {
                if (msg.Severity == ValidationSeverity.Warning) result.Add(msg);
            }
            return result;
        }

        public List<ValidationMessage> GetInfos()
        {
            var result = new List<ValidationMessage>();
            foreach (var msg in Messages)
            {
                if (msg.Severity == ValidationSeverity.Info) result.Add(msg);
            }
            return result;
        }

        public List<string> GetDisplayMessages()
        {
            var result = new List<string>();
            foreach (var msg in Messages)
            {
                result.Add(msg.ToDisplayString());
            }
            return result;
        }

        public string GetDisplayText()
        {
            if (Messages.Count == 0) return "✅ 校验通过，未发现问题。";

            var sb = new StringBuilder();
            sb.AppendLine($"校验结果: {(IsValid ? "✅ 基本通过" : "❌ 存在错误")}");
            sb.AppendLine($"  错误: {ErrorCount} 个，警告: {WarningCount} 个，提示: {InfoCount} 个");
            sb.AppendLine();
            foreach (var msg in Messages)
            {
                sb.AppendLine("  " + msg.ToDisplayString());
            }
            return sb.ToString();
        }

        public List<ValidationDisplayItem> GetDisplayItems()
        {
            var items = new List<ValidationDisplayItem>();
            foreach (var msg in Messages)
            {
                items.Add(new ValidationDisplayItem
                {
                    Code = msg.Code,
                    Severity = msg.Severity,
                    SeverityText = msg.SeverityText,
                    SeverityIcon = msg.SeverityIcon,
                    Category = msg.Category,
                    Message = msg.Message,
                    ExpectedValue = msg.ExpectedValue,
                    ActualValue = msg.ActualValue,
                    Suggestion = msg.Suggestion,
                    UserFriendlyText = msg.ToDisplayString()
                });
            }
            return items;
        }

        public Dictionary<string, List<ValidationMessage>> GetMessagesByCategory()
        {
            var dict = new Dictionary<string, List<ValidationMessage>>();
            foreach (var msg in Messages)
            {
                string key = string.IsNullOrWhiteSpace(msg.Category) ? "其他" : msg.Category;
                if (!dict.ContainsKey(key)) dict[key] = new List<ValidationMessage>();
                dict[key].Add(msg);
            }
            return dict;
        }

        public void Merge(ValidationResult other)
        {
            Messages.AddRange(other.Messages);
        }
    }

    public class ValidationDisplayItem
    {
        public string Code { get; set; } = string.Empty;

        public ValidationSeverity Severity { get; set; }

        public string SeverityText { get; set; } = string.Empty;

        public string SeverityIcon { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public decimal? ExpectedValue { get; set; }

        public decimal? ActualValue { get; set; }

        public string Suggestion { get; set; } = string.Empty;

        public string UserFriendlyText { get; set; } = string.Empty;
    }
}
