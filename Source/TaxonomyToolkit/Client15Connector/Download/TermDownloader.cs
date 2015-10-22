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
using TaxonomyToolkit.Taxml;

namespace TaxonomyToolkit.Sync
{
    internal class TermDownloader : TaxonomyItemDownloader<Term, LocalTerm>
    {
        private class DescriptionQuery
        {
            public int Lcid;
            public ClientResult<string> Description;
        }

        private List<DescriptionQuery> descriptionQueries;

        public TermDownloader(TaxonomyItemDownloaderContext downloaderContext, Term clientObject, int treeDepth)
            : base(downloaderContext, clientObject, treeDepth)
        {
        }

        public Term ClientTerm
        {
            get { return this.ClientObject; }
        }

        public LocalTerm LocalTerm
        {
            get { return this.LocalObject; }
        }

        public static Expression<Func<Term, object>>[] GetRetrievalsForMinimalProperties()
        {
            return new Expression<Func<Term, object>>[]
            {
                term => term.Id,
                term => term.Name,
                term => term.IsSourceTerm,
                term => term.IsPinnedRoot
            };
        }

        protected override void QueryMinimalProperties()
        {
            this.ClientContext.Load(this.ClientTerm, TermDownloader.GetRetrievalsForMinimalProperties());
        }

        internal override void AssignMinimalProperties() // abstract
        {
            if (this.LocalTerm == null)
            {
                LocalTerm term;
                if (this.ClientTerm.IsSourceTerm)
                {
                    term = LocalTerm.CreateTerm(this.ClientTerm.Id, this.ClientTerm.Name, 
                        this.TermStoreDefaltLanguageLcid);
                }
                else
                {
                    term = LocalTerm.CreateTermLinkUsingId(this.ClientTerm.Id, this.TermStoreDefaltLanguageLcid,
                        this.ClientTerm.Name, isPinnedRoot: this.ClientTerm.IsPinnedRoot);
                }
                term.IncompleteObject = true;
                term.IncompleteChildItems = true;

                this.SetLocalObject(term);
            }
            else
            {
                if (this.LocalTerm.IsSourceTerm != this.ClientTerm.IsSourceTerm)
                    throw new NotImplementedException("Changing IsSourceTerm is not yet supported");
            }

            if (this.LocalTerm.TermKind == LocalTermKind.NormalTerm)
            {
                this.LocalTerm.Name = this.ClientTerm.Name;
            }
            else
            {
                this.LocalTerm.TermLinkNameHint = this.ClientTerm.Name;
            }
        }

        protected override void QueryExtendedProperties() // abstract
        {
            var clientTermStore = this.DownloaderContext.TermStoreDownloader.ClientObject;
            int defaultLcid = clientTermStore.DefaultLanguage;

            this.ClientContext.Load(this.ClientTerm,
                term => term.CustomSortOrder,
                term => term.IsAvailableForTagging,
                term => term.LocalCustomProperties
                );

            if (this.LocalTerm.TermKind == LocalTermKind.NormalTerm)
            {
                this.ClientContext.Load(this.ClientTerm,
                    term => term.IsDeprecated,
                    term => term.Owner,
                    term => term.CustomProperties,
                    term => term.Description,
                    term => term.Labels.Include(
                        label => label.IsDefaultForLanguage,
                        label => label.Language,
                        label => label.Value
                        )
                    );

                this.descriptionQueries = new List<DescriptionQuery>();

                foreach (int lcid in this.DownloaderContext.GetLcidsToRead())
                {
                    if (lcid == defaultLcid)
                        continue; // we already read this as clientTermSet.Description

                    var descriptionQuery = new DescriptionQuery();
                    descriptionQuery.Lcid = lcid;
                    descriptionQuery.Description = this.ClientTerm.GetDescription(lcid);
                    this.descriptionQueries.Add(descriptionQuery);
                }
            }
        }

        // These property assignments follow the same order as TermUploader.OnProcessAssignProperties()
        protected override void AssignExtendedProperties() // abstract
        {
            this.LocalTerm.CustomSortOrder.AsTextForServer = this.ClientTerm.CustomSortOrder;

            this.LocalTerm.IsAvailableForTagging = this.ClientTerm.IsAvailableForTagging;

            this.LocalTerm.LocalCustomProperties.Clear();
            foreach (var pair in this.ClientTerm.LocalCustomProperties)
                this.LocalTerm.LocalCustomProperties.Add(pair.Key, pair.Value);

            if (this.LocalTerm.TermKind == LocalTermKind.NormalTerm)
            {
                var clientTermStore = this.DownloaderContext.TermStoreDownloader.ClientObject;
                if (this.LocalTerm.DefaultLanguageLcid != clientTermStore.DefaultLanguage)
                    throw new InvalidOperationException("The DefaultLanguageLcid does not match");

                this.LocalTerm.IsDeprecated = this.ClientTerm.IsDeprecated;
                this.LocalTerm.Owner = this.ClientTerm.Owner;

                this.LocalTerm.CustomProperties.Clear();
                foreach (var pair in this.ClientTerm.CustomProperties)
                    this.LocalTerm.CustomProperties.Add(pair.Key, pair.Value);

                this.LocalTerm.ClearLabels(this.ClientTerm.Name);
                foreach (var label in this.ClientTerm.Labels)
                {
                    this.LocalTerm.AddLabel(label.Value, label.Language, label.IsDefaultForLanguage);
                }

                this.LocalTerm.ClearDescriptions();
                this.LocalTerm.SetDescription(this.ClientTerm.Description, this.LocalTerm.DefaultLanguageLcid);
                foreach (DescriptionQuery descriptionQuery in this.descriptionQueries)
                {
                    string description = descriptionQuery.Description.Value;
                    if (!string.IsNullOrEmpty(description))
                    {
                        this.LocalTerm.SetDescription(description, descriptionQuery.Lcid);
                    }
                }
            }

            this.descriptionQueries = null;
        }

        protected override void QueryChildObjects()
        {
            var childRetrievals = TermDownloader.GetRetrievalsForMinimalProperties();
            this.ClientContext.Load(this.ClientTerm,
                termSet => termSet.Terms.Include(childRetrievals)
                );
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
