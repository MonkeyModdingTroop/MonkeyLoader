using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonkeyLoader.Logging
{
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

    /// <summary>
    /// Represents the possible window style modes these represent <see cref="ProcessWindowStyle"/> values with out the hidden mode
    /// </summary>
    public enum ConsoleWindowStyle
    {
        /// <summary>
        /// Window will be presented as normal
        /// </summary>
        Normal = ProcessWindowStyle.Normal,

        /// <summary>
        /// Window will be minimized
        /// </summary>
        Minimized = ProcessWindowStyle.Minimized,

        /// <summary>
        /// Window will be maximized
        /// </summary>
        Maximized = ProcessWindowStyle.Maximized,
    }
}