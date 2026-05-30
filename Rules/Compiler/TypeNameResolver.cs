using System;
using System.Linq;

namespace Rules.Compiler
{
    /// <summary>
    /// Converts CLR <see cref="Type"/u003e instances into C#-friendly type name strings.
    /// Handles primitives, generics, arrays, and fully-qualified names.
    /// </summary>
    internal static class TypeNameResolver
    {
        /// <summary>
        /// Returns a C# type name suitable for embedding in generated code.
        /// Handles primitives, generics, and fully-qualified names.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <returns>C# type name string.</returns>
        public static string GetTypeName(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // Handle C# primitive aliases.
            if (type == typeof(void))  return "void";
            if (type == typeof(string)) return "string";
            if (type == typeof(int))    return "int";
            if (type == typeof(bool))   return "bool";
            if (type == typeof(object)) return "object";
            if (type == typeof(long))   return "long";
            if (type == typeof(double)) return "double";
            if (type == typeof(float))  return "float";
            if (type == typeof(decimal))return "decimal";
            if (type == typeof(char))   return "char";
            if (type == typeof(byte))   return "byte";

            // Handle generic types like Func<T, TResult>, List<T>, etc.
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                var args = string.Join(", ", type.GetGenericArguments().Select(GetTypeName));
                return $"{genericDef.Name.Split('`')[0]}<{args}>";
            }

            // Handle arrays.
            if (type.IsArray)
            {
                return $"{GetTypeName(type.GetElementType()!)}[]";
            }

            // Nullable types.
            if (Nullable.GetUnderlyingType(type) != null)
            {
                return $"{GetTypeName(Nullable.GetUnderlyingType(type)!)}?";
            }

            // Fall back to fully-qualified name or simple name.
            return type.FullName ?? type.Name;
        }
    }
}
