﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using G9LogManagement;
using G9LogManagement.Config;
using G9LogManagement.Enums;

namespace G9LogManagementTest
{
    class Program
    {
        static void Main(string[] args)
        {
            G9LogDefaultConfigInitialize.InitializeDefaultInstanceLogConfig(new LogConfig()
            {
                BaseApp = "AppDomain.CurrentDomain.BaseDirectory",
                SaveTime = 3,
                SaveCount = 500,
                LogUserName = "ImanKari",
                LogPassword = "1990",
                MaxFileSize = 6,
                LogReaderStarterPage = LogReaderPages.LogsManagement,
                LogReaderDefaultCulture = CultureType.fa,
                DirectoryNameDateType = DateTimeType.GregorianShamsi
            });

            // Test for 1 million log
            for (var index = 0; index < 1000; index++)
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

            Console.ReadLine();
        }
    }
}