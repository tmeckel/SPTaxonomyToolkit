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
using System.Management.Automation;
using TaxonomyToolkit.Taxml;

namespace TaxonomyToolkit.PowerShell
{
    [Cmdlet(VerbsData.Export, "Taxml")]
    public class ExportTaxmlCommand : PSCmdlet
    {
        private const string PropertyName_TermStoreId = "TermStoreId";

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string SiteUrl { get; set; }

        [Parameter]
        public Guid[] GroupIdFilter { get; set; }

        [Parameter]
        public Guid TermStoreId { get; set; }

        [Parameter, Credential]
        public PSCredential Credential { get; set; }

        [Parameter]
        public SwitchParameter CloudCredential { get; set; }

        protected AppLog AppLog;

        public ExportTaxmlCommand()
        {
            this.AppLog = new AppLog(this);
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            ExportImportHelpers.WriteStartupBanner(this.AppLog);

            // Validate parameters
            string targetFilename = this.GetTargetFilename();

            this.AppLog.WriteInfo("TAXML output will be written to " + targetFilename);
            this.AppLog.WriteLine();

            ExportImportHelpers.ValidateSiteUrl(this.SiteUrl);

            // Fetch objects from SharePoint
            this.AppLog.WriteInfo("Connecting to SharePoint site: " + this.SiteUrl);
            Client15Connector clientConnector = ExportImportHelpers.CreateClientConnector(
                this.SiteUrl, this.Credential, this.CloudCredential, this.AppLog);

            clientConnector.DownloadedItem += this.clientConnector_DownloadedItem;

            List<LocalTermStore> fetchedTermStores = clientConnector.FetchTermStores();

            Guid? optionalTermStoreId = null;
            if (this.MyInvocation.BoundParameters.ContainsKey(ExportTaxmlCommand.PropertyName_TermStoreId))
                optionalTermStoreId = this.TermStoreId;
            LocalTermStore termStore = ExportImportHelpers.SelectTermStore(fetchedTermStores, optionalTermStoreId);

            this.AppLog.WriteInfo("Fetching items from TermStore \"" + termStore.Name + "\"");

            ClientConnectorDownloadOptions options = new ClientConnectorDownloadOptions();

            if (this.GroupIdFilter != null && this.GroupIdFilter.Length > 0)
                options.EnableGroupIdFilter(this.GroupIdFilter);

            LocalTermStore outputTermStore = new LocalTermStore(termStore.Id, termStore.Name);
            clientConnector.Download(outputTermStore, options);
            this.AppLog.WriteInfo("Finished fetching items.");

            // Write output
            this.AppLog.WriteLine();
            this.AppLog.WriteInfo("Writing TAXML output: " + targetFilename);
            TaxmlSaver saver = new TaxmlSaver();
            saver.SaveToFile(targetFilename, outputTermStore);

            this.AppLog.WriteLine();
            this.AppLog.WriteInfo("The operation completed successfully.");
            this.AppLog.WriteLine();
        }

        private void clientConnector_DownloadedItem(object sender, DownloadedItemEventArgs e)
        {
            var item = e.LocalTaxonomyItem;
            switch (item.Kind)
            {
                case LocalTaxonomyItemKind.TermGroup:
                    this.AppLog.WriteInfo("> GROUP:   \"{0}\"", item.Name);
                    break;
                case LocalTaxonomyItemKind.TermSet:
                    this.AppLog.WriteInfo(">     TERMSET: \"{0}\"", item.Name);
                    break;
                case LocalTaxonomyItemKind.Term:
                    if (!this.AppLog.IsVerbose)
                        return;

                    string indent = "    ";
                    var term = (LocalTerm) item;
                    for (var t = (LocalTerm) item; t != null; t = t.ParentItem as LocalTerm)
                    {
                        indent += "    ";
                    }
                    switch (term.TermKind)
                    {
                        case LocalTermKind.TermLinkUsingId:
                            if (!string.IsNullOrEmpty(term.TermLinkNameHint))
                            {
                                this.AppLog.WriteInfo("> {0}LINK: \"{1}\" (hint)", indent, term.TermLinkNameHint);
                            }
                            else
                            {
                                this.AppLog.WriteInfo("> {0}LINK: {1}", indent, term.Id);
                            }
                            break;
                        case LocalTermKind.TermLinkUsingPath:
                            this.AppLog.WriteInfo("> {0}LINK: \"{1}\"", indent, term.TermLinkSourcePath);
                            break;
                        case LocalTermKind.NormalTerm:
                            this.AppLog.WriteInfo("> {0}TERM: \"{1}\"", indent, item.Name);
                            break;
                    }
                    break;
            }
        }

        private string GetTargetFilename()
        {
            string targetFilename = ExportImportHelpers.GetAbsolutePath(this.Path, this.SessionState);

            if (Directory.Exists(targetFilename))
            {
                throw new InvalidOperationException("The target path is a directory: " + targetFilename);
            }

            var targetDirectory = System.IO.Path.GetDirectoryName(targetFilename);

            if (string.IsNullOrEmpty(System.IO.Path.GetExtension(targetFilename)) && !targetFilename.EndsWith("."))
                targetFilename += ".taxml";

            if (!Directory.Exists(targetDirectory))
            {
                throw new DirectoryNotFoundException("The target directory does not exist: " + targetDirectory);
            }

            return targetFilename;
        }
    }
}
