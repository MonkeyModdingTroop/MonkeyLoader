using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader
{
    /// <summary>
    /// Contains helpful methods to help with reflection.
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Gets a compact, human-readable description of a type.
        /// </summary>
        /// <param name="type">The type to format.</param>
        /// <returns>The human-readable description of the type.</returns>
        public static string CompactDescription(this Type type)
        {
            if (type is null)
                return "null";

            if (type.IsGenericType)
                return $"{type.Name}<{type.GetGenericArguments().Select(CompactDescription).Join()}>";

            return type.Name;
        }

        /// <summary>
        /// Gets a compact, human-readable description of any kind of method without assembly details but with generics.
        /// </summary>
        /// <param name="member">The method to format.</param>
        /// <returns>The human-readable description of the method.</returns>
        ///
        public static string CompactDescription(this MethodBase member)
        {
            if (member is null)
                return "null";

            var returnType = AccessTools.GetReturnedType(member);

            var result = new StringBuilder();
            if (member.IsStatic)
                result.Append("static ");

            if (member.IsAbstract)
                result.Append("abstract ");

            if (member.IsVirtual)
                result.Append("virtual ");

            result.Append($"{returnType.CompactDescription()} ");

            if (member.DeclaringType is not null)
                result.Append($"{member.DeclaringType.CompactDescription()}::");

            var parameterString = member.GetParameters().Join(p => $"{p.ParameterType.CompactDescription()} {p.Name}");
            result.Append($"{member.Name}({parameterString})");

            return result.ToString();
        }
    }
}