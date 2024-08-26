using EnumerableToolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Logging
{
    /// <summary>
    /// Implements an <see cref="LoggingHandler"/> that can delegate messages to multiple other handlers.
    /// </summary>
    public sealed class MulticastLoggingHandler : LoggingHandler, IEnumerable<LoggingHandler>
    {
        private readonly LoggingHandler[] _loggingHandlers;

        /// <inheritdoc/>
        public override bool Connected => ConnectedHandlers.Any();

        /// <summary>
        /// Gets the currently <see cref="LoggingHandler.Connected">connected</see> logging handlers that this one delegates messages to.
        /// </summary>
        public IEnumerable<LoggingHandler> ConnectedHandlers => _loggingHandlers.Where(IsConnected);

        /// <summary>
        /// Gets the number of other handlers that messages are delegated to.
        /// </summary>
        public int Count => _loggingHandlers.Length;

        /// <summary>
        /// Gets all logging handlers that this one delegates messages to.
        /// </summary>
        public IEnumerable<LoggingHandler> LoggingHandlers => _loggingHandlers.AsSafeEnumerable();

        /// <summary>
        /// Creates a new multicast logging handler with the given handlers to delegate messages to.
        /// </summary>
        /// <param name="loggingHandlers">The logging handlers to delegate messages to.</param>
        public MulticastLoggingHandler(params LoggingHandler[] loggingHandlers)
            : this((IEnumerable<LoggingHandler>)loggingHandlers)
        { }

        /// <summary>
        /// Creates a new multicast logging handler with the given handlers to delegate messages to.
        /// </summary>
        /// <param name="loggingHandlers">The logging handlers to delegate messages to.</param>
        public MulticastLoggingHandler(IEnumerable<LoggingHandler> loggingHandlers)
        {
            _loggingHandlers = loggingHandlers.Where(handler => handler is not (null or MissingLoggingHandler)).ToArray();
        }

        /// <inheritdoc/>
        public override void Debug(Func<object> messageProducer)
        {
            messageProducer = PreloadMessage(messageProducer);

            foreach (var loggingHandler in ConnectedHandlers)
                loggingHandler.Debug(messageProducer);
        }

        /// <inheritdoc/>
        public override void Error(Func<object> messageProducer)
        {
            messageProducer = PreloadMessage(messageProducer);

            foreach (var loggingHandler in ConnectedHandlers)
                loggingHandler.Error(messageProducer);
        }

        /// <inheritdoc/>
        public override void Fatal(Func<object> messageProducer)
        {
            messageProducer = PreloadMessage(messageProducer);

            foreach (var loggingHandler in ConnectedHandlers)
                loggingHandler.Fatal(messageProducer);
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            foreach (var loggingHandler in ConnectedHandlers)
                loggingHandler.Flush();
        }

        /// <inheritdoc/>
        public IEnumerator<LoggingHandler> GetEnumerator() => ((IEnumerable<LoggingHandler>)_loggingHandlers).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _loggingHandlers.GetEnumerator();

        /// <inheritdoc/>
        public override int GetHashCode() => Count;

        /// <inheritdoc/>
        public override void Info(Func<object> messageProducer)
        {
            messageProducer = PreloadMessage(messageProducer);

            foreach (var loggingHandler in ConnectedHandlers)
                loggingHandler.Info(messageProducer);
        }

        /// <inheritdoc/>
        public override void Trace(Func<object> messageProducer)
        {
            messageProducer = PreloadMessage(messageProducer);

            foreach (var loggingHandler in ConnectedHandlers)
                loggingHandler.Trace(messageProducer);
        }

        /// <inheritdoc/>
        public override void Warn(Func<object> messageProducer)
        {
            messageProducer = PreloadMessage(messageProducer);

            foreach (var loggingHandler in ConnectedHandlers)
                loggingHandler.Warn(messageProducer);
        }

        private static bool IsConnected(LoggingHandler loggingHandler) => loggingHandler.Connected;

        private static Func<object> PreloadMessage(Func<object> messageProducer)
        {
            var message = messageProducer();
            return () => message;
        }
    }
}