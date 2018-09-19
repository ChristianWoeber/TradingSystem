using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelperLibrary;
using HelperLibrary.Util.Logging;

namespace TestConsole
{
    class Program
    {
        private static readonly FileLogger log = FileLogger.CreateClassLogger();
        static void Main(string[] args)
        {
            log.Info("This is a test");
            new TEST1();
            new TEST2();
        }
    }

    public class TEST1
    {
        private static readonly FileLogger log = FileLogger.CreateClassLogger();
        public TEST1()
        {
            log.Error("This is a ERROR test");
        }
    }

    public class TEST2
    {
        private static readonly FileLogger log = FileLogger.CreateClassLogger();
        public TEST2()
        {
            log.Debug("This is a DEBUG test");
        }
    }
}
