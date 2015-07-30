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
using System.Collections.ObjectModel;
using TaxonomyToolkit.General;

namespace TaxonomyToolkit.Taxml
{
    /// <summary>
    ///     Represents a taxonomy term set group in the LocalTermStore object model.
    /// </summary>
    public sealed class LocalTermGroup : LocalTaxonomyItem<LocalTermSet>
    {
        private LocalTermStore parentItem;

        private bool isSystemGroup;

        private string name = "";
        private string description = "";

        public LocalTermGroup(Guid id, string name)
            : base(id)
        {
            this.Name = name;
        }

        #region Properties

        /// <summary>
        ///     The child items for this object.
        /// </summary>
        public ReadOnlyCollection<LocalTermSet> TermSets
        {
            get { return this.readOnlyChildItems; }
        }

        /// <summary>
        ///     Indicates whether this is the system group, which contains the special
        ///     "Keywords" and "Orphaned Terms" terms sets.
        /// </summary>
        public bool IsSystemGroup
        {
            get { return this.isSystemGroup; }
            set { this.isSystemGroup = value; }
        }

        public new string Name
        {
            get { return this.name; }
            set
            {
                string normalizedName = ToolkitUtilities.GetNormalizedTaxonomyName(value, "value");
                this.name = normalizedName;
            }
        }

        // Note that TermGroup and TermSet have non-localized descriptions, unlike Term
        public string Description
        {
            get { return this.description; }
            set
            {
                ToolkitUtilities.ConfirmNotNull(value, "value");
                this.description = value;
            }
        }

        #endregion

        #region LocalTaxonomyItem Boilerplate

        public override LocalTaxonomyItemKind Kind
        {
            get { return LocalTaxonomyItemKind.TermGroup; }
        }

        public new LocalTermStore ParentItem
        {
            get { return this.parentItem; }
            set { this.SetParentItem(ref this.parentItem, value); }
        }

        protected override LocalTaxonomyItem GetParentItem()
        {
            return this.ParentItem;
        }

        protected override void SetParentItem(LocalTaxonomyItem value)
        {
            this.ParentItem = (LocalTermStore) value;
        }

        #endregion

        #region Add/Remove Helpers

        public LocalTermSet AddTermSet(LocalTermSet child)
        {
            return this.AddChildItem<LocalTermSet>(child);
        }

        public LocalTermSet AddTermSet(Guid id, string name)
        {
            return this.AddTermSet(new LocalTermSet(id, name));
        }

        public void RemoveTermSet(LocalTermSet child)
        {
            this.RemoveChildItem<LocalTermSet>(child);
        }

        #endregion

        protected override string GetName()
        {
            return this.name;
        }
    }
}
