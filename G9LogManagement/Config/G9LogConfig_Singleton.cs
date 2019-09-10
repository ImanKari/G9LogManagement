using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using G9LogManagement.Enums;

namespace G9LogManagement.Config
{
    /// <summary>
    ///     Class managed config
    /// </summary>
    public class G9LogConfigSingleton
    {
        #region Methods

        /// <summary>
        ///     Constructor
        ///     Initialize requirement data
        /// </summary>

        #region G9LogConfig

        private G9LogConfigSingleton()
        {
            // Initialize folders and files
            var initializeFoldersAndFiles =
                new InitializeFoldersAndFilesForLogReaders();

            try
            {
                // Read config
                var configuration = new XmlDocument();
                configuration.Load("G9Log.config");

                #region LogManagementConfig

                #region Set path

                try
                {
                    Path = configuration["Configuration"]?["LogManagementConfig"]?["Path"]?.InnerText;
                    System.IO.Path.GetFullPath(Path);
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Path)} in config file 'G9Log.config' is not correct!", ex);
                }

                #endregion

                #region Set 'DirectoryNameDateType' And 'ComponentLog'

                // Set DirectoryNameDateType
                if (Enum.TryParse(
                    configuration["Configuration"]?["LogManagementConfig"]?["DirectoryNameDateType"]?.InnerText,
                    out DateTimeType directory))
                    DirectoryNameDateType = directory;
                else
                    throw new InvalidEnumArgumentException(nameof(DirectoryNameDateType),
                        new Exception(
                            $"value {nameof(DirectoryNameDateType)} not correct for type {typeof(DateTimeType)}"));

                // Set ComponentLog
                if (bool.TryParse(configuration["Configuration"]?["LogManagementConfig"]?["ComponentLog"]?.InnerText,
                    out var result1))
                    ComponentLog = result1;
                else
                    throw new ArgumentException($"{nameof(ComponentLog)} is not correct in config.",
                        nameof(ComponentLog));

                #endregion

                // Fields save config
                bool EVENT, INFO, WARN, ERROR, EXCEPTION;

                #region Initialize config for 'ActiveLogs'

                if (!bool.TryParse(
                    configuration["Configuration"]?["LogManagementConfig"]?["ActiveLogs"]?["EVENT"]?.InnerText,
                    out EVENT))
                    throw new ArgumentException($"{nameof(ActiveLogs)} => {nameof(EVENT)}  is not correct in config.",
                        nameof(ActiveLogs) + "." + nameof(EVENT));

                if (!bool.TryParse(
                    configuration["Configuration"]?["LogManagementConfig"]?["ActiveLogs"]?["INFO"]?.InnerText,
                    out INFO))
                    throw new ArgumentException($"{nameof(ActiveLogs)} => {nameof(INFO)} is not correct in config.",
                        nameof(ActiveLogs) + "." + nameof(INFO));

                if (!bool.TryParse(
                    configuration["Configuration"]?["LogManagementConfig"]?["ActiveLogs"]?["WARN"]?.InnerText,
                    out WARN))
                    throw new ArgumentException($"{nameof(ActiveLogs)} => {nameof(WARN)} is not correct in config.",
                        nameof(ActiveLogs) + "." + nameof(WARN));

                if (!bool.TryParse(
                    configuration["Configuration"]?["LogManagementConfig"]?["ActiveLogs"]?["ERROR"]?.InnerText,
                    out ERROR))
                    throw new ArgumentException($"{nameof(ActiveLogs)} => {nameof(ERROR)} is not correct in config.",
                        nameof(ActiveLogs) + "." + nameof(ERROR));

                if (!bool.TryParse(
                    configuration["Configuration"]?["LogManagementConfig"]?["ActiveLogs"]?["EXCEPTION"]?.InnerText,
                    out EXCEPTION))
                    throw new ArgumentException(
                        $"{nameof(ActiveLogs)} => {nameof(EXCEPTION)} is not correct in config.",
                        nameof(ActiveLogs) + "." + nameof(EXCEPTION));

                ActiveLogs = new LogsConfig(EVENT, INFO, WARN, ERROR, EXCEPTION);

                #endregion

                #region Initialize config for 'EnableStackTraceInformation'

                if (!bool.TryParse(
                    configuration["Configuration"]?["LogManagementConfig"]?["EnableStackTraceInformation"]?["EVENT"]
                        ?.InnerText, out EVENT))
                    throw new ArgumentException(
                        $"{nameof(EnableStackTraceInformation)} => {nameof(EVENT)}  is not correct in config.",
                        nameof(EnableStackTraceInformation) + "." + nameof(EVENT));

                if (!bool.TryParse(
                    configuration["Configuration"]?["LogManagementConfig"]?["EnableStackTraceInformation"]?["INFO"]
                        ?.InnerText, out INFO))
                    throw new ArgumentException(
                        $"{nameof(EnableStackTraceInformation)} => {nameof(INFO)} is not correct in config.",
                        nameof(EnableStackTraceInformation) + "." + nameof(INFO));

                if (!bool.TryParse(
                    configuration["Configuration"]?["LogManagementConfig"]?["EnableStackTraceInformation"]?["WARN"]
                        ?.InnerText, out WARN))
                    throw new ArgumentException(
                        $"{nameof(EnableStackTraceInformation)} => {nameof(WARN)} is not correct in config.",
                        nameof(EnableStackTraceInformation) + "." + nameof(WARN));

                if (!bool.TryParse(
                    configuration["Configuration"]?["LogManagementConfig"]?["EnableStackTraceInformation"]?["ERROR"]
                        ?.InnerText, out ERROR))
                    throw new ArgumentException(
                        $"{nameof(EnableStackTraceInformation)} => {nameof(ERROR)} is not correct in config.",
                        nameof(EnableStackTraceInformation) + "." + nameof(ERROR));

                if (!bool.TryParse(
                    configuration["Configuration"]?["LogManagementConfig"]?["EnableStackTraceInformation"]?["EXCEPTION"]
                        ?.InnerText, out EXCEPTION))
                    throw new ArgumentException(
                        $"{nameof(EnableStackTraceInformation)} => {nameof(EXCEPTION)} is not correct in config.",
                        nameof(EnableStackTraceInformation) + "." + nameof(EXCEPTION));

                EnableStackTraceInformation = new LogsConfig(EVENT, INFO, WARN, ERROR, EXCEPTION);

                #endregion

                #region #region Initialize config for 'LogUserName', 'LogPassword' And 'EnableEncryptionLog'

                // Xor between LogUserName And LogPassword
                if (!string.IsNullOrEmpty(configuration["Configuration"]?["LogManagementConfig"]?["LogUserName"]
                        ?.InnerText) &&
                    string.IsNullOrEmpty(configuration["Configuration"]?["LogManagementConfig"]?["LogPassword"]
                        ?.InnerText) ||
                    string.IsNullOrEmpty(configuration["Configuration"]?["LogManagementConfig"]?["LogUserName"]
                        ?.InnerText) &&
                    !string.IsNullOrEmpty(configuration["Configuration"]?["LogManagementConfig"]?["LogPassword"]
                        ?.InnerText))
                    throw new Exception(
                        $"Config '{nameof(LogUserName)}' and '{nameof(LogPassword)}' Both must have value or both must be empty!");

                if (!string.IsNullOrEmpty(configuration["Configuration"]?["LogManagementConfig"]?["LogUserName"]
                        ?.InnerText) &&
                    !string.IsNullOrEmpty(configuration["Configuration"]?["LogManagementConfig"]?["LogPassword"]
                        ?.InnerText))
                {
                    // Set User And Pass
                    LogUserName = configuration["Configuration"]?["LogManagementConfig"]?["LogUserName"]?.InnerText
                                      .Length == 16
                        ? configuration["Configuration"]?["LogManagementConfig"]?["LogUserName"]?.InnerText
                        : configuration["Configuration"]?["LogManagementConfig"]?["LogUserName"]?.InnerText.Length > 16
                            ? configuration["Configuration"]?["LogManagementConfig"]?["LogUserName"]?.InnerText
                                .Substring(0, 16)
                            : configuration["Configuration"]?["LogManagementConfig"]?["LogUserName"]?.InnerText
                                .PadRight(16, '9');
                    LogPassword = configuration["Configuration"]?["LogManagementConfig"]?["LogPassword"]?.InnerText
                                      .Length == 16
                        ? configuration["Configuration"]?["LogManagementConfig"]?["LogPassword"]?.InnerText
                        : configuration["Configuration"]?["LogManagementConfig"]?["LogPassword"]?.InnerText.Length > 16
                            ? configuration["Configuration"]?["LogManagementConfig"]?["LogPassword"]?.InnerText
                                .Substring(0, 16)
                            : configuration["Configuration"]?["LogManagementConfig"]?["LogPassword"]?.InnerText
                                .PadRight(16, '9');

                    // Generate MD5 with sum user pass and generate key and iv
                    var tempAllKeys = LogUserName + LogPassword;
                    tempAllKeys = CreateMD5(tempAllKeys);
                    LogUserName = tempAllKeys.Substring(0, 16);
                    LogPassword = tempAllKeys.Substring(16, 16);

                    EnableEncryptionLog = true;
                }
                else
                {
                    LogUserName = LogPassword = null;
                    EnableEncryptionLog = false;
                }

                #endregion

                #region Set MaxFileSize

                try
                {
                    MaxFileSize = (int) Math.Abs(Math.Ceiling(
                        decimal.Parse(configuration["Configuration"]?["LogManagementConfig"]?["MaxFileSize"]
                            ?.InnerText) * 1000));
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        $"{nameof(MaxFileSize)} in config file 'G9Log.config' is not correct!", ex);
                }

                #endregion

                #region Set SaveTime

                try
                {
                    SaveTime = int.Parse(configuration["Configuration"]?["LogManagementConfig"]?["SaveTime"]
                        ?.InnerText);
                    SaveTime = SaveTime < 1 ? 1 : SaveTime;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(SaveTime)} in config file 'G9Log.config' is not correct!",
                        ex);
                }

                #endregion

                #region Set SaveCount

                try
                {
                    SaveCount = int.Parse(configuration["Configuration"]?["LogManagementConfig"]?["SaveCount"]
                        ?.InnerText);
                    SaveCount = SaveCount < 0 ? 0 : SaveCount;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(SaveCount)} in config file 'G9Log.config' is not correct!",
                        ex);
                }

                #endregion

                #region Set ZipArchivePreviousDay

                if (bool.TryParse(configuration["Configuration"]?["LogManagementConfig"]?["ComponentLog"]?.InnerText,
                    out var result2))
                    ZipArchivePreviousDay = result2;
                else
                    throw new ArgumentException($"{nameof(ZipArchivePreviousDay)} is not correct in config.",
                        nameof(ZipArchivePreviousDay));

                #endregion

                #endregion

                #region LogReaderConfig

                #region Set LogReaderDefaultCulture

                LogReaderDefaultCulture = configuration["Configuration"]?["LogReaderConfig"]?["LogReaderDefaultCulture"]
                    ?.InnerText;
                if (string.IsNullOrEmpty(LogReaderDefaultCulture))
                    LogReaderDefaultCulture = "en-us";

                #endregion

                #region Set LogReaderStarterPage

                if (Enum.TryParse(typeof(LogReaderPages),
                    configuration["Configuration"]?["LogReaderConfig"]?["LogReaderStarterPage"]?.InnerText,
                    out var pageResult))
                    LogReaderStarterPage = (LogReaderPages) pageResult;
                else
                    throw new InvalidEnumArgumentException($"{nameof(LogReaderStarterPage)} is not correct in config.");

                #endregion

                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception($"Error when read file 'G9Log.config' and parse config...\n{ex}");
            }
        }

        #endregion

        /// <summary>
        ///     Generate MD5 from text
        /// </summary>
        /// <param name="s">Specify text</param>
        /// <returns>Return MD5 from text</returns>

        #region CreateMD5

        private string CreateMD5(string s)
        {
            using (var md5 = MD5.Create())
            {
                var encoding = Encoding.ASCII;
                var data = encoding.GetBytes(s);

                Span<byte> hashBytes = stackalloc byte[16];
                md5.TryComputeHash(data, hashBytes, out var written);
                if (written != hashBytes.Length)
                    throw new OverflowException();


                Span<char> stringBuffer = stackalloc char[32];
                for (var i = 0; i < hashBytes.Length; i++)
                    hashBytes[i].TryFormat(stringBuffer.Slice(2 * i), out _, "x2");
                return new string(stringBuffer);
            }
        }

        #endregion

        /// <summary>
        ///     Get instance
        ///     Singleton pattern
        /// </summary>
        /// <returns>Instance of class</returns>

        #region G9LogConfig_Singleton

        public static G9LogConfigSingleton GetInstance()
        {
            if (_saveInstanceOfThisClass == null) _saveInstanceOfThisClass = new G9LogConfigSingleton();

            return _saveInstanceOfThisClass;
        }

        #endregion

        #endregion

        #region Fields And Properties

        private static G9LogConfigSingleton _saveInstanceOfThisClass;

        /// <summary>
        ///     Specify path for create log directory
        /// </summary>
        public string Path { get; }

        /// <summary>
        ///     Specify directory name date type
        ///     'Gregorian' or 'Shamsi'
        /// </summary>
        public DateTimeType DirectoryNameDateType { get; }

        /// <summary>
        ///     Enable log for component G9LogHandler
        ///     Logs like: start datetime, end datetime and other additional
        /// </summary>
        public bool ComponentLog { get; }

        /// <summary>
        ///     Enable stack info
        /// </summary>
        public LogsConfig EnableStackTraceInformation { get; }

        /// <summary>
        ///     Specify log type enable
        /// </summary>
        public LogsConfig ActiveLogs { get; }

        /// <summary>
        ///     Specify need encrypt data for log
        ///     open just by user pass
        /// </summary>
        public bool EnableEncryptionLog { get; }

        /// <summary>
        ///     Log UserName
        ///     If need open log with user and password
        /// </summary>
        public string LogUserName { get; }

        /// <summary>
        ///     Log Password
        ///     If need open log with user and password
        /// </summary>
        public string LogPassword { get; }

        /// <summary>
        ///     Specify max file size in byte
        /// </summary>
        public decimal MaxFileSize { get; }

        /// <summary>
        ///     Specify default culture for log reader
        /// </summary>
        public string LogReaderDefaultCulture { get; }

        /// <summary>
        ///     Specify time in second for save logs
        /// </summary>
        public int SaveTime { get; }

        /// <summary>
        ///     Specify count of logs for save logs
        /// </summary>
        public int SaveCount { get; }

        /// <summary>
        ///     Specify enable archive previous day
        /// </summary>
        public bool ZipArchivePreviousDay { get; }

        /// <summary>
        ///     Specify log reader starter page
        /// </summary>
        public LogReaderPages LogReaderStarterPage { get; }

        #endregion
    }
}