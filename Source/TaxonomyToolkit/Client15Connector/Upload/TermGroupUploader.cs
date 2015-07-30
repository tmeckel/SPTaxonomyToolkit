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
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;

namespace TaxonomyToolkit.Taxml
{
    internal class TermGroupUploader : TaxonomyItemUploader
    {
        private LocalTermGroup localTermGroup;
        private TermGroup clientTermGroup;

        private List<TermSet> clientChildTermSets = null;

        public TermGroupUploader(LocalTermGroup localTermGroup, UploadController controller)
            : base(controller)
        {
            this.localTermGroup = localTermGroup;
        }

        public override LocalTaxonomyItem LocalTaxonomyItem
        {
            get { return this.localTermGroup; }
        }

        public override TaxonomyItem ClientTaxonomyItem
        {
            get { return this.clientTermGroup; }
        }

        public TermGroup ClientTermGroup
        {
            get { return this.clientTermGroup; }
        }

        public List<TermSet> ClientChildTermSets
        {
            get { return this.clientChildTermSets; }
        }

        protected override bool OnProcessCheckExistence()
        {
            this.clientTermGroup = null;

            this.exceptionHandlingScope = new ExceptionHandlingScope(this.ClientContext);

            using (this.exceptionHandlingScope.StartScope())
            {
                using (this.exceptionHandlingScope.StartTry())
                {
                    TermStore clientTermStore = this.ClientTermStore;
                    CsomHelpers.FlushCachedProperties(clientTermStore.Groups);
                    if (this.FindByName)
                    {
                        this.clientTermGroup = clientTermStore.Groups
                            .GetByName(this.localTermGroup.Name);
                    }
                    else
                    {
                        this.clientTermGroup = clientTermStore.Groups
                            .GetById(this.localTermGroup.Id);
                    }

                    // TODO: If SyncAction.DeleteExtraChildItems==false, then we can skip this
                    this.ClientContext.Load(this.clientTermGroup,
                        g => g.Id,
                        g => g.Name,

                        // For AssignClientChildItems()
                        // TODO: We can sometimes skip this
                        g => g.TermSets.Include(ts => ts.Id)
                        );
                }
                using (this.exceptionHandlingScope.StartCatch())
                {
                }
            }
            return true;
        }

        protected override void AssignClientChildItems()
        {
            if (this.Existence == TaxonomyItemExistence.Missing)
            {
                this.clientChildTermSets = new List<TermSet>();
            }
            else
            {
                this.clientChildTermSets = this.ClientTermGroup.TermSets.ToList();
            }
        }

        protected override bool IsPlacedCorrectly()
        {
            // Groups can't be "elsewhere"
            return true;
        }

        protected override bool OnProcessCreate()
        {
            var clientTermStore = this.ClientTermStore;
            Guid id = this.localTermGroup.Id;
            if (id == Guid.Empty)
                id = this.Controller.ClientConnector.GetNewGuid();
            this.clientTermGroup = clientTermStore.CreateGroup(this.localTermGroup.Name, id);

            this.ClientContext.Load(this.clientTermGroup,
                ts => ts.Id,
                ts => ts.Name
                );

            return true;
        }

        protected override bool OnProcessAssignProperties()
        {
            this.clientTermGroup.Description = this.localTermGroup.Description;
            return true;
        }

        protected override bool OnProcessUpdateProperties()
        {
            string name = this.localTermGroup.Name;
            this.UpdateIfChanged(
                () => this.clientTermGroup.Name != name,
                () => this.clientTermGroup.Name = name
                );

            string description = this.localTermGroup.Description;
            this.UpdateIfChanged(
                () => this.clientTermGroup.Description != description,
                () => this.clientTermGroup.Description = description
                );

            return true;
        }
    }
}
