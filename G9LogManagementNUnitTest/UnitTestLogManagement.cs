using System;
using System.Threading;
using System.Threading.Tasks;
using G9LogManagement;
using G9LogManagement.Config;
using G9LogManagement.Enums;
using G9LogManagement.Structures;
using NUnit.Framework;

namespace G9LogManagementNUnitTest
{
    public class UnitTestLogManagement
    {

        public G9Log Logging;

        [SetUp]
        public void Setup()
        {
            
        }

        [Test]
        [Order(1)]
        public void CheckInitializeFoldersAndFilesLogReader()
        {
            var initialize = new InitializeFoldersAndFilesForLogReaders("G9Log");

        }

        [Test]
        [Order(2)]
        public void CheckExistsLogReadersIndexFile()
        {
            Thread.Sleep(1000);
            FileAssert.Exists("G9LogReaderTemplate/Index.html");
        }

        [Test]
        [Order(3)]
        public void CheckInitializeG9LogWithCustomConfig()
        {
            Logging = new G9Log("Test", new LogConfig()
            {
                SaveTime = 3,
                SaveCount = 500,
                LogUserName = "ImanKari",
                LogPassword = "1990",
                MaxFileSize = 6,
                LogReaderStarterPage = LogReaderPages.LogsManagement,
                LogReaderDefaultCulture = CultureType.en_us,
                DirectoryNameDateType = DateTimeType.Gregorian
            });
        }

        [Test]
        [Order(3)]
        public void CheckExistsG9LogConfigFile()
        {
            FileAssert.Exists(string.Format(G9LogConst.G9ConfigName, Logging.LogName));
        }

        [Test]
        [Order(4)]
        public void CheckInsertThousandLog_ExtensionDefaultClassTest()
        {
            // Test for 1 million log
            for(var index = 0; index < 1000; index++)
            {
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

            Thread.Sleep(10000);
        }

        [Test]
        [Order(5)]
        public void CheckInsertThousandLogWithCustomConfigAndEncryption()
        {
            // Test for 1 million log
            for (var index = 0; index < 1000; index++)
            {
                // Event
                Logging.G9LogEvent($"Event {index}", $"Event {index}", $"Event {index}");
                // Information
                Logging.G9LogInformation($"Information {index}", $"Information {index}", $"Information {index}");
                // Warning
                Logging.G9LogWarning($"Warning {index}", $"Warning {index}", $"Warning {index}");
                // Error
                Logging.G9LogError($"Error {index}", $"Error {index}", "Error");
                // Exception And Inner Exception
                Logging.G9LogException(
                    new Exception("Exception", new Exception("Exception", new Exception("Exception"))), $"Exception {index}",
                    $"Exception {index}", $"Exception {index}");
            };

            Thread.Sleep(10000);
        }
    }
}