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
using System.Diagnostics;
using System.Linq;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;

namespace TaxonomyToolkit.Taxml
{
    internal class TermUploader : TermContainerUploader
    {
        #region class TermLinkSourcePathPartLookup

        // This is used by the LocalTermKind.TermLinkUsingPath algorithm, which needs
        // walk a path like "Example Group;Example TermSet;Parent Term;Child Term".
        // Each part of the path becomes an item in the termLinkSourcePathPartLookups list.
        // If the object exists somewhere in the LocalTermStore (i.e. ItemIsInLocalTermStore=true),
        // then we can use its uploader to find the ClientObject (which might not exist until
        // we create it).  Otherwise, we have to do a CSOM query to find it.
        private class TermLinkSourcePathPartLookup
        {
            public readonly string PartName;
            public ClientObject ClientObject = null;

            public LocalTaxonomyItem LocalTaxonomyItem = null;
            public TaxonomyItemUploader Uploader = null;

            public TermLinkSourcePathPartLookup(string partName)
            {
                this.PartName = partName;
            }

            // If ItemIsInLocalTermStore=true then we are finding the object
            // via its uploader.  Otherwise we're finding it via a CSOM query,
            // and the LocalTaxonomyItem and Uploader properties will both be null.
            public bool ItemIsInLocalTermStore
            {
                get { return this.LocalTaxonomyItem != null; }
            }
        }

        #endregion

        private LocalTerm localTerm;
        private Term clientTerm;

        // If clientTerm is a TermLink that we need to create by reusing another term instance,
        // then clientTermOtherInstance stores that other term.
        private Term clientTermOtherInstance;

        private Dictionary<int, ClientResult<string>> descriptionResultsByLcid = null;

        // Used only with LocalTermKind.TermLinkUsingPath; see TermLinkSourcePathPartLookup notes above
        private List<TermLinkSourcePathPartLookup> termLinkSourcePathPartLookups = null;
        private ExceptionHandlingScope termLinkSourcePathExceptionHandlingScope = null;

        public TermUploader(LocalTerm localTerm, UploadController controller)
            : base(controller)
        {
            this.localTerm = localTerm;

            if (this.localTerm.IsPinnedRoot)
                throw new NotImplementedException("The IsPinnedRoot attribute is not implemented yet");
        }

        public override LocalTaxonomyItem LocalTaxonomyItem
        {
            get { return this.localTerm; }
        }

        public override LocalTermContainer LocalTermContainer
        {
            get { return this.localTerm; }
        }

        public LocalTerm LocalTerm
        {
            get { return this.localTerm; }
        }

        public override TaxonomyItem ClientTaxonomyItem
        {
            get { return this.clientTerm; }
        }

        public override TermSetItem ClientTermContainer
        {
            get { return this.clientTerm; }
        }

        public Term ClientTerm
        {
            get { return this.clientTerm; }
        }

        protected override bool OnProcessCheckExistence()
        {
            this.clientTerm = null;
            this.clientTermOtherInstance = null;
            this.descriptionResultsByLcid = null;
            this.termLinkSourcePathPartLookups = null;
            this.termLinkSourcePathExceptionHandlingScope = null;

            TermContainerUploader parentUploader = (TermContainerUploader) this.GetParentUploader();
            if (!parentUploader.FoundClientObject)
            {
                this.WaitForBlocker(parentUploader);
                return false;
            }

            TermSetUploader termSetUploader = this.GetTermSetUploader();
            if (!termSetUploader.FoundClientObject)
            {
                this.WaitForBlocker(termSetUploader);
                return false;
            }

            if (this.localTerm.TermKind != LocalTermKind.NormalTerm)
            {
                if (!this.LoadClientTermOtherInstanceForTermLink())
                    return false;
            }

            if (this.FindByName)
            {
                Debug.Assert(this.localTerm.TermKind == LocalTermKind.NormalTerm
                    || this.localTerm.TermKind == LocalTermKind.TermLinkUsingPath);

                // TODO: If "elsewhere" isn't needed, then we
                // can get this directly from parentUploader.ClientChildTerms

                CsomHelpers.FlushCachedProperties(parentUploader.ClientTermContainer.Terms);

                this.exceptionHandlingScope = new ExceptionHandlingScope(this.ClientContext);
                using (this.exceptionHandlingScope.StartScope())
                {
                    using (this.exceptionHandlingScope.StartTry())
                    {
                        this.clientTerm = parentUploader.ClientTermContainer.Terms
                            .GetByName(this.localTerm.Name);

                        this.ClientContext.Load(this.clientTerm,
                            t => t.Id,
                            t => t.Name,
                            t => t.IsReused,
                            t => t.IsSourceTerm,
                            t => t.IsPinned,
                            t => t.IsPinnedRoot,
                            t => t.CustomProperties,
                            t => t.LocalCustomProperties,
                            t => t.Labels.Include(
                                label => label.IsDefaultForLanguage,
                                label => label.Language,
                                label => label.Value
                                ),

                            // For AssignClientChildItems()
                            // TODO: We can sometimes skip this
                            t => t.Terms.Include(ct => ct.Id)
                            );

                        this.descriptionResultsByLcid = new Dictionary<int, ClientResult<string>>();

                        foreach (int lcid in this.Controller.ClientLcids)
                        {
                            CsomHelpers.FlushCachedProperties(this.clientTerm);
                            ClientResult<string> result = this.clientTerm.GetDescription(lcid);
                            this.descriptionResultsByLcid.Add(lcid, result);
                        }
                    }
                    using (this.exceptionHandlingScope.StartCatch())
                    {
                    }
                }
            }
            else
            {
                Debug.Assert(this.localTerm.TermKind == LocalTermKind.NormalTerm
                    || this.localTerm.TermKind == LocalTermKind.TermLinkUsingId);

                // The term/link is considered "missing" unless it's somewhere in the intended term set,
                // since otherwise it's ambiguous which instance should be moved/deleted/etc
                CsomHelpers.FlushCachedProperties(this.ClientTermStore);
                this.clientTerm = termSetUploader.ClientTermSet.GetTerm(this.localTerm.Id);
                this.ClientContext.Load(this.clientTerm,
                    t => t.Id,
                    t => t.Name,
                    t => t.IsReused,
                    t => t.IsSourceTerm,
                    t => t.IsPinned,
                    t => t.IsPinnedRoot,
                    t => t.CustomProperties,
                    t => t.LocalCustomProperties,
                    t => t.Labels.Include(
                        label => label.IsDefaultForLanguage,
                        label => label.Language,
                        label => label.Value
                        ),
                    t => t.Parent.Id,

                    // For AssignClientChildItems()
                    // TODO: We can sometimes skip this
                    t => t.Terms.Include(ct => ct.Id)
                    );

                // If we didn't find it in this term set, then do an expensive query to find
                // all other instances.
                var scope = new ConditionalScope(this.ClientContext,
                    () => this.clientTerm.ServerObjectIsNull.Value,
                    allowAllActions: true);
                using (scope.StartScope())
                {
                    if (this.clientTermOtherInstance == null)
                    {
                        using (scope.StartIfTrue())
                        {
                            CsomHelpers.FlushCachedProperties(this.ClientTermStore);
                            this.clientTermOtherInstance = this.ClientTermStore
                                .GetTerm(this.localTerm.Id);

                            this.ClientContext.Load(this.clientTermOtherInstance,
                                t => t.Id,
                                t => t.Name,
                                t => t.IsReused,
                                t => t.IsSourceTerm,
                                t => t.IsPinned,
                                t => t.IsPinnedRoot
                                );
                        }
                    }

                    using (scope.StartIfFalse())
                    {
                        this.descriptionResultsByLcid = new Dictionary<int, ClientResult<string>>();

                        foreach (int lcid in this.Controller.ClientLcids)
                        {
                            CsomHelpers.FlushCachedProperties(this.clientTerm);
                            ClientResult<string> result = this.clientTerm.GetDescription(lcid);
                            this.descriptionResultsByLcid.Add(lcid, result);
                        }
                    }
                }
            }

            return true;
        }

        // If we are creating a reused instance of a term, this locates the source term
        // (i.e. this.clientTermOtherInstance).
        private bool LoadClientTermOtherInstanceForTermLink()
        {
            if (this.localTerm.TermKind == LocalTermKind.TermLinkUsingId)
            {
                // Is the source term something that we're supposed to be creating in this data set?
                var localTermStore = this.Controller.LocalTermStore;

                var localSourceTerm = this.localTerm.SourceTerm;
                if (localSourceTerm != null)
                {
                    TermUploader sourceTermUploader = (TermUploader) this.Controller.GetUploader(localSourceTerm);
                    if (!sourceTermUploader.FoundClientObject)
                    {
                        this.WaitForBlocker(sourceTermUploader);
                        return false;
                    }

                    this.clientTermOtherInstance = sourceTermUploader.ClientTerm;
                }
            }
            else
            {
                Debug.Assert(this.localTerm.TermKind == LocalTermKind.TermLinkUsingPath);

                if (this.termLinkSourcePathPartLookups == null)
                {
                    this.termLinkSourcePathPartLookups = new List<TermLinkSourcePathPartLookup>();
                    foreach (string partName in this.localTerm.GetTermLinkSourcePathParts())
                    {
                        this.termLinkSourcePathPartLookups.Add(new TermLinkSourcePathPartLookup(partName));
                    }

                    // See how far we can get by matching objects from the LocalTermStore
                    LocalTaxonomyItem localParentItem = this.Controller.LocalTermStore;

                    foreach (TermLinkSourcePathPartLookup lookup in this.termLinkSourcePathPartLookups)
                    {
                        LocalTaxonomyItem localItem = localParentItem.ChildItems
                            .FirstOrDefault(x => x.Name == lookup.PartName);

                        if (localItem != null)
                        {
                            lookup.LocalTaxonomyItem = localItem;
                            Debug.Assert(lookup.ItemIsInLocalTermStore);
                            lookup.Uploader = this.Controller.GetUploader(localItem);
                            localParentItem = localItem;
                        }
                    }
                }

                // Make sure the local parents have been created/found
                foreach (TermLinkSourcePathPartLookup lookup in this.termLinkSourcePathPartLookups)
                {
                    if (!lookup.ItemIsInLocalTermStore)
                        break;

                    if (!lookup.Uploader.FoundClientObject)
                    {
                        this.WaitForBlocker(lookup.Uploader);
                        return false;
                    }

                    lookup.ClientObject = lookup.Uploader.ClientTaxonomyItem;
                    Debug.Assert(lookup.ClientObject != null);
                }

                if (this.termLinkSourcePathPartLookups.Any(x => !x.ItemIsInLocalTermStore))
                {
                    this.termLinkSourcePathExceptionHandlingScope = new ExceptionHandlingScope(this.ClientContext);
                    using (this.termLinkSourcePathExceptionHandlingScope.StartScope())
                    {
                        using (this.termLinkSourcePathExceptionHandlingScope.StartTry())
                        {
                            // For anything else, we need to query it ourselves
                            for (int i = 0; i < this.termLinkSourcePathPartLookups.Count; ++i)
                            {
                                TermLinkSourcePathPartLookup lookup = this.termLinkSourcePathPartLookups[i];
                                if (lookup.ItemIsInLocalTermStore)
                                    continue;

                                // NOTE: For these queries we don't use CsomHelpers.FlushCachedProperties()
                                // because the objects are outside the LocalTermStore, so we don't need to
                                // worry about the cached copies getting invalidated by the uploaders.
                                if (i == 0)
                                {
                                    // Find a Group
                                    lookup.ClientObject =
                                        this.Controller.ClientTermStore.Groups.GetByName(lookup.PartName);
                                }
                                else if (i == 1)
                                {
                                    // Find a TermSet
                                    TermGroup clientGroup =
                                        (TermGroup) this.termLinkSourcePathPartLookups[0].ClientObject;
                                    lookup.ClientObject = clientGroup.TermSets.GetByName(lookup.PartName);
                                }
                                else
                                {
                                    // Find a term
                                    TermSetItem clientTermContainer =
                                        (TermSetItem) this.termLinkSourcePathPartLookups[i - 1].ClientObject;
                                    lookup.ClientObject = clientTermContainer.Terms.GetByName(lookup.PartName);
                                }
                            }
                        }
                        using (this.termLinkSourcePathExceptionHandlingScope.StartCatch())
                        {
                        }
                    }
                }
                this.clientTermOtherInstance = (Term) this.termLinkSourcePathPartLookups.Last().ClientObject;
                Debug.Assert(this.clientTermOtherInstance != null);
            }

            return true;
        }

        protected override bool IsPlacedCorrectly()
        {
            if (this.FindByName)
            {
                Debug.Assert(this.FoundClientObject);

                // When finding by name, the term cannot be "elsewhere"
                return true;
            }
            else
            {
                // Does it have the right parent?
                TermContainerUploader parentUploader = (TermContainerUploader) this.GetParentUploader();

                if (parentUploader.Kind == LocalTaxonomyItemKind.TermSet)
                {
                    // The parent should be the term set, so Term.Parent should be null
                    return this.clientTerm.Parent.ServerObjectIsNull.Value == true;
                }
                else
                {
                    // The parent should be a term
                    if (this.clientTerm.Parent.ServerObjectIsNull.Value == true)
                        return false; // parent is the term set

                    return this.clientTerm.Parent.Id == parentUploader.ClientTermContainer.Id;
                }
            }
        }

        protected override bool OnProcessCreate()
        {
            switch (this.localTerm.TermKind)
            {
                case LocalTermKind.NormalTerm:
                    return this.OnProcessCreateNormalTerm();
                case LocalTermKind.TermLinkUsingId:
                case LocalTermKind.TermLinkUsingPath:
                    return this.OnProcessCreateTermLink();
                default:
                    throw new NotSupportedException();
            }
        }

        private bool OnProcessCreateNormalTerm()
        {
            this.exceptionHandlingScope = new ExceptionHandlingScope(this.ClientContext);
            using (this.exceptionHandlingScope.StartScope())
            {
                using (this.exceptionHandlingScope.StartTry())
                {
                    TermContainerUploader parentUploader = (TermContainerUploader) this.GetParentUploader();

                    Guid id = this.localTerm.Id;
                    if (id == Guid.Empty)
                        id = this.Controller.ClientConnector.GetNewGuid();

                    this.clientTerm = parentUploader.ClientTermContainer.CreateTerm(this.localTerm.Name,
                        this.localTerm.DefaultLanguageLcid, id);

                    this.ClientContext.Load(this.clientTerm,
                        ts => ts.Id,
                        ts => ts.Name
                        );

                    this.ClientTermStore.CommitAll();
                }
                using (this.exceptionHandlingScope.StartCatch())
                {
                }
            }
            return true;
        }

        private bool OnProcessCreateTermLink()
        {
            // Determine the source term
            if (this.clientTermOtherInstance.ServerObjectIsNull.Value)
            {
                throw new InvalidOperationException("Source term not found for " + this.localTerm.ToString());
            }

            this.exceptionHandlingScope = new ExceptionHandlingScope(this.ClientContext);
            using (this.exceptionHandlingScope.StartScope())
            {
                using (this.exceptionHandlingScope.StartTry())
                {
                    TermContainerUploader parentUploader = (TermContainerUploader) this.GetParentUploader();

                    this.clientTerm = parentUploader.ClientTermContainer.ReuseTerm(this.clientTermOtherInstance,
                        reuseBranch: false);

                    this.ClientContext.Load(this.clientTerm,
                        ts => ts.Id,
                        ts => ts.Name
                        );

                    this.ClientTermStore.CommitAll();
                }
                using (this.exceptionHandlingScope.StartCatch())
                {
                }
            }
            return true;
        }

        // These property assignments follow the same order as TermDownloader.AssignExtendedProperties()
        protected override bool OnProcessAssignProperties()
        {
            this.clientTerm.CustomSortOrder = this.localTerm.CustomSortOrder.AsTextForServer;
            this.clientTerm.IsAvailableForTagging = this.localTerm.IsAvailableForTagging;

            // NOTE: If we got here by reusing a term, then some properties will have been copied
            // from the source term.  We need to overwrite everything.
            this.clientTerm.DeleteAllLocalCustomProperties();
            foreach (var pair in this.localTerm.LocalCustomProperties)
                this.clientTerm.SetLocalCustomProperty(pair.Key, pair.Value);

            if (this.localTerm.TermKind == LocalTermKind.NormalTerm)
            {
                this.clientTerm.Deprecate(this.localTerm.IsDeprecated);
                this.clientTerm.Owner = this.localTerm.Owner;

                // NOTE: By contrast, for a normal term we assume that we got here
                // by creating the term, and therefore we don't worry about deleting
                // already existing properties (which would require an extra CSOM query)
                foreach (var pair in this.localTerm.CustomProperties)
                    this.clientTerm.SetCustomProperty(pair.Key, pair.Value);

                foreach (var label in this.localTerm.Labels)
                {
                    // Skip the default label, since CreateTerm() already added it
                    if (label.IsDefault && label.Lcid == this.localTerm.DefaultLanguageLcid)
                    {
                        Debug.Assert(label.Value == this.localTerm.Name);
                        continue;
                    }

                    this.clientTerm.CreateLabel(label.Value, label.Lcid, label.IsDefault);
                }

                foreach (var description in this.localTerm.Descriptions)
                {
                    this.clientTerm.SetDescription(description.Value, description.Lcid);
                }
            }

            return true;
        }

        protected override bool OnProcessUpdateProperties()
        {
            if (this.localTerm.TermKind == LocalTermKind.NormalTerm)
            {
                // If TermKind=NormalTerm, then this instance is intended to be the source term.
                // If it's not, we need to fix that.
                if (!this.clientTerm.IsSourceTerm)
                {
                    this.clientTerm.SourceTerm.ReassignSourceTerm(this.clientTerm);
                }
            }

            string customSortOrder = this.localTerm.CustomSortOrder.AsTextForServer;
            this.UpdateIfChanged(
                () => this.clientTerm.CustomSortOrder != customSortOrder,
                () => this.clientTerm.CustomSortOrder = customSortOrder
                );

            bool isAvailableForTagging = this.localTerm.IsAvailableForTagging;
            this.UpdateIfChanged(
                () => this.clientTerm.IsAvailableForTagging != isAvailableForTagging,
                () => this.clientTerm.IsAvailableForTagging = isAvailableForTagging
                );

            TermContainerUploader.UpdatePropertyBag(
                this.clientTerm.LocalCustomProperties,
                (name) => this.clientTerm.DeleteLocalCustomProperty(name),
                (name, value) => this.clientTerm.SetLocalCustomProperty(name, value),
                this.localTerm.LocalCustomProperties);

            if (this.localTerm.TermKind == LocalTermKind.NormalTerm)
            {
                bool isDeprecated = this.localTerm.IsDeprecated;
                this.UpdateIfChanged(
                    () => this.clientTerm.IsDeprecated != isDeprecated,
                    () => this.clientTerm.Deprecate(isDeprecated)
                    );

                string owner = this.localTerm.Owner;
                this.UpdateIfChanged(
                    () => this.clientTerm.Owner != owner,
                    () => this.clientTerm.Owner = owner
                    );

                TermContainerUploader.UpdatePropertyBag(
                    this.clientTerm.CustomProperties,
                    (name) => this.clientTerm.DeleteCustomProperty(name),
                    (name, value) => this.clientTerm.SetCustomProperty(name, value),
                    this.localTerm.CustomProperties);

                this.UpdateLabels();
                this.UpdateDescriptions();
            }

            return true;
        }

        private void UpdateLabels()
        {
            var localLabelsToAdd = new HashSet<LocalTermLabel>(this.localTerm.Labels);

            // First, determine any extra labels that should be deleted.
            // We defer deleting them because removing/readding the default label
            // is not allowed; the default label must always exist.
            List<Label> labelsToDelete = new List<Label>();

            foreach (var clientLabel in this.clientTerm.Labels)
            {
                var equatableClientLabel = new LocalTermLabel(clientLabel.Language,
                    clientLabel.Value, clientLabel.IsDefaultForLanguage);
                if (!localLabelsToAdd.Contains(equatableClientLabel))
                {
                    labelsToDelete.Add(clientLabel);
                }
                else
                {
                    localLabelsToAdd.Remove(equatableClientLabel);
                }
            }

            // Next add any missing labels
            foreach (var localLabel in localLabelsToAdd.OrderBy(x => x.Value))
            {
                if (this.Controller.ClientLcidsContains(localLabel.Lcid))
                {
                    this.clientTerm.CreateLabel(localLabel.Value, localLabel.Lcid, localLabel.IsDefault);
                }
            }

            // Finally delete the extra labels
            foreach (var labelToDelete in labelsToDelete)
            {
                labelToDelete.DeleteObject();
            }
        }

        private void UpdateDescriptions()
        {
            Debug.Assert(this.descriptionResultsByLcid != null);

            Dictionary<int, string> localDescriptionsByLcid = new Dictionary<int, string>();
            foreach (var description in this.localTerm.Descriptions)
            {
                localDescriptionsByLcid.Add(description.Lcid, description.Value);
            }

            foreach (var pair in this.descriptionResultsByLcid)
            {
                int lcid = pair.Key;
                string clientDescription = pair.Value.Value;

                // This should be true because we queried them based on ClientLcids
                Debug.Assert(this.Controller.ClientLcidsContains(lcid));

                string localDescription;
                if (!localDescriptionsByLcid.TryGetValue(lcid, out localDescription))
                {
                    localDescription = "";
                }

                if (localDescription != clientDescription)
                {
                    this.clientTerm.SetDescription(localDescription, lcid);
                }
            }
        }

        public override void NotifyQueryExecuted()
        {
            if (this.termLinkSourcePathExceptionHandlingScope != null)
            {
                if (this.termLinkSourcePathExceptionHandlingScope.HasException)
                {
                    throw new InvalidOperationException("Unable to find term specified by the TermLinkSourcePath:\r\n"
                        + this.localTerm.ToString());
                }
            }

            base.NotifyQueryExecuted();
        }
    }
}
