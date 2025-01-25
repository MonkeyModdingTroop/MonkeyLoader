using System;

namespace MonkeyLoader.Sync
{
    /// <summary>
    /// Marks a <see cref="MonkeySyncObject{T}"/>'s instance method as
    /// being triggerable through the Sync system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class MonkeySyncMethodAttribute : MonkeyLoaderAttribute
    { }
}