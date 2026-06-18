using System;

namespace FinanceReimbursement.Models
{
    public class Employee
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string DepartmentId { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public EmployeeLevel Level { get; set; }

        public string Position { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string BankAccount { get; set; } = string.Empty;

        public string BankName { get; set; } = string.Empty;

        public string SupervisorId { get; set; } = string.Empty;
    }
}
