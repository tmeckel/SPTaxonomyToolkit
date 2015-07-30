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
using System.Linq;

namespace TaxonomyToolkit.Taxml
{
    internal class TaxonomyItemDownloaderContext
    {
        public readonly Client15Connector ClientConnector;
        public readonly ClientConnectorDownloadOptions Options;
        private TermStoreDownloader termStoreDownloader;
        private List<int> lcidsToRead = null;

        internal TaxonomyItemDownloaderContext(Client15Connector clientConnector, ClientConnectorDownloadOptions options)
        {
            this.ClientConnector = clientConnector;
            this.Options = options;
        }

        public TermStoreDownloader TermStoreDownloader
        {
            get { return this.termStoreDownloader; }
            set
            {
                if (this.termStoreDownloader == value)
                    return;
                if (this.termStoreDownloader != null)
                    throw new InvalidOperationException("The TermStoreDownloader has already been set");
                this.termStoreDownloader = value;
            }
        }

        public ReadOnlyCollection<int> GetLcidsToRead()
        {
            if (this.lcidsToRead == null)
                throw new InvalidOperationException("SetLcidsToRead() has not been called yet");
            return new ReadOnlyCollection<int>(this.lcidsToRead);
        }

        public void SetLcidsToRead(IEnumerable<int> lcidsToRead)
        {
            this.lcidsToRead = lcidsToRead.ToList();
        }
    }
}
