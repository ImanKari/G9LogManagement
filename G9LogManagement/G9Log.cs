using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using G9LogManagement.AESEncryptionDecryption;
using G9LogManagement.Config;
using G9LogManagement.Enums;
using G9LogManagement.Structures;
using G9ScheduleManagement;

namespace G9LogManagement
{
    /// <summary>
    ///     Static class for managed logs
    /// </summary>
    public static class G9Log
    {
        #region Fields And Properties

        /// <summary>
        ///     Scheduler for handle duration save log
        /// </summary>
        private static readonly G9Schedule _scheduler;

        /// <summary>
        ///     Concurrent Queue for data logs
        /// </summary>
        private static readonly ConcurrentQueue<G9LogItem> _queueOfLogData = new ConcurrentQueue<G9LogItem>();

        /// <summary>
        ///     Queue for data logs next day
        /// </summary>
        private static readonly Queue<G9LogItem> _queueOfLogDataNextDay = new Queue<G9LogItem>();

        /// <summary>
        ///     Specify G9LogReaderTemplate path
        /// </summary>
        public const string G9LogReaderPath = "G9LogReaderTemplate/";

        /// <summary>
        ///     Initialize unique identity for component log
        /// </summary>
        public const string G9LogIdentity = "G9TM";

        /// <summary>
        ///     Specify directory name for log data
        /// </summary>
        public const string DataDirectory = "Data/";

        /// <summary>
        ///     Specify default data file name
        /// </summary>
        public const string DataFileName = "G9DataLog.js";

        /// <summary>
        ///     Specify default setting file name
        /// </summary>
        public const string SettingFileName = "G9Setting.js";

        /// <summary>
        ///     Specify default config file name
        /// </summary>
        public const string ConfigFileName = "G9Config.js";

        /// <summary>
        ///     Specify default encoding sample text
        /// </summary>
        public const string DefaultEncodingSampleText = "This Is G9™ Team!";

        /// <summary>
        ///     Specify default change path sample text
        /// </summary>
        public const string DefaultChangePathText = "-ChangeConfig-";

        /// <summary>
        ///     Specify default language culture file path
        /// </summary>
        public const string DefaultLanguageCultureFile = "Utilities/LanguageHandler/DefaultCulture.js";

        /// <summary>
        ///     Specify minimum max file size in byte
        /// </summary>
        public const int MinimumMaxFileSizeInByte = 5000000;

        /// <summary>
        ///     Default time out for close stream when close app
        /// </summary>
        public const int DefaultTimeOutToCloseStreamWhenExitApp = 963;

        /// <summary>
        ///     Access to parsed config file
        ///     <para>Config file: 'G9Log.config'</para>
        /// </summary>
        public static G9LogConfigSingleton Configuration { get; }

        /// <summary>
        ///     Encoding for log write
        /// </summary>
        private static readonly UTF8Encoding Encoding = new UTF8Encoding(true);

        /// <summary>
        ///     Get full path of directory
        /// </summary>
        public static string CurrentLogDirectory { get; private set; }

        /// <summary>
        ///     Get full path of directory
        /// </summary>
        public static string CurrentLogFileAddress { get; private set; }

        /// <summary>
        ///     Field save start date time
        /// </summary>
        private static DateTime _startDateTime = DateTime.Now;

        private static DateTime _lastHourOfDateTime = DateTime.Parse($"{DateTime.Now:yyyy-MM-dd} 23:59:59.999");

        /// <summary>
        ///     After time out duration active field for save
        /// </summary>
        private static bool _activeTimeOutForSave;

        /// <summary>
        ///     Flag field specify ready for flush logs item
        /// </summary>
        private static bool _readyForFlushLogItems;

        /// <summary>
        ///     Field save stream length => file size
        /// </summary>
        private static long _streamLengthFileSize;

        /// <summary>
        ///     Field for lock
        /// </summary>
        private static readonly object WaitForFlushLogItems = new object();

        #endregion

        #region Methods

        /// <summary>
        ///     Constructor
        ///     Initialize Requirements
        /// </summary>

        #region G9Log

        static G9Log()
        {
            // Initialize folders and files
            var initializeFoldersAndFiles =
                new InitializeFoldersAndFilesForLogReaders();

            Configuration = G9LogConfigSingleton.GetInstance();
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.DomainUnload += OnProcessExit;

            if (Configuration.ComponentLog)
                G9LogInformationForComponentLog("Start G9LogManagement...", G9LogIdentity, "Start");

            // Initialize scheduler
            _scheduler = new G9Schedule();

            // Start scheduler handler
            ScheduleHandler();
        }

        #endregion

        /// <summary>
        ///     Scheduler handler
        /// </summary>

        #region ScheduleHandler

        private static void ScheduleHandler()
        {
            _scheduler
                .AddScheduleAction(() =>
                {
                    // Active flag for save logs in file
                    _activeTimeOutForSave = true;
                    // Run save logs
                    FlushLogsAndSave();
                })
                .SetDuration(TimeSpan.FromSeconds(Configuration.SaveTime))
                .AddErrorCallBack(exception => exception.G9LogException("Scheduler Error!", "Scheduler", "Scheduler"));
        }

        #endregion

        /// <summary>
        ///     Handle exception log
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="message">Additional message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>

        #region ExceptionLog

        public static void G9LogException(this Exception ex, string message = null, string identity = null,
            string title = null)
        {
            // Handle log
            G9LogManagement(LogsType.EXCEPTION, ExceptionErrorGenerate(ex, message), identity, title, ex);
        }

        #endregion

        /// <summary>
        ///     Handle error log
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>

        #region ErrorLog

        public static void G9LogError(this string message, string identity = null, string title = null)
        {
            // Handle log
            G9LogManagement(LogsType.ERROR, message, identity, title);
        }

        #endregion

        /// <summary>
        ///     Handle warning log
        /// </summary>
        /// <param name="message">Warning message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>

        #region G9LogWarning

        public static void G9LogWarning(this string message, string identity = null, string title = null)
        {
            // Handle log
            G9LogManagement(LogsType.WARN, message, identity, title);
        }

        #endregion

        /// <summary>
        ///     Handle information log
        /// </summary>
        /// <param name="message">Information message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>
        /// <param name="customDateTime">Set custom date time for log if need</param>
        /// <param name="forceSaveLogs">When need force save</param>

        #region G9LogInformationForComponentLog

        private static void G9LogInformationForComponentLog(this string message, string identity = null,
            string title = null, DateTime? customDateTime = null, bool forceSaveLogs = false)
        {
            // Handle log
            // Without check file size
            G9LogManagement(LogsType.EVENT, message, identity, title, null, false, customDateTime, forceSaveLogs);
        }

        #endregion

        /// <summary>
        ///     Handle information log
        /// </summary>
        /// <param name="message">Information message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>

        #region G9LogInformation

        public static void G9LogInformation(this string message, string identity = null, string title = null)
        {
            // Handle log
            G9LogManagement(LogsType.INFO, message, identity, title);
        }

        #endregion

        /// <summary>
        ///     Handle event log
        /// </summary>
        /// <param name="message">Event message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>

        #region G9LogEvent

        public static void G9LogEvent(this string message, string identity = null, string title = null)
        {
            // Handle log
            G9LogManagement(LogsType.EVENT, message, identity, title);
        }

        #endregion

        /// <summary>
        ///     Handler for events
        /// </summary>
        /// <param name="logType">Type of log</param>
        /// <param name="message">Message for log</param>
        /// <param name="identity">Identity for log</param>
        /// <param name="title">Log title</param>
        /// <param name="ex">Exception if exists</param>
        /// <param name="fileSizeCheck">Specify check file size or no</param>
        /// <param name="customDateTime">If need custom date time set it</param>
        /// <param name="forceSaveLogs">When need force save</param>

        #region G9LogManagement

        private static void G9LogManagement(LogsType logType, string message, string identity = null,
            string title = null,
            Exception ex = null, bool fileSizeCheck = true, DateTime? customDateTime = null, bool forceSaveLogs = false)
        {
            // fields for save stack trace information
            string fileName = string.Empty, methodBase = string.Empty, lineNumber = string.Empty;

            // Check if enable stack information log for this type
            if (Configuration.EnableStackTraceInformation.CheckValueByType(logType))
                if (ex != null)
                    (fileName, methodBase, lineNumber) = GetStackInformation(new StackTrace(ex, true));
                else
                    (fileName, methodBase, lineNumber) = GetStackInformation(new StackTrace(true));

            // Write log
            WriteLogAutomatic(
                new G9LogItem(logType, identity, title, message, fileName, methodBase, lineNumber,
                    customDateTime ?? DateTime.Now),
                fileSizeCheck, forceSaveLogs);
        }

        #endregion

        /// <summary>
        ///     Write logs data to log file
        /// </summary>
        /// <param name="logItem">Specify log item object</param>
        /// <param name="fileSizeCheck">Specify check file size or no</param>
        /// <param name="forceSaveLogs">When need force save</param>

        #region WriteLogAutomatic

        private static void WriteLogAutomatic(G9LogItem logItem, bool fileSizeCheck = true, bool forceSaveLogs = false)
        {
            if (!forceSaveLogs)
            {
                if (_readyForFlushLogItems)
                    lock (WaitForFlushLogItems)
                    {
                        // Add log item to queue
                        _queueOfLogData.Enqueue(logItem);
                    }
                else
                    // Add log item to queue
                    _queueOfLogData.Enqueue(logItem);


                FlushLogsAndSave(fileSizeCheck);
            }
            else
            {
                FlushLogsAndSave(fileSizeCheck, true, logItem);
            }
        }

        #endregion

        /// <summary>
        ///     Check requirement if true save logs to file
        /// </summary>
        /// <param name="fileSizeCheck">Specify check file size or no</param>
        /// <param name="forceSaveLogs">When need force save</param>

        #region FlushLogsAndSave

        private static void FlushLogsAndSave(bool fileSizeCheck = true, bool forceSaveLogs = false,
            G9LogItem? forceLogItem = null)
        {
            if (forceSaveLogs || _activeTimeOutForSave || _queueOfLogData.Count >= Configuration.SaveCount)
            {
                // Stop scheduler
                _scheduler?.Stop();
                lock (WaitForFlushLogItems)
                {
                    if (forceSaveLogs || _activeTimeOutForSave || _queueOfLogData.Count >= Configuration.SaveCount)
                    {
                        // Check path if save not force
                        if (!forceSaveLogs)
                        {
                            var generateNewFile = false;
                            if (fileSizeCheck &&
                                Configuration.MaxFileSize > MinimumMaxFileSizeInByte &&
                                Configuration.MaxFileSize <= _streamLengthFileSize)
                            {
                                generateNewFile = true;
                                _streamLengthFileSize = 0;
                            }

                            CheckAndHandlePath(generateNewFile);
                        }
                        else if (string.IsNullOrEmpty(CurrentLogFileAddress))
                        {
                            CheckAndHandlePath();
                        }

                        // Set flags
                        _readyForFlushLogItems = true;
                        _activeTimeOutForSave = false;

                        // Ignore other log for next day
                        var ignoreOtherLogForNextDay = false;

                        // Instance new stream and open
                        using (var _logFileStream = new FileStream(
                            CurrentLogFileAddress,
                            FileMode.OpenOrCreate,
                            FileAccess.ReadWrite,
                            FileShare.Read))
                        {
                            // Write bytes for next day
                            while (_queueOfLogDataNextDay.TryDequeue(out var logItemData))
                                WriteLogsToStream(logItemData, _logFileStream);

                            // Dequeue log Item for save to file
                            while (_queueOfLogData.TryDequeue(out var logItemData))
                            {
                                if (forceSaveLogs)
                                    if (logItemData.LogDateTime > _lastHourOfDateTime)
                                    {
                                        _queueOfLogDataNextDay.Enqueue(logItemData);
                                        ignoreOtherLogForNextDay = true;
                                    }

                                if (!ignoreOtherLogForNextDay) WriteLogsToStream(logItemData, _logFileStream);

                                // break loop if item for next day
                                if (ignoreOtherLogForNextDay && forceLogItem != null)
                                {
                                    WriteLogsToStream(forceLogItem.Value, _logFileStream);
                                    break;
                                }
                            }

                            // Set file size
                            _streamLengthFileSize = _logFileStream.Length;
                        }

                        // Reset duration of scheduler
                        _scheduler.ResetDuration();
                        // Resume scheduler
                        _scheduler.Resume();
                        // Finish flush logs
                        _readyForFlushLogItems = false;
                    }
                }
            }
        }

        #endregion

        /// <summary>
        ///     Write log item in file stream
        /// </summary>
        /// <param name="logItemData">Log item data</param>
        /// <param name="fileStream">File stream</param>

        #region WriteLogsToStream

        private static void WriteLogsToStream(G9LogItem logItemData, FileStream fileStream)
        {
            if (fileStream.Length <= 2)
            {
                fileStream.Write(Encoding.GetBytes(EncodeJsString(
                    $"G9DataLog.push({GenerateLogByInformation(logItemData.LogType, logItemData.Identity, logItemData.Title, logItemData.Body, logItemData.FileName, logItemData.MethodBase, logItemData.LineNumber, logItemData.LogDateTime, false)});"
                )));
            }
            else
            {
                fileStream.Seek(-2, SeekOrigin.End);
                fileStream.Write(Encoding.GetBytes(
                    EncodeJsString(
                        $"{GenerateLogByInformation(logItemData.LogType, logItemData.Identity, logItemData.Title, logItemData.Body, logItemData.FileName, logItemData.MethodBase, logItemData.LineNumber, logItemData.LogDateTime, true)});"
                    )));
            }
        }

        #endregion

        /// <summary>
        ///     Generate log with standard format with parameter information
        /// </summary>
        /// <returns>Generated log</returns>

        #region GenerateLogByInformation

        private static string GenerateLogByInformation(LogsType logType, string identity, string title, string body,
            string fileName, string methodBase, string lineNumber, DateTime logDateTime, bool addCommaInFirst)
        {
            /* JavaScript G9DataLog Format:
            * Array [
            * byte logType,
            * string identity,
            * string ({title}🅖➒{body}🅖➒{path}🅖➒{method}🅖➒{line}), // Encrypt this column if encryption enable
            * string logDateTime:yyyy/MM/dd HH:mm:ss
            * ]
            */

            // If encrypt enable => encrypt all data before write
            if (Configuration.EnableEncryptionLog)
            {
                string key = Configuration.LogUserName, iv = Configuration.LogPassword;
                return
                    $"{(addCommaInFirst ? "," : string.Empty)}[{(byte) logType},'{identity}','{AES128.EncryptString($"{title}🅖➒{body}🅖➒{fileName}🅖➒{methodBase}🅖➒{lineNumber}", key, iv, out _)}','{logDateTime.ToString("yyyy/MM/dd HH:mm:ss.ff")}']";
            }

            // Normal write
            return
                $"{(addCommaInFirst ? "," : string.Empty)}[{(byte) logType},'{identity}','{title}🅖➒{body}🅖➒{fileName}🅖➒{methodBase}🅖➒{lineNumber}','{logDateTime:yyyy/MM/dd HH:mm:ss.ff}']";
        }

        #endregion

        /// <summary>
        ///     Add stack trace information
        /// </summary>
        /// <param name="stackTrace">Stack trace object</param>
        /// <returns>return (string FileName, string MethodBase, string LineNumber)</returns>

        #region GetStackInformation

        private static (string, string, string) GetStackInformation(StackTrace stackTrace)
        {
            return
                // Check if exists stack trace frame count => add stack trace information
                stackTrace.FrameCount > 1
                    ? (
                        stackTrace.GetFrame(stackTrace.FrameCount - 1).GetFileName(),
                        stackTrace.GetFrame(stackTrace.FrameCount - 1).GetMethod().ToString(),
                        stackTrace.GetFrame(stackTrace.FrameCount - 1).GetFileLineNumber().ToString()
                    )
                    // Else return empty
                    : (string.Empty, string.Empty, string.Empty);
        }

        #endregion

        /// <summary>
        ///     On process exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        #region OnProcessExit

        private static void OnProcessExit(object sender, EventArgs e)
        {
            // Log for exit
            if (Configuration.ComponentLog)
                G9LogInformationForComponentLog("Stop G9LogManagement and close app...", G9LogIdentity,
                    "Stop And Close");

            // Update old setting file
            CloseSettingFileAndSetCloseReason(ReasonCloseType.ExitApp);

            // Flush force file
            FlushLogsAndSave(false, true);

            // Sleep for wait insert data
            Thread.Sleep(DefaultTimeOutToCloseStreamWhenExitApp);
        }

        #endregion

        /// <summary>
        ///     Check path and date time
        ///     Create new path and initialize requirement
        /// </summary>

        #region CheckAndHandlePath

        private static void CheckAndHandlePath(bool generateNewPathBecauseFileSizeIsLarge = false)
        {
            // If directory path is null or change day => Generate new path and initialize requirement
            if (generateNewPathBecauseFileSizeIsLarge ||
                string.IsNullOrEmpty(CurrentLogDirectory) || _startDateTime.Year != DateTime.Now.Year ||
                _startDateTime.Month != DateTime.Now.Month || _startDateTime.Day != DateTime.Now.Day)
            {
                // Save old path
                var oldPath = CurrentLogDirectory;

                // Set close reason
                var closeReason = generateNewPathBecauseFileSizeIsLarge
                    ? ReasonCloseType.FileSize
                    : oldPath == null
                        ? ReasonCloseType.Unknown
                        : ReasonCloseType.ChangeDay;

                // Log for Change path
                if (closeReason == ReasonCloseType.ChangeDay && Configuration.ComponentLog)
                {
                    // Generate last time for previous date
                    _lastHourOfDateTime = DateTime.Parse($"{_startDateTime:yyyy-MM-dd} 23:59:59.999");
                    // Set log change day
                    G9LogInformationForComponentLog(
                        $"Finish day and generate path for new day...\nOld path is: {oldPath}\nNew path is: {CurrentLogDirectory}",
                        G9LogIdentity, "Change path",
                        DateTime.Now > _lastHourOfDateTime ? _lastHourOfDateTime : DateTime.Now, true);
                }

                // Set new date time
                _startDateTime = DateTime.Now;

                // Generate directory name
                if (Configuration.DirectoryNameDateType == DateTimeType.Gregorian)
                {
                    CurrentLogDirectory =
                        Path.Combine(Configuration.Path, _startDateTime.ToString("yyyy-MM-dd/"));
                }
                else
                {
                    var pc = new PersianCalendar();
                    CurrentLogDirectory = Path.Combine(Configuration.Path,
                        $"{pc.GetYear(_startDateTime)}-{pc.GetMonth(_startDateTime)}-{pc.GetDayOfMonth(_startDateTime)}/");
                }

                // Close update old setting file
                CloseSettingFileAndSetCloseReason(closeReason, oldPath);

                // Generate data file path js
                var (settingFilePath, tempCurrentLogFileAddress) = GenerateSettingAndDataFilePath();
                CurrentLogFileAddress = tempCurrentLogFileAddress;

                // Check if not exists directory create it
                CopyDirectoryAndFileBetweenTwoPath(G9LogReaderPath, CurrentLogDirectory);

                // Set log reader configuration
                SetLogReaderConfiguration(CurrentLogDirectory);

                // Create setting file
                CreateSettingFile(settingFilePath);

                // Log for Change path
                if (Configuration.ComponentLog)
                    G9LogInformationForComponentLog(
                        $"Generate new path and for new day...\nNew path is: {CurrentLogDirectory}",
                        G9LogIdentity, "Generate new path");

                // Generate last time for current date
                _lastHourOfDateTime = DateTime.Parse($"{_startDateTime:yyyy-MM-dd} 23:59:59.999");

                // Set new stream length file size
                _streamLengthFileSize = 0;
            }
        }

        #endregion


        /// <summary>
        ///     Generate path for setting and data file
        /// </summary>
        /// <returns>return (string SettingFilePath, string DataFilePath)</returns>

        #region GenerateSettingAndDataFilePath

        private static (string, string) GenerateSettingAndDataFilePath()
        {
            int i = -1, j = 0;
            var existNewConfigPath = false;
            // Check main directory
            while (true)
                if (Directory.Exists(
                    Path.Combine(
                        CurrentLogDirectory.Substring(0,
                            CurrentLogDirectory.Length - 1) + DefaultChangePathText + ++i)))
                {
                    existNewConfigPath = true;
                }
                else
                {
                    i--;
                    break;
                }

            // Check and generate config file
            CurrentLogDirectory = CheckAndGenerateMainConfigAndGenerateNewPath(existNewConfigPath, i);

            // Check inner directory
            while (true)
            {
                var newSettingPath = Path.Combine(CurrentLogDirectory, DataDirectory, $"{j}-{SettingFileName}");
                var newDataPath = Path.Combine(CurrentLogDirectory, DataDirectory, $"{j}-{DataFileName}");

                if (!File.Exists(newSettingPath))
                    return (newSettingPath, newDataPath);

                j++;
            }
        }

        #endregion

        /// <summary>
        ///     Check config file, generate new config or change path if need
        ///     Generate new config if not exist
        ///     Change path if config changed
        /// </summary>
        /// <returns>New CurrentLogDirectory path</returns>

        #region CheckAndGenerateMainConfigAndGenerateNewPath

        private static string CheckAndGenerateMainConfigAndGenerateNewPath(
            bool existNewConfigPath, int newConfigPathIndex)
        {
            var newPathForCheck = existNewConfigPath
                ? CurrentLogDirectory.Substring(0, CurrentLogDirectory.Length - 1)
                  + DefaultChangePathText + newConfigPathIndex
                : CurrentLogDirectory;

            var dicrectoryPath = Path.Combine(newPathForCheck, DataDirectory);
            if (!Directory.Exists(dicrectoryPath))
                Directory.CreateDirectory(dicrectoryPath);

            var configPathFile = Path.Combine(dicrectoryPath, ConfigFileName);
            // Check config file if exists
            if (File.Exists(configPathFile))
            {
                var decoding = string.Empty;
                var configFileLines = File.ReadAllLines(configPathFile);
                for (var i = 0; i < configFileLines.Length; i++)
                    if (configFileLines[i].Contains("G9Encoding"))
                    {
                        var encryptionText = configFileLines[i]
                            .Replace("var G9Encoding = '", string.Empty).Replace("';", string.Empty);

                        // If encryption is true but encrypt text is empty => generate new path
                        // Previous config with out encryption but current config with encryption
                        if (string.IsNullOrEmpty(encryptionText) && Configuration.EnableEncryptionLog)
                            return CheckAndGenerateMainConfigAndGenerateNewPath(true, newConfigPathIndex + 1);
                        // Else If encryption is false but encrypt text is not empty => generate new path
                        // Previous config with encryption but current config with out encryption

                        if (!Configuration.EnableEncryptionLog && !string.IsNullOrEmpty(encryptionText))
                            return CheckAndGenerateMainConfigAndGenerateNewPath(true, newConfigPathIndex + 1);
                        // Previous config and current config without encryption

                        if (!Configuration.EnableEncryptionLog)
                            return newPathForCheck;
                        // Else check encrypt user pass between Previous and current config

                        decoding = AES128.DecryptString(
                            encryptionText, Configuration.LogUserName, Configuration.LogPassword, out var error);
                        // If exist and config is true
                        // Previous config Equal current config
                        if (string.IsNullOrEmpty(error) && decoding.Contains(DefaultEncodingSampleText))
                            return newPathForCheck;
                        // if exist and config change Generate new path
                        // Previous config User Pass changed
                        return CheckAndGenerateMainConfigAndGenerateNewPath(true, newConfigPathIndex + 1);
                    }

                // If encoding not found! delete and remake
                File.Delete(configPathFile);
                return CheckAndGenerateMainConfigAndGenerateNewPath(existNewConfigPath, newConfigPathIndex);
            }
            // Create new config file

            var encoding = string.Empty;

            if (Configuration.EnableEncryptionLog)
            {
                encoding = AES128.EncryptString(
                    $"{DefaultEncodingSampleText}- {Guid.NewGuid()}-{DateTime.Now:yyyy/MM/dd HH:mm:ss}",
                    Configuration.LogUserName, Configuration.LogPassword, out var error);

                if (!string.IsNullOrEmpty(error))
                    throw new Exception(error, new Exception(new StackTrace().ToString()));
            }

            using var configFile = File.CreateText(configPathFile);
            configFile.WriteLine($"var G9Encoding = '{encoding}';");
            configFile.WriteLine($"var G9DefaultPage = '{(byte) Configuration.LogReaderStarterPage}';");

            return newPathForCheck;
        }

        #endregion

        /// <summary>
        ///     Update old setting file and set close date time and reason
        /// </summary>

        #region CloseSettingFileAndSetCloseReason

        private static void CloseSettingFileAndSetCloseReason(ReasonCloseType closeReason, string oldPath = null)
        {
            var i = 0;
            var oldSettingPath = string.Empty;
            string archivePath = null;
            // Specify path
            if (closeReason == ReasonCloseType.ChangeDay && !string.IsNullOrEmpty(oldPath))
            {
                archivePath = oldPath;
                oldSettingPath = Path.Combine(oldPath, DataDirectory);
            }
            else
            {
                oldSettingPath = Path.Combine(CurrentLogDirectory, DataDirectory);
            }


            // If firs file or folder not exists = exit func
            if (!Directory.Exists(oldSettingPath) ||
                !File.Exists(Path.Combine(oldSettingPath, $"{i}-{SettingFileName}")))
                return;

            while (true)
            {
                var checkPath = Path.Combine(oldSettingPath, $"{i}-{SettingFileName}");

                if (!File.Exists(checkPath))
                    if (i > 0)
                    {
                        checkPath = Path.Combine(oldSettingPath, $"{i - 1}-{SettingFileName}");

                        var oldDataPathFile = string.Empty;
                        if (File.Exists(Path.Combine(oldSettingPath, $"{i - 1}-{DataFileName}")))
                            oldDataPathFile = Path.Combine(oldSettingPath, $"{i - 1}-{DataFileName}");

                        var settingFileLines = File.ReadAllLines(checkPath);

                        for (var j = 0; j < settingFileLines.Length; j++)
                            if (settingFileLines[j].Contains("G9FinishDateTime"))
                            {
                                settingFileLines[j] = $"G9FinishDateTime.push('{DateTime.Now:yyyy-MM-dd HH:mm:ss}');";
                            }
                            else if (settingFileLines[j].Contains("G9FileCloseReason"))
                            {
                                if (!settingFileLines[j].Contains(ReasonCloseType.ExitApp.ToString()))
                                    settingFileLines[j] =
                                        $"G9FileCloseReason.push('{(closeReason == ReasonCloseType.Unknown ? ReasonCloseType.Restart : closeReason)}');";
                            }
                            else if (settingFileLines[j].Contains("G9FileSize"))
                            {
                                settingFileLines[j] =
                                    $"G9FileSize.push('{(string.IsNullOrEmpty(oldDataPathFile) ? string.Empty : (new FileInfo(oldDataPathFile).Length / (decimal) 1024).ToString("#.###"))}');";
                            }

                        using var newTask = new StreamWriter(checkPath, false);
                        foreach (var line in settingFileLines) newTask.WriteLine(line);

                        break;
                    }

                i++;
            }

            // If enable LogHandlerConfig.ZipArchivePreviousDay and
            // If change day => archive previous day
            if (Configuration.ZipArchivePreviousDay && closeReason == ReasonCloseType.ChangeDay &&
                !string.IsNullOrEmpty(oldPath))
                GenerateDirectoryZipArchiveAndRemoveDirectoty(archivePath);
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

        public static string EncodeJsString(string text)
        {
            var sb = new StringBuilder();
            foreach (var c in text)
                switch (c)
                {
                    case '\"':
                        sb.Append("\\\"");
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
                        var i = c;
                        if (i < 32 || i > 127)
                            sb.AppendFormat("\\u{0:X04}", i);
                        else
                            sb.Append(c);
                        break;
                }

            sb.Replace(Environment.NewLine, "\n");

            return sb.ToString();
        }

        #endregion

        /// <summary>
        ///     Copy file and directory between two path address
        /// </summary>
        /// <param name="sourcePath">Source path address</param>
        /// <param name="destinationPath">Destination path address</param>

        #region CopyDirectoryAndFileBetweenTwoPath

        private static void CopyDirectoryAndFileBetweenTwoPath(string sourcePath, string destinationPath)
        {
            //Now Create all of the directories
            foreach (var dirPath in Directory.GetDirectories(sourcePath, "*",
                SearchOption.AllDirectories))
            {
                var pathChecked = dirPath.Replace(sourcePath, destinationPath);
                if (!Directory.Exists(pathChecked))
                    Directory.CreateDirectory(pathChecked);
            }

            //Copy all the files & Replaces any files with the same name
            foreach (var newPath in Directory.GetFiles(sourcePath, "*.*",
                SearchOption.AllDirectories))
            {
                var pathFileChecked = newPath.Replace(sourcePath, destinationPath);
                if (!File.Exists(pathFileChecked)) File.Copy(newPath, pathFileChecked, true);
            }
        }

        #endregion

        /// <summary>
        ///     Create new setting file for log
        /// </summary>
        /// <param name="path">Path of setting file</param>

        #region CreateSettingFile

        private static void CreateSettingFile(string path)
        {
            using var settingFile = File.CreateText(path);
            settingFile.WriteLine($"G9StartDateTime.push('{DateTime.Now:yyyy-MM-dd HH:mm:ss}');");
            settingFile.WriteLine("G9FinishDateTime.push('');");
            settingFile.WriteLine("G9FileSize.push('');");
            settingFile.WriteLine($"G9FileCloseReason.push('{ReasonCloseType.OpenOrForceClose}');");
        }

        #endregion

        /// <summary>
        ///     Write log reader configuration
        ///     Set default language culture
        /// </summary>
        /// <param name="path">Path for log reader</param>

        #region SetLogReaderConfiguration

        private static void SetLogReaderConfiguration(string path)
        {
            using (var cultureStreamWriter = new StreamWriter(Path.Combine(path, DefaultLanguageCultureFile), false))
            {
                cultureStreamWriter.Write($"var DefaultCulture = '{Configuration.LogReaderDefaultCulture}';");
            }
        }

        #endregion

        /// <summary>
        ///     Generate message from exception
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="additionalMessage">Additional message</param>
        /// <returns>Generated message</returns>

        #region ExceptionErrorGenerate

        private static string ExceptionErrorGenerate(Exception ex, string additionalMessage)
        {
            var exceptionMessage = new StringBuilder();
            // Add additioanal
            if (!string.IsNullOrEmpty(additionalMessage))
                exceptionMessage.Append(
                    $"###### Additional Message ######{Environment.NewLine}{additionalMessage}{Environment.NewLine}");
            // Add exception message
            exceptionMessage.Append(
                $"###### Exception Message ######{Environment.NewLine}{ex.Message}{Environment.NewLine}");
            // Add stack trace if exists
            if (!string.IsNullOrEmpty(ex.StackTrace))
                exceptionMessage.Append(
                    $"###### StackTrace ######{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");
            // Add inner exception if exists
            if (ex.InnerException != null)
                exceptionMessage.Append(
                    $"###### Inner Exception ######{Environment.NewLine}{ExceptionErrorGenerate(ex.InnerException, null)}{Environment.NewLine}");
            return exceptionMessage.ToString();
        }

        #endregion

        /// <summary>
        ///     Generate zip by directory path
        ///     remove directory with all files after generate zip
        /// </summary>
        /// <param name="directoryPath">Specify directory path</param>

        #region GenerateDirectoryZipArchiveAndRemoveDirectoty

        private static void GenerateDirectoryZipArchiveAndRemoveDirectoty(string directoryPath)
        {
            // Generate archive
            ZipFile.CreateFromDirectory(directoryPath, directoryPath.Trim('/') + ".zip"
                , CompressionLevel.Optimal, true);

            var di = new DirectoryInfo(directoryPath);
            // Delete all files
            foreach (var file in di.GetFiles())
                file.Delete();
            // Delete all directoy
            foreach (var dir in di.GetDirectories())
                dir.Delete(true);
            // Delete main directory
            Directory.Delete(directoryPath);
        }

        #endregion

        #endregion
    }
}