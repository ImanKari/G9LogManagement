using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using G9ConfigManagement;
using G9LogManagement.AESEncryptionDecryption;
using G9LogManagement.Config;
using G9LogManagement.Enums;
using G9LogManagement.Structures;
using G9ScheduleManagement;

#if (NETSTANDARD2_1 || NETSTANDARD2_0)
using System.ComponentModel;
#endif

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
        private readonly G9Schedule _scheduleForSaveLogsWithTime;

        /// <summary>
        ///     Schedule check 'HasShutdownStarted' for application exit
        /// </summary>
        private readonly G9Schedule _scheduleForCheckHasShutdownStarted;

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
        private readonly G9ConfigManagementSingleton<LogConfig> _configuration;

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
        ///     Flag field => run just one time on exit
        /// </summary>
        private bool _flagOnApplicationExit;

        #region Enable Logging Fields And Properties

        /// <summary>
        ///     Specified active logging for files
        /// </summary>
        public LogsTypeConfig ActiveFileLogging => _configuration.Configuration.ActiveFileLogs;

        /// <summary>
        ///     Specified active logging for console
        /// </summary>
        public LogsTypeConfig ActiveConsoleLogging => _configuration.Configuration.ActiveConsoleLogs;

        /// <summary>
        ///     Specified base app path for logging file
        /// </summary>
        public readonly string BaseApp;

        /// <summary>
        ///     Thread lock for console logging
        /// </summary>
        private readonly object _consoleLoggingLock = new object();

        #endregion

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

            // Load configs
            _configuration = G9ConfigManagementSingleton<LogConfig>.GetInstance(
                string.Format(G9LogConst.G9ConfigName, customLogName), customLogConfig, customLogConfig != null);

            // Set base app
            BaseApp = _configuration.Configuration.BaseApp;

            // Initialize folders and files
            // ReSharper disable once ObjectCreationAsStatement
            new InitializeFoldersAndFilesForLogReaders(_configuration.Configuration.BaseApp);

            if (_configuration.Configuration.ComponentLog)
                G9LogInformationForComponentLog("Start G9LogManagement...", G9LogConst.G9LogIdentity, "Start");

            // Initialize schedules
            _scheduleForSaveLogsWithTime = new G9Schedule();
            _scheduleForCheckHasShutdownStarted = new G9Schedule();

            // Start scheduler handler
            ScheduleHandler();
            OnApplicationExitHandler();
        }

        #endregion

        /// <summary>
        ///     Scheduler handler
        /// </summary>

        #region ScheduleHandler

        private void ScheduleHandler()
        {
            _scheduleForSaveLogsWithTime
                .AddScheduleAction(() =>
                {
                    // Active flag for save logs in file
                    _activeTimeOutForSave = true;
                    // Run save logs
                    FlushLogsAndSave();
                })
                .SetDuration(TimeSpan.FromSeconds(_configuration.Configuration.SaveTime))
                .AddErrorCallBack(exception =>
                    G9LogException(exception, "Scheduler Save Log Error!", "Scheduler", "Scheduler"));
        }

        #endregion

        /// <summary>
        ///     Handle exception log
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="message">Additional message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>
        /// <param name="customCallerPath">Custom caller path</param>
        /// <param name="customCallerName">Custom caller name</param>
        /// <param name="customLineNumber">Custom line number</param>

        #region G9LogException

        public void G9LogException(Exception ex, string message = null, string identity = null,
            string title = null,
            [CallerFilePath] string customCallerPath = null,
            [CallerMemberName] string customCallerName = null,
            [CallerLineNumber] int customLineNumber = 0)
        {
            // Ignore if disable log type
            if (!CheckEnableConsoleLoggingOrFileLoggingByType(LogsType.EXCEPTION)) return;

            // Handle log
            Task.Run(
                () => G9LogManagement(LogsType.EXCEPTION, ExceptionErrorGenerate(ex, message), identity, title,
                    customCallerPath: customCallerPath, customCallerName: customCallerName,
                    customLineNumber: customLineNumber));
        }

        #endregion

        /// <summary>
        ///     Handle error log
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>
        /// <param name="customCallerPath">Custom caller path</param>
        /// <param name="customCallerName">Custom caller name</param>
        /// <param name="customLineNumber">Custom line number</param>

        #region G9LogError

        public void G9LogError(string message, string identity = null, string title = null,
            [CallerFilePath] string customCallerPath = null,
            [CallerMemberName] string customCallerName = null,
            [CallerLineNumber] int customLineNumber = 0)
        {
            // Ignore if disable log type
            if (!CheckEnableConsoleLoggingOrFileLoggingByType(LogsType.ERROR)) return;

            // Handle log
            Task.Run(() => G9LogManagement(LogsType.ERROR, message, identity, title, customCallerPath: customCallerPath,
                customCallerName: customCallerName, customLineNumber: customLineNumber));
        }

        #endregion

        /// <summary>
        ///     Handle warning log
        /// </summary>
        /// <param name="message">Warning message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>
        /// <param name="customCallerPath">Custom caller path</param>
        /// <param name="customCallerName">Custom caller name</param>
        /// <param name="customLineNumber">Custom line number</param>

        #region G9LogWarning

        public void G9LogWarning(string message, string identity = null, string title = null,
            [CallerFilePath] string customCallerPath = null,
            [CallerMemberName] string customCallerName = null,
            [CallerLineNumber] int customLineNumber = 0)
        {
            // Ignore if disable log type
            if (!CheckEnableConsoleLoggingOrFileLoggingByType(LogsType.WARN)) return;

            // Handle log
            Task.Run(() => G9LogManagement(LogsType.WARN, message, identity, title, customCallerPath: customCallerPath,
                customCallerName: customCallerName, customLineNumber: customLineNumber));
        }

        #endregion

        /// <summary>
        ///     Handle information log
        /// </summary>
        /// <param name="message">Information message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>
        /// <param name="customCallerPath">Custom caller path</param>
        /// <param name="customCallerName">Custom caller name</param>
        /// <param name="customLineNumber">Custom line number</param>

        #region G9LogInformation

        public void G9LogInformation(string message, string identity = null, string title = null,
            [CallerFilePath] string customCallerPath = null,
            [CallerMemberName] string customCallerName = null,
            [CallerLineNumber] int customLineNumber = 0)
        {
            // Ignore if disable log type
            if (!CheckEnableConsoleLoggingOrFileLoggingByType(LogsType.INFO)) return;

            // Handle log
            Task.Run(() => G9LogManagement(LogsType.INFO, message, identity, title, customCallerPath: customCallerPath,
                customCallerName: customCallerName, customLineNumber: customLineNumber));
        }

        #endregion

        /// <summary>
        ///     Handle event log
        /// </summary>
        /// <param name="message">Event message</param>
        /// <param name="identity">Insert identity if need found easy in logs</param>
        /// <param name="title">Custom title for log</param>
        /// <param name="customCallerPath">Custom caller path</param>
        /// <param name="customCallerName">Custom caller name</param>
        /// <param name="customLineNumber">Custom line number</param>

        #region G9LogEvent

        public void G9LogEvent(string message, string identity = null, string title = null,
            [CallerFilePath] string customCallerPath = null,
            [CallerMemberName] string customCallerName = null,
            [CallerLineNumber] int customLineNumber = 0
        )
        {
            // Ignore if disable log type
            if (!CheckEnableConsoleLoggingOrFileLoggingByType(LogsType.EVENT)) return;

            // Handle log
            Task.Run(() => G9LogManagement(LogsType.EVENT, message, identity, title, customCallerPath: customCallerPath,
                customCallerName: customCallerName, customLineNumber: customLineNumber));
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
        /// <param name="customCallerPath">Custom caller path</param>
        /// <param name="customCallerName">Custom caller name</param>
        /// <param name="customLineNumber">Custom line number</param>

        #region G9LogInformationForComponentLog

        private void G9LogInformationForComponentLog(string message, string identity = null,
            string title = null, DateTime? customDateTime = null, bool forceSaveLogs = false,
            [CallerFilePath] string customCallerPath = null,
            [CallerMemberName] string customCallerName = null,
            [CallerLineNumber] int customLineNumber = 0)
        {
            // If disable component log => return
            if (!_configuration.Configuration.ComponentLog) return;

            // Handle log
            // Without check file size
            Task.Run(() =>
                G9LogManagement(LogsType.EVENT, message, identity, title, false, customDateTime, forceSaveLogs,
                    customCallerPath, customCallerName, customLineNumber));
        }

        #endregion

        /// <summary>
        ///     Handler for events
        /// </summary>
        /// <param name="logType">Type of log</param>
        /// <param name="message">Message for log</param>
        /// <param name="identity">Identity for log</param>
        /// <param name="title">Log title</param>
        /// <param name="fileSizeCheck">Specify check file size or no</param>
        /// <param name="customDateTime">If need custom date time set it</param>
        /// <param name="forceSaveLogs">When need force save</param>
        /// <param name="customCallerPath">Custom caller path</param>
        /// <param name="customCallerName">Custom caller name</param>
        /// <param name="customLineNumber">Custom line number</param>

        #region G9LogManagement

        private void G9LogManagement(LogsType logType, string message, string identity = null,
            string title = null, bool fileSizeCheck = true, DateTime? customDateTime = null, bool forceSaveLogs = false,
            string customCallerPath = null,
            string customCallerName = null,
            int customLineNumber = 0)
        {
            string fileName = string.Empty, methodBase = string.Empty, lineNumber = string.Empty;

            // Check if enable stack information log for type
            if (_configuration.Configuration.EnableStackTraceInformation.CheckValueByType(logType))
            {
                fileName = customCallerPath;
                methodBase = customCallerName;
                lineNumber = customLineNumber.ToString();
            }

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
            // Management console log
            if (CheckEnableConsoleLoggingByType(logItem.LogType))
                ConsoleLogging(logItem);

            // Management file log
            if (CheckEnableFileLoggingByType(logItem.LogType))
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
                _scheduleForSaveLogsWithTime?.Stop();
                lock (_waitForFlushLogItems)
                {
                    if (flushSpace.Any() && (forceSaveLogs || _activeTimeOutForSave ||
                                             flushSpace.Count >= _configuration.Configuration.SaveCount))
                    {
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

                        var flagBreakSize = false;

                        // Instance new stream and open
                        using (var logFileStream = new FileStream(
                            CurrentLogFileAddress,
                            FileMode.OpenOrCreate,
                            FileAccess.ReadWrite,
                            FileShare.Read))
                        {
                            // Write bytes for next day
                            while (_queueOfLogDataNextDay.Any())
#if NETSTANDARD2_1
                                if (_queueOfLogDataNextDay.TryDequeue(out var logItemData))
                                    WriteLogsToStream(logItemData, logFileStream);
#else
                                WriteLogsToStream(_queueOfLogDataNextDay.Dequeue(), logFileStream);
#endif

                            // Dequeue log Item for save to file
                            while (!flushSpace.IsEmpty)
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

                            // Set file size
                            _streamLengthFileSize = logFileStream.Length;
                        }

                        if (flagBreakSize)
                            goto Start2;

                        // Reset duration of scheduler
                        if (_scheduleForSaveLogsWithTime != null)
                        {
                            _scheduleForSaveLogsWithTime.ResetDuration();
                            // Resume scheduler
                            _scheduleForSaveLogsWithTime.Resume();
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
#if NETSTANDARD2_1
            if (fileStream.Length <= 2)
            {
                fileStream.Write(_encoding.GetBytes(
                    $"G9DataLog.push({GenerateLogByInformation(logItemData.LogType, logItemData.Identity, logItemData.Title, logItemData.Body, logItemData.FileName, logItemData.MethodBase, logItemData.LineNumber, logItemData.LogDateTime, false)});"
                ));
            }
            else
            {
                fileStream.Seek(-2, SeekOrigin.End);
                fileStream.Write(_encoding.GetBytes(
                    $"{GenerateLogByInformation(logItemData.LogType, logItemData.Identity, logItemData.Title, logItemData.Body, logItemData.FileName, logItemData.MethodBase, logItemData.LineNumber, logItemData.LogDateTime, true)});"
                ));
            }
#else
            if (fileStream.Length <= 2)
            {
                var dataByte = _encoding.GetBytes(
                    $"G9DataLog.push({GenerateLogByInformation(logItemData.LogType, logItemData.Identity, logItemData.Title, logItemData.Body, logItemData.FileName, logItemData.MethodBase, logItemData.LineNumber, logItemData.LogDateTime, false)});"
                );
                fileStream.Write(dataByte, 0, dataByte.Length);
            }
            else
            {
                var dataByte = _encoding.GetBytes(
                    $"{GenerateLogByInformation(logItemData.LogType, logItemData.Identity, logItemData.Title, logItemData.Body, logItemData.FileName, logItemData.MethodBase, logItemData.LineNumber, logItemData.LogDateTime, true)});"
                );
                fileStream.Seek(-2, SeekOrigin.End);
                fileStream.Write(dataByte, 0, dataByte.Length);
            }
#endif
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
                    $"{(addCommaInFirst ? "," : string.Empty)}[{(byte) logType},'{EncodeJs(identity)}','{AES128.EncryptString($"{title}🅖➒{body}🅖➒{fileName}🅖➒{methodBase}🅖➒{lineNumber}", key, iv, out _)}','{logDateTime:yyyy/MM/dd HH:mm:ss.ff}']";
            }

            // Normal write
            return
                $"{(addCommaInFirst ? "," : string.Empty)}[{(byte) logType},'{EncodeJs(identity)}','{EncodeJs($"{title}🅖➒{body}🅖➒{fileName}🅖➒{methodBase}🅖➒{lineNumber}")}','{logDateTime:yyyy/MM/dd HH:mm:ss.ff}']";
        }

        #endregion

        /// <summary>
        ///     event Handler for on application exit
        /// </summary>

        #region OnApplicationExitHandler

        private void OnApplicationExitHandler()
        {
#if (NETSTANDARD2_1 || NETSTANDARD2_0)
            // Set event for exit or unload
            AppDomain.CurrentDomain.ProcessExit += OnApplicationExit;
            AppDomain.CurrentDomain.DomainUnload += OnApplicationExit;
#else


            _scheduleForCheckHasShutdownStarted.AddScheduleAction(() =>
                {
                    if (Environment.HasShutdownStarted) OnApplicationExit();
                }).SetDuration(TimeSpan.FromMilliseconds(1))
                .AddErrorCallBack(exception =>
                    G9LogException(exception, "Scheduler Save Log Error!", "Scheduler", "Scheduler"));
#endif
        }

        #endregion

        // On application exit => execute requirement

        #region OnApplicationExit

#if (NETSTANDARD2_1 || NETSTANDARD2_0)
        /// <summary>
        ///     On application exit => execute requirement
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnApplicationExit(object sender, EventArgs e)
#else
        /// <summary>
        ///     On application exit => execute requirement
        /// </summary>
        private void OnApplicationExit()
#endif
        {
            // Check for run on time
            if (_flagOnApplicationExit) return;

            // Set flag exit
            _flagOnApplicationExit = true;

            // Log for exit
            if (_configuration.Configuration.ComponentLog)
                G9LogInformationForComponentLog("Stop G9LogManagement and close app...", G9LogConst.G9LogIdentity,
                    "Stop And Close");

            // Update old setting file
            CloseSettingFileAndSetCloseReason(ReasonCloseType.ExitApp);

            // Flush force file
            FlushLogsAndSave(false, true);

            // Dispose scheduler
            _scheduleForSaveLogsWithTime.Dispose();
            _scheduleForCheckHasShutdownStarted.Dispose();

            // Sleep for wait insert data
            Task.Delay(G9LogConst.DefaultTimeOutToCloseStreamWhenExitApp).Wait(100);
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
                if (closeReason == ReasonCloseType.ChangeDay)
                {
                    // Generate last time for previous date
                    _lastHourOfDateTime = DateTime.Parse($"{_startDateTime:yyyy-MM-dd} 23:59:59.999");

                    if (_configuration.Configuration.ComponentLog)
                        // Set log change day
                        G9LogInformationForComponentLog(
                            $"Finish day and generate path for new day...\nOld path is: {oldPath}\nNew path is: {CurrentLogDirectory}",
                            G9LogConst.G9LogIdentity, "Change path",
                            DateTime.Now > _lastHourOfDateTime ? _lastHourOfDateTime : DateTime.Now, true);
                }

                // Set new date time
                _startDateTime = DateTime.Now;

                // Generate directory name
                CurrentLogDirectory = Path.Combine(BaseApp, _configuration.Configuration.Path, LogName,
                    GetFolderNameByDateTimeType(_startDateTime, _configuration.Configuration.DirectoryNameDateType));

                // Close update old setting file
                CloseSettingFileAndSetCloseReason(closeReason, oldPath);

                // Generate data file path js
#if (NETSTANDARD2_1 || NETSTANDARD2_0)
                var (settingFilePath, tempCurrentLogFileAddress) = GenerateSettingAndDataFilePath();
#else
                GenerateSettingAndDataFilePath(out var settingFilePath, out var tempCurrentLogFileAddress);
#endif

                CurrentLogFileAddress = tempCurrentLogFileAddress;

                // Check if not exists directory create it
                CopyDirectoryAndFileBetweenTwoPath(Path.Combine(BaseApp, G9LogConst.G9LogReaderPath),
                    CurrentLogDirectory);

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

#if (NETSTANDARD2_1 || NETSTANDARD2_0)
        private (string, string) GenerateSettingAndDataFilePath()
#else
        private void GenerateSettingAndDataFilePath(out string settingFilePath, out string tempCurrentLogFileAddress)
#endif
        {
            int i = -1, j = 0;
            var existNewConfigPath = false;
            // Check main directory
            while (true)
                if (Directory.Exists(
                    Path.Combine(BaseApp,
                        CurrentLogDirectory.Substring(0,
                            CurrentLogDirectory.Length - 1) + G9LogConst.DefaultChangeConfigText + ++i)))
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
                var newSettingPath = Path.Combine(BaseApp, CurrentLogDirectory, G9LogConst.DataDirectory,
                    $"{j}-{G9LogConst.SettingFileName}");
                var newDataPath = Path.Combine(BaseApp, CurrentLogDirectory, G9LogConst.DataDirectory,
                    $"{j}-{G9LogConst.DataFileName}");

                if (!File.Exists(newSettingPath))
                {
#if (NETSTANDARD2_1 || NETSTANDARD2_0)
                    return (newSettingPath, newDataPath);
#else
                    settingFilePath = newSettingPath;
                    tempCurrentLogFileAddress = newDataPath;
                    return;
#endif
                }

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
                  + G9LogConst.DefaultChangeConfigText + newConfigPathIndex
                : CurrentLogDirectory;

            var directoryPath = Path.Combine(BaseApp, newPathForCheck, G9LogConst.DataDirectory);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var configPathFile = Path.Combine(BaseApp, directoryPath, G9LogConst.ConfigFileName);
            // Check config file if exists
            if (File.Exists(configPathFile))
            {
                var configFileLines = File.ReadAllLines(configPathFile);
                foreach (var t in configFileLines)
                    if (t.Contains("G9Encoding"))
                    {
                        var encryptionText = t
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
                    $"{Guid.NewGuid()}-{G9LogConst.DefaultEncodingSampleText}-{DateTime.Now:yyyy/MM/dd HH:mm:ss}",
                    _configuration.Configuration.EncryptedUserName, _configuration.Configuration.EncryptedPassword,
                    out var error);

                if (!string.IsNullOrEmpty(error))
#if (NETSTANDARD2_1 || NETSTANDARD2_0)
                    throw new Exception(error, new Exception(new StackTrace().ToString()));
#else
                    throw new Exception(error, new Exception(new StackTrace(new Exception(), true).ToString()));
#endif
            }

            using (var configFile = File.CreateText(configPathFile))
            {
                configFile.WriteLine($"var G9Encoding = '{encoding}';");
                configFile.WriteLine(
                    $"var G9DefaultPage = '{(byte) _configuration.Configuration.LogReaderStarterPage}';");
            }

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
                oldSettingPath = Path.Combine(BaseApp, oldPath, G9LogConst.DataDirectory);
            }
            else
            {
                oldSettingPath = Path.Combine(BaseApp, CurrentLogDirectory, G9LogConst.DataDirectory);
            }


            // If firs file or folder not exists = exit func
            if (!Directory.Exists(oldSettingPath) ||
                !File.Exists(Path.Combine(BaseApp, oldSettingPath, $"{i}-{G9LogConst.SettingFileName}")))
                return;

            while (true)
            {
                var checkPath = Path.Combine(BaseApp, oldSettingPath, $"{i}-{G9LogConst.SettingFileName}");

                if (!File.Exists(checkPath))
                    if (i > 0)
                    {
                        checkPath = Path.Combine(BaseApp, oldSettingPath, $"{i - 1}-{G9LogConst.SettingFileName}");

                        var oldDataPathFile = string.Empty;
                        if (File.Exists(Path.Combine(BaseApp, oldSettingPath, $"{i - 1}-{G9LogConst.DataFileName}")))
                            oldDataPathFile = Path.Combine(BaseApp, oldSettingPath,
                                $"{i - 1}-{G9LogConst.DataFileName}");

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

#if (NETSTANDARD2_1 || NETSTANDARD2_0)
                        using (var newTask = new StreamWriter(checkPath, false))
#else
                        using (var newTask =
                            new StreamWriter(new FileStream(checkPath, FileMode.Create), Encoding.UTF8))
#endif
                        {
                            foreach (var line in settingFileLines) newTask.WriteLine(line);
                        }

                        break;
                    }

                i++;
            }

            // If enable LogHandlerConfig.ZipArchivePreviousDay and
            // If change day => archive previous day
            if (_configuration.Configuration.ZipArchivePreviousDay && closeReason == ReasonCloseType.ChangeDay &&
                !string.IsNullOrEmpty(oldPath))
                DirectoryToZipArchiveAndRemoveDirectory(archivePath);
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
            if (!destinationPath.EndsWith("/")) destinationPath += "/";

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
            using (var settingFile = File.CreateText(path))
            {
                settingFile.WriteLine($"G9StartDateTime.push('{DateTime.Now:yyyy-MM-dd HH:mm:ss}');");
                settingFile.WriteLine("G9FinishDateTime.push('');");
                settingFile.WriteLine("G9FileSize.push('');");
                settingFile.WriteLine($"G9FileCloseReason.push('{ReasonCloseType.OpenOrForceClose}');");
            }
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
#if (NETSTANDARD2_1 || NETSTANDARD2_0)
            using (var cultureStreamWriter =
                new StreamWriter(Path.Combine(BaseApp, path, G9LogConst.DefaultLanguageCultureFile), false))
#else
            using (var cultureStreamWriter =
                new StreamWriter(
                    new FileStream(Path.Combine(BaseApp, path, G9LogConst.DefaultLanguageCultureFile), FileMode.Create),
                    Encoding.UTF8))
#endif
            {
                cultureStreamWriter.Write(
                    $"var DefaultCulture = '{_configuration.Configuration.LogReaderDefaultCulture}';");
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

        private string ExceptionErrorGenerate(Exception ex, string additionalMessage)
        {
            var exceptionMessage = new StringBuilder();
            // Add additional
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
        ///     <para>Generate zip by directory path</para>
        ///     <para>Remove directory with all files after generate zip</para>
        ///     <para>Check all change config folders</para>
        /// </summary>
        /// <param name="directoryPath">Specify directory path</param>

        #region DirectoryToZipArchiveAndRemoveDirectory

        private void DirectoryToZipArchiveAndRemoveDirectory(string directoryPath)
        {
            int foundPos;
            if (
                // if found 'DefaultChangeConfigText' in directory path
                (foundPos = directoryPath.IndexOf(G9LogConst.DefaultChangeConfigText, StringComparison.Ordinal)) != -1 &&
                // if exist directory path when remove 'DefaultChangeConfigText' from end
                Directory.Exists(directoryPath.Substring(0, foundPos)) &&
                // if length found position + 'DefaultChangeConfigText' length greater than directoryPath.Length - 3
                // this section specified 'DefaultChangeConfigText' at end of directory path
                foundPos + G9LogConst.DefaultChangeConfigText.Length > directoryPath.Length - 3)
                directoryPath = directoryPath.Substring(0, foundPos);



            // Archive default path
            ArchiveAndDeletePath(directoryPath);

            //Set counter for check other directory
            var changeConfigCounter = 0;
            // ReSharper disable once TooWideLocalVariableScope
            string changeConfigDirectoryPath;
            // Repeat for change config folders
            do
            {
                // Check if exists set path else break from loop
                if (Directory.Exists(
                    $"{directoryPath.Trim('/')}{G9LogConst.DefaultChangeConfigText}{changeConfigCounter}"))
                    changeConfigDirectoryPath =
                        $"{directoryPath.Trim('/')}{G9LogConst.DefaultChangeConfigText}{changeConfigCounter}";
                else
                    break;

                // Archive change config path
                ArchiveAndDeletePath(changeConfigDirectoryPath);

                changeConfigCounter++;
            } while (true);
        }

        #endregion

        /// <summary>
        ///     Archive directory then delete all files and folders
        /// </summary>
        /// <param name="directoryPath">Specify directory path</param>

        #region ArchiveAndDeletePath

        private void ArchiveAndDeletePath(string directoryPath)
        {
            // Generate archive
            ZipFile.CreateFromDirectory(directoryPath, directoryPath.Trim('/') + ".zip"
                , CompressionLevel.Optimal, true);

            var di = new DirectoryInfo(directoryPath);
            // Delete all files
            foreach (var file in di.GetFiles())
                file.Delete();
            // Delete all directory
            foreach (var dir in di.GetDirectories())
                dir.Delete(true);
            // Delete main directory
            Directory.Delete(directoryPath);
        }

        #endregion

        /// <summary>
        ///     Ready log data for show in the console
        /// </summary>
        /// <param name="logItem">Specify log item for show</param>

        #region ConsoleLogging

        private void ConsoleLogging(G9LogItem logItem)
        {
            // Lock for console logging
            lock (_consoleLoggingLock)
            {
                // Set console color
                SetConsoleLoggingColorByLogType(logItem.LogType);

                // Show console log
                Console.WriteLine(
                    $"################################################# Log Type: {logItem.LogType} #################################################\nDate & Time: {logItem.LogDateTime:yyyy/MM/ss HH:mm:ss.fff}\tIdentity: {logItem.Identity}\tTitle: {logItem.Title}\nBody: {logItem.Body}\nPath: {logItem.FileName}\tMethod: {logItem.MethodBase}\tLine: {logItem.LineNumber}\n");

                // Reset console
                ResetLoggingColor();
            }
        }

        #endregion

        /// <summary>
        ///     Set console 'BackgroundColor' and 'ForegroundColor' by log type
        /// </summary>
        /// <param name="type">Specified log type</param>

        #region SetConsoleLoggingColorByLogType

        private void SetConsoleLoggingColorByLogType(LogsType type)
        {
            switch (type)
            {
                case LogsType.EXCEPTION:
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogsType.ERROR:
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogsType.WARN:
                    Console.BackgroundColor = ConsoleColor.DarkYellow;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogsType.INFO:
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogsType.EVENT:
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        #endregion

        /// <summary>
        ///     Reset 'BackgroundColor' and 'ForegroundColor' for console
        /// </summary>

        #region ResetLoggingColor

        private void ResetLoggingColor()
        {
            Console.ResetColor();
        }

        #endregion

        /// <summary>
        ///     Check enable console log by type
        /// </summary>
        /// <param name="type">Specified type</param>
        /// <returns>Return 'true' if enable</returns>

        #region CheckEnableConsoleLoggingByType

        public bool CheckEnableConsoleLoggingByType(LogsType type)
        {
#if NETSTANDARD2_1
            return type switch
            {
                LogsType.EVENT => ActiveConsoleLogging.EVENT,
                LogsType.INFO => ActiveConsoleLogging.INFO,
                LogsType.WARN => ActiveConsoleLogging.WARN,
                LogsType.ERROR => ActiveConsoleLogging.ERROR,
                LogsType.EXCEPTION => ActiveConsoleLogging.EXCEPTION,
                _ => throw new InvalidEnumArgumentException(nameof(type), (int) type, typeof(LogsType))
            };
#else
            switch (type)
            {
                case LogsType.EVENT:
                    return ActiveConsoleLogging.EVENT;
                case LogsType.INFO:
                    return ActiveConsoleLogging.INFO;
                case LogsType.WARN:
                    return ActiveConsoleLogging.WARN;
                case LogsType.ERROR:
                    return ActiveConsoleLogging.ERROR;
                case LogsType.EXCEPTION:
                    return ActiveConsoleLogging.EXCEPTION;
                default:
#if (NETSTANDARD2_0)
                    throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(LogsType));
#else
                    throw new ArgumentOutOfRangeException(nameof(type), type,
                        $"Value enum type {typeof(LogsType)} not supported!");
#endif
            }
#endif
        }

        #endregion

        /// <summary>
        ///     Check enable file logging by type
        /// </summary>
        /// <param name="type">Specified type</param>
        /// <returns>Return 'true' if enable</returns>

        #region CheckEnableFileLoggingByType

        public bool CheckEnableFileLoggingByType(LogsType type)
        {
#if NETSTANDARD2_1
            return type switch
            {
                LogsType.EVENT => ActiveFileLogging.EVENT,
                LogsType.INFO => ActiveFileLogging.INFO,
                LogsType.WARN => ActiveFileLogging.WARN,
                LogsType.ERROR => ActiveFileLogging.ERROR,
                LogsType.EXCEPTION => ActiveFileLogging.EXCEPTION,
                _ => throw new InvalidEnumArgumentException(nameof(type), (int) type, typeof(LogsType))
            };
#else
            switch (type)
            {
                case LogsType.EVENT:
                    return ActiveFileLogging.EVENT;
                case LogsType.INFO:
                    return ActiveFileLogging.INFO;
                case LogsType.WARN:
                    return ActiveFileLogging.WARN;
                case LogsType.ERROR:
                    return ActiveFileLogging.ERROR;
                case LogsType.EXCEPTION:
                    return ActiveFileLogging.EXCEPTION;
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

        /// <summary>
        ///     Check enable console logging or file logging is enable
        /// </summary>
        /// <param name="type"></param>
        /// <returns>Return true if console logging or file logging is enable</returns>

        #region CheckEnableConsoleLoggingOrFileLoggingByType

        public bool CheckEnableConsoleLoggingOrFileLoggingByType(LogsType type)
        {
            return CheckEnableConsoleLoggingByType(type) || CheckEnableFileLoggingByType(type);
        }

        #endregion

        /// <summary>
        ///     Specified folder name
        /// </summary>
        /// <param name="folderDateTime">Folder date time</param>
        /// <param name="folderDateTimeType">Folder date time type</param>
        /// <returns>Folder name</returns>

        #region GetFolderNameByDateTimeType

        private string GetFolderNameByDateTimeType(DateTime folderDateTime, DateTimeType folderDateTimeType)
        {
            switch (folderDateTimeType)
            {
                case DateTimeType.Gregorian:
                    return folderDateTime.ToString("yyyy-MM-dd/");
                case DateTimeType.Shamsi:
                    var pc = new PersianCalendar();
                    return
                        $"{pc.GetYear(_startDateTime)}-{pc.GetMonth(_startDateTime).ToString().PadLeft(2, '0')}-{pc.GetDayOfMonth(_startDateTime).ToString().PadLeft(2, '0')}/";
                case DateTimeType.GregorianShamsi:
                    var pc1 = new PersianCalendar();
                    return
                        $"{folderDateTime:yyyy-MM-dd}_{pc1.GetYear(_startDateTime)}-{pc1.GetMonth(_startDateTime).ToString().PadLeft(2, '0')}-{pc1.GetDayOfMonth(_startDateTime).ToString().PadLeft(2, '0')}/";
                case DateTimeType.ShamsiGregorian:
                    var pc2 = new PersianCalendar();
                    return
                        $"{pc2.GetYear(_startDateTime)}-{pc2.GetMonth(_startDateTime).ToString().PadLeft(2, '0')}-{pc2.GetDayOfMonth(_startDateTime).ToString().PadLeft(2, '0')}_{folderDateTime:yyyy-MM-dd}/";
                default:
                    throw new ArgumentOutOfRangeException(nameof(folderDateTimeType), folderDateTimeType,
                        "Enum value not supported!");
            }
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

        #region EncodeJs

        public string EncodeJs(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

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

            return sb.ToString();
        }

        #endregion

        #endregion
    }
}