using System;
using System.Diagnostics;

namespace MonkeyLoader
{
    internal static class ExceptionExtensions
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
    }
}