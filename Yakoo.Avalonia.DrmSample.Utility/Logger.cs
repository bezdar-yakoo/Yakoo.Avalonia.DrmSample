using System;
using System.IO;

namespace Yakoo.Avalonia.DrmSample.Utility
{
    public static class Logger
    {
        public static string FilePath { get; } = "/var/log/WBStand.Utilities.log";

        public static void LogInfo(string message, bool logToConsole = true)
        {
            string data = $"info: {message}";
            try
            {
                FileInfo fileInfo = new FileInfo(FilePath);
                if (fileInfo.Exists == false)
                    fileInfo.Create();
                using var stream = fileInfo.AppendText();
                stream.WriteLine(data);
            }
            catch { }

            if (logToConsole)
                Console.WriteLine(data);
        }
        public static void LogError(string message, bool logToConsole = true)
        {
            string data = $"Error: {message}";
            try
            {
                FileInfo fileInfo = new FileInfo(FilePath);
                if (fileInfo.Exists == false)
                    fileInfo.Create();
                using var stream = fileInfo.AppendText();
                stream.WriteLine(data);
            }
            catch { }

            if (logToConsole)
                Console.WriteLine(data);
        }
    }
}
