using System;

namespace Fishare.Shared
{
    public static class Debugger
    {
        public static int LogLevel = 9;
        public static void Log(int level, string message) {
            if (level <= LogLevel) {
                Console.WriteLine("[{0}] [{2}.{3}.{4}] {1}", level, message, DateTime.Now.Hour, DateTime.Now.Second, DateTime.Now.Millisecond);
            }
        }

        public static bool CheckLogLevel() {
            if (LogLevel < 0 || LogLevel > 9) {
                return false;
            }

            return true;
        }
    }
}
