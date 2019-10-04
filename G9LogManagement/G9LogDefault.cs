﻿using System;
using System.ComponentModel;
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
            G9Logging = new G9Log();
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

        #region G9LogException_Default

        public static void G9LogException_Default(this Exception ex, string message = null, string identity = null,
            string title = null)
        {
            G9Logging.G9LogException(ex, message, identity);
        }

        #endregion

        /// <summary>
        ///     Handle error log
        ///     Used to default instance
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>

        #region G9LogError_Default

        public static void G9LogError_Default(this string message, string identity = null, string title = null)
        {
            G9Logging.G9LogError(message, identity, title);
        }

        #endregion

        /// <summary>
        ///     Handle warning log
        ///     Used to default instance
        /// </summary>
        /// <param name="message">Warning message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>

        #region G9LogWarning_Default

        public static void G9LogWarning_Default(this string message, string identity = null, string title = null)
        {
            G9Logging.G9LogWarning(message, identity, title);
        }

        #endregion

        /// <summary>
        ///     Handle information log
        ///     Used to default instance
        /// </summary>
        /// <param name="message">Information message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>

        #region G9LogInformation_Default

        public static void G9LogInformation_Default(this string message, string identity = null, string title = null)
        {
            G9Logging.G9LogInformation(message, identity, title);
        }

        #endregion

        /// <summary>
        ///     Handle event log
        ///     Used to default instance
        /// </summary>
        /// <param name="message">Event message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>

        #region G9LogEvent_Default

        public static void G9LogEvent_Default(this string message, string identity = null, string title = null)
        {
            G9Logging.G9LogEvent(message, identity, title);
        }

        #endregion

        /// <summary>
        ///     Check active logging by log type
        ///     Used to default instance
        /// </summary>
        /// <param name="type">Specify type of log</param>
        /// <returns>If active logging for specified type return true</returns>

        #region CheckActiveLogType_Default

        public static bool CheckActiveLogType_Default(this LogsType type)
        {
            return type switch
            {
                LogsType.EVENT => G9Logging.IsEnableEventLog,
                LogsType.INFO => G9Logging.IsEnableInformationLog,
                LogsType.WARN => G9Logging.IsEnableWarningLog,
                LogsType.ERROR => G9Logging.IsEnableErrorLog,
                LogsType.EXCEPTION => G9Logging.IsEnableExceptionLog,
                _ => throw new InvalidEnumArgumentException(nameof(type), (int) type, typeof(LogsType))
            };
        }

        #endregion
    }

    #endregion
}