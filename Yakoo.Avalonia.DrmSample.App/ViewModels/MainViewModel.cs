using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Yakoo.Avalonia.DrmSample.App.ViewModels
{
    public class MainViewModel : ReactiveObject
    {

        private static readonly string SearchPath = "/mnt/";
        private static readonly string TempDirectory = "/tmp/updater";
        private static readonly string MainDirectory = "/usr/local/bin/kiosk";
        private static readonly string TargetFile = "/usr/local/bin/kiosk/WBStand.Avalonia";
        private static readonly string ServiceName = "Kiosk.service";

        private static readonly string _userName = "kiosk";

        public ReactiveCommand<Unit, Unit> UpdateCommand { get; }
        public ReactiveCommand<Unit, Unit> GetFilesCommand { get; }

        public MainViewModel()
        {
            UpdateCommand = ReactiveCommand.CreateFromTask(UpdateAsync);
            GetFilesCommand = ReactiveCommand.CreateFromTask(GetFilesAsync);
        }

        private async Task GetFilesAsync()
        {

        }

        private async Task UpdateAsync()
        {
            var currentVersion = typeof(Program).Assembly.GetName().Version;

            string pattern = @"WBStand\.Avalonia-(?<Version>\d+\.\d+\.\d+\.\d+)\.zip";
            Regex regex = new Regex(pattern);

            var archives = FindFiles("/mnt/", "WBStand.Avalonia-*.zip");
            Log.Information("Found {Count} archives", archives.Count);
            if (!archives.Any())
                throw new FileNotFoundException();

            Log.Information("Archives:\n{Archives}", string.Join("\n", archives));

            var archiveVersions = archives
                .Select(t => new Tuple<Version, string>(new Version(regex.Match(t).Groups["Version"].Value), t));
            var newestArchive = archiveVersions.MaxBy(t => t.Item1);

            if (currentVersion >= newestArchive.Item1)
            {
                Log.Information("CurrentVersion: {CurrentVersion}, newestArchive: {NewestArchive} - nothing to update",
                    currentVersion);
                throw new Exception(currentVersion.ToString());
                //Log.Information("CurrentVersion: {CurrentVersion}, newestArchive: {NewestArchive} - nothing to update",
                //    currentVersion, newestArchive.Item1);
                //throw new Exception(currentVersion.ToString(), newestArchive.Item1.ToString());
            }

            if (!Directory.Exists(TempDirectory))
                Directory.CreateDirectory(TempDirectory);

            Log.Information("Unzipping file: {Archive}", newestArchive.Item2);
            UnzipFile(newestArchive.Item2, TempDirectory, "263329c2-474d-496d-b7e3-9b9f221496c3");

            Log.Information("Start utility");
            var utility = FindFiles(TempDirectory, "WBStand.Avalonia.Utilities").FirstOrDefault();
            if (utility == null)
            {
                utility = "/usr/local/bin/balancer/WBStand.Avalonia.Utilities";
            }

            SetFileChown(utility);
            MakeFileExecutable(utility);
            Log.Information("Using utility file: {Utility}", utility);

            RunCommand(utility, "--smartupdate");
        }
        private static void RunCommand(string command, string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/nohup", // Путь к nohup
                Arguments = $"{command} {arguments}" + " &", // Передача аргументов и символ '&' для запуска в фоне
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
            }
        }
        private static List<string> FindFiles(string searchPath, string fileName)
        {
            List<string> foundFiles = new List<string>();
            try
            {
                foundFiles.AddRange(Directory.GetFiles(searchPath, fileName));
            }
            catch (Exception ex)
            {
            }

            foreach (string subDirectory in Directory.GetDirectories(searchPath))
            {
                try
                {
                    foundFiles.AddRange(FindFiles(subDirectory, fileName));

                }
                catch (Exception ex1)
                {
                }
            }
            return foundFiles;
        }
        public static void MakeFileExecutable(string filePath)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = "+x " + filePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string error = process.StandardError.ReadToEnd();
                    throw new Exception($"chmod завершился с кодом ошибки {process.ExitCode}: {error}");
                }
            }
        }
        public static void SetFileChown(string filePath)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "chown",
                Arguments = " wbstand " + filePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string error = process.StandardError.ReadToEnd();
                    throw new Exception($"chmod завершился с кодом ошибки {process.ExitCode}: {error}");
                }
            }
        }

        private static string EscapeArgument(string arg)
        {
            if (string.IsNullOrEmpty(arg))
                return "\"\"";

            // Проверка на необходимость экранирования
            if (arg.IndexOfAny(new char[] { ' ', '"', '\\' }) == -1)
                return arg;

            var sb = new StringBuilder();
            sb.Append('"');
            int backslashCount = 0;

            foreach (char c in arg)
            {
                switch (c)
                {
                    case '\\':
                        backslashCount++;
                        break;

                    case '"':
                        sb.Append('\\', backslashCount * 2 + 1);
                        sb.Append('"');
                        backslashCount = 0;
                        break;

                    default:
                        if (backslashCount > 0)
                        {
                            sb.Append('\\', backslashCount);
                            backslashCount = 0;
                        }
                        sb.Append(c);
                        break;
                }
            }

            if (backslashCount > 0)
                sb.Append('\\', backslashCount * 2);

            sb.Append('"');
            return sb.ToString();
        }
        private string BuildArguments(string archivePath, string extractPath, string password)
        {
            string args = $"x {EscapeArgument(archivePath)} -o{EscapeArgument(extractPath)} -y -bso0";

            if (!string.IsNullOrEmpty(password))
            {
                args += $" -p{EscapeArgument(password)}";
            }

            return args;
        }
        private void UnzipFile(string archivePath, string extractPath, string password = null)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "7z";
            psi.Arguments = BuildArguments(archivePath, extractPath, password);
            psi.WorkingDirectory = "/";
            psi.UseShellExecute = false;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = psi;

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            process.OutputDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                    output.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                    error.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            Console.WriteLine("Output: " + output);
            Console.WriteLine("Error: " + error);
            Console.WriteLine("Exit code: " + process.ExitCode);
        }

    }
    public static class Log
    {
        public static string FilePath { get; } = "/var/log/WBStand.Utilities.log";

        public static void Information(string message, object param = null, bool logToConsole = true)
        {
            string data = $"info: {message}";
            if(param != null)
            {
                data = $"{param.ToString()} | {data}";
            }
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
