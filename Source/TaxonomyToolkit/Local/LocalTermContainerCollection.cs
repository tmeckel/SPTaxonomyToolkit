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
using System.Globalization;
using System.Linq;
using TaxonomyToolkit.General;

namespace TaxonomyToolkit.Taxml
{
    /// <summary>
    /// A read-only collection of LocalTermSet or LocalTerm items.
    /// </summary>
    public class LocalTermContainerCollection<T> : ReadOnlyCollection<T>
        where T : LocalTermContainer
    {
        internal LocalTermContainerCollection(IList<T> list)
            : base(list)
        {

        }

        public bool TryGetByName(string name, int lcid, out T item)
        {
            string normalizedName = ToolkitUtilities.GetNormalizedTaxonomyName(name, "name");

            // TODO: This is inefficient; ideally we should maintain a dictionary
            foreach (T currentItem in this.Items)
            {
                string nameWithDefault = currentItem.GetNameWithDefault(lcid);

                if (nameWithDefault.Equals(normalizedName, StringComparison.OrdinalIgnoreCase))
                {
                    item = currentItem;
                    return true;
                }
            }
            item = null;
            return false;
        }

        public T GetByName(string name, int lcid)
        {
            T item;
            if (TryGetByName(name, lcid, out item))
                return item;
            throw new KeyNotFoundException("No item was found with name \"" + name + "\"");
        }
    }

}
