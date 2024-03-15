using System;
using System.Collections.Generic;
using System.Linq;

namespace MonkeyLoader.Logging
{
    /// <summary>
    /// Defines the interface used by the <see cref="MonkeyLogger"/> class to send
    /// its logging requests to the game-specific channels.
    /// </summary>
    public abstract class LoggingHandler
    {
        /// <summary>
        /// Gets whether this logging handler has somewhere to log.
        /// </summary>
        public abstract bool Connected { get; }

        /// <summary>
        /// Removes the right logging handler(s) from the left one(s).<br/>
        /// Uses <see cref="MulticastLoggingHandler"/>s.
        /// </summary>
        /// <param name="left">The left logging handler(s).</param>
        /// <param name="right">The right logging handler(s).</param>
        /// <returns>A new logging handler containing the remaining handler(s).</returns>
        public static LoggingHandler operator -(LoggingHandler? left, LoggingHandler? right)
        {
            if (left == null)
            {
                if (right == null)
                    return MissingLoggingHandler.Instance;

                return right;
            }

            if (right == null)
                return left;

            if (left is MulticastLoggingHandler multiLeft)
            {
                var newHandlers = multiLeft.LoggingHandlers.ToHashSet();

                if (right is MulticastLoggingHandler multiRight)
                {
                    newHandlers.ExceptWith(multiRight.LoggingHandlers);
                    return new MulticastLoggingHandler(newHandlers);
                }

                newHandlers.Remove(right);
                return new MulticastLoggingHandler(newHandlers);
            }

            if (right is MulticastLoggingHandler rightMulti)
            {
                var newHandlers = rightMulti.LoggingHandlers.ToHashSet();
                newHandlers.Remove(left);

                return new MulticastLoggingHandler(newHandlers);
            }

            if (left == right)
                return MissingLoggingHandler.Instance;

            return left;
        }

        /// <summary>
        /// Determines whether two <see cref="LoggingHandler"/>s are considered unequal.
        /// </summary>
        /// <param name="left">The first handler.</param>
        /// <param name="right">The second handler.</param>
        /// <returns><c>true</c> if they're considered unequal; otherwise <c>false</c>.</returns>
        public static bool operator !=(LoggingHandler? left, LoggingHandler? right)
            => !(left != right);

        /// <summary>
        /// Adds the right logging handler(s) to the left one(s).<br/>
        /// Uses <see cref="MulticastLoggingHandler"/>s.
        /// </summary>
        /// <param name="left">The left logging handler(s).</param>
        /// <param name="right">The right logging handler(s).</param>
        /// <returns>A new logging handler containing the combined handler(s).</returns>
        public static LoggingHandler operator +(LoggingHandler? left, LoggingHandler? right)
        {
            // Remove MissingLoggingHandler
            if (left == null)
            {
                if (right == null)
                    return MissingLoggingHandler.Instance;

                return right;
            }

            if (right == null)
                return left;

            if (left is MulticastLoggingHandler multiLeft)
            {
                if (right is MulticastLoggingHandler multiRight)
                    return new MulticastLoggingHandler(multiLeft.LoggingHandlers.Concat(multiRight.LoggingHandlers));

                return new MulticastLoggingHandler(multiLeft.LoggingHandlers.Concat(right));
            }

            if (right is MulticastLoggingHandler rightMulti)
                return new MulticastLoggingHandler(left.Yield().Concat(rightMulti.LoggingHandlers));

            return new MulticastLoggingHandler(left, right);
        }

        /// <summary>
        /// Determines whether two <see cref="LoggingHandler"/>s are considered equal.
        /// </summary>
        /// <param name="left">The first handler.</param>
        /// <param name="right">The second handler.</param>
        /// <returns><c>true</c> if they're considered equal; otherwise <c>false</c>.</returns>
        public static bool operator ==(LoggingHandler? left, LoggingHandler? right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is MissingLoggingHandler or null && right is MissingLoggingHandler or null)
                return true;

            if (left is MulticastLoggingHandler multiLeft && right is MulticastLoggingHandler multiRight && multiLeft.Count == multiRight.Count)
                return multiLeft.LoggingHandlers.Intersect(multiRight.LoggingHandlers).Count() == multiLeft.Count;

            return false;
        }

        /// <summary>
        /// Logs events considered to be useful during debugging when more granular information is needed.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public abstract void Debug(Func<object> messageProducer);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is LoggingHandler otherHandler && this == otherHandler;

        /// <summary>
        /// Logs that one or more functionalities are not working, preventing some from working correctly.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public abstract void Error(Func<object> messageProducer);

        /// <summary>
        /// Logs that one or more key functionalities, or the whole system isn't working.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public abstract void Fatal(Func<object> messageProducer);

        /// <summary>
        /// Attempts to flush any not yet fully logged messages.
        /// </summary>
        public abstract void Flush();

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// Logs that something happened, which is purely informative and can be ignored during normal use.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public abstract void Info(Func<object> messageProducer);

        /// <summary>
        /// Logs step by step execution of code that can be ignored during standard operation,
        /// but may be useful during extended debugging sessions.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public abstract void Trace(Func<object> messageProducer);

        /// <summary>
        /// Logs that unexpected behavior happened, but work is continuing and the key functionalities are operating as expected.
        /// </summary>
        /// <param name="messageProducer">The producer to log if possible.</param>
        public abstract void Warn(Func<object> messageProducer);
    }
}