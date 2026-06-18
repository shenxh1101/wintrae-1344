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
    }

    public class RuleSchemeManager : IRuleProvider
    {
        private readonly Dictionary<string, RuleScheme> _schemes;
        private readonly Dictionary<string, string> _activeSchemeMap;
        private readonly object _lock = new object();

        public RuleSchemeManager()
        {
            _schemes = new Dictionary<string, RuleScheme>(StringComparer.OrdinalIgnoreCase);
            _activeSchemeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
                        return activeScheme;
                    }
                }

                var matched = _schemes.Values
                    .Where(s => s.Matches(employee, cityCode) && s.Id != "DEFAULT")
                    .OrderByDescending(s => s.Priority)
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
