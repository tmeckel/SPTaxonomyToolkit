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

using System.Collections.Generic;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;

namespace TaxonomyToolkit.Sync
{
    internal class TermSetLocalizedNameQuery
    {
        public int Lcid;
        public TermSet ClientTermSet;

        public string Name
        {
            get
            {
                if (this.ClientTermSet == null || this.ClientTermSet.ServerObjectIsNull != false)
                    return null;
                return this.ClientTermSet.Name;
            }
        }

        /// <remarks>
        /// This method is sometimes called inside a CSOM execution scope, so it
        /// has to use SetUnmanagedWorkingLanguageForTermStore().  The caller must 
        /// set up an unmanaged scope using WorkingLanguageManager.StartUnmanagedScope().
        /// </remarks>
        public static List<TermSetLocalizedNameQuery> Load(TermSet clientTermSet,
            IEnumerable<int> lcidsToRead, int defaultLcid,
            TermStore clientTermStore, Client15Connector clientConnector)
        {
            var clientContext = clientConnector.ClientContext;

            var list = new List<TermSetLocalizedNameQuery>();

            foreach (int lcid in lcidsToRead)
            {
                if (lcid == defaultLcid)
                    continue; // we already read this as clientTermSet.Name

                clientConnector.WorkingLanguageManager
                    .SetUnmanagedWorkingLanguageForTermStore(clientTermStore, lcid);

                var localizedNameQuery = new TermSetLocalizedNameQuery();
                // Since we want to read just one property, which is a property that we already
                // read in the same ExecuteQuery() transaction, we need to construct a separate
                // CSOM object to hold the result.
                // (This apparently causes the Id/Name to get refetched a second time
                // by QueryChildObjects().)
                localizedNameQuery.ClientTermSet = new TermSet(clientContext, clientTermSet.Path);
                localizedNameQuery.Lcid = lcid;

                clientContext.Load(localizedNameQuery.ClientTermSet, ts => ts.Name);

                list.Add(localizedNameQuery);
            }

            return list;
        }
    }
}
