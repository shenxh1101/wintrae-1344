using System;
using System.Collections.Generic;

namespace FinanceReimbursement.Models
{
    public class ApprovalRule
    {
        public List<ApprovalThresholdRule> ThresholdRules { get; set; } = new List<ApprovalThresholdRule>();

        public static ApprovalRule CreateDefault()
        {
            var rule = new ApprovalRule();

            rule.ThresholdRules.Add(new ApprovalThresholdRule
            {
                MinAmount = 0,
                MaxAmount = 2000,
                NodeTypes = new List<ApprovalNodeType>
                {
                    ApprovalNodeType.DepartmentHead,
                    ApprovalNodeType.FinanceReview
                }
            });

            rule.ThresholdRules.Add(new ApprovalThresholdRule
            {
                MinAmount = 2000,
                MaxAmount = 10000,
                NodeTypes = new List<ApprovalNodeType>
                {
                    ApprovalNodeType.DepartmentHead,
                    ApprovalNodeType.SuperiorLeader,
                    ApprovalNodeType.FinanceReview,
                    ApprovalNodeType.FinanceManager
                }
            });

            rule.ThresholdRules.Add(new ApprovalThresholdRule
            {
                MinAmount = 10000,
                MaxAmount = 50000,
                NodeTypes = new List<ApprovalNodeType>
                {
                    ApprovalNodeType.DepartmentHead,
                    ApprovalNodeType.SuperiorLeader,
                    ApprovalNodeType.FinanceReview,
                    ApprovalNodeType.FinanceManager,
                    ApprovalNodeType.GeneralManager
                }
            });

            rule.ThresholdRules.Add(new ApprovalThresholdRule
            {
                MinAmount = 50000,
                MaxAmount = decimal.MaxValue,
                NodeTypes = new List<ApprovalNodeType>
                {
                    ApprovalNodeType.DepartmentHead,
                    ApprovalNodeType.SuperiorLeader,
                    ApprovalNodeType.FinanceReview,
                    ApprovalNodeType.FinanceManager,
                    ApprovalNodeType.GeneralManager,
                    ApprovalNodeType.CEO
                }
            });

            return rule;
        }
    }

    public class ApprovalThresholdRule
    {
        public decimal MinAmount { get; set; }

        public decimal MaxAmount { get; set; }

        public List<ApprovalNodeType> NodeTypes { get; set; } = new List<ApprovalNodeType>();
    }
}
