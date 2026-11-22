using System;
using System.Diagnostics;
using System.IO;

namespace lumen_sante
{
    internal static class PluginPaths
    {
        internal static string InstallationDirectory { get; } = ReadInstallationDirectory();

        private static string ReadInstallationDirectory()
        {
            using (Process process = Process.GetCurrentProcess())
                return ResolveInstallationDirectory(typeof(PluginPaths).Assembly.Location, process.MainModule.FileName);
        }

        internal static string ResolveInstallationDirectory(string assemblyLocation, string executablePath)
        {
            // RPH executes a cached copy of the assembly. Its Location points into Temp.
            if (string.Equals(Path.GetFileName(executablePath), "GTA5.exe", StringComparison.OrdinalIgnoreCase))
                return Path.Combine(Path.GetDirectoryName(executablePath), "Plugins");
            return Path.GetDirectoryName(assemblyLocation);
        }
    }
}
