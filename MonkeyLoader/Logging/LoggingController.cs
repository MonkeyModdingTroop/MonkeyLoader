using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace MonkeyLoader.Logging
{
    /// <summary>
    /// Manages the connection between <see cref="LoggingHandler"/>s and <see cref="Logger"/>s.
    /// </summary>
    public sealed class LoggingController
    {
        private readonly ConcurrentQueue<DeferredMessage> _deferredMessages = new();

        private readonly Timer _flushTimer;
        private bool _autoFlush = true;
        private LoggingHandler _handler = MissingLoggingHandler.Instance;
        private Task _lastLogTask = Task.CompletedTask;

        /// <summary>
        /// Gets or sets whether this logger will automatically trigger <see cref="Flush()">flushing</see> of messages.
        /// </summary>
        public bool AutoFlush
        {
            get
            {
                lock (this)
                    return _autoFlush;
            }

            set
            {
                lock (this)
                {
                    _autoFlush = value;
                    _flushTimer.Change(TimeSpan.Zero, AutoFlushTimeout);
                }
            }
        }

        /// <summary>
        /// Gets or sets the time to wait for more logging before
        /// <see cref="Logger.Flush()">flushing</see> after non-critical messages.
        /// </summary>
        /// <remarks>
        /// <i>Default:</i> 2 seconds.
        /// </remarks>
        public TimeSpan FlushTimeout { get; set; } = TimeSpan.FromMilliseconds(2000);

        /// <summary>
        /// Gets the <see cref="LoggingHandler"/> used to send logging requests to the game-specific channels.<br/>
        /// Messages get queued when it isn't <see cref="LoggingHandler.Connected">connected</see> and they would otherwise have been logged.
        /// </summary>
        public LoggingHandler Handler
        {
            get => _handler;

            [MemberNotNull(nameof(_handler))]
            set
            {
                _handler = value ?? MissingLoggingHandler.Instance;

                if (_handler.Connected)
                    FlushDeferredMessages();
            }
        }

        /// <summary>
        /// Gets this controller's id.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets or sets the current <see cref="Level"/> used to filter requests on <see cref="Logger"/> instances.
        /// </summary>
        /// <remarks>
        /// <i>Default:</i> <see cref="LoggingLevel.Info"/>.
        /// </remarks>
        public LoggingLevel Level { get; set; } = LoggingLevel.Info;

        /// <summary>
        /// Gets the timeout currently used for <see cref="AutoFlush">automatic flushing</see>.
        /// </summary>
        /// <value>
        /// The loader's <c><see cref="FlushTimeout">LoggingFlushTimeout</see></c> when
        /// <see cref="AutoFlush">automatic flushing</see> is active; otherwise, <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </value>
        private TimeSpan AutoFlushTimeout => AutoFlush ? FlushTimeout : Timeout.InfiniteTimeSpan;

        /// <summary>
        /// Creates a new <see cref="LoggingHandler"/> to control <see cref="Logger"/>s.
        /// </summary>
        /// <param name="id">The controller's id.</param>
        public LoggingController(string id)
        {
            Id = id;
            _flushTimer = new(Flush);
        }

        /// <summary>
        /// Flushes any not yet fully logged messages.
        /// </summary>
        public void Flush()
        {
            Handler.Flush();
            _flushTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Determines whether the given <see cref="Level"/> should be logged at the current <see cref="Level">Level</see>.
        /// </summary>
        /// <param name="level">The <see cref="Level"/> to check.</param>
        /// <returns><c>true</c> if the given <see cref="Level"/> should be logged right now; otherwise, <c>false</c>.</returns>
        public bool ShouldLog(LoggingLevel level) => Level >= level;

        internal void LogInternal(LoggingLevel level, string identifier, Func<object> messageProducer)
        {
            if (!ShouldLog(level))
                return;

            QueueLogging(() =>
            {
                LogLevelToLogger(level)(MakeMessageProducer(level, identifier, messageProducer));

                HandleAutoFlush(level);
            });
        }

        internal void LogInternal(LoggingLevel level, string identifier, IEnumerable<Func<object>> messageProducers)
        {
            if (!ShouldLog(level))
                return;

            QueueLogging(() =>
            {
                var logger = LogLevelToLogger(level);

                foreach (var messageProducer in messageProducers)
                    logger(MakeMessageProducer(level, identifier, messageProducer));

                HandleAutoFlush(level);
            });
        }

        internal void LogInternal(LoggingLevel level, string identifier, IEnumerable<object> messages)
        {
            if (!ShouldLog(level))
                return;

            QueueLogging(() =>
            {
                var logger = LogLevelToLogger(level);

                foreach (var message in messages)
                    logger(MakeMessageProducer(level, identifier, message));

                HandleAutoFlush(level);
            });
        }

        private static string LogLevelToString(LoggingLevel level) => level switch
        {
            LoggingLevel.Fatal => "[FATAL]",
            LoggingLevel.Error => "[ERROR]",
            LoggingLevel.Warn => "[WARN] ",
            LoggingLevel.Info => "[INFO] ",
            LoggingLevel.Debug => "[DEBUG]",
            LoggingLevel.Trace => "[TRACE]",
            _ => "[WHAT?]"
        };

        private Action<Func<object>> DeferMessage(LoggingLevel level)
            => messageProducer => _deferredMessages.Enqueue(new DeferredMessage(level, messageProducer()));

        private void Flush(object _) => Flush();

        private void FlushDeferredMessages()
        {
            lock (this)
            {
                var autoFlush = AutoFlush;
                AutoFlush = false;

                while (_deferredMessages.Count > 0)
                {
                    if (!_deferredMessages.TryDequeue(out var deferredMessage))
                        continue;

                    LogLevelToLogger(deferredMessage.LoggingLevel)(() => deferredMessage.Message);
                }

                AutoFlush = autoFlush;
                Flush();
            }
        }

        private void HandleAutoFlush(LoggingLevel level)
        {
            if (!AutoFlush)
                return;

            // "Low Priority" Message(s)
            if (level is < LoggingLevel.Debug and > LoggingLevel.Error)
            {
                _flushTimer.Change(TimeSpan.Zero, AutoFlushTimeout);
                return;
            }

            Flush();
        }

        private Action<Func<object>> LogLevelToLogger(LoggingLevel level)
        {
            if (!Handler.Connected)
                return DeferMessage(level);

            return level switch
            {
                LoggingLevel.Fatal => Handler.Fatal,
                LoggingLevel.Error => Handler.Error,
                LoggingLevel.Warn => Handler.Warn,
                LoggingLevel.Info => Handler.Info,
                LoggingLevel.Debug => Handler.Debug,
                LoggingLevel.Trace => Handler.Trace,
                _ => _ => { }
            };
        }

        private Func<object> MakeMessageProducer(LoggingLevel level, string identifier, Func<object> messageProducer)
            => () => $"{LogLevelToString(level)} [{identifier}] {messageProducer()}";

        private Func<object> MakeMessageProducer(LoggingLevel level, string identifier, object message)
            => () => $"{LogLevelToString(level)} [{identifier}] {message}";

        private void QueueLogging(Action handleLogging)
        {
            lock (this)
            {
                _lastLogTask = _lastLogTask.ContinueWith(_ => handleLogging(),
                    TaskContinuationOptions.RunContinuationsAsynchronously);
            }
        }

        private sealed class DeferredMessage
        {
            public readonly LoggingLevel LoggingLevel;
            public readonly object Message;

            public DeferredMessage(LoggingLevel level, object message)
            {
                LoggingLevel = level;
                Message = message;
            }
        }
    }
}