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

namespace TaxonomyToolkit.Taxml
{
    public enum SyncActionIfMissing
    {
        /// <summary>
        /// Create the missing object and assign all properties from the the
        /// LocalTaxonomyItem object.
        /// </summary>
        Create,

        /// <summary>
        /// Not yet implemented.
        /// </summary>
        DoNothing,

        /// <summary>
        /// Report an error and abort the import operation.
        /// </summary>
        Error
    }

    public enum SyncActionIfPresent
    {
        /// <summary>
        /// Reassign all properties of the object to exactly match the LocalTaxonomyItem
        /// object; any extra properties will be deleted.
        /// </summary>
        Update,

        /// <summary>
        /// Make no changes to the object, but proceed with processing any child objects.
        /// </summary>
        OnlyUpdateChildItems,

        /// <summary>
        /// Not yet implemented.
        /// </summary>
        DeleteAndRecreate,

        /// <summary>
        /// Not yet implemented.
        /// </summary>
        DoNothing,

        /// <summary>
        /// Report an error and abort the import operation.
        /// </summary>
        Error
    }

    public enum SyncActionIfElsewhere
    {
        /// <summary>
        /// Not yet implemented.
        /// </summary>
        MoveAndUpdate,

        /// <summary>
        /// Not yet implemented.
        /// </summary>
        DeleteAndRecreate,

        /// <summary>
        /// Not yet implemented.
        /// </summary>
        DoNothing,

        /// <summary>
        /// Report an error and abort the import operation.
        /// </summary>
        Error
    }

    /// <summary>
    /// This object applies to any LocalTaxonomyItem subclass, and describes how incremental
    /// updates will be performed during an import operation.  The SyncAction describes
    /// the action to take if the object is completely "missing", if it is "present" in
    /// the expected location, or if it exists "elsewhere".   By "elsewhere" we mean a term set
    /// that belongs to the wrong group, or a term instance that is in the correct term set
    /// but under the wrong parent.  (If the term exists  in a different term set, the
    /// TAXML specification assumes that the intent is to create a reused term instance.)
    /// </summary>
    public class SyncAction
    {
        private SyncActionIfMissing ifMissing;
        private SyncActionIfPresent ifPresent;
        private SyncActionIfElsewhere ifElsewhere;
        private bool deleteExtraChildItems;
        public bool ReadOnly { get; internal set; }

        public SyncAction()
        {
            this.IfMissing = SyncActionIfMissing.Create;
            this.IfPresent = SyncActionIfPresent.Error;
            this.IfElsewhere = SyncActionIfElsewhere.Error;
            this.DeleteExtraChildItems = false;
        }

        #region Properties

        public SyncActionIfMissing IfMissing
        {
            get { return this.ifMissing; }
            set
            {
                if (this.ifMissing == value)
                    return;
                this.RequireNotReadOnly();
                this.ifMissing = value;
            }
        }

        public SyncActionIfPresent IfPresent
        {
            get { return this.ifPresent; }
            set
            {
                if (this.ifPresent == value)
                    return;
                this.RequireNotReadOnly();
                this.ifPresent = value;
            }
        }

        public SyncActionIfElsewhere IfElsewhere
        {
            get { return this.ifElsewhere; }
            set
            {
                if (this.ifElsewhere == value)
                    return;
                this.RequireNotReadOnly();
                this.ifElsewhere = value;
            }
        }

        /// <summary>
        /// If matching server object is found to have extra child items with no counterparts
        /// in LocalTaxonomyItem.ChildItems, this option indicates whether the sync operation
        /// should delete the extra server objects.
        /// </summary>
        public bool DeleteExtraChildItems
        {
            get { return this.deleteExtraChildItems; }
            set
            {
                if (this.deleteExtraChildItems == value)
                    return;
                this.RequireNotReadOnly();
                this.deleteExtraChildItems = value;
            }
        }

        #endregion

        private void RequireNotReadOnly()
        {
            if (this.ReadOnly)
                throw new InvalidOperationException("The object cannot be changed because it is marked as read-only");
        }

        #region Static Members

        private static SyncAction defaultSyncAction;

        static SyncAction()
        {
            SyncAction.defaultSyncAction = new SyncAction()
            {
                ReadOnly = true
            };
        }

        public static SyncAction Default
        {
            get { return SyncAction.defaultSyncAction; }
        }

        #endregion
    }
}
