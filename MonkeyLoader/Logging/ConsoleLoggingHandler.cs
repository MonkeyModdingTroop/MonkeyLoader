using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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
        private static readonly NamedPipeClientStream _pipeClient;

        private static readonly StreamWriter _writer;

        /// <summary>
        /// Gets the instance of the <see cref="ConsoleLoggingHandler"/>.
        /// </summary>
        public static ConsoleLoggingHandler Instance { get; } = new ConsoleLoggingHandler();

        /// <remarks>
        /// For this logger, that means that a <see cref="Console"/> is available.
        /// </remarks>
        /// <inheritdoc/>
        public override bool Connected => _pipeClient.IsConnected;

        static ConsoleLoggingHandler()
        {
            var startInfo = new ProcessStartInfo("S:\\Projects\\MonkeyLoader\\MonkeyLoader.ConsoleHost\\bin\\Release\\net8.0\\MonkeyLoader.ConsoleHost.exe", "Resonite");
            var process = new Process() { StartInfo = startInfo };
            process.Start();

            while (true)
            {
                try
                {
                    _pipeClient = new(".", $"MonkeyLoader.ConsoleHost.{MonkeyLoader.GameName}", PipeDirection.Out, PipeOptions.None);
                    _pipeClient.Connect();
                    _writer = new(_pipeClient);
                    break;
                }
                catch (Exception ex)
                {
                }
            }
        }

        private ConsoleLoggingHandler()
        { }

        /// <inheritdoc/>
        public override void Debug(Func<object> messageProducer)
        {
            //Console.ForegroundColor = ConsoleColor.Cyan;
            Log(messageProducer().ToString());
        }

        /// <inheritdoc/>
        public override void Error(Func<object> messageProducer)
        {
            //Console.ForegroundColor = ConsoleColor.Red;
            Log(messageProducer().ToString());
        }

        /// <inheritdoc/>
        public override void Fatal(Func<object> messageProducer)
        {
            //Console.ForegroundColor = ConsoleColor.Red;
            Log(messageProducer().ToString());
        }

        /// <inheritdoc/>
        public override void Flush()
        { }

        /// <inheritdoc/>
        public override void Info(Func<object> messageProducer)
        {
            //Console.ForegroundColor = ConsoleColor.White;
            Log(messageProducer().ToString());
        }

        /// <summary>
        /// Writes a message prefixed with a timestamp to the <see cref="Console"/>.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public void Log(string message)
        {
            _writer.WriteLine($"[{DateTime.UtcNow:HH:mm:ss:ffff}] {message}");
            _writer.Flush();
            //Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <inheritdoc/>
        public override void Trace(Func<object> messageProducer)
        {
            //Console.ForegroundColor = ConsoleColor.Cyan;
            Log(messageProducer().ToString());
        }

        /// <inheritdoc/>
        public override void Warn(Func<object> messageProducer)
        {
            //Console.ForegroundColor = ConsoleColor.Yellow;
            Log(messageProducer().ToString());
        }
    }
}