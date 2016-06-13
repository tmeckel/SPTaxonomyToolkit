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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using TaxonomyToolkit.General;

namespace TaxonomyToolkit.Taxml
{
    /// <summary>
    /// Represents a taxonomy term store in the LocalTermStore object model.
    /// </summary>
    public sealed class LocalTermStore : LocalTaxonomyItem<LocalTermGroup>
    {
        internal const int EnglishLanguageLcid = 1033;

        private string serviceName;

        private Dictionary<Guid, List<LocalTerm>> termsByGuid = null;

        private LocalTaxonomyItemCollection<LocalTermGroup> readOnlyTermGroups;

        private List<int> availableLanguageLcids = new List<int>();

        public LocalTermStore(Guid id, string serviceName, int defaultLanguageLcid = LocalTermStore.EnglishLanguageLcid)
            : base(id, defaultLanguageLcid)
        {
            ToolkitUtilities.ConfirmNotNull(serviceName, "serviceName");

            this.serviceName = serviceName;
            this.SetAvailableLanguageLcids(new[] { defaultLanguageLcid });
        }

        #region Properties

        /// <summary>
        /// The child items for this object.
        /// </summary>
        public LocalTaxonomyItemCollection<LocalTermGroup> TermGroups
        {
            get { return this.readOnlyTermGroups; }
        }

        public new string Name
        {
            get { return this.serviceName; }
            set
            {
                ToolkitUtilities.ConfirmNotNull(value, "value");
                this.serviceName = value;
            }
        }

        /// <summary>
        /// When downloaded from SharePoint, this collection reports the term store languages
        /// that were configured for use on the server.  When uploading to SharePoint,
        /// this collection specifies the langauges that will be synced; if the LocalTerm objects
        /// contain strings for other languages that are not included in AvailableLanguageLcids,
        /// then those strings will not be synced.
        /// </summary>
        /// <remarks>
        /// The sync engine never modifies the SharePoint server's term store language
        /// configuration, because that is an administrative operation with nontrivial
        /// consequences.
        /// </remarks>
        public ReadOnlyCollection<int> AvailableLanguageLcids
        {
            get { return new ReadOnlyCollection<int>(this.availableLanguageLcids); }
        }

        #endregion

        #region LocalTaxonomyItem Boilerplate

        public override LocalTaxonomyItemKind Kind
        {
            get { return LocalTaxonomyItemKind.TermStore; }
        }

        protected override LocalTaxonomyItem GetParentItem()
        {
            return null;
        }

        protected override void SetParentItem(LocalTaxonomyItem value)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region Add/Remove Helpers

        public LocalTermGroup AddTermGroup(LocalTermGroup child)
        {
            return this.AddChildItem<LocalTermGroup>(child);
        }

        public LocalTermGroup AddTermGroup(Guid id, string name)
        {
            return this.AddTermGroup(new LocalTermGroup(id, name, this.DefaultLanguageLcid));
        }

        public void RemoveTermGroup(LocalTermGroup child)
        {
            this.RemoveChildItem<LocalTermGroup>(child);
        }

        #endregion

        protected override string GetName()
        {
            return this.Name;
        }

        protected override void ConstructReadOnlyCollection(List<LocalTermGroup> writableChildItems) // abstract
        {
            this.readOnlyTermGroups = new LocalTaxonomyItemCollection<LocalTermGroup>(writableChildItems);
        }

        public void SetAvailableLanguageLcids(IEnumerable<int> lcids)
        {
            var cleandUpList = lcids.OrderBy(x => x).Distinct().ToList();
            foreach (int lcid in cleandUpList)
            {
                // Validate the LCID
                CultureInfo.GetCultureInfo(lcid);
            }

            this.availableLanguageLcids.Clear();
            this.availableLanguageLcids.AddRange(cleandUpList);
        }

        /// <summary>
        /// Returns a list of all LocalTerm objects that are attached to this LocalTermStore
        /// object and have the specified LocalTerm.Id.  If there is a source term, it will
        /// be the first item in the list.
        /// </summary>
        public ReadOnlyCollection<LocalTerm> GetTermsWithId(Guid termId)
        {
            this.EnsureTable();
            List<LocalTerm> termList;
            if (!this.termsByGuid.TryGetValue(termId, out termList))
                return new ReadOnlyCollection<LocalTerm>(new LocalTerm[0]);
            return new ReadOnlyCollection<LocalTerm>(termList);
        }

        private void AddSubtreeToTable(LocalTaxonomyItem rootItem)
        {
            foreach (LocalTaxonomyItem localItem in ToolkitUtilities.GetPreorder(rootItem,
                (LocalTaxonomyItem x) => x.ChildItems))
            {
                LocalTerm localTerm = localItem as LocalTerm;
                if (localTerm != null && localItem.Id != Guid.Empty)
                {
                    // Add localTerm to the table
                    List<LocalTerm> termList = null;
                    if (!this.termsByGuid.TryGetValue(localTerm.Id, out termList))
                    {
                        // Create the missing list
                        termList = new List<LocalTerm>();
                        this.termsByGuid.Add(localTerm.Id, termList);
                    }

                    // Add the localTerm to the list
                    if (localTerm.IsSourceTerm)
                    {
                        for (int i = 0; i < termList.Count; ++i)
                        {
                            if (termList[i].IsSourceTerm)
                            {
                                throw new InvalidOperationException(string.Format(
                                    "The term \"{0}\" cannot be the source term in two different term sets (TermId={1})",
                                    localTerm.Name, localTerm.Id));
                            }
                        }
                        // Source term is first in the list
                        termList.Insert(0, localTerm);
                    }
                    else
                    {
                        termList.Add(localTerm);
                    }
                }
            }
        }

        private void EnsureTable()
        {
            if (this.termsByGuid != null)
                return;

            this.termsByGuid = new Dictionary<Guid, List<LocalTerm>>();
            this.AddSubtreeToTable(this);

            // If this becomes a performance issue, we will need to replace InvalidateTable()
            // with something more efficient
            Debug.WriteLine("Rebuilding table for TermStore ({0} items)", this.termsByGuid.Count);
        }

        private void InvalidateTable()
        {
            // Discard the table, causing it to be regenerated
            this.termsByGuid = null;
        }

        internal void OnBeforeAddSubtree(LocalTaxonomyItem localTaxonomyItem)
        {
            this.EnsureTable();

            // Try adding the subtree to the table; if an exception is thrown, then
            // the item will not be added
            bool succeeded = false;
            try
            {
                this.AddSubtreeToTable(localTaxonomyItem);
                succeeded = true;
            }
            finally
            {
                // If the operation failed, then revert the changes that we made
                if (!succeeded)
                    this.InvalidateTable();
            }
        }

        internal void OnBeforeRemoveSubtree(LocalTaxonomyItem localTaxonomyItem)
        {
            this.InvalidateTable();
        }
    }
}
