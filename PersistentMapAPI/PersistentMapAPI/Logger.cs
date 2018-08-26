using System;

namespace PersistentMapAPI {
        public class Logger {
            public static void LogError(Exception ex) {
                Console.WriteLine("Message :" + ex.Message + "<br/>" + Environment.NewLine + "StackTrace :" + ex.StackTrace +
                    "" + Environment.NewLine + "Date :" + DateTime.Now.ToString());
            }


            public static void LogLine(string line) {
                Console.WriteLine("Date :" + DateTime.Now.ToString() + Environment.NewLine + line);
            }
        }

    }
