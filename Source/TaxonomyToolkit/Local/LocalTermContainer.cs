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
using System.Text.RegularExpressions;
using TaxonomyToolkit.General;

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
        private bool isAvailableForTagging = true;
        private CustomSortOrder customSortOrder = new CustomSortOrder();

        internal LocalTermContainer(Guid id)
            : base(id)
        {
        }

        internal LocalTermContainer(Guid id, int defaultLanguageLcid)
            : base(id, defaultLanguageLcid)
        {
        }

        /// <summary>
        /// The child items for this object.
        /// </summary>
        public ReadOnlyCollection<LocalTerm> Terms
        {
            get { return this.readOnlyChildItems; }
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

        #region Add/Remove Helpers

        public LocalTerm AddTerm(LocalTerm child)
        {
            return this.AddChildItem<LocalTerm>(child);
        }

        public LocalTerm AddTerm(Guid id, string name, int defaultLanguageLcid = LocalTermStore.EnglishLanguageLcid)
        {
            return this.AddTerm(LocalTerm.CreateTerm(id, name, defaultLanguageLcid));
        }

        public LocalTerm AddTermLinkUsingId(Guid id, string nameHint, bool isPinnedRoot = false)
        {
            return this.AddTerm(LocalTerm.CreateTermLinkUsingId(id, nameHint, isPinnedRoot));
        }

        public LocalTerm AddTermLinkUsingId(LocalTerm sourceTerm, bool isPinnedRoot = false)
        {
            return this.AddTerm(LocalTerm.CreateTermLinkUsingId(sourceTerm, isPinnedRoot));
        }

        public LocalTerm AddTermLinkUsingPath(string termLinkSourcePath, bool isPinnedRoot = false)
        {
            var reusedTerm = LocalTerm.CreateTermLinkUsingPath(termLinkSourcePath, isPinnedRoot);
            reusedTerm.TermLinkSourcePath = termLinkSourcePath;
            return this.AddTerm(reusedTerm);
        }

        public void RemoveTerm(LocalTerm child)
        {
            this.RemoveChildItem<LocalTerm>(child);
        }

        #endregion

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

    /// <summary>
    /// This class is used to represent the LocalTermContainer.CustomSortOrder property.
    /// It is a parser/generator for the special text string used by TermSetItem.CustomSortOrder
    /// from the SharePoint API.  The format is a list of Term GUIDs delimited by colon characters.
    /// </summary>
    public sealed class CustomSortOrder : Collection<Guid>
    {
        private readonly HashSet<Guid> hashSet = new HashSet<Guid>();

        internal CustomSortOrder()
            : base(new List<Guid>())
        {
        }

        public string AsText
        {
            get
            {
                return string.Join(":", this.Items.Select(x => x.ToString("D")));
            }
            set
            {
                ToolkitUtilities.ConfirmNotNull(value, "value");

                if (!Regex.IsMatch(value, @"^[0-9a-f:\-\s]*$", RegexOptions.IgnoreCase))
                {
                    throw new InvalidOperationException(
                        "The custom sort order must be a seqeuence of GUID's delimited by colons");
                }

                string[] parts = value.Split(':');
                this.Clear();

                foreach (string part in parts)
                {
                    if (!string.IsNullOrWhiteSpace(part))
                    {
                        // It's possible for a GUID to appear multiple times in the CustomSortOrder.
                        // Normalize this by discarding extra copies.
                        Guid guid = Guid.Parse(part);
                        if (!this.hashSet.Contains((guid)))
                        {
                            this.Add(guid);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// In the SharePoint API, the "CustomSortOrder" property allows null strings
        /// and tolerates invalid or duplicated GUIDs (by simply ignoring those parts
        /// of ths string).  TaxonomyToolkit's API has stricter validation, but uses
        /// this internal API for interacting with SharePoint.
        /// </summary>
        internal string AsTextForServer
        {
            get
            {
                string value = this.AsText;
                if (value == "")
                    value = null;
                return value;
            }
            set
            {
                string[] parts = (value ?? "").Split(':');
                this.Clear();

                bool extraGuids = false;
                bool invalidGuids = false;

                foreach (string part in parts)
                {
                    if (!string.IsNullOrWhiteSpace(part))
                    {
                        Guid guid;
                        if (Guid.TryParse(part, out guid))
                        {
                            if (!this.hashSet.Contains((guid)))
                            {
                                this.Add(guid);
                            }
                            else
                            {
                                extraGuids = true;
                            }
                        }
                        else
                        {
                            invalidGuids = true;
                        }
                    }
                }

                if (invalidGuids)
                {
                    Debug.WriteLine("Ignoring invalid guids in CustomSortOrder string \"" 
                        + (value ?? "" + "\""));
                }
                else if (extraGuids)
                {
                    Debug.WriteLine("Ignoring extra guid in CustomSortOrder string \""
                        + (value ?? "" + "\""));
                }
            }
        }

        public void AddRange(List<Guid> itemIds)
        {
            foreach (var itemId in itemIds)
                this.Add(itemId);
        }

        internal void AssignFrom(List<Guid> itemIds)
        {
            this.Clear();
            this.AddRange(itemIds);
        }

        #region Validation

        protected override void ClearItems()
        {
            base.ClearItems();
            this.hashSet.Clear();
        }

        protected override void InsertItem(int index, Guid item)
        {
            if (!this.hashSet.Add(item))
                throw new ArgumentException("The specified GUID cannot be added twice to the same CustomSortOrder");
            base.InsertItem(index, item);
            Debug.Assert(this.hashSet.Count == this.Count);
        }

        protected override void RemoveItem(int index)
        {
            this.hashSet.Remove(this[index]);
            base.RemoveItem(index);
            Debug.Assert(this.hashSet.Count == this.Count);
        }

        protected override void SetItem(int index, Guid item)
        {
            this.hashSet.Remove(this[index]);
            base.SetItem(index, item);
            this.hashSet.Add(item);
            Debug.Assert(this.hashSet.Count == this.Count);
        }

        #endregion
    }
}
