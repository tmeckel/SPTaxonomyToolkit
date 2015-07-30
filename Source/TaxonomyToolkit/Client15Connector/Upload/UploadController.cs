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
using Microsoft.SharePoint.Client.Taxonomy;
using TaxonomyToolkit.General;

namespace TaxonomyToolkit.Taxml
{
    internal class UploadControllerKey
        : IComparable<UploadControllerKey>, IEquatable<UploadControllerKey>
    {
        internal bool Started;
        internal int Sequence;

        public int CompareTo(UploadControllerKey other) // IComparable<UploadControllerKey>
        {
            // Already started items come first
            int comparison = this.Started.CompareTo(other.Started);
            if (comparison != 0)
                return -comparison;

            // Otherwise follow the original sequence
            return this.Sequence.CompareTo(other.Sequence);
        }


        public bool Equals(UploadControllerKey other) // IEquatable<UploadControllerKey>
        {
            return ((IComparable<UploadControllerKey>) this).CompareTo(other) == 0;
        }

        public override bool Equals(object other)
        {
            return this.Equals((UploadControllerKey) other);
        }

        public override int GetHashCode()
        {
            return ToolkitUtilities.CombineHashCodes(this.Started.GetHashCode(), this.Sequence.GetHashCode());
        }
    }

    internal class UploadController
    {
        private class TaxonomyItemUploaderComparer : IComparer<TaxonomyItemUploader>
        {
            public int Compare(TaxonomyItemUploader x, TaxonomyItemUploader y)
            {
                return x.ControllerKey.CompareTo(y.ControllerKey);
            }
        }

        private readonly Client15Connector clientConnector;
        private readonly LocalTermStore localTermStore;
        private readonly TermStore clientTermStore;
        private readonly ClientConnectorUploadOptions options;

        private Dictionary<LocalTaxonomyItem, TaxonomyItemUploader> uploadersByItem
            = new Dictionary<LocalTaxonomyItem, TaxonomyItemUploader>();

        private PriorityQueue<TaxonomyItemUploader> queuedUploaders;
        private HashSet<TaxonomyItemUploader> blockedUploaders = new HashSet<TaxonomyItemUploader>();
        private LinkedList<TaxonomyItemUploader> executingUploaders = new LinkedList<TaxonomyItemUploader>();
        private List<TaxonomyItemUploader> finishedUploaders = new List<TaxonomyItemUploader>();

        private List<int> clientLcids = new List<int>();
        private HashSet<int> clientLcidsHashSet = new HashSet<int>();
        private List<int> localLcids = new List<int>();

        public UploadController(Client15Connector clientConnector, LocalTermStore localTermStore,
            TermStore clientTermStore, ClientConnectorUploadOptions options)
        {
            this.clientConnector = clientConnector;
            this.localTermStore = localTermStore;
            this.clientTermStore = clientTermStore;
            this.options = options;

            this.queuedUploaders = new PriorityQueue<TaxonomyItemUploader>(new TaxonomyItemUploaderComparer());
        }

        #region Properties

        public ClientConnectorUploadOptions Options
        {
            get { return this.options; }
        }

        internal TermStore ClientTermStore
        {
            get { return this.clientTermStore; }
        }

        internal LocalTermStore LocalTermStore
        {
            get { return this.localTermStore; }
        }

        public Client15Connector ClientConnector
        {
            get { return this.clientConnector; }
        }

        public ReadOnlyCollection<int> ClientLcids
        {
            get { return new ReadOnlyCollection<int>(this.clientLcids); }
        }

        public ReadOnlyCollection<int> LocalLcids
        {
            get { return new ReadOnlyCollection<int>(this.localLcids); }
        }

        public int DefaultLanguageLcid
        {
            get { return this.localTermStore.DefaultLanguageLcid; }
        }

        #endregion

        public void PerformUpload()
        {
            this.clientLcids.Clear();
            this.clientLcidsHashSet.Clear();
            this.localLcids.Clear();
            this.uploadersByItem.Clear();
            this.queuedUploaders.Clear();
            this.blockedUploaders.Clear();
            this.executingUploaders.Clear();

            this.DetermineLcidsToProcess();

            this.QueueUploaders();

            int itemsFinished = 0;

            for (;;)
            {
                // Issue queries for the items in executingUploaders
                int uploadersInBatch = 0;
                for (var node = this.executingUploaders.First; node != null;)
                {
                    var uploader = node.Value;
                    var nextNode = node.Next;

                    if (uploadersInBatch >= this.Options.MaximumBatchSize)
                    {
                        // too many uploaders; start moving them back to the queue
                        this.executingUploaders.Remove(node);
                        this.queuedUploaders.Add(uploader);
                    }
                    else
                    {
                        bool issuedQuery = false;
                        if (uploader.Processable)
                            issuedQuery = uploader.Process();

                        if (issuedQuery)
                        {
                            ++uploadersInBatch;
                        }
                        else
                        {
                            // If the uploader didn't issue a query, then move it to the blocked list
                            this.executingUploaders.Remove(node);
                            this.blockedUploaders.Add(uploader);
                        }
                    }
                    node = nextNode; // iterate in a way that tolerates remove operations
                }

                // If we don't have a full batch, pull more items from the queue
                while (uploadersInBatch < this.Options.MaximumBatchSize)
                {
                    var uploader = this.queuedUploaders.RemoveTop();
                    if (uploader == null)
                        break;

                    bool issuedQuery = false;
                    if (uploader.Processable)
                        issuedQuery = uploader.Process();

                    if (issuedQuery)
                    {
                        ++uploadersInBatch;
                        this.executingUploaders.AddLast(uploader);
                        if (!uploader.ControllerKey.Started)
                        {
                            uploader.ControllerKey.Started = true;

                            this.ClientConnector.NotifyUploadingItem(
                                uploader.LocalTaxonomyItem,
                                itemsUploaded: itemsFinished,
                                totalItems: this.uploadersByItem.Count
                                );
                        }
                    }
                    else
                    {
                        // If the uploader didn't issue a query, then move it to the blocked list
                        this.blockedUploaders.Add(uploader);
                    }
                }

                if (uploadersInBatch == 0)
                    break; // nothing left to do

                // Execute the batch
                Debug.WriteLine("Batch contains " + uploadersInBatch + " uploaders");
                this.ClientConnector.ExecuteQuery();

                // Notify the uploaders that their query succeeded
                for (var node = this.executingUploaders.First; node != null;)
                {
                    var uploader = node.Value;
                    var nextNode = node.Next;

                    uploader.NotifyQueryExecuted();
                    if (uploader.Finished)
                    {
                        ++itemsFinished;
                        this.executingUploaders.Remove(node);
                        this.finishedUploaders.Add(uploader);
                    }

                    node = nextNode; // iterate in a way that tolerates remove operations
                }
            }

            if (this.blockedUploaders.Count > 0)
                throw new InvalidOperationException("Program Bug: UploadController is deadlocked");
        }

        internal void NotifyUploaderUnblocked(TaxonomyItemUploader uploader)
        {
            // If the uploader was added to the blockedUploaders set, then remove it.
            // It's possible that it was blocked and unblocked before the controller saw it,
            // in which case it won't be in the set.
            if (this.blockedUploaders.Remove(uploader))
            {
                // If we found it, put it back in the queue
                this.queuedUploaders.Add(uploader);
            }
        }

        private void QueueUploaders()
        {
            int sequence = 1;

            foreach (var item in ToolkitUtilities.GetPreorder<LocalTaxonomyItem>(this.localTermStore, x => x.ChildItems)
                )
            {
                this.CheckForUnimplementedSyncActions(item);

                TaxonomyItemUploader uploader = this.CreateUploader(item);

                if (uploader == null)
                    continue;

                uploader.ControllerKey.Started = false;
#if false
                // FOR DEBUGGING, BLOCK AS MUCH AS POSSIBLE
                uploader.ControllerKey.Sequence = -sequence;
#else
                uploader.ControllerKey.Sequence = sequence;
#endif
                ++sequence;
            }

            foreach (var uploader in this.queuedUploaders.EnumerateUnordered())
            {
                uploader.Initialize();
            }
        }

        private void CheckForUnimplementedSyncActions(LocalTaxonomyItem item)
        {
            string error = null;

            var syncAction = item.SyncAction;
            if (syncAction != null)
            {
                if (syncAction.DeleteExtraChildItems)
                    error = "The DeleteExtraChildItems attribute is not implemented yet";

                switch (syncAction.IfElsewhere)
                {
                    case SyncActionIfElsewhere.Error:
                        break;
                    default:
                        error = "The IfElsewhere=" + syncAction.IfElsewhere.ToString() +
                            " option is not implemented yet";
                        break;
                }

                switch (syncAction.IfPresent)
                {
                    case SyncActionIfPresent.Update:
                    case SyncActionIfPresent.OnlyUpdateChildItems:
                    case SyncActionIfPresent.Error:
                        break;
                    default:
                        error = "The IfPresent=" + syncAction.IfPresent.ToString() + " option is not implemented yet";
                        break;
                }

                switch (syncAction.IfMissing)
                {
                    case SyncActionIfMissing.Create:
                    case SyncActionIfMissing.Error:
                        break;
                    default:
                        error = "The IfMissing=" + syncAction.IfMissing.ToString() + " option is not implemented yet";
                        break;
                }
            }

            if (error != null)
            {
                throw new NotImplementedException(error + "\r\nItem: " + item.ToString());
            }
        }

        private TaxonomyItemUploader CreateUploader(LocalTaxonomyItem item)
        {
            TaxonomyItemUploader uploader;
            switch (item.Kind)
            {
                case LocalTaxonomyItemKind.TermStore:
                    return null;
                case LocalTaxonomyItemKind.TermGroup:
                    uploader = new TermGroupUploader((LocalTermGroup) item, this);
                    break;
                case LocalTaxonomyItemKind.TermSet:
                    uploader = new TermSetUploader((LocalTermSet) item, this);
                    break;
                case LocalTaxonomyItemKind.Term:
                    uploader = new TermUploader((LocalTerm) item, this);
                    break;
                default:
                    throw new NotSupportedException();
            }

            this.uploadersByItem.Add(item, uploader);
            this.queuedUploaders.Add(uploader);
            return uploader;
        }

        internal TaxonomyItemUploader GetUploader(LocalTaxonomyItem item)
        {
            return this.uploadersByItem[item];
        }

        private void DetermineLcidsToProcess()
        {
            CsomHelpers.FlushCachedProperties(this.clientTermStore);
            this.ClientConnector.ClientContext.Load(this.clientTermStore,
                ts => ts.Languages,
                ts => ts.DefaultLanguage);
            this.ClientConnector.ExecuteQuery();

            if (this.localTermStore.DefaultLanguageLcid != this.clientTermStore.DefaultLanguage)
            {
                throw new InvalidOperationException(
                    "The SharePoint term store default language does not match the LocalTermStore default language");
            }

            this.clientLcids.Clear();
            this.clientLcids.AddRange(this.clientTermStore.Languages);
            this.clientLcids.Sort();

            this.clientLcidsHashSet.Clear();
            foreach (int lcid in this.clientLcids)
                this.clientLcidsHashSet.Add(lcid);

            this.localLcids.Clear();
            this.localLcids.AddRange(this.localTermStore.AvailableLanguageLcids);
            this.localLcids.Sort();
        }

        public bool ClientLcidsContains(int lcid)
        {
            return this.clientLcidsHashSet.Contains(lcid);
        }
    }
}
