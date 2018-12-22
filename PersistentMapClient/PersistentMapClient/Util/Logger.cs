using System;
using System.IO;

namespace PersistentMapClient {
    public class Logger {
        //static string filePath = $"{PersistentMapClient.ModDirectory}/Log.txt";
        //public static void LogError(Exception ex) {
        //    (new FileInfo(filePath)).Directory.Create();
        //    using (StreamWriter writer = new StreamWriter(filePath, true)) {
        //        writer.WriteLine("Message :" + ex.Message + "<br/>" + Environment.NewLine + "StackTrace :" + ex.StackTrace +
        //           "" + Environment.NewLine + "Date :" + DateTime.Now.ToString());
        //        writer.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
        //    }
        //}

        //public static void LogLine(string line) {
        //    (new FileInfo(filePath)).Directory.Create();
        //    using (StreamWriter writer = new StreamWriter(filePath, true)) {
        //        writer.WriteLine(line + Environment.NewLine + "Date :" + DateTime.Now.ToString());
        //        writer.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
        //    }
        //}

        private static StreamWriter LogStream;
        private readonly bool isDebug = false;

        public Logger(string modDir, string logName, bool isDebug) {
            string logFile = Path.Combine(modDir, $"{logName}.log");
            if (File.Exists(logFile)) {
                File.Delete(logFile);
            }

            LogStream = File.AppendText(logFile);
            LogStream.AutoFlush = true;

            this.isDebug = isDebug;
        }

        ~Logger() {
            if (LogStream != null) {
                LogStream.Flush();
                LogStream.Close();
            }
        }

        public void LogIfDebug(string message) {
            if (this.isDebug) {
                Log(message);
            }
        }

        public void Log(string message) {
            string now = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            LogStream.WriteLine($"{now} - {message}");
        }

        public void LogError(Exception error) {
            string now = DateTime.UtcNow.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
            LogStream.WriteLine($"{now} - {error.Message}");
        }


        public void Flush() {
            LogStream.Flush();
        }
    }
}