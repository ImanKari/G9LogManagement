using System;
using System.Runtime.CompilerServices;
using G9LogManagement.Enums;

namespace G9LogManagement
{
    public static class G9LogDefault
    {
        #region Fields And Properties

        /// <summary>
        ///     Access to default G9Log
        /// </summary>
        private static readonly G9Log G9Logging;

        #endregion

        #region Methods

        /// <summary>
        ///     Constructor
        /// </summary>

        #region G9LogDefault

        static G9LogDefault()
        {
            G9Logging = new G9Log(customLogConfig: G9LogDefaultConfigInitialize.DefaultInstanceLogConfig);
        }

        #endregion

        /// <summary>
        ///     Handle exception log
        ///     Used to default instance
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="message">Additional message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>
        /// <param name="customCallerPath">Custom caller path</param>
        /// <param name="customCallerName">Custom caller name</param>
        /// <param name="customLineNumber">Custom line number</param>

        #region G9LogException_Default

        public static void G9LogException_Default(this Exception ex, string message = null, string identity = null,
            string title = null,
            [CallerFilePath] string customCallerPath = null,
            [CallerMemberName] string customCallerName = null,
            [CallerLineNumber] int customLineNumber = 0)
        {
            G9Logging.G9LogException(ex, message, identity, title, customCallerPath, customCallerName,
                customLineNumber);
        }

        #endregion

        /// <summary>
        ///     Handle error log
        ///     Used to default instance
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>
        /// <param name="customCallerPath">Custom caller path</param>
        /// <param name="customCallerName">Custom caller name</param>
        /// <param name="customLineNumber">Custom line number</param>

        #region G9LogError_Default

        public static void G9LogError_Default(this string message, string identity = null, string title = null,
            [CallerFilePath] string customCallerPath = null,
            [CallerMemberName] string customCallerName = null,
            [CallerLineNumber] int customLineNumber = 0)
        {
            G9Logging.G9LogError(message, identity, title, customCallerPath, customCallerName, customLineNumber);
        }

        #endregion

        /// <summary>
        ///     Handle warning log
        ///     Used to default instance
        /// </summary>
        /// <param name="message">Warning message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>
        /// <param name="customCallerPath">Custom caller path</param>
        /// <param name="customCallerName">Custom caller name</param>
        /// <param name="customLineNumber">Custom line number</param>

        #region G9LogWarning_Default

        public static void G9LogWarning_Default(this string message, string identity = null, string title = null,
            [CallerFilePath] string customCallerPath = null,
            [CallerMemberName] string customCallerName = null,
            [CallerLineNumber] int customLineNumber = 0)
        {
            G9Logging.G9LogWarning(message, identity, title, customCallerPath, customCallerName, customLineNumber);
        }

        #endregion

        /// <summary>
        ///     Handle information log
        ///     Used to default instance
        /// </summary>
        /// <param name="message">Information message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>
        /// <param name="customCallerPath">Custom caller path</param>
        /// <param name="customCallerName">Custom caller name</param>
        /// <param name="customLineNumber">Custom line number</param>

        #region G9LogInformation_Default

        public static void G9LogInformation_Default(this string message, string identity = null, string title = null,
            [CallerFilePath] string customCallerPath = null,
            [CallerMemberName] string customCallerName = null,
            [CallerLineNumber] int customLineNumber = 0)
        {
            G9Logging.G9LogInformation(message, identity, title, customCallerPath, customCallerName, customLineNumber);
        }

        #endregion

        /// <summary>
        ///     Handle event log
        ///     Used to default instance
        /// </summary>
        /// <param name="message">Event message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>
        /// <param name="customCallerPath">Custom caller path</param>
        /// <param name="customCallerName">Custom caller name</param>
        /// <param name="customLineNumber">Custom line number</param>

        #region G9LogEvent_Default

        public static void G9LogEvent_Default(this string message, string identity = null, string title = null,
            [CallerFilePath] string customCallerPath = null,
            [CallerMemberName] string customCallerName = null,
            [CallerLineNumber] int customLineNumber = 0)
        {
            G9Logging.G9LogEvent(message, identity, title, customCallerPath, customCallerName, customLineNumber);
        }

        #endregion

        /// <summary>
        ///     Check active file logging by log type
        ///     Used to default instance
        /// </summary>
        /// <param name="type">Specify type of log</param>
        /// <returns>If active file logging for specified type return true</returns>

        #region CheckActiveFileLoggingByType_Default

        public static bool CheckActiveFileLoggingByType_Default(this LogsType type)
        {
            return G9Logging.CheckEnableFileLoggingByType(type);
        }

        #endregion

        /// <summary>
        ///     Check active console logging by log type
        ///     Used to default instance
        /// </summary>
        /// <param name="type">Specify type of log</param>
        /// <returns>If active console logging for specified type return true</returns>

        #region CheckActiveFileLoggingByType_Default

        public static bool CheckActiveConsoleLoggingByType_Default(this LogsType type)
        {
            return G9Logging.CheckEnableConsoleLoggingByType(type);
        }

        #endregion

        /// <summary>
        ///     Check active console logging by log type
        ///     Used to default instance
        /// </summary>
        /// <param name="type">Specify type of log</param>
        /// <returns>If active file logging or console logging for specified type return true</returns>

        #region CheckActiveFileLoggingByType_Default

        public static bool CheckActiveFileLoggingOrConsoleLoggingByType_Default(this LogsType type)
        {
            return G9Logging.CheckEnableConsoleLoggingOrFileLoggingByType(type);
        }

        #endregion

        #endregion
    }
}