using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace _3dCartImportConsole.Helper
{
    static class Log
    {
        private static ILog _log;
        static Log()
        {
            _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            string path = String.Format("{0}{1}{2}{3}{4}", "Log_", DateTime.Now.Month.ToString()
                , DateTime.Now.Day.ToString()
                , DateTime.Now.Year.ToString(), ".log");

            ChangeFilePath("MyRollingFileAppender", path);
        }

        public static void Info(string message)
        {
            _log.Info(message);
            Console.WriteLine(message);
        }

        public static void Debug(string message)
        {
            _log.Info(message);
            Console.WriteLine(message);
        }
        public static void Error(string message)
        {
            _log.Info(message);
            Console.WriteLine($"System.ApplicationException: {0}", message);
        }
        public static void ChangeFilePath(string appenderName, string newFilename)
        {
            log4net.Repository.ILoggerRepository repository = log4net.LogManager.GetRepository();
            foreach (log4net.Appender.IAppender appender in repository.GetAppenders())
            {
                if (appender.Name.CompareTo(appenderName) == 0 && appender is log4net.Appender.FileAppender)
                {
                    log4net.Appender.FileAppender fileAppender = (log4net.Appender.FileAppender)appender;
                    fileAppender.File = System.IO.Path.Combine(fileAppender.File, newFilename);
                    fileAppender.ActivateOptions();
                }
            }
        }
    }
}
