using System;

namespace FinanceReimbursement.Models
{
    public class ApprovalNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public int Order { get; set; }

        public ApprovalNodeType NodeType { get; set; }

        public string NodeName { get; set; } = string.Empty;

        public string ApproverId { get; set; } = string.Empty;

        public string ApproverName { get; set; } = string.Empty;

        public string ApproverPosition { get; set; } = string.Empty;

        public string DepartmentId { get; set; } = string.Empty;

        public bool IsRequired { get; set; } = true;

        public string RuleDescription { get; set; } = string.Empty;

        public decimal? AmountThreshold { get; set; }
    }
}
