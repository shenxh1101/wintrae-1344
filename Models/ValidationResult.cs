using System;
using System.Collections.Generic;

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

        public List<string> GetDisplayMessages()
        {
            var result = new List<string>();
            foreach (var msg in Messages)
            {
                string severity = msg.Severity == ValidationSeverity.Error ? "[错误]" :
                                  msg.Severity == ValidationSeverity.Warning ? "[警告]" : "[提示]";
                result.Add($"{severity} {msg.Message}");
            }
            return result;
        }

        public void Merge(ValidationResult other)
        {
            Messages.AddRange(other.Messages);
        }
    }
}
