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
using System.Linq.Expressions;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;

namespace TaxonomyToolkit.Taxml
{
    internal class TermSetDownloader : TaxonomyItemDownloader<TermSet, LocalTermSet>
    {
        private List<TermSetLocalizedNameQuery> localizedNameQueries = null;

        public TermSetDownloader(TaxonomyItemDownloaderContext downloaderContext, TermSet clientObject, int treeDepth)
            : base(downloaderContext, clientObject, treeDepth)
        {
        }

        public TermSet ClientTermSet
        {
            get { return this.ClientObject; }
        }

        public LocalTermSet LocalTermSet
        {
            get { return this.LocalObject; }
        }

        public static Expression<Func<TermSet, object>>[] GetRetrievalsForMinimalProperties()
        {
            return new Expression<Func<TermSet, object>>[]
            {
                termSet => termSet.Id,
                termSet => termSet.Name
            };
        }

        protected override void QueryMinimalProperties()
        {
            this.ClientContext.Load(this.ClientTermSet, TermSetDownloader.GetRetrievalsForMinimalProperties());
        }

        internal override void AssignMinimalProperties() // abstract
        {
            if (this.LocalTermSet == null)
            {
                var termSet = new LocalTermSet(this.ClientTermSet.Id, this.ClientTermSet.Name);
                termSet.IncompleteObject = true;
                termSet.IncompleteChildItems = true;
                this.SetLocalObject(termSet);
            }
            else
            {
                this.LocalTermSet.SetName(this.ClientTermSet.Name, 1033);
            }
        }

        protected override void QueryExtendedProperties() // abstract
        {
            var clientTermStore = this.DownloaderContext.TermStoreDownloader.ClientTermStore;

            int defaultLcid = clientTermStore.DefaultLanguage;

            this.ClientContext.Load(this.ClientTermSet,
                termSet => termSet.Contact,
                termSet => termSet.CustomSortOrder,
                termSet => termSet.Description,
                termSet => termSet.IsAvailableForTagging,
                termSet => termSet.IsOpenForTermCreation,
                termSet => termSet.Owner,
                termSet => termSet.CustomProperties,
                termSet => termSet.Stakeholders);

            this.localizedNameQueries = TermSetLocalizedNameQuery.Load(this.ClientTermSet,
                this.DownloaderContext.GetLcidsToRead(),
                defaultLcid,
                clientTermStore,
                this.ClientContext);
        }

        // These property assignments follow the same order as TermSetUploader.OnProcessAssignProperties()
        protected override void AssignExtendedProperties() // abstract
        {
            this.LocalTermSet.Contact = this.ClientTermSet.Contact ?? ""; // seen null in EDog, but not Prod
            this.LocalTermSet.CustomSortOrder.AsText = this.ClientTermSet.CustomSortOrder ?? "";
            this.LocalTermSet.Description = this.ClientTermSet.Description ?? ""; // seen null in EDog, but not Prod
            this.LocalTermSet.IsAvailableForTagging = this.ClientTermSet.IsAvailableForTagging;
            this.LocalTermSet.IsOpenForTermCreation = this.ClientTermSet.IsOpenForTermCreation;
            this.LocalTermSet.Owner = this.ClientTermSet.Owner;

            this.LocalTermSet.ClearNames(this.ClientTermSet.Name);

            foreach (var localizedNameQuery in this.localizedNameQueries)
            {
                // TODO: In the SharePoint Taxonomy, a TermSet name can be unset for a certain LCID,
                // in which case it dynamically mirrors the name from the default LCID.  (Whereas once you
                // assign it, it no longer mirrors the default LCID.)  This check attempts to detect that
                // state in a trivial way.  With some thought it could probably be improved (e.g. try 
                // changing the  default name to see if the mirroring occurs?).
                if (localizedNameQuery.Name != this.LocalTermSet.Name)
                {
                    this.LocalTermSet.SetName(localizedNameQuery.ClientTermSet.Name, localizedNameQuery.Lcid);
                }
            }
            this.localizedNameQueries = null;

            this.LocalTermSet.CustomProperties.Clear();
            foreach (var pair in this.ClientTermSet.CustomProperties)
                this.LocalTermSet.CustomProperties.Add(pair.Key, pair.Value);

            this.LocalTermSet.ClearStakeholders();
            foreach (var stakeholder in this.ClientTermSet.Stakeholders)
                this.LocalTermSet.AddStakeholder(stakeholder);
        }

        protected override void QueryChildObjects()
        {
            var childRetrievals = TermDownloader.GetRetrievalsForMinimalProperties();
            this.ClientContext.Load(this.ClientTermSet, termSet => termSet.Terms.Include(childRetrievals));
        }

        protected override void AssignChildObjects()
        {
            foreach (Term childClientTerm in this.ClientObject.Terms)
            {
                var termDownloader = new TermDownloader(this.DownloaderContext, childClientTerm, this.TreeDepth + 1);
                termDownloader.AssignMinimalProperties();

                LocalTerm childLocalTerm = termDownloader.LocalObject;
                this.LocalObject.AddTerm(childLocalTerm);

                if (this.ShouldRecurse)
                {
                    string indent = "";
                    for (int i = 0; i < this.TreeDepth - 1; ++i)
                        indent += "    ";
                    Debug.WriteLine(indent + "--> Fetching children for term: " + childLocalTerm.ToString());

                    termDownloader.FetchItem();
                }
            }
        }
    }
}
