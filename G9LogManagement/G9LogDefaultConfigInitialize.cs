using G9LogManagement.Config;

namespace G9LogManagement
{
    public static class G9LogDefaultConfigInitialize
    {
        /// <summary>
        ///     Specified logging config for default instance
        /// </summary>
        public static LogConfig DefaultInstanceLogConfig { private set; get; }

        /// <summary>
        ///     Run before use default instance of logging
        ///     Run just first time
        /// </summary>
        /// <param name="logConfigForDefaultInstance"></param>
        public static void InitializeDefaultInstanceLogConfig(LogConfig logConfigForDefaultInstance)
        {
            DefaultInstanceLogConfig = logConfigForDefaultInstance;
        }
    }
}