using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using G9ConfigManagement;
using G9LogManagement.AESEncryptionDecryption;
using G9LogManagement.Config;
using G9LogManagement.Enums;
using G9LogManagement.Structures;
using G9ScheduleManagement;

namespace G9LogManagement
{
    /// <summary>
    ///     Abstract Class for managed logs
    /// </summary>
    public class G9Log
    {
        #region Fields And Properties

        /// <summary>
        ///     Scheduler for handle duration save log
        /// </summary>
        private readonly G9Schedule _scheduler;

        /// <summary>
        ///     Concurrent Queue for data logs
        /// </summary>
        private readonly ConcurrentQueue<G9LogItem> _queueOfLogData1 = new ConcurrentQueue<G9LogItem>();

        /// <summary>
        ///     Concurrent Queue for data logs
        /// </summary>
        private readonly ConcurrentQueue<G9LogItem> _queueOfLogData2 = new ConcurrentQueue<G9LogItem>();

        /// <summary>
        ///     Queue for data logs next day
        /// </summary>
        private readonly Queue<G9LogItem> _queueOfLogDataNextDay = new Queue<G9LogItem>();

        /// <summary>
        ///     Specify log name
        /// </summary>
        public readonly string LogName;

        /// <summary>
        ///     Access to parsed config file
        ///     <para>Config file: 'G9Log.config'</para>
        /// </summary>
        private readonly G9ConfigManagement_Singleton<LogConfig> _configuration;

        /// <summary>
        ///     Encoding for log write
        /// </summary>
        private readonly UTF8Encoding _encoding = new UTF8Encoding(true);

        /// <summary>
        ///     Get full path of directory
        /// </summary>
        public string CurrentLogDirectory { get; private set; }

        /// <summary>
        ///     Get full path of directory
        /// </summary>
        public string CurrentLogFileAddress { get; private set; }

        /// <summary>
        ///     Field save start date time
        /// </summary>
        private DateTime _startDateTime = DateTime.Now;

        /// <summary>
        ///     Specify last hour of date time
        /// </summary>
        private DateTime _lastHourOfDateTime = DateTime.Parse($"{DateTime.Now:yyyy-MM-dd} 23:59:59.999");

        /// <summary>
        ///     After time out duration active field for save
        /// </summary>
        private bool _activeTimeOutForSave;

        /// <summary>
        ///     Flag field specify ready space for flush logs item
        /// </summary>
        private FlushLogsSpace _spaceForFlushLogItems;

        /// <summary>
        ///     Flag true when flush log items to file
        /// </summary>
        private bool _workOnFlushLogItem;

        /// <summary>
        ///     Field save stream length => file size
        /// </summary>
        private long _streamLengthFileSize;

        /// <summary>
        ///     Field for lock
        /// </summary>
        private readonly object _waitForFlushLogItems = new object();

        /// <summary>
        ///     Specified enable event log
        /// </summary>
        public readonly bool IsEnableEventLog;

        /// <summary>
        ///     Specified enable information log
        /// </summary>
        public readonly bool IsEnableInformationLog;

        /// <summary>
        ///     Specified enable warning log
        /// </summary>
        public readonly bool IsEnableWarningLog;

        /// <summary>
        ///     Specified enable error log
        /// </summary>
        public readonly bool IsEnableErrorLog;

        /// <summary>
        ///     Specified enable exception log
        /// </summary>
        public readonly bool IsEnableExceptionLog;

        #endregion

        #region Methods

        /// <summary>
        ///     Constructor
        ///     Initialize Requirements
        /// </summary>
        /// <param name="customLogName">
        ///     <para>Optional</para>
        ///     <para>Specified custom log name.</para>
        ///     <para>You will have a different configuration and config path for each name</para>
        /// </param>
        /// <param name="customLogConfig">
        ///     <para>Optional</para>
        ///     <para>Specify custom object for create config xml file.</para>
        ///     <para>Just for create, if created don't use </para>
        /// </param>

        #region G9Log

        public G9Log(string customLogName = "Default", LogConfig customLogConfig = null)
        {
            if (string.IsNullOrEmpty(customLogName))
                throw new ArgumentNullException(nameof(customLogName),
                    $"Config version property '{nameof(customLogName)}', can be null!");

            // Set log name
            LogName = customLogName;

            // Initialize folders and files
            new InitializeFoldersAndFilesForLogReaders();

            // Load configs
            _configuration = G9ConfigManagement_Singleton<LogConfig>.GetInstance(
                string.Format(G9LogConst.G9ConfigName, customLogName), customLogConfig, customLogConfig != null);

            // Set enable item
                IsEnableEventLog = _configuration.Configuration.ActiveLogs.EVENT;
            IsEnableInformationLog = _configuration.Configuration.ActiveLogs.INFO;
            IsEnableWarningLog = _configuration.Configuration.ActiveLogs.WARN;
            IsEnableErrorLog = _configuration.Configuration.ActiveLogs.ERROR;
            IsEnableExceptionLog = _configuration.Configuration.ActiveLogs.EXCEPTION;

            // Set event for exit or unload
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.DomainUnload += OnProcessExit;

            if (_configuration.Configuration.ComponentLog)
                G9LogInformationForComponentLog("Start G9LogManagement...", G9LogConst.G9LogIdentity, "Start");

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

        private void ScheduleHandler()
        {
            _scheduler
                .AddScheduleAction(() =>
                {
                    // Active flag for save logs in file
                    _activeTimeOutForSave = true;
                    // Run save logs
                    FlushLogsAndSave();
                })
                .SetDuration(TimeSpan.FromSeconds(_configuration.Configuration.SaveTime))
                .AddErrorCallBack(exception => G9LogException(exception, "Scheduler Error!", "Scheduler", "Scheduler"));
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

        public void G9LogException(Exception ex, string message = null, string identity = null,
            string title = null)
        {
            // Ignore if disable log type
            if (!IsEnableExceptionLog) return;

            // Handle log
            Task.Run(() => G9LogManagement(LogsType.EXCEPTION, ExceptionErrorGenerate(ex, message), identity, title, ex));
        }

        #endregion

        /// <summary>
        ///     Handle error log
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>

        #region ErrorLog

        public void G9LogError(string message, string identity = null, string title = null)
        {
            // Ignore if disable log type
            if (!IsEnableErrorLog) return;

            // Handle log
            Task.Run(() => G9LogManagement(LogsType.ERROR, message, identity, title));
        }

        #endregion

        /// <summary>
        ///     Handle warning log
        /// </summary>
        /// <param name="message">Warning message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>

        #region G9LogWarning

        public void G9LogWarning(string message, string identity = null, string title = null)
        {
            // Ignore if disable log type
            if (!IsEnableWarningLog) return;

            // Handle log
            Task.Run(() => G9LogManagement(LogsType.WARN, message, identity, title));
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

        private void G9LogInformationForComponentLog(string message, string identity = null,
            string title = null, DateTime? customDateTime = null, bool forceSaveLogs = false)
        {
            // Handle log
            // Without check file size
            Task.Run(() => G9LogManagement(LogsType.EVENT, message, identity, title, null, false, customDateTime, forceSaveLogs));
        }

        #endregion

        /// <summary>
        ///     Handle information log
        /// </summary>
        /// <param name="message">Information message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>

        #region G9LogInformation

        public void G9LogInformation(string message, string identity = null, string title = null)
        {
            // Ignore if disable log type
            if (!IsEnableInformationLog) return;

            // Handle log
            Task.Run(() => G9LogManagement(LogsType.INFO, message, identity, title));
        }

        #endregion

        /// <summary>
        ///     Handle event log
        /// </summary>
        /// <param name="message">Event message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>

        #region G9LogEvent

        public void G9LogEvent(string message, string identity = null, string title = null)
        {
            // Ignore if disable log type
            if (!IsEnableEventLog) return;

            // Handle log
            Task.Run(() => G9LogManagement(LogsType.EVENT, message, identity, title));
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

        private void G9LogManagement(LogsType logType, string message, string identity = null,
            string title = null,
            Exception ex = null, bool fileSizeCheck = true, DateTime? customDateTime = null, bool forceSaveLogs = false)
        {
            // fields for save stack trace information
            string fileName = string.Empty, methodBase = string.Empty, lineNumber = string.Empty;

            // Check if enable stack information log for type
            if (_configuration.Configuration.EnableStackTraceInformation.CheckValueByType(logType))
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

        private void WriteLogAutomatic(G9LogItem logItem, bool fileSizeCheck = true, bool forceSaveLogs = false)
        {
            if (!forceSaveLogs)
            {
                // Add log item to queue
                if (_spaceForFlushLogItems == FlushLogsSpace.QueueOfLogData1)
                    // if flush is space 1 add items to space 2
                    _queueOfLogData2.Enqueue(logItem);
                else
                    // if flush is space 2 add items to space 1
                    _queueOfLogData1.Enqueue(logItem);
                
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
        /// <param name="forceLogItem">Specify item for save in the last</param>

        #region FlushLogsAndSave

        private void FlushLogsAndSave(bool fileSizeCheck = true, bool forceSaveLogs = false,
            G9LogItem? forceLogItem = null)
        {
            if (!forceSaveLogs && _workOnFlushLogItem)
                return;

            // Set flags
            _workOnFlushLogItem = true;

            start1:

            // if previous space is 1 current item add to space 2 else add item to space 1
            var flushSpace = _spaceForFlushLogItems == FlushLogsSpace.QueueOfLogData1
                ? _queueOfLogData2
                : _queueOfLogData1;

            if (flushSpace.Any() && (forceSaveLogs || _activeTimeOutForSave ||
                                     flushSpace.Count >= _configuration.Configuration.SaveCount))
            {
                // Stop scheduler
                _scheduler?.Stop();
                lock (_waitForFlushLogItems)
                {
                    if (flushSpace.Any() && (forceSaveLogs || _activeTimeOutForSave ||
                                             flushSpace.Count >= _configuration.Configuration.SaveCount))
                    {
                        Debug.WriteLine($"######### FlushLogsSpace: {_spaceForFlushLogItems} #########");
                        Debug.WriteLine($"######### FlushLogsSpaceCount: {flushSpace.LongCount()} #########");

                        // Switch space 
                        _spaceForFlushLogItems = _spaceForFlushLogItems == FlushLogsSpace.QueueOfLogData1
                            ? FlushLogsSpace.QueueOfLogData2
                            : FlushLogsSpace.QueueOfLogData1;

                        Start2:
                        _activeTimeOutForSave = false;

                        // Check path if save not force
                        if (!forceSaveLogs)
                        {
                            var generateNewFile = false;
                            if (fileSizeCheck &&
                                _configuration.Configuration.MaxFileSizeInByte <= _streamLengthFileSize)
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

                        // Ignore other log for next day
                        var ignoreOtherLogForNextDay = false;

                        bool flagBreakSize = false;

                        // Instance new stream and open
                        using (var logFileStream = new FileStream(
                            CurrentLogFileAddress,
                            FileMode.OpenOrCreate,
                            FileAccess.ReadWrite,
                            FileShare.Read))
                        {
                            // Write bytes for next day
                            while (_queueOfLogDataNextDay.Any())
                                if (_queueOfLogDataNextDay.TryDequeue(out var logItemData))
                                    WriteLogsToStream(logItemData, logFileStream);

                            // Dequeue log Item for save to file
                            while (!flushSpace.IsEmpty)
                            {
                                if (flushSpace.TryDequeue(out var logItemData))
                                {
                                    if (forceSaveLogs)
                                        if (logItemData.LogDateTime > _lastHourOfDateTime)
                                        {
                                            _queueOfLogDataNextDay.Enqueue(logItemData);
                                            ignoreOtherLogForNextDay = true;
                                        }

                                    if (!ignoreOtherLogForNextDay) WriteLogsToStream(logItemData, logFileStream);

                                    // break loop if item for next day
                                    if (ignoreOtherLogForNextDay && forceLogItem != null)
                                    {
                                        WriteLogsToStream(forceLogItem.Value, logFileStream);
                                        break;
                                    }

                                    if (fileSizeCheck && _configuration.Configuration.MaxFileSizeInByte <=
                                        logFileStream.Length)
                                    {
                                        flagBreakSize = true;
                                        break;
                                    }
                                }
                            }

                            // Set file size
                            _streamLengthFileSize = logFileStream.Length;
                        }

                        if (flagBreakSize)
                            goto Start2;

                        // Reset duration of scheduler
                        if (_scheduler != null)
                        {
                            _scheduler.ResetDuration();
                            // Resume scheduler
                            _scheduler.Resume();
                        }
                    }
                }
                goto start1;
            }

            // Finish flush logs
            _workOnFlushLogItem = false;
        }

        #endregion

        /// <summary>
        ///     Write log item in file stream
        /// </summary>
        /// <param name="logItemData">Log item data</param>
        /// <param name="fileStream">File stream</param>

        #region WriteLogsToStream

        private void WriteLogsToStream(G9LogItem logItemData, FileStream fileStream)
        {
            if (fileStream.Length <= 2)
            {
                fileStream.Write(_encoding.GetBytes(EncodeJsString(
                    $"G9DataLog.push({GenerateLogByInformation(logItemData.LogType, logItemData.Identity, logItemData.Title, logItemData.Body, logItemData.FileName, logItemData.MethodBase, logItemData.LineNumber, logItemData.LogDateTime, false)});"
                )));
            }
            else
            {
                fileStream.Seek(-2, SeekOrigin.End);
                fileStream.Write(_encoding.GetBytes(
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

        private string GenerateLogByInformation(LogsType logType, string identity, string title, string body,
            string fileName, string methodBase, string lineNumber, DateTime logDateTime, bool addCommaInFirst)
        {
            /* JavaScript G9DataLog Format:
            * Array [
            * byte logType,
            * string identity,
            * string ({title}🅖➒{body}🅖➒{path}🅖➒{method}🅖➒{line}), // Encrypt column if encryption enable
            * string logDateTime:yyyy/MM/dd HH:mm:ss
            * ]
            */

            // If encrypt enable => encrypt all data before write
            if (_configuration.Configuration.EnableEncryptionLog)
            {
                string key = _configuration.Configuration.EncryptedUserName,
                    iv = _configuration.Configuration.EncryptedPassword;
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

        private (string, string, string) GetStackInformation(StackTrace stackTrace)
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

        private void OnProcessExit(object sender, EventArgs e)
        {
            // Log for exit
            if (_configuration.Configuration.ComponentLog)
                G9LogInformationForComponentLog("Stop G9LogManagement and close app...", G9LogConst.G9LogIdentity,
                    "Stop And Close");

            // Update old setting file
            CloseSettingFileAndSetCloseReason(ReasonCloseType.ExitApp);

            // Flush force file
            FlushLogsAndSave(false, true);

            // Sleep for wait insert data
            Thread.Sleep(G9LogConst.DefaultTimeOutToCloseStreamWhenExitApp);
        }

        #endregion

        /// <summary>
        ///     Check path and date time
        ///     Create new path and initialize requirement
        /// </summary>

        #region CheckAndHandlePath

        private void CheckAndHandlePath(bool generateNewPathBecauseFileSizeIsLarge = false)
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
                if (closeReason == ReasonCloseType.ChangeDay && _configuration.Configuration.ComponentLog)
                {
                    // Generate last time for previous date
                    _lastHourOfDateTime = DateTime.Parse($"{_startDateTime:yyyy-MM-dd} 23:59:59.999");
                    // Set log change day
                    G9LogInformationForComponentLog(
                        $"Finish day and generate path for new day...\nOld path is: {oldPath}\nNew path is: {CurrentLogDirectory}",
                        G9LogConst.G9LogIdentity, "Change path",
                        DateTime.Now > _lastHourOfDateTime ? _lastHourOfDateTime : DateTime.Now, true);
                }

                // Set new date time
                _startDateTime = DateTime.Now;

                // Generate directory name
                if (_configuration.Configuration.DirectoryNameDateType == DateTimeType.Gregorian)
                {
                    CurrentLogDirectory =
                        Path.Combine(_configuration.Configuration.Path, LogName,
                            _startDateTime.ToString("yyyy-MM-dd/"));
                }
                else
                {
                    var pc = new PersianCalendar();
                    CurrentLogDirectory = Path.Combine(_configuration.Configuration.Path, LogName,
                        $"{pc.GetYear(_startDateTime)}-{pc.GetMonth(_startDateTime)}-{pc.GetDayOfMonth(_startDateTime)}/");
                }

                // Close update old setting file
                CloseSettingFileAndSetCloseReason(closeReason, oldPath);

                // Generate data file path js
                var (settingFilePath, tempCurrentLogFileAddress) = GenerateSettingAndDataFilePath();
                CurrentLogFileAddress = tempCurrentLogFileAddress;

                // Check if not exists directory create it
                CopyDirectoryAndFileBetweenTwoPath(G9LogConst.G9LogReaderPath, CurrentLogDirectory);

                // Set log reader configuration
                SetLogReaderConfiguration(CurrentLogDirectory);

                // Create setting file
                CreateSettingFile(settingFilePath);

                // Log for Change path
                if (_configuration.Configuration.ComponentLog)
                    G9LogInformationForComponentLog(
                        $"Generate new path and for new day...\nNew path is: {CurrentLogDirectory}",
                        G9LogConst.G9LogIdentity, "Generate new path");

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

        private (string, string) GenerateSettingAndDataFilePath()
        {
            int i = -1, j = 0;
            var existNewConfigPath = false;
            // Check main directory
            while (true)
                if (Directory.Exists(
                    Path.Combine(
                        CurrentLogDirectory.Substring(0,
                            CurrentLogDirectory.Length - 1) + G9LogConst.DefaultChangePathText + ++i)))
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
                var newSettingPath = Path.Combine(CurrentLogDirectory, G9LogConst.DataDirectory,
                    $"{j}-{G9LogConst.SettingFileName}");
                var newDataPath = Path.Combine(CurrentLogDirectory, G9LogConst.DataDirectory,
                    $"{j}-{G9LogConst.DataFileName}");

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

        private string CheckAndGenerateMainConfigAndGenerateNewPath(
            bool existNewConfigPath, int newConfigPathIndex)
        {
            var newPathForCheck = existNewConfigPath
                ? CurrentLogDirectory.Substring(0, CurrentLogDirectory.Length - 1)
                  + G9LogConst.DefaultChangePathText + newConfigPathIndex
                : CurrentLogDirectory;

            var dicrectoryPath = Path.Combine(newPathForCheck, G9LogConst.DataDirectory);
            if (!Directory.Exists(dicrectoryPath))
                Directory.CreateDirectory(dicrectoryPath);

            var configPathFile = Path.Combine(dicrectoryPath, G9LogConst.ConfigFileName);
            // Check config file if exists
            if (File.Exists(configPathFile))
            {
                var configFileLines = File.ReadAllLines(configPathFile);
                for (var i = 0; i < configFileLines.Length; i++)
                    if (configFileLines[i].Contains("G9Encoding"))
                    {
                        var encryptionText = configFileLines[i]
                            .Replace("var G9Encoding = '", string.Empty).Replace("';", string.Empty);

                        // If encryption is true but encrypt text is empty => generate new path
                        // Previous config with out encryption but current config with encryption
                        if (string.IsNullOrEmpty(encryptionText) && _configuration.Configuration.EnableEncryptionLog)
                            return CheckAndGenerateMainConfigAndGenerateNewPath(true, newConfigPathIndex + 1);
                        // Else If encryption is false but encrypt text is not empty => generate new path
                        // Previous config with encryption but current config with out encryption

                        if (!_configuration.Configuration.EnableEncryptionLog && !string.IsNullOrEmpty(encryptionText))
                            return CheckAndGenerateMainConfigAndGenerateNewPath(true, newConfigPathIndex + 1);
                        // Previous config and current config without encryption

                        if (!_configuration.Configuration.EnableEncryptionLog)
                            return newPathForCheck;
                        // Else check encrypt user pass between Previous and current config

                        var decoding = AES128.DecryptString(
                            encryptionText, _configuration.Configuration.EncryptedUserName,
                            _configuration.Configuration.EncryptedPassword, out var error);
                        // If exist and config is true
                        // Previous config Equal current config
                        if (string.IsNullOrEmpty(error) && decoding.Contains(G9LogConst.DefaultEncodingSampleText))
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

            if (_configuration.Configuration.EnableEncryptionLog)
            {
                encoding = AES128.EncryptString(
                    $"{G9LogConst.DefaultEncodingSampleText}- {Guid.NewGuid()}-{DateTime.Now:yyyy/MM/dd HH:mm:ss}",
                    _configuration.Configuration.EncryptedUserName, _configuration.Configuration.EncryptedPassword,
                    out var error);

                if (!string.IsNullOrEmpty(error))
                    throw new Exception(error, new Exception(new StackTrace().ToString()));
            }

            using var configFile = File.CreateText(configPathFile);
            configFile.WriteLine($"var G9Encoding = '{encoding}';");
            configFile.WriteLine($"var G9DefaultPage = '{(byte) _configuration.Configuration.LogReaderStarterPage}';");

            return newPathForCheck;
        }

        #endregion

        /// <summary>
        ///     Update old setting file and set close date time and reason
        /// </summary>

        #region CloseSettingFileAndSetCloseReason

        private void CloseSettingFileAndSetCloseReason(ReasonCloseType closeReason, string oldPath = null)
        {
            var i = 0;
            string oldSettingPath;
            string archivePath = null;
            // Specify path
            if (closeReason == ReasonCloseType.ChangeDay && !string.IsNullOrEmpty(oldPath))
            {
                archivePath = oldPath;
                oldSettingPath = Path.Combine(oldPath, G9LogConst.DataDirectory);
            }
            else
            {
                oldSettingPath = Path.Combine(CurrentLogDirectory, G9LogConst.DataDirectory);
            }


            // If firs file or folder not exists = exit func
            if (!Directory.Exists(oldSettingPath) ||
                !File.Exists(Path.Combine(oldSettingPath, $"{i}-{G9LogConst.SettingFileName}")))
                return;

            while (true)
            {
                var checkPath = Path.Combine(oldSettingPath, $"{i}-{G9LogConst.SettingFileName}");

                if (!File.Exists(checkPath))
                    if (i > 0)
                    {
                        checkPath = Path.Combine(oldSettingPath, $"{i - 1}-{G9LogConst.SettingFileName}");

                        var oldDataPathFile = string.Empty;
                        if (File.Exists(Path.Combine(oldSettingPath, $"{i - 1}-{G9LogConst.DataFileName}")))
                            oldDataPathFile = Path.Combine(oldSettingPath, $"{i - 1}-{G9LogConst.DataFileName}");

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
            if (_configuration.Configuration.ZipArchivePreviousDay && closeReason == ReasonCloseType.ChangeDay &&
                !string.IsNullOrEmpty(oldPath))
                GenerateDirectoryZipArchiveAndRemoveDirectory(archivePath);
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

        private void CopyDirectoryAndFileBetweenTwoPath(string sourcePath, string destinationPath)
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

        private void CreateSettingFile(string path)
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

        private void SetLogReaderConfiguration(string path)
        {
            using var cultureStreamWriter =
                new StreamWriter(Path.Combine(path, G9LogConst.DefaultLanguageCultureFile), false);
            cultureStreamWriter.Write(
                $"var DefaultCulture = '{_configuration.Configuration.LogReaderDefaultCulture}';");
        }

        #endregion

        /// <summary>
        ///     Generate message from exception
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="additionalMessage">Additional message</param>
        /// <returns>Generated message</returns>

        #region ExceptionErrorGenerate

        private string ExceptionErrorGenerate(Exception ex, string additionalMessage)
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

        private void GenerateDirectoryZipArchiveAndRemoveDirectory(string directoryPath)
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