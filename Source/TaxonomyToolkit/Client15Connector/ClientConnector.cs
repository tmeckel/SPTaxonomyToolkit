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
using System.Diagnostics;
using System.Net;
using System.Security;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using TaxonomyToolkit.General;
using TaxonomyToolkit.Taxml;

namespace TaxonomyToolkit.Sync
{
    public class Client15Connector
    {
        public event EventHandler<ExecutingQueryEventArgs> ExecutingQuery;
        public event EventHandler<DownloadedItemEventArgs> DownloadedItem;
        public event EventHandler<UploadingItemEventArgs> UploadingItem;

        private readonly ClientContext clientContext;
        private TaxonomySession taxonomySession;

        private WorkingLanguageManager workingLanguageManager = new WorkingLanguageManager();

        private int executeQueryCount = 0;

        public Client15Connector(string webFullUrl)
            : this(new ClientContext(webFullUrl))
        {
        }

        public Client15Connector(ClientContext clientContext)
        {
            ToolkitUtilities.ConfirmNotNull(clientContext, "clientContext");
            this.clientContext = clientContext;

            this.GetNewGuid = () => { return Guid.NewGuid(); };

            if (this.clientContext.Tag != null)
                throw new ArgumentException("The ClientContext is already tagged by another object");
            this.clientContext.Tag = this;
        }

        public ClientContext ClientContext
        {
            get { return this.clientContext; }
        }

        internal WorkingLanguageManager WorkingLanguageManager
        {
            get { return this.workingLanguageManager; }
        }

        /// <summary>
        /// Used by unit tests to assign new GUIDs deterministically
        /// </summary>
        public Func<Guid> GetNewGuid { get; set; }

        /// <summary>
        /// Authenticate using "Domain\Alias" credentials for an on-prem SharePoint server.
        /// </summary>
        public void SetCredentialsForOnPrem(string userNameWithDomain, SecureString password)
        {
            var credential = new NetworkCredential(userNameWithDomain, password);
            this.clientContext.Credentials = credential;
        }

        /// <summary>
        /// Authenticate using the special service library for SharePoint Online / Office 365.
        /// </summary>
        public void SetCredentialsForCloud(string userEmail, SecureString password, Action<string> logVerbose = null)
        {
            CsomHelpers.LoadOnlineServiceLibrary(logVerbose);
            var credential = new SharePointOnlineCredentials(userEmail, password);
            this.clientContext.Credentials = credential;
        }

        // This is not a property because it may set up a CSOM query.
        public TaxonomySession GetTaxonomySession()
        {
            if (this.taxonomySession == null)
            {
                this.taxonomySession = TaxonomySession.GetTaxonomySession(this.clientContext);
            }
            CsomHelpers.FlushCachedProperties(this.taxonomySession);
            CsomHelpers.FlushCachedProperties(this.taxonomySession.TermStores);
            return this.taxonomySession;
        }

        public List<LocalTermStore> FetchTermStores()
        {
            TaxonomySession taxonomySession = this.GetTaxonomySession();

            ClientConnectorDownloadOptions options = new ClientConnectorDownloadOptions();
            options.MaximumDepth = 0;
            options.MinimalAtMaximumDepth = true;

            var retrievals = TermStoreDownloader.GetRetrievalsForMinimalProperties();
            this.clientContext.Load(taxonomySession, session => session.TermStores.Include(retrievals));

            this.ExecuteQuery();

            List<LocalTermStore> list = new List<LocalTermStore>();
            foreach (TermStore clientTermStore in taxonomySession.TermStores)
            {
                var downloaderContext = new TaxonomyItemDownloaderContext(this, options);
                TermStoreDownloader termStoreDownloader = new TermStoreDownloader(downloaderContext, clientTermStore);
                termStoreDownloader.AssignMinimalProperties();
                list.Add(termStoreDownloader.LocalObject);
            }

            return list;
        }

        public void Download(LocalTermStore termStore, ClientConnectorDownloadOptions options = null)
        {
            if (options == null)
                options = new ClientConnectorDownloadOptions();

            TermStore clientTermStore = this.FetchClientTermStore(termStore.Id);

            // Load full data for the TermStore object
            var downloaderContext = new TaxonomyItemDownloaderContext(this, options);
            TermStoreDownloader termStoreDownloader = new TermStoreDownloader(downloaderContext, clientTermStore);
            termStoreDownloader.SetLocalObject(termStore);
            termStoreDownloader.FetchItem();
        }

        public void Upload(LocalTermStore termStore, Guid overrideTermStoreId,
            ClientConnectorUploadOptions options = null)
        {
            if (options == null)
                options = new ClientConnectorUploadOptions();

            TermStore clientTermStore = this.FetchClientTermStore(overrideTermStoreId);
            var uploadController = new UploadController(this, termStore, clientTermStore, options);
            uploadController.PerformUpload();
        }

        public TermStore FetchClientTermStore(Guid termStoreId)
        {
            TaxonomySession taxonomySession = this.GetTaxonomySession();

            TermStore clientTermStore = null;
            ExceptionHandlingScope scope = new ExceptionHandlingScope(this.ClientContext);
            using (scope.StartScope())
            {
                using (scope.StartTry())
                {
                    clientTermStore = taxonomySession.TermStores.GetById(termStoreId);
                }
                using (scope.StartCatch())
                {
                }
            }

            this.ExecuteQuery();

            if (scope.HasException || clientTermStore.ServerObjectIsNull.Value)
                throw new InvalidOperationException(string.Format("The term store was not found with ID={0}",
                    termStoreId));
            return clientTermStore;
        }

        public void ExecuteQuery()
        {
            if (this.ExecutingQuery != null)
            {
                var args = new ExecutingQueryEventArgs(this.clientContext);
                this.ExecutingQuery(this, args);
            }

            this.workingLanguageManager.NotifyBeforeExecuteQuery();

            ++this.executeQueryCount;
            Debug.WriteLine("ExecuteQuery #" + this.executeQueryCount);
            this.clientContext.ExecuteQuery();
        }

        internal void NotifyDownloadedItem(LocalTaxonomyItem localTaxonomyItem, bool willFetchChildren)
        {
            if (this.DownloadedItem != null)
                this.DownloadedItem(this, new DownloadedItemEventArgs(localTaxonomyItem, willFetchChildren));
        }

        internal void NotifyUploadingItem(LocalTaxonomyItem localTaxonomyItem, int itemsUploaded, int totalItems)
        {
            if (this.UploadingItem != null)
                this.UploadingItem(this, new UploadingItemEventArgs(localTaxonomyItem, itemsUploaded, totalItems));
        }
    }
}
