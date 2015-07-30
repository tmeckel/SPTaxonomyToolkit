#region MIT License

// Taxonomy Toolkit
// Copyright (c) Microsoft Corporation
// All rights reserved. 
// http://taxonomytoolkit.codeplex.com/
// 
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
// associated documentation files (the "Software"), to deal in the Software without restriction, 
// including without limitation the rights to use, copy, modify, merge, publish, distribute, 
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or 
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT 
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TaxonomyToolkit.TaxmlManager
{
    public static class Utilities
    {
        public static string GetExceptionSummary(Exception ex, bool omitCallstack)
        {
            string indent = "  --> ";
            string message = indent + ex.Message;

            // Inner exceptions generally give more information, so prepend them.
            Exception inner = ex.InnerException;
            while (inner != null)
            {
                if (inner.Message != "")
                    message = indent + inner.Message + "\r\n\r\n" + message;
                inner = inner.InnerException;
            }

#if DEBUG
            if (!omitCallstack)
                message += "\r\n\r\n" + ex.StackTrace;
#endif
            return message;
        }

        public static string GetExceptionSummary(Exception ex)
        {
            return Utilities.GetExceptionSummary(ex, false);
        }

        #region Utilities Helpers

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName,
            string lpString, string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName,
            string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);

        private static string iniPath = null;

        private static string IniFilename
        {
            get
            {
                if (Utilities.iniPath == null)
                {
                    string filename = Path.GetFileNameWithoutExtension(System.Windows.Forms.Application.ExecutablePath) 
                        + "-Settings.ini";
                    string path = Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath),
                        filename);
                    Utilities.iniPath = Path.GetFullPath(path);
                }
                return Utilities.iniPath;
            }
        }

        #endregion

        public static void WriteSetting(string iniFilename, string iniSection, string keyName, string value)
        {
            Utilities.WritePrivateProfileString("Application Settings", keyName, value, Utilities.IniFilename);
        }

        public static string ReadSetting(string iniFilename, string iniSection, string keyName, string defaultValue)
        {
            try
            {
                // GetPrivateProfileString()'s default value does not understand "null" or whitespace.
                // Instead, we use this "impossible" value as the default, and then replace it with
                // the C# defaultValue parameter.
                const string NotFoundString = "{0893B224-ED4E-4812-AA7D-26508E088B3D}";
                StringBuilder builder = new StringBuilder(65536);
                Utilities.GetPrivateProfileString("Application Settings", keyName, NotFoundString, builder, 65536,
                    Utilities.IniFilename);

                string result = builder.ToString();
                if (result == NotFoundString)
                    return defaultValue;
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Discarding exception:" + ex.Message);
                return defaultValue;
            }
        }

        public static void WriteAppSetting(string keyName, string value)
        {
            Utilities.WriteSetting(Utilities.IniFilename, "Application Settings", keyName, value);
        }

        public static string ReadAppSetting(string keyName, string defaultValue)
        {
            return Utilities.ReadSetting(Utilities.IniFilename, "Application Settings", keyName, defaultValue);
        }

        #region CreateVolatileRegistryKey()

        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        private static extern int RegCreateKeyEx(IntPtr hKey, string lpSubKey, int Reserved, string lpClass,
            int dwOptions, int samDesired, IntPtr lpSecurityAttributes,
            out IntPtr hkResult, out int lpdwDisposition);

        [DllImport("advapi32.dll")]
        private static extern int RegCloseKey(IntPtr hKey);

        public enum HKEY
        {
            CLASSES_ROOT = -2147483648,
            CURRENT_USER = -2147483647,
            LOCAL_MACHINE = -2147483646
        }

        public static bool CreateVolatileRegistryKey(HKEY baseKey, string subkey)
        {
            const int REG_OPTION_VOLATILE = 1;
            const int KEY_CREATE_SUB_KEY = 4;

            IntPtr handle;
            int disposition = 0;

            int errorCode = Utilities.RegCreateKeyEx(new IntPtr((int) baseKey), subkey, 0, null, REG_OPTION_VOLATILE,
                KEY_CREATE_SUB_KEY, IntPtr.Zero,
                out handle, out disposition);
            if (errorCode == 0)
                Utilities.RegCloseKey(handle);
            else
                throw new Exception("Error creating registry key");

            const int REG_CREATED_NEW_KEY = 1;
            return disposition == REG_CREATED_NEW_KEY;
        }

        #endregion

        // This is a simpler alternative to SPUrlUtility.CombineUrl()
        public static string CombineUrl(string baseUrl, string relativeUrl)
        {
            // Trim any leading slashes from relativeUrl
            relativeUrl = relativeUrl.TrimStart('/');
            return new Uri(new Uri(Utilities.AppendSlashIfMissing(baseUrl)), relativeUrl).ToString();
        }

        // Appends a slash if the specified URL does not already end with a slash
        public static string AppendSlashIfMissing(string baseUrl)
        {
            if (baseUrl.EndsWith("/"))
                return baseUrl;
            return baseUrl + "/";
        }
    }
}
