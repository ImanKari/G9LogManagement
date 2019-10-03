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

        #region Fields And Properties

        /// <summary>
        ///     Event log enable or disable
        /// </summary>
        [DefaultValue(true)]
        [Hint("'true' or 'false' for EVENT")]
        public bool EVENT { get; }

        /// <summary>
        ///     Info log enable or disable
        /// </summary>
        [DefaultValue(true)]
        [Hint("'true' or 'false' for EVENT")]
        public bool INFO { get; }

        /// <summary>
        ///     Warning log enable or disable
        /// </summary>
        [DefaultValue(true)]
        [Hint("'true' or 'false' for EVENT")]
        public bool WARN { get; }

        /// <summary>
        ///     Error log enable or disable
        /// </summary>
        [DefaultValue(true)]
        [Hint("'true' or 'false' for EVENT")]
        public bool ERROR { get; }

        /// <summary>
        ///     Exception error log enable or disable
        /// </summary>
        [DefaultValue(true)]
        [Hint("'true' or 'false' for EVENT")]
        public bool EXCEPTION { get; }

        #endregion
    }
}