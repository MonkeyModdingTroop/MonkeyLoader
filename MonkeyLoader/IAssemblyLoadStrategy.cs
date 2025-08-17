using System.Reflection;

namespace MonkeyLoader
{
    public interface IAssemblyLoadStrategy
    {
        public Assembly LoadFile(string assemblyPath);
        
        public Assembly Load(byte[] assemblyBytes, byte[]? pdbBytes = null);
    }
}