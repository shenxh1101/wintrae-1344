using System;
using System.Collections.Generic;
using System.Linq;
using FinanceReimbursement.Models;

namespace FinanceReimbursement.Rules
{
    public interface IRuleProvider
    {
        RuleScheme? GetRuleScheme(string schemeId);
        RuleScheme ResolveRule(Employee employee, string cityCode = "");
        List<RuleScheme> GetAllSchemes();
        void AddScheme(RuleScheme scheme);
        void RemoveScheme(string schemeId);
        string GetActiveSchemeId(string departmentId = "");
        void SetActiveScheme(string departmentId, string schemeId);
        List<RuleScheme> GetEffectiveSchemes(DateTime? atDate = null);
        RuleSchemeSnapshot CaptureRuleSnapshot(string schemeId);
        RuleSchemeSnapshot CaptureResolvedSnapshot(Employee employee, string cityCode = "");
    }

    public class RuleSchemeManager : IRuleProvider
    {
        private readonly Dictionary<string, RuleScheme> _schemes;
        private readonly Dictionary<string, string> _activeSchemeMap;
        private readonly Dictionary<string, List<RuleSchemeSnapshot>> _snapshots;
        private readonly object _lock = new object();

        public RuleSchemeManager()
        {
            _schemes = new Dictionary<string, RuleScheme>(StringComparer.OrdinalIgnoreCase);
            _activeSchemeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _snapshots = new Dictionary<string, List<RuleSchemeSnapshot>>(StringComparer.OrdinalIgnoreCase);

            var defaultScheme = RuleScheme.CreateDefault();
            _schemes[defaultScheme.Id] = defaultScheme;
            _activeSchemeMap[""] = defaultScheme.Id;
            _activeSchemeMap["DEFAULT"] = defaultScheme.Id;
        }

        public RuleSchemeManager(IEnumerable<RuleScheme> schemes) : this()
        {
            if (schemes == null) return;
            foreach (var s in schemes)
            {
                _schemes[s.Id] = s;
            }
        }

        public RuleScheme? GetRuleScheme(string schemeId)
        {
            if (string.IsNullOrWhiteSpace(schemeId)) return null;
            lock (_lock)
            {
                return _schemes.TryGetValue(schemeId, out var s) ? s : null;
            }
        }

        public RuleScheme ResolveRule(Employee employee, string cityCode = "")
        {
            if (employee == null)
            {
                return GetDefaultScheme();
            }

            lock (_lock)
            {
                if (_activeSchemeMap.TryGetValue(employee.DepartmentId, out var activeId))
                {
                    if (_schemes.TryGetValue(activeId, out var activeScheme) && activeScheme.IsEnabled)
                    {
                        if (IsEffectiveAt(activeScheme, DateTime.Now))
                            return activeScheme;
                    }
                }

                var matched = _schemes.Values
                    .Where(s => s.Matches(employee, cityCode) && s.Id != "DEFAULT")
                    .Where(s => IsEffectiveAt(s, DateTime.Now))
                    .OrderByDescending(s => s.Priority)
                    .ThenByDescending(s => s.OrgPath?.Length ?? 0)
                    .ToList();

                if (matched.Count > 0)
                {
                    return matched[0];
                }

                return GetDefaultScheme();
            }
        }

        public List<RuleScheme> GetAllSchemes()
        {
            lock (_lock)
            {
                return _schemes.Values.OrderByDescending(s => s.Priority).ToList();
            }
        }

        public List<RuleScheme> GetEffectiveSchemes(DateTime? atDate = null)
        {
            var date = atDate ?? DateTime.Now;
            lock (_lock)
            {
                return _schemes.Values
                    .Where(s => s.IsEnabled && IsEffectiveAt(s, date))
                    .OrderByDescending(s => s.Priority)
                    .ToList();
            }
        }

        public void AddScheme(RuleScheme scheme)
        {
            if (scheme == null || string.IsNullOrWhiteSpace(scheme.Id)) return;
            lock (_lock)
            {
                _schemes[scheme.Id] = scheme;
            }
        }

        public void RemoveScheme(string schemeId)
        {
            if (string.IsNullOrWhiteSpace(schemeId)) return;
            lock (_lock)
            {
                if (schemeId == "DEFAULT") return;
                _schemes.Remove(schemeId);

                var toRemove = _activeSchemeMap
                    .Where(kv => kv.Value.Equals(schemeId, StringComparison.OrdinalIgnoreCase))
                    .Select(kv => kv.Key)
                    .ToList();
                foreach (var key in toRemove)
                {
                    _activeSchemeMap[key] = "DEFAULT";
                }
            }
        }

        public string GetActiveSchemeId(string departmentId = "")
        {
            var key = string.IsNullOrWhiteSpace(departmentId) ? "" : departmentId;
            lock (_lock)
            {
                return _activeSchemeMap.TryGetValue(key, out var id) ? id : "DEFAULT";
            }
        }

        public void SetActiveScheme(string departmentId, string schemeId)
        {
            lock (_lock)
            {
                if (_schemes.ContainsKey(schemeId))
                {
                    _activeSchemeMap[departmentId ?? ""] = schemeId;
                }
            }
        }

        public RuleSchemeSnapshot CaptureRuleSnapshot(string schemeId)
        {
            var scheme = GetRuleScheme(schemeId);
            if (scheme == null) return new RuleSchemeSnapshot();
            var snapshot = scheme.CaptureSnapshot();
            RecordSnapshot(schemeId, snapshot);
            return snapshot;
        }

        public RuleSchemeSnapshot CaptureResolvedSnapshot(Employee employee, string cityCode = "")
        {
            var scheme = ResolveRule(employee, cityCode);
            var snapshot = scheme.CaptureSnapshot();
            RecordSnapshot($"{scheme.Id}_{employee?.Id ?? "UNKNOWN"}", snapshot);
            return snapshot;
        }

        public List<RuleSchemeSnapshot> GetSnapshots(string formNo)
        {
            lock (_lock)
            {
                if (_snapshots.TryGetValue(formNo, out var list)) return new List<RuleSchemeSnapshot>(list);
                return new List<RuleSchemeSnapshot>();
            }
        }

        public void RecordFormRule(string formNo, RuleSchemeSnapshot snapshot)
        {
            RecordSnapshot(formNo, snapshot);
        }

        private void RecordSnapshot(string key, RuleSchemeSnapshot snapshot)
        {
            lock (_lock)
            {
                if (!_snapshots.ContainsKey(key)) _snapshots[key] = new List<RuleSchemeSnapshot>();
                _snapshots[key].Add(snapshot);
            }
        }

        private bool IsEffectiveAt(RuleScheme scheme, DateTime date)
        {
            if (scheme.EffectiveDate > date) return false;
            if (scheme.ExpiryDate.HasValue && scheme.ExpiryDate.Value < date) return false;
            return true;
        }

        private RuleScheme GetDefaultScheme()
        {
            lock (_lock)
            {
                if (_schemes.TryGetValue("DEFAULT", out var s)) return s;
                var def = RuleScheme.CreateDefault();
                _schemes["DEFAULT"] = def;
                return def;
            }
        }
    }
}
