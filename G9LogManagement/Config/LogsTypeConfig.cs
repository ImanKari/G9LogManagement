using System;
using System.ComponentModel;
using G9ConfigManagement.Attributes;
using G9LogManagement.Enums;

namespace G9LogManagement.Config
{
    /// <summary>
    ///     Class value for log config
    /// </summary>
    public class LogsTypeConfig
    {
        #region Methods

        /// <summary>
        ///     Check value by type
        /// </summary>
        /// <param name="type">Type of log</param>
        /// <returns>'true' or 'false' value</returns>

        #region CheckValueByType

        public bool CheckValueByType(LogsType type)
        {
#if NETSTANDARD2_1
            return type switch
            {
                LogsType.EVENT => EVENT,
                LogsType.INFO => INFO,
                LogsType.WARN => WARN,
                LogsType.ERROR => ERROR,
                LogsType.EXCEPTION => EXCEPTION,
                _ => throw new InvalidEnumArgumentException(nameof(type), (int) type, typeof(LogsType))
            };
#else
            switch (type)
            {
                case LogsType.EVENT:
                    return EVENT;
                case LogsType.INFO:
                    return INFO;
                case LogsType.WARN:
                    return WARN;
                case LogsType.ERROR:
                    return ERROR;
                case LogsType.EXCEPTION:
                    return EXCEPTION;
                default:
#if (NETSTANDARD2_0)
                    throw new InvalidEnumArgumentException(nameof(type), (int) type, typeof(LogsType));
#else
                    throw new ArgumentOutOfRangeException(nameof(type), type,
                        $"Value enum type {typeof(LogsType)} not supported!");
#endif
            }
#endif
        }

#endregion

#endregion

#region Fields And Properties

        /// <summary>
        ///     Event log enable or disable
        /// </summary>
        public bool EVENT { set; get; } = true;

        /// <summary>
        ///     Info log enable or disable
        /// </summary>
        public bool INFO { set; get; } = true;

        /// <summary>
        ///     Warning log enable or disable
        /// </summary>
        public bool WARN { set; get; } = true;

        /// <summary>
        ///     Error log enable or disable
        /// </summary>
        public bool ERROR { set; get; } = true;

        /// <summary>
        ///     Exception error log enable or disable
        /// </summary>
        public bool EXCEPTION { set; get; } = true;

#endregion
    }
}