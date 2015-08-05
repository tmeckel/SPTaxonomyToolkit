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
using System.ComponentModel;
using System.IO;
using System.Management.Automation;
using TaxonomyToolkit.Taxml;

namespace TaxonomyToolkit.PowerShell
{
    [Cmdlet(VerbsData.Import, "Taxml")]
    public class ImportTaxmlCommand : PSCmdlet
    {
        private const string ProgressRecordTitle = "Uploading taxonomy objects to server...";
        private const string PropertyName_TermStoreId = "TermStoreId";
        private const string PropertyName_MaximumBatchSize = "MaximumBatchSize";

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string SiteUrl { get; set; }

        [Parameter]
        public Guid TermStoreId { get; set; }

        [Parameter]
        [ValidateRange(1, int.MaxValue)]
        [DefaultValue(ClientConnectorUploadOptions.MaximumBatchSizeDefault)]
        public int MaximumBatchSize { get; set; }

        [Parameter, Credential]
        public PSCredential Credential { get; set; }

        [Parameter]
        public SwitchParameter CloudCredential { get; set; }

        protected AppLog AppLog;

        public ImportTaxmlCommand()
        {
            this.AppLog = new AppLog(this);
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            ExportImportHelpers.WriteStartupBanner(this.AppLog);

            string inputFilename = ExportImportHelpers.GetAbsolutePath(this.Path, this.SessionState);

            // Validate parameters
            if (!File.Exists(inputFilename))
            {
                // If the "taxml" extension was omitted, try adding it
                if (string.IsNullOrEmpty(System.IO.Path.GetExtension(inputFilename)) && !inputFilename.EndsWith("."))
                {
                    inputFilename += ".taxml";
                }

                if (!File.Exists(inputFilename))
                {
                    throw new FileNotFoundException("The TAXML input file does not exist: " + inputFilename);
                }
            }

            ExportImportHelpers.ValidateSiteUrl(this.SiteUrl);

            this.AppLog.WriteInfo("Reading TAXML input from " + inputFilename);

            TaxmlLoader loader = new TaxmlLoader();
            LocalTermStore loadedTermStore = loader.LoadFromFile(inputFilename);

            this.AppLog.WriteInfo("Connecting to SharePoint site: " + this.SiteUrl);
            Client15Connector clientConnector = ExportImportHelpers.CreateClientConnector(
                this.SiteUrl, this.Credential, this.CloudCredential, this.AppLog);

            clientConnector.UploadingItem += this.clientConnector_UploadingItem;

            List<LocalTermStore> fetchedTermStores = clientConnector.FetchTermStores();

            Guid? optionalTermStoreId = null;
            if (this.MyInvocation.BoundParameters.ContainsKey(ImportTaxmlCommand.PropertyName_TermStoreId))
                optionalTermStoreId = this.TermStoreId;
            LocalTermStore fetchedTermStore = ExportImportHelpers.SelectTermStore(fetchedTermStores, optionalTermStoreId);

            this.AppLog.WriteLine();
            this.AppLog.WriteInfo("Starting operation...");
            this.AppLog.WriteLine();

            var options = new ClientConnectorUploadOptions();
            if (this.MyInvocation.BoundParameters.ContainsKey(ImportTaxmlCommand.PropertyName_MaximumBatchSize))
                options.MaximumBatchSize = this.MaximumBatchSize;

            clientConnector.Upload(loadedTermStore, fetchedTermStore.Id, options);

            var progressRecord = new ProgressRecord(0, ImportTaxmlCommand.ProgressRecordTitle, "Finished.");
            progressRecord.RecordType = ProgressRecordType.Completed;
            this.WriteProgress(progressRecord);

            this.AppLog.WriteLine();
            this.AppLog.WriteInfo("The operation completed successfully.");
            this.AppLog.WriteLine();
        }

        private void clientConnector_UploadingItem(object sender, UploadingItemEventArgs e)
        {
            string message = "";

            bool wroteTerm = false;
            for (LocalTaxonomyItem item = e.LocalTaxonomyItem; item != null; item = item.ParentItem)
            {
                string header = null;
                switch (item.Kind)
                {
                    case LocalTaxonomyItemKind.Term:
                        if (!wroteTerm)
                        {
                            switch (((LocalTerm) item).TermKind)
                            {
                                case LocalTermKind.NormalTerm:
                                    header = "TERM";
                                    break;
                                default:
                                    header = "TERMLINK";
                                    break;
                            }
                        }
                        wroteTerm = true;
                        break;
                    case LocalTaxonomyItemKind.TermSet:
                        header = "TERMSET";
                        break;
                    case LocalTaxonomyItemKind.TermGroup:
                        header = "GROUP";
                        break;
                }

                if (header != null)
                {
                    if (message.Length > 0)
                        message = "  " + message;
                    message = header + ": \"" + item.Name + "\"" + message;
                }
            }

            double percent = 0;
            if (e.TotalItems != 0)
                percent = e.ItemsUploaded*100.0/e.TotalItems;

            var progressRecord = new ProgressRecord(0, ImportTaxmlCommand.ProgressRecordTitle,
                string.Format("{0} of {1} items completed", e.ItemsUploaded, e.TotalItems));
            progressRecord.PercentComplete = (int) (e.ItemsUploaded*100.0/e.TotalItems);
            this.WriteProgress(progressRecord);

            this.AppLog.WriteInfo(message);
        }
    }
}
