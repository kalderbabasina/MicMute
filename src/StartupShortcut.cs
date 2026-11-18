using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MicMute
{
    internal static class StartupShortcut
    {
        internal const string Argument = "--startup";

        internal static string PathForCurrentUser()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            if (String.IsNullOrEmpty(folder)) throw new IOException("Windows Startup folder is unavailable.");
            return Path.Combine(folder, "MicMute.lnk");
        }

        internal static bool? GetState(string shortcut, string executable)
        {
            try { File.GetAttributes(shortcut); }
            catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
            string target = null;
            WithShortcut(shortcut, link => target = (string)Access(link, "TargetPath", BindingFlags.GetProperty));
            return String.Equals(target, executable, StringComparison.OrdinalIgnoreCase) ? (bool?)true : null;
        }

        internal static void SetEnabled(string shortcut, string executable, bool enabled)
        {
            bool? state = GetState(shortcut, executable);
            if (!state.HasValue) throw new InvalidOperationException("The startup shortcut belongs to another installation.");
            if (state.Value == enabled) return;
            if (!enabled) { File.Delete(shortcut); return; }

            Directory.CreateDirectory(Path.GetDirectoryName(shortcut));
            WithShortcut(shortcut, link =>
            {
                Access(link, "TargetPath", BindingFlags.SetProperty, executable);
                Access(link, "Arguments", BindingFlags.SetProperty, Argument);
                Access(link, "WorkingDirectory", BindingFlags.SetProperty, Path.GetDirectoryName(executable));
                Access(link, "Save", BindingFlags.InvokeMethod);
            });
            bool saved = false;
            WithShortcut(shortcut, link => saved =
                String.Equals((string)Access(link, "TargetPath", BindingFlags.GetProperty), executable, StringComparison.OrdinalIgnoreCase) &&
                String.Equals((string)Access(link, "Arguments", BindingFlags.GetProperty), Argument, StringComparison.Ordinal));
            if (!File.Exists(shortcut) || !saved)
                throw new IOException("Windows did not save the startup shortcut.");
        }

        private static void WithShortcut(string path, Action<object> action)
        {
            // WScript.Shell COM stores the executable and arguments separately; no script is run.
            Type shellType = Type.GetTypeFromProgID("WScript.Shell", true);
            object shell = Activator.CreateInstance(shellType);
            object link = null;
            try
            {
                link = Access(shell, "CreateShortcut", BindingFlags.InvokeMethod, path);
                action(link);
            }
            finally
            {
                if (link != null) Marshal.ReleaseComObject(link);
                Marshal.ReleaseComObject(shell);
            }
        }

        private static object Access(object instance, string member, BindingFlags flags, params object[] args)
        { return instance.GetType().InvokeMember(member, flags, null, instance, args); }
    }
}
