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
using System.ComponentModel;
using System.Windows.Forms;
using TaxonomyToolkit.Taxml;

namespace TaxonomyToolkit.TaxmlManager
{
    public partial class TermSetListView : UserControl
    {
        private LocalTermStore termStore;
        private bool showCheckBoxes = false;

        public TermSetListView()
        {
            this.InitializeComponent();

            this.UpdateUI();
        }

        [DefaultValue(false)]
        public bool ShowCheckboxes
        {
            get { return this.showCheckBoxes; }
            set
            {
                if (this.showCheckBoxes == value)
                    return;
                this.showCheckBoxes = value;
                this.UpdateUI();
            }
        }

        private void UpdateUI()
        {
            if (this.showCheckBoxes)
            {
                this.ctlListView.CheckBoxes = true;
                this.ctlListView.SmallImageList = null;
            }
            else
            {
                this.ctlListView.CheckBoxes = false;
                this.ctlListView.SmallImageList = this.ctlAppIcons.ImageList;
            }
        }

        [Browsable(false)]
        public LocalTermStore TermStore
        {
            get { return this.termStore; }
            set
            {
                this.termStore = value;
                this.RebuildList();
            }
        }

        [Browsable(false)]
        public LocalTermSet FocusedTermSet
        {
            get
            {
                ListViewItem focusedItem = this.ctlListView.FocusedItem;
                if (focusedItem == null)
                    return null;

                return focusedItem.Tag as LocalTermSet;
            }
        }

        [Browsable(false)]
        public IList<LocalTermSet> SelectedTermSets
        {
            get
            {
                List<LocalTermSet> list = new List<LocalTermSet>(this.ctlListView.SelectedItems.Count);
                foreach (ListViewItem item in this.ctlListView.SelectedItems)
                {
                    LocalTermSet selectedTermSet = item.Tag as LocalTermSet;
                    if (selectedTermSet != null)
                        list.Add(selectedTermSet);
                }
                return list;
            }
        }

        private void RebuildList()
        {
            this.ctlListView.BeginUpdate();
            try
            {
                this.ctlListView.Groups.Clear();
                this.ctlListView.Items.Clear();
                if (this.termStore != null)
                {
                    foreach (LocalTermGroup termGroup in this.termStore.TermGroups)
                    {
                        ListViewItem groupItem = this.ctlListView.Items.Add("(group)");
                        groupItem.Tag = termGroup;
                        groupItem.ImageKey = AppIcons.Keys.TermGroup;
                        groupItem.SubItems.Add(termGroup.Name ?? "(Untitled)");

                        foreach (LocalTermSet termSet in termGroup.TermSets)
                        {
                            ListViewItem termSetItem = this.ctlListView.Items.Add(termSet.Name);
                            termSetItem.Tag = termSet;
                            termSetItem.ImageKey = AppIcons.Keys.TermSet;
                            termSetItem.SubItems.Add(termGroup.Name ?? "(Untitled)");
                        }
                    }
                }
            }
            finally
            {
                this.ctlListView.EndUpdate();
            }
        }
    }
}
