namespace Retweety.Diagnostics
{
    using System;

    public class EventLogger : IEventLogger
    {
        #region Properties

        public Action<LogLevel, string> LogHandler { get; set; }

        #endregion

        #region Constructor(s)

        public EventLogger()
        {
            LogHandler = new Action<LogLevel, string>((logType, message) => Console.WriteLine($"{logType}: {message}"));
        }

        public EventLogger(Action<LogLevel, string> logHandler)
        {
            LogHandler = logHandler;
        }

        #endregion

        #region Public Methods

        public void Trace(string format, params object[] args)
        {
            LogEvent(LogLevel.Trace, args.Length > 0 ? string.Format(format, args) : format);
        }

        public void Debug(string format, params object[] args)
        {
            LogEvent(LogLevel.Debug, args.Length > 0 ? string.Format(format, args) : format);
        }

        public void Info(string format, params object[] args)
        {
            LogEvent(LogLevel.Info, args.Length > 0 ? string.Format(format, args) : format);
        }

        public void Warn(string format, params object[] args)
        {
            LogEvent(LogLevel.Warning, args.Length > 0 ? string.Format(format, args) : format);
        }

        public void Error(string format, params object[] args)
        {
            LogEvent(LogLevel.Error, args.Length > 0 ? string.Format(format, args) : format);
        }

        public void Error(Exception ex)
        {
            LogEvent(LogLevel.Error, ex.ToString());
        }

        #endregion

        #region Private Methods

        private void LogEvent(LogLevel logLevel, string message)
        {
            LogHandler(logLevel, message);
        }

        #endregion
    }
}