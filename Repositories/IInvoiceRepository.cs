using System;
using System.Collections.Generic;
using System.Linq;
using FinanceReimbursement.Models;

namespace FinanceReimbursement.Repositories
{
    public interface IInvoiceRepository
    {
        bool IsInvoiceUsed(string invoiceNo);
        IEnumerable<string> GetUsedInvoiceNos(string? departmentId = null);
        void MarkInvoicesAsUsed(IEnumerable<string> invoiceNos, string formNo = "", string departmentId = "");
        void UnmarkInvoices(IEnumerable<string> invoiceNos);
        int GetUsedInvoiceCount(string? departmentId = null);
        IEnumerable<UsedInvoiceRecord> GetUsedInvoiceRecords(string? departmentId = null);
    }

    public class UsedInvoiceRecord
    {
        public string InvoiceNo { get; set; } = string.Empty;
        public string FormNo { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty;
        public DateTime UsedDate { get; set; }
        public string UsedBy { get; set; } = string.Empty;
    }

    public class InMemoryInvoiceRepository : IInvoiceRepository
    {
        private readonly Dictionary<string, UsedInvoiceRecord> _records;
        private readonly object _lock = new object();

        public InMemoryInvoiceRepository()
        {
            _records = new Dictionary<string, UsedInvoiceRecord>(StringComparer.OrdinalIgnoreCase);
        }

        public InMemoryInvoiceRepository(IEnumerable<UsedInvoiceRecord> initialRecords) : this()
        {
            if (initialRecords == null) return;
            foreach (var r in initialRecords)
            {
                if (!string.IsNullOrWhiteSpace(r.InvoiceNo))
                {
                    _records[r.InvoiceNo] = r;
                }
            }
        }

        public bool IsInvoiceUsed(string invoiceNo)
        {
            if (string.IsNullOrWhiteSpace(invoiceNo)) return false;
            lock (_lock)
            {
                return _records.ContainsKey(invoiceNo);
            }
        }

        public IEnumerable<string> GetUsedInvoiceNos(string? departmentId = null)
        {
            lock (_lock)
            {
                var query = _records.Values.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(departmentId))
                {
                    query = query.Where(r => r.DepartmentId == departmentId);
                }
                return query.Select(r => r.InvoiceNo).ToList();
            }
        }

        public void MarkInvoicesAsUsed(IEnumerable<string> invoiceNos, string formNo = "", string departmentId = "")
        {
            if (invoiceNos == null) return;
            lock (_lock)
            {
                foreach (var no in invoiceNos)
                {
                    if (!string.IsNullOrWhiteSpace(no) && !_records.ContainsKey(no))
                    {
                        _records[no] = new UsedInvoiceRecord
                        {
                            InvoiceNo = no,
                            FormNo = formNo,
                            DepartmentId = departmentId,
                            UsedDate = DateTime.Now
                        };
                    }
                }
            }
        }

        public void UnmarkInvoices(IEnumerable<string> invoiceNos)
        {
            if (invoiceNos == null) return;
            lock (_lock)
            {
                foreach (var no in invoiceNos)
                {
                    if (!string.IsNullOrWhiteSpace(no))
                    {
                        _records.Remove(no);
                    }
                }
            }
        }

        public int GetUsedInvoiceCount(string? departmentId = null)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(departmentId))
                    return _records.Count;
                return _records.Values.Count(r => r.DepartmentId == departmentId);
            }
        }

        public IEnumerable<UsedInvoiceRecord> GetUsedInvoiceRecords(string? departmentId = null)
        {
            lock (_lock)
            {
                var query = _records.Values.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(departmentId))
                {
                    query = query.Where(r => r.DepartmentId == departmentId);
                }
                return query.ToList();
            }
        }
    }
}
