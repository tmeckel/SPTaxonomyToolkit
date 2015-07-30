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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.SharePoint.Client;

namespace TaxonomyToolkit.Taxml
{
    internal static class CsomHelpers
    {
        /// <summary>
        /// Certain CSOM methods cache their return values for the lifetime of the ClientContext.
        /// (In the server OM, these methods are marked with ClientCallableMethod.CacheReturnValue=true.)
        /// This can produce incorrect results if the value was changed e.g. by the same session
        /// that is now trying to read the new value.  FlushCachedProperties() can be called in
        /// this situation to flush the internal cache.  It should be called after ExecuteQuery()
        /// and before the next query expression is loaded.
        /// </summary>
        public static void FlushCachedProperties(ClientObject clientObject)
        {
            ClientObjectData objectData = CsomHelpers.GetClientObjectData(clientObject);
            objectData.MethodReturnObjects.Clear();
        }

        private static ClientObjectData GetClientObjectData(ClientObject clientObject)
        {
            return (ClientObjectData) CsomHelpers.info_ClientObject_ObjectData.GetValue(clientObject, new object[0]);
        }

        private static PropertyInfo info_ClientObject_ObjectData = typeof (ClientObject)
            .GetProperty("ObjectData", BindingFlags.NonPublic | BindingFlags.Instance);

        #region LoadOnlineServiceLibrary()

        private const string ClientSdkUrl = "http://www.microsoft.com/en-us/download/details.aspx?id=35585";

        private static bool loadOnlineServiceLibrarySucceeded = false;

        /// <summary>
        /// Load the special Microsoft Online Service library (MSOIDCLIL.DLL) that is used
        /// to authenticate requests for SharePoint Online / Office 365 cloud sites.
        /// </summary>
        public static void LoadOnlineServiceLibrary(Action<string> logVerbose = null)
        {
            if (CsomHelpers.loadOnlineServiceLibrarySucceeded)
                return;

            if (logVerbose == null)
            {
                logVerbose = (message) => { Debug.WriteLine("LoadOnlineServiceLibrary: " + message); };
            }

            // If the service library DLL's are present in the tool directory, then load the appropriate
            // version depending on whether the tool is running in 32-bit or 64-bit mode.
            string[] filenames = new[] {"MSOIDCLIL.DLL", "MSOIDRES.DLL"};

            bool first = true;
            bool failed = false;

            foreach (string filename in filenames)
            {
                if (CsomHelpers.LoadLibrary(filename) == IntPtr.Zero)
                {
                    string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
                    if (first)
                    {
                        failed = true;
                        break;
                    }
                    else
                    {
                        throw new InvalidOperationException("Unable to load library \"" + filename
                            + "\" from OS environment: " + errorMessage
                            + "\r\nFor the latest version, visit " + CsomHelpers.ClientSdkUrl);
                    }
                }
                first = false;
            }
            if (!failed)
            {
                if (logVerbose != null)
                    logVerbose("Loaded service authentication libraries from OS environment");
                CsomHelpers.loadOnlineServiceLibrarySucceeded = true;
                return;
            }

            Assembly assembly = typeof (CsomHelpers).Assembly;
            string moduleDllPath = assembly.ManifestModule.FullyQualifiedName;
            if (!System.IO.File.Exists(moduleDllPath))
                throw new InvalidOperationException("Unable to determine module folder for assembly: " +
                    assembly.FullName);
            string moduleDllDirectory = Path.GetDirectoryName(moduleDllPath);

            string dllFolder = Path.Combine(moduleDllDirectory, IntPtr.Size == 4 ? "Libs32" : "Libs64");

            if (!Directory.Exists(dllFolder))
            {
                throw new DirectoryNotFoundException("Unable to load the " + filenames[0]
                    + " library; it was not found in the system path nor the "
                    + Path.GetFileName(moduleDllPath) + " folder."
                    + "\r\nFor the latest version, visit " + CsomHelpers.ClientSdkUrl);
            }

            foreach (string filename in filenames)
            {
                string dllPath = Path.Combine(dllFolder, filename);
                if (CsomHelpers.LoadLibrary(dllPath) == IntPtr.Zero)
                {
                    string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;

                    throw new InvalidOperationException("Unable to load library \"" + dllPath + "\": "
                        + errorMessage);
                }
            }
            if (logVerbose != null)
                logVerbose("Loaded service authentication libraries from \"" + dllFolder + "\"");
            CsomHelpers.loadOnlineServiceLibrarySucceeded = true;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string dllname);

        #endregion
    }
}
