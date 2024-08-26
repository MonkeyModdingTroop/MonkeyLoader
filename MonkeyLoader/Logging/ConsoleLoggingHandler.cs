using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyLoader.Logging
{
    /// <summary>
    /// Implements an <see cref="LoggingHandler"/> that writes messages to the <see cref="Console"/>.
    /// </summary>
    public sealed class ConsoleLoggingHandler : LoggingHandler
    {
        private static bool _hasConsole = false;

        private static StreamWriter? _writer;

        /// <summary>
        /// Gets the instance of the <see cref="ConsoleLoggingHandler"/>.
        /// </summary>
        public static ConsoleLoggingHandler Instance { get; } = new ConsoleLoggingHandler();

        /// <remarks>
        /// For this logger, that means that a <see cref="Console"/> is available.
        /// </remarks>
        /// <inheritdoc/>
        public override bool Connected => _hasConsole;

        private ConsoleLoggingHandler()
        {
            try
            {
                // Probably doesn't work on Linux Native, should work on Wine/Proton?
                if (GetConsoleWindow() != IntPtr.Zero)
                {
                    _hasConsole = true;
                    return;
                }

                if (!AllocConsole())
                    return;

                _hasConsole = true;

                var output = Console.OpenStandardOutput();
                _writer = new StreamWriter(output) { AutoFlush = true };

                Console.SetOut(_writer);
            }
            catch
            {
                _hasConsole = false;
                return;
            }
        }

        /// <inheritdoc/>
        public override void Debug(Func<object> messageProducer)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Log(messageProducer().ToString());
        }

        /// <inheritdoc/>
        public override void Error(Func<object> messageProducer)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log(messageProducer().ToString());
        }

        /// <inheritdoc/>
        public override void Fatal(Func<object> messageProducer)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log(messageProducer().ToString());
        }

        /// <inheritdoc/>
        public override void Flush()
        { }

        /// <inheritdoc/>
        public override void Info(Func<object> messageProducer)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Log(messageProducer().ToString());
        }

        /// <summary>
        /// Writes a message prefixed with a timestamp to the <see cref="Console"/>.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public void Log(string message)
        {
            if (Console.Out != _writer)
                Console.SetOut(_writer);

            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss:ffff}] {message}");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <inheritdoc/>
        public override void Trace(Func<object> messageProducer)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Log(messageProducer().ToString());
        }

        /// <inheritdoc/>
        public override void Warn(Func<object> messageProducer)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Log(messageProducer().ToString());
        }

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
    }
}