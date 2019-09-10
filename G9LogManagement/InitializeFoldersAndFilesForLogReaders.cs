using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace G9LogManagement
{
    /// <summary>
    ///     Class Initialize and create all folders and files for log reader
    /// </summary>
    public class InitializeFoldersAndFilesForLogReaders
    {
        #region Fields And Properties

        /// <summary>
        ///     Specify initialize for first time
        ///     Override all file for first time
        /// </summary>
        private static bool _initializeFirstTime = true;

        #endregion

        #region Methods

        /// <summary>
        ///     Constructor
        ///     Initialize and create all folders and files for log reader
        /// </summary>

        #region InitializeFoldersAndFilesForLogReaders

        public InitializeFoldersAndFilesForLogReaders()
        {
            var logReaderTemplateFoldersList = new List<string>
            {
                "G9LogReaderTemplate",
                "G9LogReaderTemplate/Data",
                "G9LogReaderTemplate/Utilities",
                "G9LogReaderTemplate/Utilities/Bootstrap",
                "G9LogReaderTemplate/Utilities/CanvasJs",
                "G9LogReaderTemplate/Utilities/CSS",
                "G9LogReaderTemplate/Utilities/ICON",
                "G9LogReaderTemplate/Utilities/Images",
                "G9LogReaderTemplate/Utilities/JS",
                "G9LogReaderTemplate/Utilities/LanguageHandler",
                "G9LogReaderTemplate/Utilities/Bootstrap/css",
                "G9LogReaderTemplate/Utilities/Bootstrap/js",
                "G9LogReaderTemplate/Utilities/ICON/DashboardIcon",
                "G9LogReaderTemplate/Utilities/ICON/LogItemsIcon",
                "G9LogReaderTemplate/Utilities/ICON/LogTypeMode",
                "G9LogReaderTemplate/Utilities/Images/Background",
                "G9LogReaderTemplate/Utilities/Images/G9Logo",
                "G9LogReaderTemplate/Utilities/Images/Login",
                "G9LogReaderTemplate/Utilities/Images/Menu",
                "G9LogReaderTemplate/Utilities/Images/Pattern",
                "G9LogReaderTemplate/Utilities/JS/PersianDateTime",
                "G9LogReaderTemplate/Utilities/LanguageHandler/Images",
                "G9LogReaderTemplate/Utilities/LanguageHandler/Lang",
                "G9LogReaderTemplate/Utilities/LanguageHandler/Images/Menu",
                "G9LogReaderTemplate/Utilities/LanguageHandler/Images/Pattern"
            };

            var logReaderTemplateFilesList = new List<string>
            {
                "G9Log.config",
                "G9LogReaderTemplate/Index.html",
                "G9LogReaderTemplate/Data/DataFilePath.txt",
                "G9LogReaderTemplate/Utilities/CanvasJs/jquery.canvasjs.min.js",
                "G9LogReaderTemplate/Utilities/CSS/G9LogReaderStyle-rtl.css",
                "G9LogReaderTemplate/Utilities/CSS/G9LogReaderStyle.css",
                "G9LogReaderTemplate/Utilities/ICON/G9LogReaderIcon.ico",
                "G9LogReaderTemplate/Utilities/Images/User.png",
                "G9LogReaderTemplate/Utilities/JS/aes.js",
                "G9LogReaderTemplate/Utilities/JS/G9LogConfig.js",
                "G9LogReaderTemplate/Utilities/JS/G9LogHandler.js",
                "G9LogReaderTemplate/Utilities/JS/jquery-3.4.1.min.js",
                "G9LogReaderTemplate/Utilities/JS/jquery-ui.min.CustomiseForThisProject.js",
                "G9LogReaderTemplate/Utilities/JS/md5.js",
                "G9LogReaderTemplate/Utilities/LanguageHandler/DefaultCulture.js",
                "G9LogReaderTemplate/Utilities/LanguageHandler/G9LanguageHandler.css",
                "G9LogReaderTemplate/Utilities/LanguageHandler/G9LanguageHandler.js",
                "G9LogReaderTemplate/Utilities/LanguageHandler/Languages.js",
                "G9LogReaderTemplate/Utilities/Bootstrap/css/bootstrap-grid.min.css",
                "G9LogReaderTemplate/Utilities/Bootstrap/css/bootstrap-reboot.min.css",
                "G9LogReaderTemplate/Utilities/Bootstrap/css/bootstrap.min.css",
                "G9LogReaderTemplate/Utilities/Bootstrap/js/bootstrap.bundle.min.js",
                "G9LogReaderTemplate/Utilities/Bootstrap/js/bootstrap.min.js",
                "G9LogReaderTemplate/Utilities/Bootstrap/js/popper.min.js",
                "G9LogReaderTemplate/Utilities/Bootstrap/js/tooltip.min.js",
                "G9LogReaderTemplate/Utilities/ICON/DashboardIcon/copy-content.png",
                "G9LogReaderTemplate/Utilities/ICON/DashboardIcon/Error.png",
                "G9LogReaderTemplate/Utilities/ICON/DashboardIcon/Event.png",
                "G9LogReaderTemplate/Utilities/ICON/DashboardIcon/Fatal.png",
                "G9LogReaderTemplate/Utilities/ICON/DashboardIcon/Information.png",
                "G9LogReaderTemplate/Utilities/ICON/DashboardIcon/LogFiles.png",
                "G9LogReaderTemplate/Utilities/ICON/DashboardIcon/Total.png",
                "G9LogReaderTemplate/Utilities/ICON/DashboardIcon/Warning.png",
                "G9LogReaderTemplate/Utilities/ICON/LogItemsIcon/0-Identity.png",
                "G9LogReaderTemplate/Utilities/ICON/LogItemsIcon/1-Title.png",
                "G9LogReaderTemplate/Utilities/ICON/LogItemsIcon/2-DateTime.png",
                "G9LogReaderTemplate/Utilities/ICON/LogItemsIcon/3-Body.png",
                "G9LogReaderTemplate/Utilities/ICON/LogItemsIcon/4-Path.png",
                "G9LogReaderTemplate/Utilities/ICON/LogItemsIcon/5-Method.png",
                "G9LogReaderTemplate/Utilities/ICON/LogItemsIcon/6-Line.png",
                "G9LogReaderTemplate/Utilities/ICON/LogTypeMode/Error.png",
                "G9LogReaderTemplate/Utilities/ICON/LogTypeMode/Event.png",
                "G9LogReaderTemplate/Utilities/ICON/LogTypeMode/Exception.png",
                "G9LogReaderTemplate/Utilities/ICON/LogTypeMode/Info.png",
                "G9LogReaderTemplate/Utilities/ICON/LogTypeMode/Warn.png",
                "G9LogReaderTemplate/Utilities/Images/G9Logo/G9Logo.png",
                "G9LogReaderTemplate/Utilities/Images/G9Logo/Title.png",
                "G9LogReaderTemplate/Utilities/Images/Login/BG.jpg",
                "G9LogReaderTemplate/Utilities/Images/Menu/BG.jpg",
                "G9LogReaderTemplate/Utilities/Images/Pattern/MainPattern.png",
                "G9LogReaderTemplate/Utilities/JS/PersianDateTime/PersianDateTime.js",
                "G9LogReaderTemplate/Utilities/LanguageHandler/Images/User.png",
                "G9LogReaderTemplate/Utilities/LanguageHandler/Lang/Lang-en-us.js",
                "G9LogReaderTemplate/Utilities/LanguageHandler/Lang/Lang-fa.js",
                "G9LogReaderTemplate/Utilities/LanguageHandler/Images/Menu/BG.jpg",
                "G9LogReaderTemplate/Utilities/LanguageHandler/Images/Pattern/pattern.png"
            };

            // Create all folders if not exists
            logReaderTemplateFoldersList.ForEach(s =>
            {
                if (!Directory.Exists(s))
                    Directory.CreateDirectory(s);
            });

            // Create all files in not exists
            logReaderTemplateFilesList.ForEach(s =>
            {
                if (_initializeFirstTime)
                {
                    WriteEmbeddedResourceToFile((nameof(G9LogManagement) + "." + s).Replace('/', '.'), s);
                }
                else
                {
                    if (!File.Exists(s))
                        WriteEmbeddedResourceToFile((nameof(G9LogManagement) + "." + s).Replace('/', '.'), s);
                }
            });
            // Set false initialize for first time
            _initializeFirstTime = false;
        }

        #endregion

        /// <summary>
        ///     Write file from embedded resource to custom path
        /// </summary>
        /// <param name="embeddedResourceAddress">
        ///     Embedded resource address
        ///     NameSpace.folder...FileName
        /// </param>
        /// <param name="pathAndFileName">Specify path and file name</param>

        #region WriteEmbeddedResourceToFile

        private void WriteEmbeddedResourceToFile(string embeddedResourceAddress, string pathAndFileName)
        {
            using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedResourceAddress))
            {
                using (var file = new FileStream(pathAndFileName, FileMode.Create, FileAccess.Write))
                {
                    if (pathAndFileName.Contains("G9LogReaderTemplate/Index.html"))
                    {
                        var assembly = Assembly.GetExecutingAssembly();
                        var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                        var version = fileVersionInfo.ProductVersion;

                        var reader = new StreamReader(resource);
                        var index = reader.ReadToEnd();
                        var bytes = Encoding.UTF8.GetBytes(index.Replace("<G9AppVersion/>", version));
                        file.Write(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        resource.CopyTo(file);
                    }
                }
            }
        }

        #endregion

        #endregion
    }
}