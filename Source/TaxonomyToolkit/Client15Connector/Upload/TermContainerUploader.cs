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
using Microsoft.SharePoint.Client.Taxonomy;

namespace TaxonomyToolkit.Taxml
{
    internal abstract class TermContainerUploader : TaxonomyItemUploader
    {
        private List<Term> clientChildTerms = null;

        public TermContainerUploader(UploadController controller)
            : base(controller)
        {
        }

        public abstract LocalTermContainer LocalTermContainer { get; }

        public abstract TermSetItem ClientTermContainer { get; }

        public List<Term> ClientChildTerms
        {
            get { return this.clientChildTerms; }
        }

        protected TermSetUploader GetTermSetUploader()
        {
            if (this.Kind == LocalTaxonomyItemKind.TermSet)
                return (TermSetUploader) this;

            LocalTermSet localTermSet = this.LocalTermContainer.GetTermSet();
            if (localTermSet == null)
                throw new InvalidOperationException("The LocalTermSet is unassigned");
            return (TermSetUploader) this.Controller.GetUploader(localTermSet);
        }


        protected override void AssignClientChildItems()
        {
            if (this.Existence == TaxonomyItemExistence.Missing)
            {
                this.clientChildTerms = new List<Term>();
            }
            else
            {
                this.clientChildTerms = this.ClientTermContainer.Terms.ToList();
            }
        }

        protected static void UpdatePropertyBag(IDictionary<string, string> clientProperties,
            Action<string> deleteClientProperty,
            Action<string, string> setClientProperty,
            IDictionary<string, string> localProperties)
        {
            // The dictionaries themselves are not case sensitive, but the syncing algorithm is
            Dictionary<string, string> ordinalClientProperties = new Dictionary<string, string>(clientProperties,
                StringComparer.Ordinal);
            Dictionary<string, string> ordinalLocalProperties = new Dictionary<string, string>(localProperties,
                StringComparer.Ordinal);

            // Delete anything that shouldn't be there
            foreach (var pair in ordinalClientProperties)
            {
                if (!ordinalLocalProperties.ContainsKey(pair.Key))
                {
                    deleteClientProperty(pair.Key);
                }
            }

            // Add anything that's missing
            foreach (var pair in ordinalLocalProperties)
            {
                string value;
                if (ordinalClientProperties.TryGetValue(pair.Key, out value))
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(value, pair.Value))
                        continue;
                }

                setClientProperty(pair.Key, pair.Value);
            }
        }
    }
}
