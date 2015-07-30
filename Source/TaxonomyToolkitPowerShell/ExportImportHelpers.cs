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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using TaxonomyToolkit.General;
using TaxonomyToolkit.Taxml;

namespace TaxonomyToolkit.PowerShell
{
    internal static class ExportImportHelpers
    {
        public static void WriteStartupBanner(AppLog appLog)
        {
            appLog.WriteLine();
            appLog.WriteInfo("Taxonomy Toolkit Version {0}", ToolkitUtilities.ToolkitVersion);
            appLog.WriteInfo("Copyright (c) Microsoft Corporation.  All rights reserved.");
            appLog.WriteInfo("https://taxonomytoolkit.codeplex.com/");
            appLog.WriteLine();
            appLog.WriteInfo("Using SharePoint client library version {0}.", ToolkitUtilities.SharePointClientVersion);
            appLog.WriteLine();
        }

        public static void ValidateSiteUrl(string siteUrl)
        {
            Uri uri;
            if (!Uri.TryCreate(siteUrl, UriKind.Absolute, out uri))
                throw new ArgumentException("The SiteUrl is not a valid URL: " + siteUrl);
            switch (uri.Scheme.ToLowerInvariant())
            {
                case "http":
                case "https":
                    break;
                default:
                    throw new ArgumentException("The SiteUrl should be an absolute URL using HTTP or HTTPS: " + siteUrl);
            }
        }

        public static LocalTermStore SelectTermStore(List<LocalTermStore> termStores, Guid? termStoreId)
        {
            LocalTermStore termStore = null;

            if (termStores.Count == 0)
            {
                throw new KeyNotFoundException("The server does not have any term stores"
                    + " -- is the Managed Metadata Service running?");
            }

            if (termStoreId != null)
            {
                termStore = termStores.FirstOrDefault(x => x.Id == termStoreId);
                if (termStore == null)
                {
                    throw new KeyNotFoundException("The server does not have a term store with ID \""
                        + termStoreId.Value + "\"");
                }
            }
            else
            {
                if (termStores.Count > 1)
                {
                    throw new InvalidOperationException("Multiple term stores exist on the server."
                        + "  Use the \"-TermStoreId\" option to specify a term store.");
                }
                termStore = termStores[0];
            }
            return termStore;
        }

        public static string GetAbsolutePath(string possiblyRelativePath, SessionState cmdletSessionState)
        {
            if (Path.IsPathRooted(possiblyRelativePath))
                return possiblyRelativePath;

            string currentDirectory = cmdletSessionState.Path.CurrentFileSystemLocation.Path;
            string absolutePath = Path.Combine(currentDirectory,
                possiblyRelativePath);
            absolutePath = Path.GetFullPath(absolutePath);
            return absolutePath;
        }

        public static Client15Connector CreateClientConnector(string siteUrl,
            PSCredential credential, SwitchParameter cloudCredential, AppLog appLog)
        {
            Client15Connector clientConnector = new Client15Connector(siteUrl);
            if (credential != null)
            {
                if (cloudCredential.IsPresent)
                {
                    clientConnector.SetCredentialsForCloud(credential.UserName, credential.Password,
                        (message) => { appLog.WriteVerbose(message); }
                        );
                }
                else
                {
                    clientConnector.SetCredentialsForOnPrem(credential.UserName, credential.Password);
                }
            }
            else
            {
                if (cloudCredential.IsPresent)
                {
                    throw new InvalidOperationException(
                        "The -CloudCredential switch cannot be used without specifying a credential object");
                }
            }
            return clientConnector;
        }
    }
}
