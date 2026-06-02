using System;
using System.Reflection;
using System.Runtime.Loader;

namespace RoslynRules.Compiler
{
    /// <summary>
    /// A collectible AssemblyLoadContext for dynamically compiled expression assemblies.
    /// Allows unloading assemblies to reclaim memory in long-running applications.
    /// </summary>
    internal sealed class ExpressionAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _name;

        /// <summary>
        /// Creates a new collectible load context for expression assemblies.
        /// </summary>
        /// <param name="name">A unique name for this context (used for debugging).</param>
        public ExpressionAssemblyLoadContext(string name)
            : base(name, isCollectible: true)
        {
            _name = name;
        }

        /// <summary>
        /// Loads assembly bytes into this context.
        /// </summary>
        /// <param name="assemblyBytes">Raw assembly bytes.</param>
        /// <returns>The loaded assembly.</returns>
        public Assembly LoadAssembly(byte[] assemblyBytes)
        {
            using var stream = new System.IO.MemoryStream(assemblyBytes);
            return LoadFromStream(stream);
        }

        /// <summary>
        /// Required override. Resolves dependencies from the default context.
        /// </summary>
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Fall back to default context for dependencies (System.Runtime, etc.)
            return Default.LoadFromAssemblyName(assemblyName);
        }

        /// <summary>
        /// Human-readable identifier for debugging.
        /// </summary>
        public override string ToString() => $"ExpressionALC({_name})";
    }
}
