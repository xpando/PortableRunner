using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ConsolePortable
{
    class Program
    {
        static void Main()
        {
            var exeName = Environment.GetCommandLineArgs()[0];
            var iniName = Path.ChangeExtension(exeName, ".ini");

            try
            {
                if (!File.Exists(iniName))
                    throw new Exception(string.Format("Missing ini file {0}", iniName));

                var envKeys = GetIniSectionKeys(iniName, "Env");
                foreach (var key in envKeys)
                {
                    var value = GetIniFileString(iniName, "Env", key, string.Empty);
                    value = value.Replace("~", Environment.CurrentDirectory);
                    value = Environment.ExpandEnvironmentVariables(value);
                    Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
                }

                var fileName = GetIniFileString(iniName, "Run", "FileName", string.Empty);
                fileName = Environment.ExpandEnvironmentVariables(fileName);
                if (string.IsNullOrEmpty(fileName))
                    throw new Exception(string.Format("Missing FileName= setting in the [Run] section of ini file {0}", iniName));

                if (!File.Exists(fileName))
                    MessageBox.Show(string.Format("Could not find file {0}", fileName));

                var runArgs = GetIniFileString(iniName, "Run", "Args", string.Empty);
                runArgs = Environment.ExpandEnvironmentVariables(runArgs);
                if (!string.IsNullOrEmpty(runArgs))
                {
                    var args = Environment.GetCommandLineArgs();
                    runArgs = Environment.ExpandEnvironmentVariables(runArgs);
                    runArgs = Regex.Replace(runArgs, @"\%(?'idx'\d+)", m =>
                    {
                        var idx = (int.Parse(m.Groups["idx"].Value));
                        if (idx < args.Length)
                        {
                            var value = args[idx];
                            if (!string.IsNullOrEmpty(value))
                                return args[idx];
                        }

                        var defaultValueKey = string.Format("Arg{0}", idx);
                        var defaultValue = GetIniFileString(iniName, "Run", defaultValueKey, null);
                        defaultValue = Environment.ExpandEnvironmentVariables(defaultValue);
                        if (defaultValue != null)
                            return defaultValue;

                        throw new Exception(string.Format("Argument %{0} was not passed and default setting Arg{0}= was not found in the [Run] section of ini file {0}", idx, iniName));
                    });
                }

                var startInfo = new ProcessStartInfo(fileName, runArgs);
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        [DllImport("KERNEL32.DLL", EntryPoint = "GetPrivateProfileStringW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        static extern int GetPrivateProfileString
        (
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            string lpReturnString,
            int nSize,
            string lpFilename
        );

        static List<string> GetIniSections(string iniFile)
        {
            var returnString = new string(' ', 65536);
            GetPrivateProfileString(null, null, null, returnString, 65536, iniFile);
            var result = new List<string>(returnString.Split('\0'));
            result.RemoveRange(result.Count - 2, 2);
            return result;
        }

        static List<string> GetIniSectionKeys(string iniFile, string section)
        {
            var returnString = new string(' ', 32768);
            GetPrivateProfileString(section, null, null, returnString, 32768, iniFile);
            var result = new List<string>(returnString.Split('\0'));
            result.RemoveRange(result.Count - 2, 2);
            return result;
        }

        static string GetIniFileString(string iniFile, string section, string key, string defaultValue)
        {
            string returnString = new string(' ', 1024);
            GetPrivateProfileString(section, key, defaultValue, returnString, 1024, iniFile);
            return returnString.Split('\0')[0];
        }
    }
}
