using System;
using System.Text;
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
        /// Field save identity
        /// </summary>
        private string _identity;

        /// <summary>
        ///     Identity for log
        ///     Find easy and fast log with identity
        /// </summary>
        public string Identity{
            set => _identity = EncodeJsString(value);
            get => _identity;
        }

        /// <summary>
        /// Field save title
        /// </summary>
        private string _title;

        /// <summary>
        ///     Specify title of log
        /// </summary>
        public string Title
        {
            set => _title = EncodeJsString(value);
            get => _title;
        }

        /// <summary>
        /// Field save body
        /// </summary>
        private string _body;

        /// <summary>
        ///     Specify body of log
        /// </summary>
        public string Body
        {
            set => _body = EncodeJsString(value);
            get => _body;
        }

        /// <summary>
        /// Field save file name
        /// </summary>
        private string _fileName;

        /// <summary>
        ///     Specify stack trace file name
        /// </summary>
        public string FileName
        {
            set => _fileName = EncodeJsString(value);
            get => _fileName;
        }

        /// <summary>
        /// Field save method base
        /// </summary>
        private string _methodBase;

        /// <summary>
        ///     specify stack trace method base
        /// </summary>
        public string MethodBase
        {
            set => _methodBase = EncodeJsString(value);
            get => _methodBase;
        }

        /// <summary>
        /// Field save line number
        /// </summary>
        private string _lineNumber;

        /// <summary>
        ///     Specify stack trace line number
        /// </summary>
        public string LineNumber
        {
            set => _lineNumber = EncodeJsString(value);
            get => _lineNumber;
        }

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
            string oMethodBase, string oLineNumber, DateTime oLogDateTime) : this()
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

        /// <summary>
        ///     Encodes a string to be represented as a string literal. The format
        ///     is essentially a JSON string.
        ///     The string returned includes outer quotes
        ///     Example Output: "Hello \"Rick\"!\r\nRock on"
        /// </summary>
        /// <param name="text">Text for convert</param>
        /// <returns>Converted text</returns>

        #region EncodeJsString

        public string EncodeJsString(string text)
        {
            var sb = new StringBuilder();
            foreach (var c in text)
                switch (c)
                {
                    case '\'':
                        sb.Append("\\'");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }

            sb.Replace(Environment.NewLine, "\n");

            return sb.ToString();
        }

        #endregion

        #endregion
    }
}