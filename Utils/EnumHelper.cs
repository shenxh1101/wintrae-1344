using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using FinanceReimbursement.Models;

namespace FinanceReimbursement.Utils
{
    public static class EnumHelper
    {
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }

        public static T? ParseFromDescription<T>(string description) where T : Enum
        {
            foreach (var field in typeof(T).GetFields())
            {
                var attribute = field.GetCustomAttribute<DescriptionAttribute>();
                if (attribute != null && attribute.Description == description)
                {
                    return (T?)field.GetValue(null);
                }
                if (field.Name == description)
                {
                    return (T?)field.GetValue(null);
                }
            }
            return default;
        }

        public static Dictionary<int, string> GetEnumDictionary<T>() where T : Enum
        {
            var dict = new Dictionary<int, string>();
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                dict[(int)(object)value] = value.GetDescription();
            }
            return dict;
        }
    }
}
