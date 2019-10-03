using System;
using System.Threading;
using G9LogManagement;
using G9LogManagement.Config;
using NUnit.Framework;

namespace G9LogManagementNUnitTest
{
    public class UnitTestLogManagement
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [Order(1)]
        public void CheckInitializeFoldersAndFilesLogReader()
        {
            var initialize = new InitializeFoldersAndFilesForLogReaders();
        }

        [Test]
        [Order(2)]
        public void CheckExistsG9LogConfigFile()
        {
            Thread.Sleep(1000);
            FileAssert.Exists("G9Log.config");
        }

        //[Test]
        //[Order(3)]
        //public void CheckLoadConfig()
        //{
        //    var config = LogConfig.GetInstance();
        //}

        //[Test]
        //[Order(4)]
        //public void CheckLogManagement()
        //{
        //    for (var i = 0; i < 10; i++)
        //    {
        //        // Event
        //        "Event".G9LogEvent("Event", "Event");
        //        // Information
        //        "Information".G9LogInformation("Information", "Information");
        //        // Warning
        //        "Warning".G9LogWarning("Warning", "Warning");
        //        // Error
        //        "Error".G9LogError("Error", "Error");
        //        // Exception And Inner Exception
        //        new Exception("Exception", new Exception("Exception", new Exception("Exception"))).G9LogException(
        //            "Exception", "Exception", "Exception");
        //    }
        //    Thread.Sleep(1000);
        //}
    }
}