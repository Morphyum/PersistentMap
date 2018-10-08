using System;
using System.IO;

namespace PersistentMapAPI {
    public class Logger {
        public static string LogFilePath = $"../Logs/Log.json";

        public static void LogError(Exception ex) {
            Console.WriteLine("Message :" + ex.Message + "<br/>" + Environment.NewLine + "StackTrace :" + ex.StackTrace +
                "" + Environment.NewLine + "Date :" + DateTime.Now.ToString());
        }


        public static void LogLine(string line) {
            Console.WriteLine(Environment.NewLine + "Date :" + DateTime.Now.ToString() + Environment.NewLine + line);
        }

        public static void LogToFile(string line) {
            (new FileInfo(LogFilePath)).Directory.Create();
            using (StreamWriter writer = new StreamWriter(LogFilePath, true)) {
                writer.WriteLine(line + Environment.NewLine + "Date :" + DateTime.Now.ToString());
                writer.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
            }
        }
    }

}
