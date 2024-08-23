using System;
using System.Diagnostics;

namespace MonkeyLoader
{
    /// <summary>
    /// Contains helpful methods for formatting <see cref="Exception"/>s when logging.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Formats an <see cref="Exception"/> with a message.
        /// </summary>
        /// <param name="ex">The exception to format.</param>
        /// <param name="message">The message to prepend.</param>
        /// <returns>The formatted message and exception.</returns>
        public static string Format(this Exception ex, string message)
            => $"{message}{Environment.NewLine}{ex.Format()}";

        /// <summary>
        /// Formats an <see cref="Exception"/>.
        /// </summary>
        /// <param name="ex">The exception to format.</param>
        /// <returns>The formatted exception.</returns>
        public static string Format(this Exception ex)
            => ex.ToStringDemystified();

        /// <summary>
        /// Creates a delegate that formats an <see cref="Exception"/> with a message.
        /// </summary>
        /// <param name="ex">The exception to format.</param>
        /// <param name="message">The message to prepend.</param>
        /// <returns>The delegate that creates the formatted message and exception.</returns>
        public static Func<string> LogFormat(this Exception ex, string message)
            => () => ex.Format(message);

        /// <summary>
        /// Creates a delegate that formats an <see cref="Exception"/>.
        /// </summary>
        /// <param name="ex">The exception to format.</param>
        /// <returns>The delegate that creates the formatted exception.</returns>
        public static Func<string> LogFormat(this Exception ex)
            => ex.Format;
    }
}