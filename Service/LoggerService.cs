using NLog;
using System;

namespace MPV.Services
{
    public static class LoggerService
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static void Info(string message) => logger.Info(message);
        public static void Warn(string message) => logger.Warn(message);
        public static void Error(string message, Exception ex) => logger.Error(ex, message);
    }
}
