using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Che_ssServer.Services
{

    public class LoggingService
    {
#if DEBUG
        private static string _logDirectory = Program.MAIN_PATH + @"Logs";
#else
        private static string _logDirectory = Program.MAIN_PATH + @"Logs";
#endif
        private static string _logFile => Path.Combine(_logDirectory, $"{DateTime.UtcNow.ToString("yyyy-MM-dd")}.txt");


        private static string longest = LogSeverity.Warning.ToString() + 1;

        public static Object _logLock = new object();

        public static Task OnLogAsync(LogMessage msg)
        {
            lock (_logLock)
            {
                if (!Directory.Exists(_logDirectory))     // Create the log directory if it doesn't exist
                    Directory.CreateDirectory(_logDirectory);
                if (!File.Exists(_logFile))               // Create today's log file if it doesn't exist
                    File.Create(_logFile).Dispose();

                int spaces = longest.Length;
                spaces -= msg.Severity.ToString().Length;

                int startLength = "04:37:08.[Info] App: ".Length;

                string spaceGap = String.Concat(Enumerable.Repeat(" ", spaces));
                //for(int i = 0; i < spaces; i++) { spaceGap += " "; }

                string logText = $"{DateTime.Now.ToString("hh:mm:ss.fff")}{spaceGap}[{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
                File.AppendAllText(_logFile, logText + "\r\n");     // Write the log text to a file
                logText = logText.Replace("\n", "\n    ..." + (new string(' ', startLength)));
                switch (msg.Severity)
                {
                    case LogSeverity.Critical:
                    case LogSeverity.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogSeverity.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogSeverity.Info:
#if DEBUG
                        Console.ForegroundColor = ConsoleColor.Blue;
#else
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
#endif
                        break;
                    case LogSeverity.Verbose:
                    case LogSeverity.Debug:
                        if (Program.BOT_DEBUG)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                        break;
                }
                return Console.Out.WriteLineAsync(logText);       // Write the log text to the console
            }
        }
    }
}
