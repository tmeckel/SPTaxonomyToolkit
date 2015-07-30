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
using Microsoft.SharePoint.Client;
using TaxonomyToolkit.General;

namespace TaxonomyToolkit.Taxml
{
    internal abstract class TaxonomyItemDownloader<TClient, TLocal>
        where TClient : ClientObject
        where TLocal : LocalTaxonomyItem
    {
        private TaxonomyItemDownloaderContext downloaderContext;
        private readonly TClient clientObject;
        private TLocal localObject;
        private int treeDepth;

        public TaxonomyItemDownloader(TaxonomyItemDownloaderContext downloaderContext, TClient clientObject,
            int treeDepth)
        {
            ToolkitUtilities.ConfirmNotNull(downloaderContext, "downloaderContext");
            ToolkitUtilities.ConfirmNotNull(clientObject, "clientObject");

            if (clientObject.Context != downloaderContext.ClientConnector.ClientContext)
            {
                throw new ArgumentException("The specified clientObject is not associated with a Client15Connector",
                    "clientObject");
            }

            this.downloaderContext = downloaderContext;
            this.clientObject = clientObject;
            this.treeDepth = treeDepth;
        }

        /// <summary>
        ///     The associated CSOM object.  This is passed to the constructor, and may be
        ///     null in the special unattached mode.
        /// </summary>
        public TClient ClientObject
        {
            get { return this.clientObject; }
        }

        /// <summary>
        ///     The associated local object.  An existing object can be associated via
        ///     SetLocalObject(); otherwise it will be constructed during the query.
        ///     Once the LocalObject property has been assigned, it cannot be reassigned.
        /// </summary>
        public TLocal LocalObject
        {
            get { return this.localObject; }
        }

        public TaxonomyItemDownloaderContext DownloaderContext
        {
            get { return this.downloaderContext; }
        }

        public Client15Connector ClientConnector
        {
            get { return this.DownloaderContext.ClientConnector; }
        }

        public ClientRuntimeContext ClientContext
        {
            get { return this.ClientConnector.ClientContext; }
        }

        public ClientConnectorDownloadOptions Options
        {
            get { return this.downloaderContext.Options; }
        }

        protected bool ShouldRecurse
        {
            get
            {
                if (this.Options.MaximumDepth < 0)
                    return true; // -1 means infinite recursion
                return this.TreeDepth < this.Options.MaximumDepth;
            }
        }

        public int TreeDepth
        {
            get { return this.treeDepth; }
        }

        public void SetLocalObject(TLocal localObject)
        {
            if (this.localObject != null)
                throw new InvalidOperationException("The local object cannot be changed once it is assigned");

            this.localObject = localObject;
        }

        protected abstract void QueryMinimalProperties();
        internal abstract void AssignMinimalProperties();

        protected abstract void QueryExtendedProperties();
        protected abstract void AssignExtendedProperties();

        protected abstract void QueryChildObjects();
        protected abstract void AssignChildObjects();

        protected void VerifyWorkingLanguage()
        {
            var clientTermStore = this.DownloaderContext.TermStoreDownloader.ClientObject;
            if (clientTermStore.IsPropertyAvailable("DefaultLanguage"))
            {
                int defaultLcid = clientTermStore.DefaultLanguage;

                if (clientTermStore.WorkingLanguage != defaultLcid)
                {
                    throw new InvalidOperationException(
                        "The TermStore.WorkingLanguage was not left in the default state");
                }
            }
        }

        public void FetchItem()
        {
            this.VerifyWorkingLanguage();

            this.QueryMinimalProperties();
            this.VerifyWorkingLanguage();

            this.QueryExtendedProperties();
            this.VerifyWorkingLanguage();

            if (this.ShouldRecurse)
            {
                this.QueryChildObjects();
            }

            this.ClientConnector.ExecuteQuery();

            this.ClientConnector.NotifyDownloadedItem(this.LocalObject, this.ShouldRecurse);

            this.AssignMinimalProperties();

            if (this.localObject == null)
                throw new InvalidOperationException("AssignMinimalProperties() failed to construct the object");

            this.AssignExtendedProperties();

            this.localObject.RemoveAllChildItems();
            if (this.ShouldRecurse)
            {
                this.AssignChildObjects();

                // We have the full set of child items
                this.localObject.IncompleteChildItems = false;
            }
        }
    }
}
