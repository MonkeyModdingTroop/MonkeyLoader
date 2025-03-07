using System;
using System.Reflection;

namespace MonkeyLoader.Sync
{
    /// <summary>
    /// Marks a <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}">MonkeySync
    /// object's</see> instance method as being triggerable through the MonkeySync system.
    /// </summary>
    /// <remarks>
    /// The method must be
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class MonkeySyncMethodAttribute : MonkeyLoaderAttribute
    {
        /// <summary>
        /// Determines whether the given <paramref name="method"/>
        /// is suitable to be triggerable through the MonkeySync system.
        /// </summary>
        /// <remarks>
        /// To be suitable, a method must be a non-generic parameterless void instance method,
        /// which is decorated with <see cref="MonkeySyncMethodAttribute">this attribute</see>.
        /// </remarks>
        /// <param name="method">The method to check for suitability.</param>
        /// <returns><c>true</c> if the method is suitable; otherwise, <c>false</c>.</returns>
        public static bool IsValid(MethodInfo method)
            => !method.IsStatic && !method.ContainsGenericParameters
             && method.ReturnType == typeof(void) && method.GetParameters().Length == 0
             && method.GetCustomAttribute<MonkeySyncMethodAttribute>() is not null;
    }
}