using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using G9ConfigManagement.Attributes;
using G9ConfigManagement.Interface;
using G9LogManagement.Enums;
using G9LogManagement.Structures;

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
        }

        #endregion

        /// <summary>
        ///     Generate MD5 from text
        /// </summary>
        /// <param name="text">Specify text</param>
        /// <returns>Return MD5 from text</returns>

        #region CreateMD5

        private string CreateMD5(string text)
        {
#if NETSTANDARD2_1
            using var md5 = MD5.Create();
            var encoding = Encoding.ASCII;
            var data = encoding.GetBytes(text);

            Span<byte> hashBytes = stackalloc byte[16];
            md5.TryComputeHash(data, hashBytes, out var written);
            if (written != hashBytes.Length)
                throw new OverflowException();


            Span<char> stringBuffer = stackalloc char[32];
            for (var i = 0; i < hashBytes.Length; i++)
                hashBytes[i].TryFormat(stringBuffer.Slice(2 * i), out _, "x2");
            return new string(stringBuffer).ToLower();
#else
            // Use input string to calculate MD5 hash
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes(text);
                var hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                var sb = new StringBuilder();
                for (var i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("X2"));
                return sb.ToString().ToLower();
            }
#endif
        }

        #endregion

        #endregion

        #region Fields And Properties

        /// <summary>
        ///     Specified application version
        /// </summary>
        public static string ApplicationVersion =>
#if (NETSTANDARD2_1 || NETSTANDARD2_0)
            string.IsNullOrEmpty(Assembly.GetExecutingAssembly().GetName().Version.ToString())
                ? Assembly.GetEntryAssembly()?.GetName().Version.ToString() ?? "0.0.0.0"
                : Assembly.GetExecutingAssembly().GetName().Version.ToString();
#elif (NETSTANDARD1_6 || NETSTANDARD1_5)
        string.IsNullOrEmpty(Assembly.GetEntryAssembly().GetName().Version.ToString())
                ? Assembly.GetEntryAssembly()?.GetName().Version.ToString() ?? "0.0.0.0"
                : Assembly.GetEntryAssembly().GetName().Version.ToString();
#else
            string.IsNullOrEmpty(Assembly.Load(new AssemblyName(nameof(G9LogManagement))).GetName().Version.ToString())
                ? Assembly.Load(new AssemblyName(nameof(G9LogManagement)))?.GetName().Version.ToString() ?? "0.0.0.0"
                : Assembly.Load(new AssemblyName(nameof(G9LogManagement))).GetName().Version.ToString();
#endif

        /// <inheritdoc />
        public string ConfigVersion => ApplicationVersion;

        /// <summary>
        ///     Field for save base app
        /// </summary>
        private string _baseApp;

        /// <summary>
        ///     <para>Specified base app - project root for create logs and requirement</para>
        ///     <para>Sample value: if set empty use automatic 'BaseDirectory' value, if set 'path' like 'c:\folder\...'</para>
        /// </summary>
        [G9ConfigHint(@"Sample value: if set empty use automatic 'BaseDirectory' value, if set 'path' like 'c:\folder\...'")]
        public string BaseApp
        {
            get
            {
                // Set default value
                if (string.IsNullOrEmpty(_baseApp))
#if (NETSTANDARD2_1 || NETSTANDARD2_0)
                    return AppDomain.CurrentDomain.BaseDirectory;
#else
                    return AppContext.BaseDirectory;
#endif
                // Check base app path
                if (!Directory.Exists(_baseApp))
                    throw new DirectoryNotFoundException(
                        $"Base app directory set in config file not found: path => '{_baseApp}'");

                return _baseApp;
            }
            set => _baseApp = value;
        }

        /// <summary>
        ///     Save path
        /// </summary>
        private string _path;

        /// <summary>
        ///     Specify path for create log directory
        /// </summary>
        [G9ConfigHint("Specify path for create log directory")]
        public string Path
        {
            set => _path = value;
            get
            {
                if (string.IsNullOrEmpty(_path))
                    _path = "G9Logs/";
                return _path;
            }
        }

        /// <summary>
        ///     <para>Specify directory name date type</para>
        ///     <para>'Gregorian', 'Shamsi', 'GregorianShamsi' and 'ShamsiGregorian'</para>
        /// </summary>
        [G9ConfigHint("Specified directory name date type")]
        [G9ConfigHint("Sample value: '" + nameof(DateTimeType.Gregorian) + "','" + nameof(DateTimeType.Shamsi) + "'" + "','" +
              nameof(DateTimeType.GregorianShamsi) + "'" + "' and '" + nameof(DateTimeType.ShamsiGregorian) + "'")]
        public DateTimeType DirectoryNameDateType { set; get; }

        /// <summary>
        ///     Enable log for component G9LogHandler
        ///     Logs like: start datetime, end datetime and other additional
        /// </summary>
        [G9ConfigHint("Enable log for component G9LogHandler")]
        [G9ConfigHint("Logs like: start datetime, end datetime and other additional")]
        public bool ComponentLog { set; get; } = true;

        /// <summary>
        ///     Specify file logging type is enable
        /// </summary>
        [G9ConfigHint("Specify file logging type is enable")]
        public LogsTypeConfig ActiveFileLogs { set; get; } = new LogsTypeConfig();

        /// <summary>
        ///     Specify console logging type is enable
        /// </summary>
        [G9ConfigHint("Specify console logging type is enable")]
        public LogsTypeConfig ActiveConsoleLogs { set; get; } = new LogsTypeConfig(false);

        /// <summary>
        ///     Enable stack trace info for logs
        /// </summary>
        [G9ConfigHint("Enable stack trace info for logs")]
        public LogsTypeConfig EnableStackTraceInformation { set; get; } = new LogsTypeConfig();

        /// <summary>
        ///     Specify need encrypt data for log
        ///     open just by user pass
        /// </summary>
        [G9ConfigIgnore]
        public bool EnableEncryptionLog
        {
            get
            {
                // if user name is null of pass is null return false
                if (string.IsNullOrEmpty(LogUserName) || string.IsNullOrEmpty(LogPassword)) return false;

                // if encrypted user and pass has value return true
                if (!string.IsNullOrEmpty(_encryptedPassword) && !string.IsNullOrEmpty(_encryptedUserName)) return true;

                // Calculate encrypted user and pass for set
                // Set User And Pass
                var logUserNameTemp = LogUserName.Length == 16
                    ? LogUserName
                    : LogUserName.Length > 16
                        ? LogUserName.Substring(0, 16)
                        : LogUserName.PadRight(16, '9');

                var logPasswordTemp = LogPassword.Length == 16
                    ? LogPassword
                    : LogPassword.Length > 16
                        ? LogPassword.Substring(0, 16)
                        : LogPassword.PadRight(16, '9');

                // Generate MD5 with sum user pass and generate key and iv
                var tempAllKeys = logUserNameTemp + logPasswordTemp;
                tempAllKeys = CreateMD5(tempAllKeys);
                _encryptedUserName = tempAllKeys.Substring(0, 16);
                _encryptedPassword = tempAllKeys.Substring(16, 16);

                return true;
            }
        }

        /// <summary>
        ///     Log UserName
        ///     If need encryption log with user and password
        /// </summary>
        [G9ConfigHint("If need encryption log with user and password")]
        [G9ConfigHint("Optional: Log UserName")]
        public string LogUserName { set; get; }

        /// <summary>
        ///     Log Password
        ///     If need encryption log with user and password
        /// </summary>
        [G9ConfigHint("Optional: Log Password")]
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
        [G9ConfigIgnore]
        public string EncryptedUserName
        {
            get
            {
                return !EnableEncryptionLog ? null : _encryptedUserName;
            }
        }

        /// <summary>
        ///     Access to encrypted password
        /// </summary>
        [G9ConfigIgnore]
        public string EncryptedPassword
        {
            get
            {
                return !EnableEncryptionLog ? null : _encryptedPassword;
            }
        }

        /// <summary>
        ///     field for save max file size
        /// </summary>
        private decimal _maxFileSize = 3;

        /// <summary>
        ///     Specify max file size in byte
        /// </summary>
        [G9ConfigHint("Specify max file size in MB")]
        [G9ConfigHint("Minimum 3 and maximum 10")]
        public decimal MaxFileSize
        {
            set
            {
                _maxFileSizeInByte = 0;
                if (value < 3)
                    _maxFileSize = 3;
                else if (value > 10)
                    _maxFileSize = 10;
                else
                    _maxFileSize = value;
            }
            get => _maxFileSize;
        }

        /// <summary>
        ///     field for save max file size in byte
        /// </summary>
        private decimal _maxFileSizeInByte;

        /// <summary>
        ///     Access to max file size Mb => byte
        /// </summary>
        [G9ConfigIgnore]
        public decimal MaxFileSizeInByte
        {
            get
            {
                // if not zero mean set and return
                if (_maxFileSizeInByte != 0)
                    return _maxFileSizeInByte;
                // if max file size is zero or less zero mean infinity
                if (MaxFileSize <= 0)
                    return _maxFileSizeInByte = int.MaxValue;
                // else convert MB to byte
                _maxFileSizeInByte = MaxFileSize * 1048576;
                // if less than minimum file size => set minimum file size for it
                if (_maxFileSizeInByte < G9LogConst.MinimumFileSizeInByte)
                    _maxFileSizeInByte = G9LogConst.MinimumFileSizeInByte;

                return _maxFileSizeInByte;
            }
        }

        /// <summary>
        ///     Specify default culture for log reader
        /// </summary>
        [G9ConfigHint("Specify default culture for log reader")]
        [G9ConfigHint("values: '" + nameof(CultureType.en_us) + "' or '" + nameof(CultureType.fa) + "'")]
        public CultureType LogReaderDefaultCulture { set; get; }

        /// <summary>
        ///     Field for save time
        /// </summary>
        private int _saveTime = 1;

        /// <summary>
        ///     Specify time in second for save logs
        /// </summary>
        [G9ConfigHint("Specify time in second for save logs")]
        [G9ConfigHint("Minimum 1 and maximum 3600")]
        public int SaveTime
        {
            set
            {
                if (value < 1)
                    _saveTime = 1;
                else if (value > 3600)
                    _saveTime = 3600;
                else
                    _saveTime = value;
            }
            get => _saveTime;
        }

        /// <summary>
        ///     Field for save count
        /// </summary>
        private int _saveCount = 100;

        /// <summary>
        ///     Specify count of logs for save logs
        /// </summary>
        [G9ConfigHint("Specify count of logs for save logs")]
        [G9ConfigHint("Minimum 100 and maximum 10000")]
        public int SaveCount
        {
            set
            {
                if (value < 100)
                    _saveCount = 100;
                else if (value > 10000)
                    _saveCount = 10000;
                else
                    _saveCount = value;
            }
            get => _saveCount;
        }

        /// <summary>
        ///     Specify enable archive previous day
        /// </summary>
        [G9ConfigHint("Specify enable archive previous day")]
        public bool ZipArchivePreviousDay { set; get; } = true;

        /// <summary>
        ///     Specify log reader starter page
        /// </summary>
        [G9ConfigHint("Specify log reader starter page")]
        [G9ConfigHint("values: '" + nameof(LogReaderPages.Dashboard) + "' or '" + nameof(LogReaderPages.LogsManagement) + "'")]
        public LogReaderPages LogReaderStarterPage { set; get; }

        #endregion
    }
}