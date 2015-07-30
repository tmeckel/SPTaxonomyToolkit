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
using System.ComponentModel;
using System.Linq;

namespace TaxonomyToolkit.Taxml
{
    public class ClientConnectorDownloadOptions
    {
        private int maximumDepth = -1;

        [DefaultValue(false)]
        public bool MinimalAtMaximumDepth { get; set; }

        private ReadOnlyCollection<Guid> groupIdFilter = null;
        private HashSet<Guid> groupIdFilterHashSet = null;

        public ClientConnectorDownloadOptions()
        {
        }

        #region Properties

        /// <summary>
        /// If this is not null, then the TaxonomyItemDownloader will only read groups
        /// whose ID appears in this list.  Call EnableGroupIdFilter() to
        /// assign it.
        /// </summary>
        public ReadOnlyCollection<Guid> GroupIdFilter
        {
            get { return this.groupIdFilter; }
        }

        /// <summary>
        /// How many levels of the tree should be loaded below the current node.
        /// A depth of 0 means that only the TermStore should be loaded, whereas 1
        /// means Groups should be loaded, and 2 means TermStore, Groups, and
        /// TermSets, etc.  For unlimited recursion, use MaximumDepth=-1.
        /// </summary>
        [DefaultValue(-1)]
        public int MaximumDepth
        {
            get { return this.maximumDepth; }
            set { this.maximumDepth = value; }
        }

        #endregion

        public void EnableGroupIdFilter(IEnumerable<Guid> termGroupIds)
        {
            this.groupIdFilter = new ReadOnlyCollection<Guid>(termGroupIds.ToArray());
            this.groupIdFilterHashSet = new HashSet<Guid>(termGroupIds);
        }

        internal bool MatchesGroupIdFilter(Guid termGroupId)
        {
            if (this.groupIdFilter == null)
                return true;
            return this.groupIdFilterHashSet.Contains(termGroupId);
        }
    }

    public class ClientConnectorUploadOptions
    {
        public const int MaximumBatchSizeDefault = 100;

        private int maximumBatchSize;

        public ClientConnectorUploadOptions()
        {
            this.maximumBatchSize = ClientConnectorUploadOptions.MaximumBatchSizeDefault;
        }

        /// <summary>
        /// The maximum number of operations that can be combined as a batch into
        /// a single Client Side Object Model (CSOM) server request.  Larger values
        /// typically improve performance, but also increase the risk of failures that
        /// may occur if the network transaction takes too long to complete or exceeds
        /// the maximum number of bytes permitted by the server.
        /// </summary>
        /// <remarks>
        /// The definition of an "operation" is complex, but roughly corresponds to
        /// finding, creating, updating, or deleting a taxonomy group, term set, or term.
        /// The actual batch size may be much smaller than MaximumBatchSize, since the
        /// dependency graph restricts which operations can be combined in a single
        /// transaction.
        /// </remarks>
        [DefaultValue(ClientConnectorUploadOptions.MaximumBatchSizeDefault)]
        public int MaximumBatchSize
        {
            get { return this.maximumBatchSize; }
            set
            {
                if (this.maximumBatchSize <= 0)
                {
                    throw new ArgumentOutOfRangeException("MaximumBatchSize",
                        "The MaximumBatchSize cannot be smaller than 1");
                }
                this.maximumBatchSize = value;
            }
        }
    }
}
