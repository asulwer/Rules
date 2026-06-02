using RoslynRules.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RoslynRules.Execution
{
    /// <summary>
    /// Builds cache keys from rule parameters for memoization.
    /// </summary>
    internal static class CacheKeyBuilder
    {
        /// <summary>
        /// Creates a cache key from the rule ID and parameter values.
        /// Uses a fast non-cryptographic hash for performance.
        /// </summary>
        public static string Build(Guid ruleId, RuleParameter[] parameters)
        {
            var sb = new StringBuilder(ruleId.ToString("N"));
            sb.Append(':');

            foreach (var param in parameters)
            {
                sb.Append(param.Name);
                sb.Append('=');
                AppendValue(sb, param.Value);
                sb.Append('|');
            }

            return sb.ToString();
        }

        private static void AppendValue(StringBuilder sb, object? value)
        {
            if (value is null)
            {
                sb.Append("null");
                return;
            }

            var type = value.GetType();

            // Fast path: primitives and IFormattable
            if (value is IFormattable formattable && IsPrimitiveLike(type))
            {
                sb.Append(formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture));
                return;
            }

            // Strings
            if (value is string str)
            {
                sb.Append('"');
                sb.Append(str);
                sb.Append('"');
                return;
            }

            // Collections (arrays, lists, dictionaries, etc.)
            if (value is IEnumerable enumerable && !(value is string))
            {
                AppendCollection(sb, enumerable);
                return;
            }

            // Fallback for other reference types: use type name + hash code
            // This at least distinguishes different object instances
            sb.Append(type.FullName);
            sb.Append("#");
            sb.Append(value.GetHashCode());
        }

        private static void AppendCollection(StringBuilder sb, IEnumerable enumerable)
        {
            sb.Append('[');
            bool first = true;
            foreach (var item in enumerable)
            {
                if (!first) sb.Append(',');
                AppendValue(sb, item);
                first = false;
            }
            sb.Append(']');
        }

        private static bool IsPrimitiveLike(Type type)
        {
            return type.IsPrimitive
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || type == typeof(Guid)
                || (Nullable.GetUnderlyingType(type) is Type underlying && IsPrimitiveLike(underlying));
        }
    }
}
