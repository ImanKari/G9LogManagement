using System.ComponentModel;
using G9LogManagement.Enums;

namespace G9LogManagement.Config
{
    /// <summary>
    ///     Class value for log config
    /// </summary>
    public class LogsConfig
    {
        #region Fields And Properties

        /// <summary>
        ///     Event log  enable or disable
        /// </summary>
        public bool EVENT { get; }

        /// <summary>
        ///     Info log  enable or disable
        /// </summary>
        public bool INFO { get; }

        /// <summary>
        ///     Warning log  enable or disable
        /// </summary>
        public bool WARN { get; }

        /// <summary>
        ///     Error log enable or disable
        /// </summary>
        public bool ERROR { get; }

        /// <summary>
        ///     Exception error log enable or disable
        /// </summary>
        public bool EXCEPTION { get; }

        #endregion

        #region Methods

        /// <summary>
        ///     Constructor
        ///     Initialize Requirement
        /// </summary>
        /// <param name="eventLog">Enable or disable event log</param>
        /// <param name="infoLog">Enable or disable info log</param>
        /// <param name="WarningLog">Enable or disable warning log</param>
        /// <param name="errorLog">Enable or disable error log</param>
        /// <param name="exceptionLog">Enable or disable exception log</param>

        #region LogsConfig

        public LogsConfig(bool eventLog, bool infoLog, bool WarningLog, bool errorLog, bool exceptionLog)
        {
            EVENT = eventLog;
            INFO = infoLog;
            WARN = WarningLog;
            ERROR = errorLog;
            EXCEPTION = exceptionLog;
        }

        #endregion

        /// <summary>
        ///     Check value by type
        /// </summary>
        /// <param name="type">Type of log</param>
        /// <returns>'true' or 'false' value</returns>

        #region CheckValueByType

        public bool CheckValueByType(LogsType type)
        {
            return type switch
            {
                LogsType.EVENT => EVENT,
                LogsType.INFO => INFO,
                LogsType.WARN => WARN,
                LogsType.ERROR => ERROR,
                LogsType.EXCEPTION => EXCEPTION,
                _ => throw new InvalidEnumArgumentException(nameof(type), (int) type, typeof(LogsType))
            };
        }

        #endregion

        #endregion
    }
}