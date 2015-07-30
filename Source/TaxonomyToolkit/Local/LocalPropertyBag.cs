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
using TaxonomyToolkit.General;

namespace TaxonomyToolkit.Taxml
{
    // Wraps all Dictionary<string,string> functionality, but overrides "add" to ensure
    // that SharePoint normalization is performed.
    public class LocalPropertyBag : IDictionary<string, string>
    {
        private Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static string GetNormalized(string keyOrValue)
        {
            ToolkitUtilities.ConfirmNotNull(keyOrValue, "keyOrValue");
            return keyOrValue.Trim();
        }

        #region IDictionary<string,string> Members

        public void Add(string key, string value)
        {
            // This is what SharePoint does
            this.dictionary.Add(LocalPropertyBag.GetNormalized(key), LocalPropertyBag.GetNormalized(value));
        }

        public bool ContainsKey(string key)
        {
            return this.dictionary.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return this.dictionary.Keys; }
        }

        public bool Remove(string key)
        {
            return this.dictionary.Remove(key);
        }

        public bool TryGetValue(string key, out string value)
        {
            return this.dictionary.TryGetValue(key, out value);
        }

        public ICollection<string> Values
        {
            get { return this.dictionary.Values; }
        }

        public string this[string key]
        {
            get
            {
                return this.dictionary[key];
            }
            set
            {
                this.dictionary[GetNormalized(key)] = GetNormalized(value);
                
            }
        }

        #endregion

        #region ICollection<KeyValuePair<string,string>> Members

        public void Add(KeyValuePair<string, string> item)
        {
            var collection = (ICollection<KeyValuePair<string, string>>) this.dictionary;
            var pair = new KeyValuePair<string, string>(LocalPropertyBag.GetNormalized(item.Key),
                LocalPropertyBag.GetNormalized(item.Value));
            collection.Add(pair);
        }

        public void Clear()
        {
            this.dictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return this.dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            var collection = (ICollection<KeyValuePair<string, string>>) this.dictionary;
            collection.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            var collection = (ICollection<KeyValuePair<string, string>>) this.dictionary;
            return collection.Remove(item);
        }

        #endregion

        #region IEnumerable<KeyValuePair<string,string>> Members

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return this.dictionary.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable) this.dictionary).GetEnumerator();
        }

        #endregion
    }
}
