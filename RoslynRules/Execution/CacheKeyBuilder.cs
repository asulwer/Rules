using RoslynRules.Models;
using System;
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

                var value = param.Value;
                if (value is null)
                {
                    sb.Append("null");
                }
                else if (value is IFormattable formattable)
                {
                    sb.Append(formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture));
                }
                else
                {
                    sb.Append(value.ToString());
                }

                sb.Append('|');
            }

            return sb.ToString();
        }
    }
}
