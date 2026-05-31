using System;

namespace Rules.Testing
{
    /// <summary>
    /// Exception thrown when a rule assertion fails during testing.
    /// </summary>
    public class RuleAssertionException : Exception
    {
        public RuleAssertionException(string message) : base(message) { }

        public RuleAssertionException(string message, Exception inner) : base(message, inner) { }
    }
}
