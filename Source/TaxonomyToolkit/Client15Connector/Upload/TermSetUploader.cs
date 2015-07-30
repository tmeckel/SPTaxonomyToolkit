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
    internal class TermSetUploader : TermContainerUploader
    {
        private LocalTermSet localTermSet;
        private TermSet clientTermSet;
        private List<TermSetLocalizedNameQuery> localizedNameQueries = null;

        public TermSetUploader(LocalTermSet localTermSet, UploadController controller)
            : base(controller)
        {
            this.localTermSet = localTermSet;
        }

        public override LocalTaxonomyItem LocalTaxonomyItem
        {
            get { return this.localTermSet; }
        }

        public override LocalTermContainer LocalTermContainer
        {
            get { return this.localTermSet; }
        }

        public LocalTermSet LocalTermSet
        {
            get { return this.localTermSet; }
        }

        public override TaxonomyItem ClientTaxonomyItem
        {
            get { return this.clientTermSet; }
        }

        public override TermSetItem ClientTermContainer
        {
            get { return this.clientTermSet; }
        }

        public TermSet ClientTermSet
        {
            get { return this.clientTermSet; }
        }

        protected override bool OnProcessCheckExistence()
        {
            this.clientTermSet = null;
            this.localizedNameQueries = null;

            TermGroupUploader groupUploader = (TermGroupUploader) this.GetParentUploader();
            if (!groupUploader.FoundClientObject)
            {
                this.WaitForBlocker(groupUploader);
                return false;
            }
            Debug.Assert(groupUploader.ClientTermGroup != null);

            this.exceptionHandlingScope = new ExceptionHandlingScope(this.ClientContext);
            using (this.exceptionHandlingScope.StartScope())
            {
                using (this.exceptionHandlingScope.StartTry())
                {
                    if (this.FindByName)
                    {
                        CsomHelpers.FlushCachedProperties(groupUploader.ClientTermGroup.TermSets);
                        this.clientTermSet = groupUploader.ClientTermGroup.TermSets
                            .GetByName(this.localTermSet.Name);
                    }
                    else
                    {
                        // TODO: If "elsewhere" isn't needed, then we
                        // can get this directly from groupUploader.ClientChildTermSets

                        CsomHelpers.FlushCachedProperties(this.ClientTermStore);
                        this.clientTermSet = this.ClientTermStore
                            .GetTermSet(this.localTermSet.Id);
                    }

                    var conditionalScope = new ConditionalScope(this.ClientContext,
                        () => this.clientTermSet.ServerObjectIsNull.Value,
                        allowAllActions: true);

                    using (conditionalScope.StartScope())
                    {
                        using (conditionalScope.StartIfFalse())
                        {
                            // TODO: If SyncAction.DeleteExtraChildItems==false, then we can skip this
                            this.ClientContext.Load(this.clientTermSet,
                                ts => ts.Id,
                                ts => ts.Name,
                                ts => ts.Group.Id,

                                ts => ts.CustomProperties,
                                ts => ts.Stakeholders,

                                // For AssignClientChildItems()
                                // TODO: We can sometimes skip this
                                ts => ts.Terms.Include(t => t.Id)
                                );

                            this.localizedNameQueries = TermSetLocalizedNameQuery.Load(
                                this.clientTermSet,
                                this.Controller.ClientLcids,
                                this.Controller.DefaultLanguageLcid,
                                this.ClientTermStore,
                                this.ClientContext
                                );
                        }
                    }
                }
                using (this.exceptionHandlingScope.StartCatch())
                {
                }
            }
            return true;
        }

        protected override bool IsPlacedCorrectly()
        {
            TermGroupUploader groupUploader = (TermGroupUploader) this.GetParentUploader();
            return this.clientTermSet.Group.Id == groupUploader.ClientTermGroup.Id;
        }

        protected override bool OnProcessCreate()
        {
            TermGroupUploader groupUploader = (TermGroupUploader) this.GetParentUploader();
            Debug.Assert(groupUploader.FoundClientObject && groupUploader.ClientTermGroup != null);

            Guid id = this.localTermSet.Id;
            if (id == Guid.Empty)
                id = this.Controller.ClientConnector.GetNewGuid();

            this.clientTermSet = groupUploader.ClientTermGroup.CreateTermSet(this.localTermSet.Name, id,
                this.Controller.DefaultLanguageLcid);

            this.ClientContext.Load(this.clientTermSet,
                ts => ts.Id,
                ts => ts.Name
                );
            return true;
        }

        // These property assignments follow the same order as TermSetDownloader.AssignExtendedProperties()
        protected override bool OnProcessAssignProperties()
        {
            this.clientTermSet.Contact = this.localTermSet.Contact;
            this.clientTermSet.CustomSortOrder = this.localTermSet.CustomSortOrder.AsText;
            this.clientTermSet.Description = this.localTermSet.Description;
            this.clientTermSet.IsAvailableForTagging = this.localTermSet.IsAvailableForTagging;
            this.clientTermSet.IsOpenForTermCreation = this.localTermSet.IsOpenForTermCreation;
            this.clientTermSet.Owner = this.localTermSet.Owner;

            bool changedWorkingLanguage = false;
            foreach (var localizedName in this.localTermSet.LocalizedNames)
            {
                // This should have already been set when we created the term set
                if (localizedName.Lcid == this.Controller.DefaultLanguageLcid)
                    continue;
                if (!this.Controller.ClientLcids.Contains(localizedName.Lcid))
                    continue;
                this.ClientTermStore.WorkingLanguage = localizedName.Lcid;
                changedWorkingLanguage = true;
                this.clientTermSet.Name = localizedName.Value;
            }
            if (changedWorkingLanguage)
                this.ClientTermStore.WorkingLanguage = this.Controller.DefaultLanguageLcid;

            this.clientTermSet.DeleteAllCustomProperties();
            foreach (var pair in this.localTermSet.CustomProperties)
                this.clientTermSet.SetCustomProperty(pair.Key, pair.Value);

            foreach (string stakeholder in this.localTermSet.Stakeholders)
            {
                this.clientTermSet.AddStakeholder(stakeholder);
            }

            return true;
        }

        protected override bool OnProcessUpdateProperties()
        {
            // (localized names handled below)
            string nameWithDefaultLcid = this.localTermSet.Name;
            this.UpdateIfChanged(
                () => this.clientTermSet.Name != nameWithDefaultLcid,
                () => this.clientTermSet.Name = nameWithDefaultLcid
                );

            string contact = this.localTermSet.Contact;
            this.UpdateIfChanged(
                () => this.clientTermSet.Contact != contact,
                () => this.clientTermSet.Contact = contact
                );

            string customSortOrder = this.localTermSet.CustomSortOrder.AsTextForCsom;
            this.UpdateIfChanged(
                () => this.clientTermSet.CustomSortOrder != customSortOrder,
                () => this.clientTermSet.CustomSortOrder = customSortOrder
                );

            string description = this.localTermSet.Description;
            this.UpdateIfChanged(
                () => this.clientTermSet.Description != description,
                () => this.clientTermSet.Description = description
                );

            bool isAvailableForTagging = this.localTermSet.IsAvailableForTagging;
            this.UpdateIfChanged(
                () => this.clientTermSet.IsAvailableForTagging != isAvailableForTagging,
                () => this.clientTermSet.IsAvailableForTagging = isAvailableForTagging
                );

            bool isOpenForTermCreation = this.localTermSet.IsOpenForTermCreation;
            this.UpdateIfChanged(
                () => this.clientTermSet.IsOpenForTermCreation != isOpenForTermCreation,
                () => this.clientTermSet.IsOpenForTermCreation = isOpenForTermCreation
                );

            string owner = this.localTermSet.Owner;
            this.UpdateIfChanged(
                () => this.clientTermSet.Owner != owner,
                () => this.clientTermSet.Owner = owner
                );

            bool changedWorkingLanguage = false;
            foreach (var localizedNameQuery in this.localizedNameQueries)
            {
                string localizedName = this.localTermSet.GetNameWithDefault(localizedNameQuery.Lcid);
                if (localizedNameQuery.Name != localizedName)
                {
                    this.ClientTermStore.WorkingLanguage = localizedNameQuery.Lcid;
                    changedWorkingLanguage = true;
                    this.clientTermSet.Name = localizedName;
                }
            }
            if (changedWorkingLanguage)
                this.ClientTermStore.WorkingLanguage = this.Controller.DefaultLanguageLcid;

            TermContainerUploader.UpdatePropertyBag(
                this.clientTermSet.CustomProperties,
                (name) => this.clientTermSet.DeleteCustomProperty(name),
                (name, value) => this.clientTermSet.SetCustomProperty(name, value),
                this.localTermSet.CustomProperties);

            this.UpdateStakeholders();

            return true;
        }

        private void UpdateStakeholders()
        {
            var localStakeholdersToAdd = new HashSet<string>(this.localTermSet.Stakeholders);

            // Delete the extra stakeholders
            foreach (string stakeholder in this.clientTermSet.Stakeholders)
            {
                if (!localStakeholdersToAdd.Contains(stakeholder))
                {
                    this.clientTermSet.DeleteStakeholder(stakeholder);
                }
                else
                {
                    localStakeholdersToAdd.Remove(stakeholder);
                }
            }

            // Add the missing stakeholders
            foreach (string stakeholder in localStakeholdersToAdd.OrderBy(x => x))
            {
                this.clientTermSet.AddStakeholder(stakeholder);
            }
        }
    }
}
