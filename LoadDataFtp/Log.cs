using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadDataFtp
{
    public static class Log
    {
        private static object syncError = new object();
        private static object syncMessage = new object();
        public static void WriteError(LoadException ex)
        {
            try
            {
                string pathToLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                if (!Directory.Exists(pathToLog))
                    Directory.CreateDirectory(pathToLog); 
                string filename = Path.Combine(pathToLog, string.Format("ErrorLog_{0}_{1:dd.MM.yyy}.log", AppDomain.CurrentDomain.FriendlyName, DateTime.Now));
                string fullText = string.Format("[{0:dd.MM.yyy HH:mm:ss.fff}] [{1}.{2}()] {3} {4}\r\n", DateTime.Now, ex.e.TargetSite.DeclaringType, ex.e.TargetSite.Name, ex.e.Message, ex.Extrainfo);
                lock (syncError)
                {
                    File.AppendAllText(filename, fullText, Encoding.GetEncoding("Windows-1251"));
                }
            }
            catch
            {
            }
        }
        public static void WriteMessage(string message)
        {
            try
            {
                string pathToLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                if (!Directory.Exists(pathToLog))
                    Directory.CreateDirectory(pathToLog);
                string filename = Path.Combine(pathToLog, string.Format("MessageLog_{0}_{1:dd.MM.yyy}.log", AppDomain.CurrentDomain.FriendlyName, DateTime.Now));
                string fullText = string.Format("[{0:dd.MM.yyy HH:mm:ss.fff}] [{1}] \r\n", DateTime.Now, message);
                lock (syncMessage)
                {
                    File.AppendAllText(filename, fullText, Encoding.GetEncoding("Windows-1251"));
                }
            }
            catch
            {
            }
        }

    }
}
