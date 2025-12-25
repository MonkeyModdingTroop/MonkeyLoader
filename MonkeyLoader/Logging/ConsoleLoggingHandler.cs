using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        public const string BLUE = "\x1b[94m";
        public const string BOLD = "\x1b[1m";
        public const string CYAN = "\x1b[96m";
        public const string GRAY = "\x1b[97m";
        public const string GREEN = "\x1b[92m";
        public const string MAGENTA = "\x1b[95m";
        public const string NOBOLD = "\x1b[22m";
        public const string NOREVERSE = "\x1b[27m";
        public const string NORMAL = "\x1b[39m";
        public const string NOUNDERLINE = "\x1b[24m";
        public const string RED = "\x1b[91m";
        public const string REVERSE = "\x1b[7m";
        public const string UNDERLINE = "\x1b[4m";
        public const string YELLOW = "\x1b[93m";

        private static Process? _consoleHostProcess;
        private static NamedPipeClientStream? _pipeClient;

        private static StreamWriter? _writer;

        /// <summary>
        /// Gets whether this logging handler has a connection to the
        /// ConsoleHost process and named pipe stream.
        /// </summary>
        [MemberNotNullWhen(true, nameof(_pipeClient), nameof(_writer), nameof(_consoleHostProcess))]
        public static bool ConsoleHostConnected => _pipeClient?.IsConnected ?? false;

        /// <summary>
        /// Gets the instance of the <see cref="ConsoleLoggingHandler"/>.
        /// </summary>
        public static ConsoleLoggingHandler Instance { get; } = new ConsoleLoggingHandler();

        /// <summary>
        /// Whether
        /// </summary>
        public static bool ShouldBeConnected { get; private set; }

        /// <remarks>
        /// For this logger, that means that the ConsoleHost process
        /// and named pipe connection to it are is available.
        /// </remarks>
        /// <inheritdoc/>
        [MemberNotNullWhen(true, nameof(_pipeClient), nameof(_writer), nameof(_consoleHostProcess))]
        public override bool Connected => ConsoleHostConnected;

        /// <summary>
        /// Determines how the console window should be displayed at start-up
        /// </summary>
        public static ConsoleWindowStyle StartUpWindowStyle { get; set; }

        private ConsoleLoggingHandler()
        { }

        /// <summary>
        /// Asynchronously attempts to launch the ConsoleHost and connect to it
        /// via a named pipe until the <see cref="CancellationToken"/> is signalled.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> controlling how long attempts are made.</param>
        /// <returns>A task that returns <c>true</c> if it's now connected; or otherwise, <c>false</c>.</returns>
        public static async Task<bool> ConnectAsync(CancellationToken cancellationToken)
        {
            ShouldBeConnected = true;

            if (_consoleHostProcess is not null && (_pipeClient?.IsConnected ?? false))
                return true;

            var startInfo = new ProcessStartInfo("./MonkeyLoader/Tools/ConsoleHost/MonkeyLoader.ConsoleHost.exe", MonkeyLoader.GameName);
            startInfo.WindowStyle = (ProcessWindowStyle)StartUpWindowStyle;

            while (!cancellationToken.IsCancellationRequested)
            {
                Process? process = null;
                var launched = false;
                var connected = false;

                try
                {
                    process = new Process() { StartInfo = startInfo, EnableRaisingEvents = true };
                    process.Exited += OnConsoleHostExited;
                    process.Start();

                    launched = true;
                    connected = false;

                    var pipeClient = new NamedPipeClientStream(".", $"MonkeyLoader.ConsoleHost.{MonkeyLoader.GameName}", PipeDirection.Out, PipeOptions.None);

                    while (!cancellationToken.IsCancellationRequested && !pipeClient.IsConnected)
                    {
                        try
                        {
                            await Task.Run(pipeClient.Connect, cancellationToken);
                            connected = pipeClient.IsConnected;
                        }
                        catch
                        { }
                    }

                    if (!connected)
                        continue;

                    _consoleHostProcess = process;
                    _pipeClient = pipeClient;
                    _writer = new(_pipeClient);
                    _writer.AutoFlush = true;

                    return true;
                }
                catch
                {
                    try
                    {
                        if (launched && !connected)
                            process?.Kill();
                    }
                    catch { }
                }
            }

            return false;
        }

        /// <summary>
        /// Kills the ConsoleHost process and disconnects the named pipe.
        /// </summary>
        public static void Disconnect()
        {
            ShouldBeConnected = false;

            if (_consoleHostProcess is null)
                return;

            try
            {
                _consoleHostProcess.Exited -= OnConsoleHostExited;
                _consoleHostProcess.Kill();
            }
            catch { }
            finally
            {
                DisposeConsoleHost();
            }
        }

        /// <summary>
        /// Gives <see cref="TryConnect"/> 10s to attempt launching
        /// the ConsoleHost and connecting the named pipe.
        /// </summary>
        public static void TryConnect()
        {
            ShouldBeConnected = true;

            if (ConsoleHostConnected)
                return;

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(10000);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            ConnectAsync(cts.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        /// <inheritdoc/>
        public override void Debug(Func<object> messageProducer)
            => Log(messageProducer().ToString(), CYAN);

        /// <inheritdoc/>
        public override void Error(Func<object> messageProducer)
            => Log(messageProducer().ToString(), RED);

        /// <inheritdoc/>
        public override void Fatal(Func<object> messageProducer)
            => Log(messageProducer().ToString(), RED + BOLD);

        /// <inheritdoc/>
        public override void Flush()
        { }

        /// <inheritdoc/>
        public override void Info(Func<object> messageProducer)
            => Log(messageProducer().ToString(), NORMAL);

        /// <summary>
        /// Writes a message prefixed with a timestamp to the <see cref="Console"/>.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="textHighlight">Optional color / bold / underline codes to use for the message.</param>
        public void Log(string? message, string textHighlight = NORMAL)
        {
            if (!Connected)
                return;

            lock (_writer)
                _writer.WriteLine($"{NORMAL + GRAY}[{DateTime.UtcNow:HH:mm:ss:ffff}]{textHighlight} {message}{NORMAL + GRAY}");
        }

        /// <inheritdoc/>
        public override void Trace(Func<object> messageProducer)
            => Log(messageProducer().ToString(), CYAN);

        /// <inheritdoc/>
        public override void Warn(Func<object> messageProducer)
            => Log(messageProducer().ToString(), YELLOW);

        private static void DisposeConsoleHost()
        {
            _writer?.Dispose();
            _writer = null;

            _pipeClient?.Dispose();
            _pipeClient = null;

            _consoleHostProcess?.Dispose();
            _consoleHostProcess = null;
        }

        private static void OnConsoleHostExited(object? sender, EventArgs e)
        {
            DisposeConsoleHost();

            if (ShouldBeConnected)
                TryConnect();
        }
    }
}