using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yakoo.Avalonia.DrmSample.Utility
{
    public class SmartUpdater
    {

        private static readonly string SearchPath = "/mnt/";
        private static readonly string TempDirectory = "/tmp/updater";
        private static readonly string MainDirectory = "/usr/local/bin/kiosk";
        private static readonly string TargetFile = "/usr/local/bin/kiosk/WBStand.Avalonia";
        private static readonly string ServiceName = "WBStand.service";

        private static readonly string _userName = "kiosk";

        public int Execute(string[] args)
        {
            StopService(ServiceName);

            try
            {
                Logger.LogInfo("Staring copy files");
                ReplaceFilesInDirectoryRecursive(MainDirectory, TempDirectory);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error of copiengs files: {ex.Message}");
                throw;
            }

            MakeFileExecutable(TargetFile);
            StartService(ServiceName);


            return 0;
        }

        private static void ReplaceFilesInDirectoryRecursive(string targetDirectory, string sourceDirectory)
        {
            try
            {


                // 2. Create target directory if it doesn't exist
                Directory.CreateDirectory(targetDirectory);

                // 3. Get all files from the source directory
                string[] sourceFiles = Directory.GetFiles(sourceDirectory);

                Logger.LogInfo($"Starting copy of {sourceFiles.Length} files from '{sourceDirectory}' to '{targetDirectory}'");

                // 4. Iterate over the files and copy them to the target directory
                foreach (string sourceFile in sourceFiles)
                {
                    string fileName = Path.GetFileName(sourceFile);
                    string targetFile = Path.Combine(targetDirectory, fileName);

                    try
                    {
                        File.Copy(sourceFile, targetFile, true);
                        Logger.LogInfo($"File '{targetFile}' replaced with '{sourceFile}'.");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error replacing file '{targetFile}' with '{sourceFile}': {ex.Message}");
                    }
                }

                Logger.LogInfo($"Finished copying files from '{sourceDirectory}' to '{targetDirectory}'");

                // 5. Recursively process subdirectories
                string[] subdirectories = Directory.GetDirectories(sourceDirectory);
                foreach (string sourceSubdirectory in subdirectories)
                {
                    string directoryName = Path.GetFileName(sourceSubdirectory);
                    string targetSubdirectory = Path.Combine(targetDirectory, directoryName);

                    ReplaceFilesInDirectoryRecursive(targetSubdirectory, sourceSubdirectory); // Recursive call
                }


            }
            catch (Exception ex)
            {
                Logger.LogError($"Error replacing files in directory: {ex.Message}");
            }
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
        private static void StopService(string serviceName)
        {
            try
            {
                RunCommand("systemctl", $"stop {serviceName}");
                Logger.LogInfo($"Service '{serviceName}' stopped.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error stopping service '{serviceName}': {ex.Message}");
            }
        }

        private static void StartService(string serviceName)
        {
            Logger.LogInfo("Starting service");
            try
            {
                RunCommand("systemctl", $"start {serviceName}");
                Logger.LogInfo($"Service '{serviceName}' started.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error starting service '{serviceName}': {ex.Message}");
            }
        }

        private static void RunCommand(string command, string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                UserName = _userName
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string errorOutput = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Command '{command} {arguments}' failed with exit code {process.ExitCode}. Error output: {errorOutput}");
                }
            }
        }
    }
}
