using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace HelperLibrary.Util.Logging
{
    public class FileLogger : ILogger
    {    
        private static Type _classNameType;

        public static FileLogger CreateClassLogger()
        {
            var frame = new StackFrame(1, false);
            _classNameType = frame.GetMethod().DeclaringType;
            return new FileLogger();
        }

        public object lockObj => new object();

        public void Debug(string logMesage)
        {
            var sb = new StringBuilder();
            sb.Append($"{_classNameType.Name}|DEBUG|");
            sb.Append(logMesage);
            AppendDateTime(sb);
            Log(sb.ToString());
        }

        private void AppendDateTime(StringBuilder sb)
        {
            sb.Append($" |{DateTime.Now}");
        }

        public void Error(string logMesage)
        {
            var sb = new StringBuilder();
            sb.Append($"{_classNameType.Name}|ERROR|");
            sb.Append(logMesage);
            AppendDateTime(sb);
            Log(sb.ToString());
        }

        public void Info(string logMesage)
        {
            var sb = new StringBuilder();
            sb.Append($"{_classNameType.Name}|INFO|");
            sb.Append(logMesage);
            AppendDateTime(sb);
            Log(sb.ToString());
        }

        private void Log(string logMesage)
        {
            lock (lockObj)
            {
                using (var fs = new FileStream(LoggerSettings.FILE_PATH, FileMode.Append))
                {
                    using (var sw = new StreamWriter(fs))
                    {
                        sw.WriteLine(logMesage);
                    }
                }
            }
        }
    }
}



