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
using System.Linq;
using System.Collections.ObjectModel;

namespace TaxonomyToolkit.Taxml
{
    /// <summary>
    /// Abstraction of <see cref="LocalTermSet" /> and <see cref="LocalTerm" />, which both
    /// contain terms as their children.
    /// This class is analagous to the <b>TermSetItem</b> base class from
    /// Microsoft.SharePoint.Taxonomy.dll.
    /// </summary>
    public abstract class LocalTermContainer : LocalTaxonomyItem<LocalTerm>
    {
        private LocalTermContainerCollection<LocalTerm> terms;
        private bool isAvailableForTagging = true;
        private CustomSortOrder customSortOrder = new CustomSortOrder();

        internal LocalTermContainer(Guid id, int defaultLanguageLcid)
            : base(id, defaultLanguageLcid)
        {

        }

        /// <summary>
        /// Gets or sets the name of this item for the term store's default language.
        /// </summary>
        /// <remarks>
        /// LocalTermStore has no analogue of the TermStore.WorkingLanguage property from the
        /// Taxonomy API.  If you care about localization, consider using GetNameWithDefault()
        /// and SetName() instead of the Name property.
        /// </remarks>
        public new string Name
        {
            get { return this.GetNameWithDefault(this.DefaultLanguageLcid); }
            set { this.SetName(value, this.DefaultLanguageLcid); }
        }

        /// <summary>
        /// The child items for this object.
        /// </summary>
        public LocalTermContainerCollection<LocalTerm> Terms
        {
            get { return this.terms; }
        }

        /// <summary>
        /// A table of user-defined key/value pairs.
        /// </summary>
        public abstract IDictionary<string, string> CustomProperties { get; }

        /// <summary>
        /// Indicates whether the Term is visible to SharePoint controls used for tagging.
        /// </summary>
        public bool IsAvailableForTagging
        {
            get { return this.isAvailableForTagging; }
            set { this.isAvailableForTagging = value; }
        }

        public CustomSortOrder CustomSortOrder
        {
            get { return this.customSortOrder; }
        }

        /// <summary>
        /// Returns the name for the specified language.
        /// </summary>
        public abstract string GetNameWithDefault(int lcid);

        /// <summary>
        /// Assigns the name for the specified language.
        /// </summary>
        public abstract void SetName(string name, int lcid);

        #region Add/Remove Helpers

        public LocalTerm AddTerm(LocalTerm child)
        {
            return this.AddChildItem<LocalTerm>(child);
        }

        public LocalTerm AddTerm(Guid id, string name)
        {
            return this.AddTerm(LocalTerm.CreateTerm(id, name, this.DefaultLanguageLcid));
        }

        public LocalTerm AddTermLinkUsingId(Guid id, string nameHint = "", bool isPinnedRoot = false)
        {
            return this.AddTerm(LocalTerm.CreateTermLinkUsingId(id, this.DefaultLanguageLcid, nameHint, isPinnedRoot));
        }

        public LocalTerm AddTermLinkUsingId(LocalTerm sourceTerm, bool isPinnedRoot = false)
        {
            return this.AddTerm(LocalTerm.CreateTermLinkUsingId(sourceTerm, isPinnedRoot));
        }

        public LocalTerm AddTermLinkUsingPath(string termLinkSourcePath, bool isPinnedRoot = false)
        {
            var reusedTerm = LocalTerm.CreateTermLinkUsingPath(this.DefaultLanguageLcid, termLinkSourcePath, isPinnedRoot);
            reusedTerm.TermLinkSourcePath = termLinkSourcePath;
            return this.AddTerm(reusedTerm);
        }

        public void RemoveTerm(LocalTerm child)
        {
            this.RemoveChildItem<LocalTerm>(child);
        }

        #endregion

        protected override void ConstructReadOnlyCollection(List<LocalTerm> writableChildItems) // abstract
        {
            this.terms = new LocalTermContainerCollection<LocalTerm>(writableChildItems);
        }

        protected override string ExplainIsAllowableParentFor(LocalTaxonomyItem proposedChild)
        {
            string objection = base.ExplainIsAllowableParentFor(proposedChild);
            if (objection != null)
                return objection;

            LocalTerm proposedChildTerm = (LocalTerm)proposedChild;
            if (proposedChildTerm.TermKind == LocalTermKind.NormalTerm)
            {
                // Check each default label against the target term set to make sure it doesn't conflict.
                foreach (LocalTerm siblingTerm in this.Terms)
                {
                    if (siblingTerm.TermKind == LocalTermKind.NormalTerm)
                    {
                        objection = siblingTerm.ExplainHasLabelConflictWith(proposedChildTerm);
                        if (objection != null)
                            return objection;
                    }
                }
            }
            return null; // no problem
        }


        /// <summary>
        /// Returns the containing <see cref="LocalTermSet" /> instance (scanning upwards in the tree),
        /// or null if not found.
        /// </summary>
        public LocalTermSet GetTermSet()
        {
            for (LocalTaxonomyItem item = this; item != null; item = item.ParentItem)
            {
                if (item is LocalTermSet)
                    return (LocalTermSet) item;
            }
            return null;
        }

        internal string GetLocalizedStringWithFallback(IDictionary<int, string> dictionary, int lcid)
        {
            string value;
            if (dictionary.TryGetValue(lcid, out value))
                return value;
            if (dictionary.TryGetValue(this.DefaultLanguageLcid, out value))
                return value;
            // This should be impossible
            throw new InvalidOperationException("Data constraint violation -- no string exists in the default language");
        }
    }

}
