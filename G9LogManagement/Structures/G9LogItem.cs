using System;
using G9LogManagement.Enums;

namespace G9LogManagement.Structures
{
    /// <summary>
    ///     Structure for save requirement data log
    /// </summary>
    public struct G9LogItem
    {
        #region Fileds And Properties

        /// <summary>
        ///     Specify log type
        /// </summary>
        public LogsType LogType;

        /// <summary>
        ///     Identity for log
        ///     Find easy and fast log with identity
        /// </summary>
        public string Identity;

        /// <summary>
        ///     Specify title of log
        /// </summary>
        public string Title;

        /// <summary>
        ///     Specify body of log
        /// </summary>
        public string Body;

        /// <summary>
        ///     Specify stack trace file name
        /// </summary>
        public string FileName;

        /// <summary>
        ///     specify stack trace method base
        /// </summary>
        public string MethodBase;

        /// <summary>
        ///     Specify stack trace line number
        /// </summary>
        public string LineNumber;

        /// <summary>
        ///     Specify log register date time
        /// </summary>
        public DateTime LogDateTime;

        #endregion

        #region Methods

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="oLogType">Specify log type</param>
        /// <param name="oIdentity">Specify custom identity of log</param>
        /// <param name="oTitle">Specify title of log</param>
        /// <param name="oBody">Specify body of log</param>
        /// <param name="oFileName">Specify stack trace file name</param>
        /// <param name="oMethodBase">specify stack trace method base</param>
        /// <param name="oLineNumber">Specify stack trace line number</param>
        /// <param name="oLogDateTime">Specify log register date time</param>

        #region G9LogItem

        public G9LogItem(LogsType oLogType, string oIdentity, string oTitle, string oBody, string oFileName,
            string oMethodBase, string oLineNumber, DateTime oLogDateTime)
        {
            LogType = oLogType;
            Identity = oIdentity;
            Title = oTitle;
            Body = oBody;
            FileName = oFileName;
            MethodBase = oMethodBase;
            LineNumber = oLineNumber;
            LogDateTime = oLogDateTime;
        }

        #endregion

        #endregion
    }
}