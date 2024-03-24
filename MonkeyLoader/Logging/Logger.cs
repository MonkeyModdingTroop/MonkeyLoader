using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonkeyLoader.Logging
{
    /// <summary>
    /// Contains the logging functionality mods and patchers can use to log messages to game-specific channels.
    /// </summary>
    public sealed class Logger
    {
        /// <summary>
        /// Gets the <see cref="LoggingController"/> instance that this logger works for.
        /// </summary>
        public LoggingController Controller { get; }

        /// <summary>
        /// Gets the identifier that's added to this logger's messages to determine the source.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Gets the current <see cref="LoggingLevel"/> used to filter requests, determined by the <see cref="LoggingController"/> instance this logger works for.
        /// </summary>
        public LoggingLevel Level => Controller.Level;

        /// <summary>
        /// Creates a new logger instance owned by the same <see cref="Controller">MonkeyLoggingController</see>.
        /// </summary>
        /// <param name="logger">The logger instance to copy.</param>
        /// <param name="extraIdentifier">The extra identifier to append to the <paramref name="logger"/>'s.</param>
        public Logger(Logger logger, string extraIdentifier)
        {
            Controller = logger.Controller;
            Identifier = $"{logger.Identifier}|{extraIdentifier}";
        }

        /// <summary>
        /// Creates a new logger instance working for the given controller.
        /// </summary>
        /// <param name="controller">The controller that this logger works for.</param>
        internal Logger(LoggingController controller)
        {
            Controller = controller;
            Identifier = controller.Id;
        }

        /// <summary>
        /// Logs events considered to be useful during debugging when more granular information is needed.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public void Debug(Func<object> messageProducer) => Controller.LogInternal(LoggingLevel.Debug, Identifier, messageProducer);

        /// <summary>
        /// Logs events considered to be useful during debugging when more granular information is needed.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public void Debug(params Func<object>[] messageProducers) => Controller.LogInternal(LoggingLevel.Debug, Identifier, messageProducers);

        /// <summary>
        /// Logs events considered to be useful during debugging when more granular information is needed.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public void Debug(IEnumerable<Func<object>> messageProducers) => Controller.LogInternal(LoggingLevel.Debug, Identifier, messageProducers);

        /// <summary>
        /// Logs events considered to be useful during debugging when more granular information is needed.
        /// </summary>
        /// <param name="messages">The messages to log as individual lines if possible.</param>
        public void Debug(IEnumerable<object> messages) => Controller.LogInternal(LoggingLevel.Debug, Identifier, messages);

        /// <summary>
        /// Logs that one or more functionalities are not working, preventing some from working correctly.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public void Error(Func<object> messageProducer) => Controller.LogInternal(LoggingLevel.Error, Identifier, messageProducer);

        /// <summary>
        /// Logs that one or more functionalities are not working, preventing some from working correctly.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public void Error(params Func<object>[] messageProducers) => Controller.LogInternal(LoggingLevel.Error, Identifier, messageProducers);

        /// <summary>
        /// Logs that one or more functionalities are not working, preventing some from working correctly.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public void Error(IEnumerable<Func<object>> messageProducers) => Controller.LogInternal(LoggingLevel.Error, Identifier, messageProducers);

        /// <summary>
        /// Logs that one or more functionalities are not working, preventing some from working correctly.
        /// </summary>
        /// <param name="messages">The messages to log as individual lines if possible.</param>
        public void Error(IEnumerable<object> messages) => Controller.LogInternal(LoggingLevel.Error, Identifier, messages);

        /// <summary>
        /// Logs that one or more key functionalities, or the whole system isn't working.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public void Fatal(Func<object> messageProducer) => Controller.LogInternal(LoggingLevel.Fatal, Identifier, messageProducer);

        /// <summary>
        /// Logs that one or more key functionalities, or the whole system isn't working.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public void Fatal(params Func<object>[] messageProducers) => Controller.LogInternal(LoggingLevel.Fatal, Identifier, messageProducers);

        /// <summary>
        /// Logs that one or more key functionalities, or the whole system isn't working.
        /// </summary>
        /// <param name="messages">The messages to log as individual lines if possible.</param>
        public void Fatal(IEnumerable<object> messages) => Controller.LogInternal(LoggingLevel.Fatal, Identifier, messages);

        /// <summary>
        /// Logs that one or more key functionalities, or the whole system isn't working.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public void Fatal(IEnumerable<Func<object>> messageProducers) => Controller.LogInternal(LoggingLevel.Fatal, Identifier, messageProducers);

        /// <summary>
        /// Flushes any not yet fully logged messages.
        /// </summary>
        public void Flush() => Controller.Flush();

        /// <summary>
        /// Logs that something happened, which is purely informative and can be ignored during normal use.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public void Info(Func<object> messageProducer) => Controller.LogInternal(LoggingLevel.Info, Identifier, messageProducer);

        /// <summary>
        /// Logs that something happened, which is purely informative and can be ignored during normal use.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public void Info(params Func<object>[] messageProducers) => Controller.LogInternal(LoggingLevel.Info, Identifier, messageProducers);

        /// <summary>
        /// Logs that something happened, which is purely informative and can be ignored during normal use.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public void Info(IEnumerable<Func<object>> messageProducers) => Controller.LogInternal(LoggingLevel.Info, Identifier, messageProducers);

        /// <summary>
        /// Logs that something happened, which is purely informative and can be ignored during normal use.
        /// </summary>
        /// <param name="messages">The messages to log as individual lines if possible.</param>
        public void Info(IEnumerable<object> messages) => Controller.LogInternal(LoggingLevel.Info, Identifier, messages);

        /// <summary>
        /// Determines whether the given <see cref="LoggingLevel"/> should be logged at the current <see cref="Level">Level</see>.
        /// </summary>
        /// <param name="level">The <see cref="LoggingLevel"/> to check.</param>
        /// <returns><c>true</c> if the given <see cref="LoggingLevel"/> should be logged right now; otherwise, <c>false</c>.</returns>
        public bool ShouldLog(LoggingLevel level) => Controller.ShouldLog(level);

        /// <summary>
        /// Logs step by step execution of code that can be ignored during standard operation,
        /// but may be useful during extended debugging sessions.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public void Trace(Func<object> messageProducer) => Controller.LogInternal(LoggingLevel.Trace, Identifier, messageProducer);

        /// <summary>
        /// Logs step by step execution of code that can be ignored during standard operation,
        /// but may be useful during extended debugging sessions.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public void Trace(params Func<object>[] messageProducers) => Controller.LogInternal(LoggingLevel.Trace, Identifier, messageProducers);

        /// <summary>
        /// Logs step by step execution of code that can be ignored during standard operation,
        /// but may be useful during extended debugging sessions.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public void Trace(IEnumerable<Func<object>> messageProducers) => Controller.LogInternal(LoggingLevel.Trace, Identifier, messageProducers);

        /// <summary>
        /// Logs step by step execution of code that can be ignored during standard operation,
        /// but may be useful during extended debugging sessions.
        /// </summary>
        /// <param name="messages">The messages to log as individual lines if possible.</param>
        public void Trace(IEnumerable<object> messages) => Controller.LogInternal(LoggingLevel.Trace, Identifier, messages);

        /// <summary>
        /// Logs that unexpected behavior happened, but work is continuing and the key functionalities are operating as expected.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public void Warn(Func<object> messageProducer) => Controller.LogInternal(LoggingLevel.Warn, Identifier, messageProducer);

        /// <summary>
        /// Logs that unexpected behavior happened, but work is continuing and the key functionalities are operating as expected.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public void Warn(params Func<object>[] messageProducers) => Controller.LogInternal(LoggingLevel.Warn, Identifier, messageProducers);

        /// <summary>
        /// Logs that unexpected behavior happened, but work is continuing and the key functionalities are operating as expected.
        /// </summary>
        /// <param name="messageProducers">The producers to log as individual lines if possible.</param>
        public void Warn(IEnumerable<Func<object>> messageProducers) => Controller.LogInternal(LoggingLevel.Warn, Identifier, messageProducers);

        /// <summary>
        /// Logs that unexpected behavior happened, but work is continuing and the key functionalities are operating as expected.
        /// </summary>
        /// <param name="messages">The messages to log as individual lines if possible.</param>
        public void Warn(IEnumerable<object> messages) => Controller.LogInternal(LoggingLevel.Warn, Identifier, messages);
    }
}