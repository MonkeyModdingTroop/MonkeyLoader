#if NET5_0_OR_GREATER
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace MonkeyLoader
{
    public class AssemblyLoadContextLoadStrategy : IAssemblyLoadStrategy
    {
        private AssemblyLoadContext _assemblyLoadContext;
        
        public AssemblyLoadContextLoadStrategy()
        {
            _assemblyLoadContext = AssemblyLoadContext.GetLoadContext(typeof(AssemblyLoadContextLoadStrategy).Assembly)!;
        }
        
        public Assembly LoadFile(string assemblyPath)
        {
            Debug.WriteLine($"AssemblyLoadContextLoadStrategy: Loading assembly from path: {assemblyPath}");
            
            // Hack: Check if we already have a DLL with this name loaded and return it if so
            // This is not correct, but it works for now, we need to have the game in a separate load context in the future
            // Concretely, both ML and Reso use Mono.Cecil at different versions
            var loadedAssembly = _assemblyLoadContext.Assemblies
                .FirstOrDefault(a => !string.IsNullOrEmpty(a.Location) && Path.GetFileName(a.Location) == Path.GetFileName(assemblyPath));
            if (loadedAssembly != null)
            {
                Debug.WriteLine($"=> Found already loaded assembly: {loadedAssembly.FullName}");
                return loadedAssembly;
            }
            
            if (string.IsNullOrEmpty(assemblyPath))
            {
                throw new ArgumentException("Assembly path cannot be null or empty.", nameof(assemblyPath));
            }

            return _assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);
        }

        public Assembly Load(byte[] assemblyBytes, byte[]? pdbBytes = null)
        {
            Debug.WriteLine("AssemblyLoadContextLoadStrategy: Loading assembly from byte array.");
            if (assemblyBytes == null || assemblyBytes.Length == 0)
            {
                throw new ArgumentException("Assembly bytes cannot be null or empty.", nameof(assemblyBytes));
            }

            return _assemblyLoadContext.LoadFromStream(new MemoryStream(assemblyBytes),
                pdbBytes != null ? new  MemoryStream(pdbBytes) : null);
        }
    }
}

#endif