﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging.StructuredLogger;
using Microsoft.Win32;

namespace StructuredLogViewer
{
    public class MSBuildLocator
    {
        private static readonly VisualStudioInstanceQueryOptions visualStudioQueryOptions = new VisualStudioInstanceQueryOptions
        {
            DiscoveryTypes = DiscoveryType.DotNetSdk | DiscoveryType.DeveloperConsole | DiscoveryType.VisualStudioSetup
        };

        private static IEnumerable<string> GetMsBuildInstancesFromVisualStudio()
        {
            foreach (var instance in Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances(visualStudioQueryOptions))
            {
                yield return Path.Combine(instance.MSBuildPath, "MSBuild.exe");
            }
        }

        public static string[] GetMSBuildLocations()
        {
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var hardcoded = new[]
            {
                Path.Combine(programFilesX86, @"MSBuild\14.0\Bin\MSBuild.exe"),
                Path.Combine(programFilesX86, @"MSBuild\14.0\Bin\amd64\MSBuild.exe"),
                Path.Combine(programFilesX86, @"MSBuild\12.0\Bin\MSBuild.exe"),
                Path.Combine(programFilesX86, @"MSBuild\12.0\Bin\amd64\MSBuild.exe"),
                Path.Combine(windows, @"Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"),
                Path.Combine(windows, @"Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"),
            };

            var vs15Locations = GetVS15Locations();
            candidates.UnionWith(vs15Locations.Select(l => Path.Combine(l, "MSBuild", "15.0", "Bin", "MSBuild.exe")));
            candidates.UnionWith(vs15Locations.Select(l => Path.Combine(l, "MSBuild", "15.0", "Bin", "amd64", "MSBuild.exe")));
            candidates.UnionWith(hardcoded);
            candidates.UnionWith(GetMsBuildInstancesFromVisualStudio());

            var finalResults = candidates
                .Where(File.Exists)
                .ToArray();

            return finalResults;
        }

        private static string[] GetVS15Locations()
        {
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var installer = Path.Combine(programFilesX86, "Microsoft Visual Studio", "Installer");
            var vswhere = Path.Combine(installer, "vswhere.exe");
            if (!File.Exists(vswhere))
            {
                return Array.Empty<string>();
            }

            var args = "-prerelease -format value -property installationPath -nologo";
            var startInfo = new ProcessStartInfo
            {
                Arguments = args,
                CreateNoWindow = true,
                FileName = vswhere,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            var resultList = new List<string>();

            var process = Process.Start(startInfo);
            var output = process.StandardOutput.ReadToEnd();
            resultList.AddRange(output.GetLines().Where(Directory.Exists));

            process.WaitForExit(3000);

            if (process.ExitCode != 0)
            {
                return Array.Empty<string>();
            }

            return resultList.ToArray();
        }

        public static void BrowseForMSBuildExe()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "MSBuild.exe|MSBuild.exe";
            openFileDialog.Title = "Select MSBuild.exe location";
            openFileDialog.CheckFileExists = true;
            var result = openFileDialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            SettingsService.AddRecentMSBuildLocation(openFileDialog.FileName);
        }
    }
}