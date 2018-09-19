using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperLibrary.Util.Logging
{
    public static class LoggerSettings
    {
        private static string _name;
        private static string _path;
        public static string FILE_PATH => Path.Combine(_path, _name ?? "FILE_LOGGER.txt");

        public static void SetLogFilePath(string path)
        {
            var invalidChars = Path.GetInvalidPathChars();
            foreach (var c in invalidChars)
            {
                if (path.Contains(c))
                    throw new ArgumentOutOfRangeException("Der angebene Pfad enthält nich zulässige Zeichen");
            }

            _path = path;
        }

        public static void SetLogFileName(string name)
        {
            if (!name.Contains("."))
                throw new ArgumentException("bitte im Filenamen eine Extension angeben");
            _name = name;
        }
    }
}
