using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.ConsoleHost
{
    internal static partial class ConsoleMode
    {
        public static bool IsTerminal { get; private set; }

        public static bool EnsureTerminalProcessing()
        {
            if (!OperatingSystem.IsWindows())
            {
                IsTerminal = true;

                return true;
            }

            try
            {
                var handle = GetStdHandle(-11);

                // Ensure ENABLE_PROCESSED_OUTPUT and ENABLE_VIRTUAL_TERMINAL_PROCESSING
                var success = GetConsoleMode(handle, out var mode);
                mode |= 0x0001 | 0x0004;

                success &= SetConsoleMode(handle, mode);

                IsTerminal = success;
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while setting console mode to enable terminal processing!");
                Console.WriteLine(ex.ToString());
            }

            IsTerminal = false;
            return false;
        }

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial IntPtr GetStdHandle(int nStdHandle);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
    }
}