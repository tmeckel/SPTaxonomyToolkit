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
using System.Linq.Expressions;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using TaxonomyToolkit.Taxml;

namespace TaxonomyToolkit.Sync
{
    internal class TermGroupDownloader : TaxonomyItemDownloader<TermGroup, LocalTermGroup>
    {
        public TermGroupDownloader(TaxonomyItemDownloaderContext downloaderContext, TermGroup clientObject,
            int treeDepth)
            : base(downloaderContext, clientObject, treeDepth)
        {
        }

        public TermGroup ClientTermGroup
        {
            get { return this.ClientObject; }
        }

        public LocalTermGroup LocalTermGroup
        {
            get { return this.LocalObject; }
        }

        public static Expression<Func<TermGroup, object>>[] GetRetrievalsForMinimalProperties()
        {
            return new Expression<Func<TermGroup, object>>[]
            {
                termGroup => termGroup.Id,
                termGroup => termGroup.Name,
                termGroup => termGroup.IsSiteCollectionGroup,
                termGroup => termGroup.IsSystemGroup
            };
        }

        protected override void QueryMinimalProperties()
        {
            this.ClientContext.Load(this.ClientTermGroup, TermGroupDownloader.GetRetrievalsForMinimalProperties());
        }

        internal override void AssignMinimalProperties() // abstract
        {
            if (this.LocalTermGroup == null)
            {
                var termGroup = new LocalTermGroup(this.ClientTermGroup.Id, this.ClientTermGroup.Name);
                termGroup.IncompleteObject = true;
                termGroup.IncompleteChildItems = true;
                this.SetLocalObject(termGroup);
            }
            else
            {
                this.LocalTermGroup.Name = this.ClientTermGroup.Name;
            }
        }

        protected override void QueryExtendedProperties() // abstract
        {
            this.ClientContext.Load(this.ClientTermGroup,
                termGroup => termGroup.Description);
        }

        protected override void AssignExtendedProperties() // abstract
        {
            this.LocalTermGroup.Description = this.ClientTermGroup.Description;
        }

        protected override void QueryChildObjects() // abstract
        {
            var childRetrievals = TermSetDownloader.GetRetrievalsForMinimalProperties();
            this.ClientContext.Load(this.ClientTermGroup, group => group.TermSets.Include(childRetrievals));
        }

        protected override void AssignChildObjects() // abstract
        {
            foreach (TermSet clientTermSet in this.ClientTermGroup.TermSets)
            {
                var termSetDownloader = new TermSetDownloader(this.DownloaderContext, clientTermSet, this.TreeDepth + 1);
                termSetDownloader.AssignMinimalProperties();

                LocalTermSet localTermSet = termSetDownloader.LocalObject;
                this.LocalObject.AddTermSet(localTermSet);

                Debug.WriteLine("  ==> Fetching TermSet: " + localTermSet.Name);

                if (this.ShouldRecurse)
                {
                    termSetDownloader.FetchItem();
                }
            }
        }
    }
}
