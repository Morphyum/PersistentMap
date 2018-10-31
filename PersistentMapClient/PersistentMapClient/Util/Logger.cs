using System;
using System.IO;

namespace PersistentMapClient {
    public class Logger {
        static string filePath = $"{PersistentMapClient.ModDirectory}/Log.txt";
        public static void LogError(Exception ex) {
            (new FileInfo(filePath)).Directory.Create();
            using (StreamWriter writer = new StreamWriter(filePath, true)) {
                writer.WriteLine("Message :" + ex.Message + "<br/>" + Environment.NewLine + "StackTrace :" + ex.StackTrace +
                   "" + Environment.NewLine + "Date :" + DateTime.Now.ToString());
                writer.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
            }
        }

        public static void LogLine(string line) {
            (new FileInfo(filePath)).Directory.Create();
            using (StreamWriter writer = new StreamWriter(filePath, true)) {
                writer.WriteLine(line + Environment.NewLine + "Date :" + DateTime.Now.ToString());
                writer.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
            }
        }
    }
}