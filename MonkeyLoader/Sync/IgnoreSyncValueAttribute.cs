using System;

namespace MonkeyLoader.Sync
{
    /// <summary>
    /// Marks a field or property with a compatible <see cref="IUnlinkedMonkeySyncValue{TLink}"/> type
    /// on a class deriving from <see cref="MonkeySyncValue{TLink, T}"/>
    /// to be excluded from the automatically detected fields and properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class IgnoreSyncValueAttribute : MonkeyLoaderAttribute
    { }
}