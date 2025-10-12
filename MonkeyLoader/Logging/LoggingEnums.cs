using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Logging
{
    /// <summary>
    /// Specifies how the <see cref="ConsoleLoggingHandler">ConsoleHost</see>
    /// window should appear when its process is started.
    /// </summary>
    /// <remarks>
    /// Equivalent to <see cref="ProcessWindowStyle"/> without
    /// the <see cref="ProcessWindowStyle.Hidden">hidden</see> option.
    /// </remarks>
    public enum ConsoleWindowStyle
    {
        /// <inheritdoc cref="ProcessWindowStyle.Normal"/>
        Normal = ProcessWindowStyle.Normal,

        /// <inheritdoc cref="ProcessWindowStyle.Minimized"/>
        Minimized = ProcessWindowStyle.Minimized,

        /// <inheritdoc cref="ProcessWindowStyle.Maximized"/>
        Maximized = ProcessWindowStyle.Maximized,
    }

    /// <summary>
    /// Represents the possible logging levels.
    /// </summary>
    public enum LoggingLevel
    {
        /// <summary>
        /// One or more key functionalities, or the whole system isn't working.
        /// </summary>
        Fatal = -3,

        /// <summary>
        /// One or more functionalities are not working, preventing some from working correctly.
        /// </summary>
        Error = -2,

        /// <summary>
        /// Unexpected behavior happened, but work is continuing and the key functionalities are operating as expected.
        /// </summary>
        Warn = -1,

        /// <summary>
        /// Something happened, which is purely informative and can be ignored during normal use.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Events considered to be useful during debugging when more granular information is needed.
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Step by step execution of code that can be ignored during standard operation,
        /// but may be useful during extended debugging sessions.
        /// </summary>
        Trace = 2
    }
}