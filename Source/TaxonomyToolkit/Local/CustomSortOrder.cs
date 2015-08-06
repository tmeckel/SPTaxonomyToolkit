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
