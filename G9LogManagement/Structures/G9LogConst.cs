namespace G9LogManagement.Structures
{
    public struct G9LogConst
    {
        /// <summary>
        ///     Specify G9LogReaderTemplate path
        /// </summary>
        public const string G9ConfigName = "G9Log-{0}.config";

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
        ///     Specify default change config sample text
        /// </summary>
        public const string DefaultChangeConfigText = "-ChangeConfig-";

        /// <summary>
        ///     Specify default language culture file path
        /// </summary>
        public const string DefaultLanguageCultureFile = "Utilities/LanguageHandler/DefaultCulture.js";

        /// <summary>
        ///     Specify minimum file size in byte
        /// </summary>
        public const int MinimumFileSizeInByte = 3000000;

        /// <summary>
        ///     Default time out for close stream when close app
        /// </summary>
        public const int DefaultTimeOutToCloseStreamWhenExitApp = 963;
    }
}