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
using System.Linq;
using System.Linq.Expressions;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using TaxonomyToolkit.Taxml;

namespace TaxonomyToolkit.Sync
{
    internal class TermStoreDownloader : TaxonomyItemDownloader<TermStore, LocalTermStore>
    {
        public TermStoreDownloader(TaxonomyItemDownloaderContext downloaderContext, TermStore clientObject)
            : base(downloaderContext, clientObject, treeDepth: 0)
        {
            this.DownloaderContext.TermStoreDownloader = this;
        }

        public TermStore ClientTermStore
        {
            get { return this.ClientObject; }
        }

        public LocalTermStore LocalTermStore
        {
            get { return this.LocalObject; }
        }

        public static Expression<Func<TermStore, object>>[] GetRetrievalsForMinimalProperties()
        {
            return new Expression<Func<TermStore, object>>[]
            {
                termStore => termStore.Name,
                termStore => termStore.Id,
                termStore => termStore.IsOnline
            };
        }

        protected override void QueryMinimalProperties()
        {
            this.ClientContext.Load(this.ClientTermStore, TermStoreDownloader.GetRetrievalsForMinimalProperties());
        }

        internal override void AssignMinimalProperties() // abstract
        {
            if (this.LocalTermStore == null)
            {
                var termStore = new LocalTermStore(this.ClientTermStore.Id, this.ClientTermStore.Name);
                termStore.IncompleteObject = true;
                termStore.IncompleteChildItems = true;

                this.SetLocalObject(termStore);
            }
            else
            {
                this.LocalTermStore.Name = this.ClientTermStore.Name;
            }
        }

        protected override void QueryExtendedProperties() // abstract
        {
            this.ClientContext.Load(this.ClientTermStore,
                termStore => termStore.Languages,
                termStore => termStore.DefaultLanguage,
                termStore => termStore.WorkingLanguage,

                termStore => termStore.SystemGroup.Id,
                termStore => termStore.SystemGroup.Name,

                termStore => termStore.OrphanedTermsTermSet.Id,
                termStore => termStore.OrphanedTermsTermSet.Name,

                termStore => termStore.KeywordsTermSet.Id,
                termStore => termStore.KeywordsTermSet.Name,

                termStore => termStore.OrphanedTermsTermSet.Id,
                termStore => termStore.OrphanedTermsTermSet.Name);
        }

        protected override void AssignExtendedProperties() // abstract
        {
            if (!this.ClientTermStore.IsOnline)
                throw new Exception("TermStore is offline");

            this.LocalTermStore.SetAvailableLanguageLcids(this.ClientTermStore.Languages);

            // By default we read every language in the term store,
            // but this could be configurable.
            this.DownloaderContext.SetLcidsToRead(this.ClientTermStore.Languages);

            this.LocalTermStore.DefaultLanguageLcid = this.ClientTermStore.DefaultLanguage;
        }

        protected override void QueryChildObjects() // abstract
        {
            var childRetrievals = TermGroupDownloader.GetRetrievalsForMinimalProperties();

            if (this.Options.GroupIdFilter != null)
            {
                // Load only groups appearing on the whitelist

                // "termGroup => termGroup.Id == guid1 || termGroup.Id == guid2 || termGroup.Id == guid3"
                Expression predicateBody = null;
                var termGroupParameter = Expression.Parameter(typeof (TermGroup), "termGroup");
                foreach (var guid in this.Options.GroupIdFilter)
                {
                    Expression comparison = Expression.Equal(
                        Expression.Property(termGroupParameter, "Id"),
                        Expression.Constant(guid)
                        );

                    if (predicateBody == null)
                    {
                        predicateBody = comparison;
                    }
                    else
                    {
                        predicateBody = Expression.OrElse(predicateBody, comparison);
                    }
                }
                var wherePredicate = Expression.Lambda<Func<TermGroup, bool>>(
                    predicateBody,
                    new ParameterExpression[] {termGroupParameter}
                    );

                // "termStore => termStore.Groups.Where(wherePredicate).Include(childRetrievals)"
                var termStoreParameter = Expression.Parameter(typeof (TermStore), "termStore");
                var retrieval = Expression.Lambda<Func<TermStore, object>>(
                    Expression.Call(typeof (ClientObjectQueryableExtension), "Include",
                        new Type[] {typeof (TermGroup)},
                        new Expression[]
                        {
                            Expression.Call(typeof (Queryable), "Where",
                                new Type[] {typeof (TermGroup)},
                                new Expression[]
                                {
                                    Expression.Property(termStoreParameter, "Groups"),
                                    wherePredicate
                                }
                                ),
                            Expression.Constant(childRetrievals)
                        }
                        ),
                    new ParameterExpression[] {termStoreParameter}
                    );

                this.ClientContext.Load(this.ClientTermStore, retrieval);
            }
            else
            {
                // Load all groups
                this.ClientContext.Load(this.ClientTermStore, termStore => termStore.Groups.Include(childRetrievals));
            }
        }

        protected override void AssignChildObjects() // abstract
        {
            foreach (TermGroup clientTermGroup in this.ClientObject.Groups.ToList())
            {
                if (clientTermGroup.IsSiteCollectionGroup || clientTermGroup.IsSystemGroup)
                    continue;
                if (!this.Options.MatchesGroupIdFilter(clientTermGroup.Id))
                    continue;

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                // Copy the clientTermGroup into a new LocalTermGroup
                var termGroupDownloader = new TermGroupDownloader(this.DownloaderContext, clientTermGroup,
                    this.TreeDepth + 1);
                termGroupDownloader.AssignMinimalProperties();
                LocalTermGroup termGroup = termGroupDownloader.LocalObject;

                // Add the LocalTermGroup to the LocalTermStore
                this.LocalObject.AddTermGroup(termGroup);

                if (this.ShouldRecurse)
                {
                    Debug.WriteLine("==> Fetching children for Group: " + termGroup.Name);

                    // Fetch the rest of the tree
                    termGroupDownloader.FetchItem();
                }

                Debug.WriteLine("TOTAL TIME FOR Group" + termGroup.Name + ": " + stopwatch.ElapsedMilliseconds + " ms");
            }
        }
    }
}
