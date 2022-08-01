using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using G9LogManagement;
using G9LogManagement.Config;
using G9LogManagement.Enums;

namespace G9LogManagementTest
{
    class Program
    {
        /// <summary>
        /// Test Main
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            G9LogDefaultConfigInitialize.InitializeDefaultInstanceLogConfig(new LogConfig()
            {
                SaveTime = 3,
                SaveCount = 500,
                //LogUserName = "Admin",
                //LogPassword = "ImanKari1990",
                MaxFileSize = 9,
                LogReaderStarterPage = LogReaderPages.Dashboard,
                LogReaderDefaultCulture = CultureType.en_us,
                DirectoryNameDateType = DateTimeType.Gregorian,
                ActiveConsoleLogs = new LogsTypeConfig(true)
            });

            // Test for 1 million log
            for (var index = 0; index < 1000; index++)
            {
                Thread.Sleep(99);
                // Event
                $"Event {index}".G9LogEvent_Default($"Event {index}", $"Event {index}");
                // Information
                $"Information {index}".G9LogInformation_Default($"Information {index}", $"Information {index}");
                // Warning
                $"Warning {index}".G9LogWarning_Default($"Warning {index}", $"Warning {index}");
                // Error
                $"Error {index}".G9LogError_Default($"Error {index}", "Error");
                // Exception And Inner Exception
                new Exception("Exception", new Exception("Exception", new Exception("Exception")))
                    .G9LogException_Default("Exception {index}",
                        $"Exception {index}", $"Exception {index}");
            }

            Console.ReadLine();
        }
    }
}
