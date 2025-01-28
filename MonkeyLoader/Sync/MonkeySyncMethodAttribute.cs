using System;

namespace MonkeyLoader.Sync
{
    /// <summary>
    /// Marks a <see cref="MonkeySyncObject{TSyncObject, TSyncValue, TLink}">MonkeySync
    /// object's</see> instance method as being triggerable through the MonkeySync system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class MonkeySyncMethodAttribute : MonkeyLoaderAttribute
    { }
}