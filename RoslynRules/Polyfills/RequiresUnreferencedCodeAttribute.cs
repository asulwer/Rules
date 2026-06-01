#if NETSTANDARD2_1 || NETSTANDARD2_0
// Polyfill for RequiresUnreferencedCodeAttribute on frameworks that don't include it.
// This allows RoslynRules to annotate reflection-heavy code for AOT/trimming compatibility
// without requiring .NET 6+ target exclusively.
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
    internal sealed class RequiresUnreferencedCodeAttribute : Attribute
    {
        public RequiresUnreferencedCodeAttribute(string message)
        {
            Message = message;
        }

        public string Message { get; }

        public string? Url { get; set; }
    }
}
#endif
