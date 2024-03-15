using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyLoader.Logging
{
    /// <summary>
    /// Implements an <see cref="LoggingHandler"/> that writes messages to a file.
    /// </summary>
    public sealed class FileLoggingHandler : LoggingHandler, IDisposable
    {
        private readonly int _flushTimeout;
        private readonly Timer _flushTimer;
        private readonly StreamWriter _streamWriter;

        /// <inheritdoc/>
        public override bool Connected => _streamWriter.BaseStream.CanWrite;

        /// <summary>
        /// Creates a new file logging handler with the file at the given path as the target.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        public FileLoggingHandler(string path) : this(new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read), 10000)
        { }

        /// <summary>
        /// Creates a new file logging handler with the given <see cref="FileStream"/> as the target.
        /// </summary>
        /// <param name="fileStream">The file to write to.</param>
        /// <param name="flushTimeout">The time in ms to wait for more logging before flushing after non-critical messages.</param>
        public FileLoggingHandler(FileStream fileStream, int flushTimeout)
        {
            fileStream.SetLength(0);
            _streamWriter = new StreamWriter(fileStream);

            _flushTimeout = flushTimeout;
            _flushTimer = new Timer(Flush, null, Timeout.Infinite, flushTimeout);
        }

        /// <inheritdoc/>
        public override void Debug(Func<object> messageProducer) => Log(messageProducer().ToString());

        /// <inheritdoc/>
        public void Dispose()
        {
            _streamWriter.Flush();
            _streamWriter.Dispose();

            _flushTimer.Dispose();
        }

        /// <inheritdoc/>
        public override void Error(Func<object> messageProducer) => Log(messageProducer().ToString());

        /// <inheritdoc/>
        public override void Fatal(Func<object> messageProducer) => Log(messageProducer().ToString());

        /// <inheritdoc/>
        public override void Flush()
        {
            lock (_streamWriter)
            {
                _streamWriter.Flush();
                _flushTimer.Change(Timeout.Infinite, _flushTimeout);
            }
        }

        /// <inheritdoc/>
        public override void Info(Func<object> messageProducer) => Log(messageProducer().ToString());

        /// <summary>
        /// Writes a message prefixed with a timestamp to the log file.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public void Log(string message)
        {
            lock (_streamWriter)
            {
                _streamWriter.WriteLine($"[{DateTime.Now:HH:mm:ss.ffff}] {message}");
                _flushTimer.Change(0, _flushTimeout);
            }
        }

        /// <inheritdoc/>
        public override void Trace(Func<object> messageProducer) => Log(messageProducer().ToString());

        /// <inheritdoc/>
        public override void Warn(Func<object> messageProducer) => Log(messageProducer().ToString());

        private void Flush(object state) => Flush();
    }
}