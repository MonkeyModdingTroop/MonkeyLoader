#if !NET5_0_OR_GREATER

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MonkeyLoader
{
    /// <summary>
    /// Implements an <see cref="IAssemblyLoadStrategy"/> that uses
    /// <see cref="Assembly.Load(string)"/> and <see cref="Assembly.Load(byte[], byte[])"/>.
    /// </summary>
    internal sealed class AssemblyLoadLoadStrategy : IAssemblyLoadStrategy
    {
        /// <inheritdoc/>
        public Assembly LoadFile(string assemblyPath)
        {
            if (string.IsNullOrEmpty(assemblyPath))
                throw new ArgumentException("Assembly path cannot be null, empty, or whitespace.", nameof(assemblyPath));

            Debug.WriteLine($"AssemblyLoadLoadStrategy: Loading assembly from path: {assemblyPath}");

            // Hack: Check if we already have a DLL with this name loaded and return it if so
            // This is not correct, but it works for now, we need to have the game in a separate load  in the future
            // Concretely, both ML and Reso use Mono.Cecil at different versions
            var assemblyName = Path.GetFileName(assemblyPath);
            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.Location) && Path.GetFileName(a.Location).Equals(assemblyName, StringComparison.OrdinalIgnoreCase));

            if (loadedAssembly is not null)
            {
                Debug.WriteLine($"=> Found already loaded assembly: {loadedAssembly.FullName}");
                return loadedAssembly;
            }

            return Assembly.Load(assemblyPath);
        }

        /// <inheritdoc/>
        public Assembly Load(byte[] assemblyBytes, byte[]? pdbBytes = null)
        {
            Debug.WriteLine("AssemblyLoadLoadStrategy: Loading assembly from byte array.");

            if (assemblyBytes?.Length is not > 0)
                throw new ArgumentException("Assembly bytes cannot be null or empty.", nameof(assemblyBytes));

            return Assembly.Load(assemblyBytes, pdbBytes);
        }
    }
}

#endif