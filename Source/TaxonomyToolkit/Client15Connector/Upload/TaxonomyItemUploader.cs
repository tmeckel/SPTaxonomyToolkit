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
using System.Linq.Expressions;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using TaxonomyToolkit.Taxml;

namespace TaxonomyToolkit.Sync
{
    internal enum TaxonomyItemExistence
    {
        Unknown,
        Present,
        Missing,
        Elsewhere
    }

    internal abstract class TaxonomyItemUploaderBase
    {
        private UploadController controller;
        private readonly UploadControllerKey controllerKey = new UploadControllerKey();

        // Uploaders that are blocked by this uploader
        private LinkedList<TaxonomyItemUploader> waiters = new LinkedList<TaxonomyItemUploader>();

        // Uploaders that are blocking this uploader
        private HashSet<TaxonomyItemUploader> blockers = new HashSet<TaxonomyItemUploader>();

        public TaxonomyItemUploaderBase(UploadController controller)
        {
            this.controller = controller;
        }

        public UploadController Controller
        {
            get { return this.controller; }
        }

        internal UploadControllerKey ControllerKey
        {
            get { return this.controllerKey; }
        }

        public ReadOnlyCollection<TaxonomyItemUploader> Waiters
        {
            get { return new ReadOnlyCollection<TaxonomyItemUploader>(this.waiters.ToArray()); }
        }

        public ReadOnlyCollection<TaxonomyItemUploader> Blockers
        {
            get { return new ReadOnlyCollection<TaxonomyItemUploader>(this.blockers.ToArray()); }
        }

        protected void WaitForBlocker(TaxonomyItemUploader blocker)
        {
            if (this.blockers.Contains(blocker))
                throw new InvalidOperationException("Invalid call to StartWaiting()");
            this.blockers.Add(blocker);
            blocker.waiters.AddLast((TaxonomyItemUploader) this);
        }

        protected void NotifyWaiters()
        {
            foreach (var waiter in this.waiters)
            {
                waiter.blockers.Remove((TaxonomyItemUploader) this);

                if (waiter.blockers.Count == 0)
                {
                    this.Controller.NotifyUploaderUnblocked(waiter);
                }
            }
            this.waiters.Clear();
        }
    }

    internal abstract class TaxonomyItemUploader : TaxonomyItemUploaderBase
    {
        private enum State
        {
            Start,
            CheckExistence,
            AfterCheckExistence,
            Create,
            AssignProperties,
            UpdateProperties,
            Finished
        }

        private SyncAction syncAction;

        private State state = State.Start;
        private TaxonomyItemExistence existence = TaxonomyItemExistence.Unknown;

        protected ExceptionHandlingScope exceptionHandlingScope = null;

        public TaxonomyItemUploader(UploadController controller)
            : base(controller)
        {
        }

        #region Properties

        protected ClientContext ClientContext
        {
            get { return this.Controller.ClientConnector.ClientContext; }
        }

        protected TermStore ClientTermStore
        {
            get { return this.Controller.ClientTermStore; }
        }

        public abstract LocalTaxonomyItem LocalTaxonomyItem { get; }
        public abstract TaxonomyItem ClientTaxonomyItem { get; }

        public LocalTaxonomyItemKind Kind
        {
            get { return this.LocalTaxonomyItem.Kind; }
        }

        protected SyncAction SyncAction
        {
            get { return this.syncAction; }
        }

        public bool FoundClientObject
        {
            get
            {
                return this.ClientTaxonomyItem != null
                    && this.ClientTaxonomyItem.ServerObjectIsNull == false;
            }
        }

        public TaxonomyItemExistence Existence
        {
            get { return this.existence; }
        }

        protected bool FindByName
        {
            get { return this.LocalTaxonomyItem.Id == Guid.Empty; }
        }

        public bool Processable
        {
            get { return this.Blockers.Count == 0 && !this.Finished; }
        }

        public bool Finished
        {
            get { return this.state == State.Finished; }
        }

        #endregion

        protected internal virtual void Initialize()
        {
            this.syncAction = this.LocalTaxonomyItem.GetEffectiveSyncAction();
        }

        protected abstract bool OnProcessCheckExistence();

        protected abstract bool OnProcessCreate();

        protected abstract bool OnProcessAssignProperties();

        protected abstract bool OnProcessUpdateProperties();

        protected abstract bool IsPlacedCorrectly();

        protected abstract void AssignClientChildItems();

        protected TaxonomyItemUploader GetParentUploader()
        {
            var parentItem = this.LocalTaxonomyItem.ParentItem;
            if (parentItem == null)
                throw new InvalidOperationException("The item does not have a parent");
            return this.Controller.GetUploader(parentItem);
        }

        public bool Process()
        {
            bool issuedQuery;

            do
            {
                issuedQuery = this.OnProcess();
            } while (!issuedQuery && this.Processable);

            if (issuedQuery)
                Debug.WriteLine("Issued " + this.state + " query for " + this.ToString());

            return issuedQuery;
        }

        private bool OnProcess()
        {
            this.CleanUpCsomObjectsFromPreviousRequest();

            switch (this.state)
            {
                case State.Start:
                    this.state = State.CheckExistence;
                    return false;

                case State.CheckExistence:
                    return this.OnProcessCheckExistence();

                case State.Create:
                    return this.OnProcessCreate();

                case State.AssignProperties:
                    return this.OnProcessAssignProperties();

                case State.UpdateProperties:
                    return this.OnProcessUpdateProperties();

                case State.Finished:
                    return false;
                default:
                    throw new NotSupportedException();
            }
        }

        protected virtual void CleanUpCsomObjectsFromPreviousRequest()
        {
            this.exceptionHandlingScope = null;
        }

        public virtual void NotifyQueryExecuted()
        {
            // Don't forget to update CheckForUnimplementedSyncActions() if this changes
            switch (this.state)
            {
                case State.CheckExistence:
                    this.existence = this.DetermineExistence();

                    if (this.existence == TaxonomyItemExistence.Missing)
                    {
                        this.AssignClientChildItems();
                        this.NotifyWaiters();

                        switch (this.SyncAction.IfMissing)
                        {
                            case SyncActionIfMissing.Create:
                                this.state = State.Create;
                                break;
                            case SyncActionIfMissing.Error:
                                throw new InvalidOperationException("The item is missing, and IfMissing=Error was specified:\r\n"
                                    + this.LocalTaxonomyItem.ToString());
                            case SyncActionIfMissing.DoNothing:
                            default:
                                throw new NotImplementedException("Not implemented yet: IfMissing=" +
                                    this.SyncAction.IfMissing);
                        }
                    }
                    else if (this.existence == TaxonomyItemExistence.Present)
                    {
                        this.AssignClientChildItems();
                        this.NotifyWaiters();

                        switch (this.SyncAction.IfPresent)
                        {
                            case SyncActionIfPresent.Update:
                                this.state = State.UpdateProperties;
                                break;
                            case SyncActionIfPresent.OnlyUpdateChildItems:
                                this.state = State.Finished;
                                break;
                            case SyncActionIfPresent.Error:
                                throw new InvalidOperationException("The item is present, and IfPresent=Error was specified:\r\n"
                                    + this.LocalTaxonomyItem.ToString());
                            case SyncActionIfPresent.DeleteAndRecreate:
                            case SyncActionIfPresent.DoNothing:
                            default:
                                throw new NotImplementedException("Not implemented yet: IfPresent=" +
                                    this.SyncAction.IfPresent);
                        }
                    }
                    else
                    {
                        Debug.Assert(this.existence == TaxonomyItemExistence.Elsewhere);

                        switch (this.SyncAction.IfElsewhere)
                        {
                            case SyncActionIfElsewhere.Error:
                                throw new InvalidOperationException("The item was found in the wrong location,"
                                    + " and IfElsewhere=Error was specified:\r\n"
                                    + this.LocalTaxonomyItem.ToString());
                            case SyncActionIfElsewhere.MoveAndUpdate:
                            case SyncActionIfElsewhere.DeleteAndRecreate:
                            case SyncActionIfElsewhere.DoNothing:
                            default:
                                throw new NotImplementedException("Not implemented yet: IfElsewhere=" +
                                    this.SyncAction.IfElsewhere);
                        }
                    }
                    break;

                case State.Create:
                    if (this.exceptionHandlingScope != null && this.exceptionHandlingScope.HasException)
                    {
                        throw new InvalidOperationException("Failed to create object "
                            + this.LocalTaxonomyItem.ToString() + ":\r\n"
                            + this.exceptionHandlingScope.ServerErrorTypeName
                            + ": " + this.exceptionHandlingScope.ErrorMessage);
                    }

                    this.NotifyWaiters();
                    this.state = State.AssignProperties;
                    break;

                case State.AssignProperties:
                    this.state = State.Finished;
                    break;

                case State.UpdateProperties:
                    this.state = State.Finished;
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private TaxonomyItemExistence DetermineExistence()
        {
            TaxonomyItemExistence existence = TaxonomyItemExistence.Unknown;

            if (this.exceptionHandlingScope != null && this.exceptionHandlingScope.HasException)
            {
                if (this.exceptionHandlingScope.ServerErrorTypeName != "System.ArgumentOutOfRangeException")
                    throw new InvalidOperationException("Server error: " + this.exceptionHandlingScope.ErrorMessage);

                existence = TaxonomyItemExistence.Missing;
            }
            else if (this.ClientTaxonomyItem.ServerObjectIsNull.Value == true) // nullable comparison
            {
                existence = TaxonomyItemExistence.Missing;
            }
            else
            {
                if (this.FindByName)
                {
                    existence = TaxonomyItemExistence.Present;
                }
                else if (this.IsPlacedCorrectly())
                {
                    existence = TaxonomyItemExistence.Present;
                }
                else
                {
                    existence = TaxonomyItemExistence.Elsewhere;
                }
            }
            return existence;
        }

        protected ConditionalScope UpdateIfChanged(Expression<Func<bool>> comparison, Action assignment)
        {
            ConditionalScope scope = new ConditionalScope(this.ClientContext,
                comparison, allowAllActions: true);
            using (scope.StartScope())
            {
                using (scope.StartIfTrue())
                {
                    assignment();
                }
            }
            return scope;
        }

        protected void SetClientWorkingLanguageToDefault()
        {
            this.Controller.ClientConnector.WorkingLanguageManager
                .SetWorkingLanguageForTermStore(this.ClientTermStore, this.Controller.DefaultLanguageLcid);
        }

        public override string ToString()
        {
            if (this.LocalTaxonomyItem != null)
                return "Uploader: " + this.LocalTaxonomyItem.ToString();
            return "Uploader: " + this.GetType().Name;
        }
    }
}
