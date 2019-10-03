using System;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using G9ConfigManagement.Attributes;
using G9ConfigManagement.Interface;
using G9LogManagement.Enums;

namespace G9LogManagement.Config
{
    /// <summary>
    ///     Class managed config
    /// </summary>
    public class LogConfig : IConfigDataType
    {
        #region Methods

        /// <summary>
        ///     Constructor
        ///     Initialize requirement data
        /// </summary>

        #region G9LogConfig

        public LogConfig()
        {
            EnableStackTraceInformation = new LogsTypeConfig();
            ActiveLogs = new LogsTypeConfig();
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

        #endregion

        #region Fields And Properties

        /// <inheritdoc />
        public string ConfigVersion { set; get; }

        /// <summary>
        ///     Save path
        /// </summary>
        private string _path;

        /// <summary>
        ///     Specify path for create log directory
        /// </summary>
        [Hint("Specify path for create log directory")]
        public string Path
        {
            set => _path = value;
            get
            {
                if (string.IsNullOrEmpty(_path))
                    _path = "LogsManagement/";
                return _path;
            }
        }

        /// <summary>
        ///     Specify directory name date type
        ///     'Gregorian' or 'Shamsi'
        /// </summary>
        [Hint("Specify directory name date type")]
        [Hint("'" + nameof(DateTimeType.Gregorian) + "' or '" + nameof(DateTimeType.Shamsi) + "'")]
        public DateTimeType DirectoryNameDateType { set; get; }

        /// <summary>
        ///     Enable log for component G9LogHandler
        ///     Logs like: start datetime, end datetime and other additional
        /// </summary>
        [Hint("Enable log for component G9LogHandler")]
        [Hint("Logs like: start datetime, end datetime and other additional")]
        [DefaultValue(true)]
        public bool ComponentLog { set; get; }

        /// <summary>
        ///     Enable stack trace info for logs
        /// </summary>
        [Hint("Enable stack trace info for logs")]
        public LogsTypeConfig EnableStackTraceInformation { set; get; }

        /// <summary>
        ///     Specify log type enable
        /// </summary>
        [Hint("Specify log type enable")]
        public LogsTypeConfig ActiveLogs { set; get; }

        /// <summary>
        ///     Specify need encrypt data for log
        ///     open just by user pass
        /// </summary>
        [Ignore]
        public bool EnableEncryptionLog
        {
            get
            {
                if (!string.IsNullOrEmpty(LogUserName) && !string.IsNullOrEmpty(LogPassword))
                {
                    if (string.IsNullOrEmpty(_encryptedPassword) || string.IsNullOrEmpty(_encryptedUserName))
                    {
                        // Generate MD5 with sum user pass and generate key and iv
                        var tempAllKeys = LogUserName + LogPassword;
                        tempAllKeys = CreateMD5(tempAllKeys);
                        _encryptedUserName = tempAllKeys.Substring(0, 16);
                        _encryptedPassword = tempAllKeys.Substring(16, 16);
                    }

                    return true;
                }

                return false;
            }
        }

        /// <summary>
        ///     Log UserName
        ///     If need encryption log with user and password
        /// </summary>
        [Hint("If need encryption log with user and password")]
        [Hint("Optional: Log UserName")]
        public string LogUserName { set; get; }

        /// <summary>
        ///     Log Password
        ///     If need encryption log with user and password
        /// </summary>
        [Hint("Optional: Log Password")]
        public string LogPassword { set; get; }

        /// <summary>
        ///     Field for save encrypted username
        /// </summary>
        private string _encryptedUserName;

        /// <summary>
        ///     Field for save encrypted password
        /// </summary>
        private string _encryptedPassword;

        /// <summary>
        ///     Access to encrypted username
        /// </summary>
        [Ignore]
        public string EncryptedUserName
        {
            get
            {
                if (!EnableEncryptionLog)
                    return null;
                return _encryptedUserName;
            }
        }

        /// <summary>
        ///     Access to encrypted password
        /// </summary>
        [Ignore]
        public string EncryptedPassword
        {
            get
            {
                if (!EnableEncryptionLog)
                    return null;
                return _encryptedPassword;
            }
        }

        /// <summary>
        /// field for save max file size
        /// </summary>
        private decimal _maxFileSize = 3;

        /// <summary>
        ///     Specify max file size in byte
        /// </summary>
        [Hint("Specify max file size in MB")]
        [Hint("Set 0 => Unlimited | Min value: 3")]
        [DefaultValue(10)]
        public decimal MaxFileSize
        {
            set => _maxFileSize = value;
            get
            {
                if (_maxFileSize < 3)
                    return 3;
                return _maxFileSize;
            }
        }
        
        /// <summary>
        /// field for save max file size in byte
        /// </summary>
        private decimal _maxFileSizeInByte = 0;

        /// <summary>
        /// Access to max file size Mb => byte
        /// </summary>
        [Ignore]
        public decimal MaxFileSizeInByte
        {
            get
            {
                if (_maxFileSizeInByte != 0)
                    return _maxFileSizeInByte;
                return _maxFileSizeInByte = MaxFileSize * 1048576;
            }
        }

        /// <summary>
        ///     Specify default culture for log reader
        /// </summary>
        [Hint("Specify default culture for log reader")]
        [Hint("values: '" + nameof(CultureType.en_us) + "' or '" + nameof(CultureType.fa) + "'")]
        public CultureType LogReaderDefaultCulture { set; get; }

        /// <summary>
        ///     Field for save time
        /// </summary>
        private int _saveTime = 1;

        /// <summary>
        ///     Specify time in second for save logs
        /// </summary>
        [Hint("Specify time in second for save logs")]
        public int SaveTime
        {
            set => _saveTime = value;
            get
            {
                if (_saveTime < 1)
                    return 1;
                return _saveTime;
            }
        }

        /// <summary>
        ///     Field for save count
        /// </summary>
        private int _saveCount;

        /// <summary>
        ///     Specify count of logs for save logs
        /// </summary>
        [Hint("Specify count of logs for save logs")]
        [Hint("Minimum Set 0 => without queue")]
        public int SaveCount
        {
            set => _saveCount = value;
            get
            {
                if (_saveCount < 0)
                    return 0;
                return _saveCount;
            }
        }

        /// <summary>
        ///     Specify enable archive previous day
        /// </summary>
        [Hint("Specify enable archive previous day")]
        public bool ZipArchivePreviousDay { set; get; }

        /// <summary>
        ///     Specify log reader starter page
        /// </summary>
        [Hint("Specify log reader starter page")]
        [Hint("values: '" + nameof(LogReaderPages.Dashboard) + "' or '" + nameof(LogReaderPages.LogsManagement) + "'")]
        public LogReaderPages LogReaderStarterPage { set; get; }

        #endregion
    }
}