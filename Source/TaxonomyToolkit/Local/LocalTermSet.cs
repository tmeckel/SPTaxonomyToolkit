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
using System.Linq;
using TaxonomyToolkit.General;

namespace TaxonomyToolkit.Taxml
{
    /// <summary>
    ///     Represents a taxonomy term set in the LocalTermStore object model.
    /// </summary>
    public sealed class LocalTermSet : LocalTermContainer
    {
        private LocalTermGroup parentItem;

        private string description = "";
        private string owner = "";
        private string contact = "";

        private bool isOpenForTermCreation = false;

        private readonly LocalPropertyBag customProperties = new LocalPropertyBag();

        private SortedDictionary<int, string> nameByLcid = new SortedDictionary<int, string>();

        private readonly List<string> stakeholders = new List<string>();

        public LocalTermSet(Guid id, string name, int defaultLanguageLcid = LocalTermStore.EnglishLanguageLcid)
            : base(id, defaultLanguageLcid)
        {
            this.SetName(name, defaultLanguageLcid);
        }

        #region Properties

        public new string Name
        {
            get { return this.GetNameWithDefault(this.DefaultLanguageLcid); }
            set { this.SetName(value, this.DefaultLanguageLcid); }
        }

        /// <summary>
        ///     This returns the TermSet name for each language LCID.
        /// </summary>
        public ReadOnlyCollection<LocalizedString> LocalizedNames
        {
            get
            {
                return new ReadOnlyCollection<LocalizedString>(
                    this.nameByLcid.Select(
                        pair => new LocalizedString(pair.Key, pair.Value)
                        )
                        .OrderBy(description => description.Lcid)
                        .ToArray()
                    );
            }
        }


        public override IDictionary<string, string> CustomProperties
        {
            get { return this.customProperties; }
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

        public string Owner
        {
            get { return this.owner; }
            set
            {
                ToolkitUtilities.ConfirmNotNull(value, "value");
                this.owner = value;
            }
        }

        public string Contact
        {
            get { return this.contact; }
            set
            {
                ToolkitUtilities.ConfirmNotNull(value, "value");
                this.contact = value;
            }
        }

        public ReadOnlyCollection<string> Stakeholders
        {
            get { return new ReadOnlyCollection<string>(this.stakeholders); }
        }

        /// <summary>
        ///     Indicates whether the TermSet can be updated by all users, or alternatively just by
        ///     users with edit permissionsterm store.
        /// </summary>
        public bool IsOpenForTermCreation
        {
            get { return this.isOpenForTermCreation; }
            set { this.isOpenForTermCreation = value; }
        }

        #endregion

        #region LocalTermContainer Boilerplate

        public override LocalTaxonomyItemKind Kind
        {
            get { return LocalTaxonomyItemKind.TermSet; }
        }

        public new LocalTermGroup ParentItem
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
            this.ParentItem = (LocalTermGroup) value;
        }

        #endregion

        public void AddStakeholder(string stakeholder)
        {
            ToolkitUtilities.ConfirmNotNull(stakeholder, "stakeholder");
            this.stakeholders.Add(stakeholder);
        }

        public void RemoveStakeholder(string stakeholder)
        {
            this.stakeholders.Remove(stakeholder);
        }

        public void ClearStakeholders()
        {
            this.stakeholders.Clear();
        }

        protected override string GetName()
        {
            return this.Name;
        }

        protected override void OnPrepareNewDefaultLanguageLcid(int newDefaultLanguageLcid)
        {
            base.OnPrepareNewDefaultLanguageLcid(newDefaultLanguageLcid);

            if (!this.nameByLcid.ContainsKey(newDefaultLanguageLcid))
            {
                Debug.WriteLine("Copying term set name \"" + this.Name + "\" to new default language");
                this.nameByLcid.Add(newDefaultLanguageLcid, this.Name);
            }
        }

        public string GetNameWithDefault(int lcid)
        {
            return this.GetLocalizedStringWithFallback(this.nameByLcid, lcid);
        }

        public void SetName(string value, int lcid)
        {
            string normalizedName = ToolkitUtilities.GetNormalizedTaxonomyName(value, "value");
            this.nameByLcid[lcid] = normalizedName;
        }

        public void ClearNames(string defaultName)
        {
            string normalizedName = ToolkitUtilities.GetNormalizedTaxonomyName(defaultName, "defaultName");
            this.nameByLcid.Clear();
            this.nameByLcid[this.DefaultLanguageLcid] = normalizedName;
        }

        public IEnumerable<LocalTerm> GetAllTerms()
        {
            foreach (
                LocalTaxonomyItem item in
                    ToolkitUtilities.GetPreorder((LocalTaxonomyItem) this, item => item.ChildItems))
            {
                if (item == this)
                    continue;
                yield return (LocalTerm) item;
            }
        }
    }
}
