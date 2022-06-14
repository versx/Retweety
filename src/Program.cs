namespace Retweety
{
    using System;
    using System.Diagnostics;
    using System.IO;

    using Retweety.Configuration;
    using Retweety.Diagnostics;
    using Retweety.Services;

    internal class Program
    {
        static void Main(string[] args)
        {
            var config = Config.Load(Strings.ConfigFileName);
            var logger = new EventLogger(OnLogEvent);
            var tweeter = new TweetService(config, logger);

            logger.Debug($"Starting retweet service...");
            tweeter.Start();
            logger.Debug($"Retweet service started.");

            logger.Info($"{Strings.BotName} v{Strings.BotVersion} running...");

            Process.GetCurrentProcess().WaitForExit();
            tweeter.Stop();
        }

        public static void OnLogEvent(LogLevel logLevel, string message)
        {
            // Write log to console
            Console.ForegroundColor = GetConsoleColor(logLevel);
            var logLevelUpper = logLevel.ToString().ToUpper();
            Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: {logLevelUpper} >> {message}");
            Console.ResetColor();

            try
            {
                // Create logs directory if needed
                CreateLogsDirectory();

                // Write log to file
                var logFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                var logPath = Path.Combine(Strings.LogsFolder, logFileName);
                var logData = $"{DateTime.Now.ToLongTimeString()}: {logLevelUpper} >> {message}\r\n";
                File.AppendAllText(logPath, logData);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR]: {ex}");
                Console.ResetColor();
            }
        }

        static ConsoleColor GetConsoleColor(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Error => ConsoleColor.DarkRed,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Success => ConsoleColor.Green,
                LogLevel.Trace => ConsoleColor.Cyan,
                LogLevel.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.DarkGray,
            };
        }

        static void CreateLogsDirectory()
        {
            // Create logs folder if not exists
            if (!Directory.Exists(Strings.LogsFolder))
            {
                Directory.CreateDirectory(Strings.LogsFolder);
            }
        }
    }
}