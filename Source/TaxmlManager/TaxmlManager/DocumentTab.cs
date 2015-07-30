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
using System.IO;
using System.Windows.Forms;
using TaxonomyToolkit.General;
using TaxonomyToolkit.Taxml;

namespace TaxonomyToolkit.TaxmlManager
{
    public partial class DocumentView : UserControl
    {
        private string filename = "";
        private TabPage tabPage;
        private bool modified = false;

        public DocumentView()
        {
            this.InitializeComponent();
        }

        #region Properties

        public TabPage TabPage
        {
            get { return this.tabPage; }
        }

        public string Filename
        {
            get { return this.filename; }
            set
            {
                ToolkitUtilities.ConfirmNotNull(this.filename, "filename");
                this.filename = value;
                this.UpdateUI();
            }
        }

        public bool Modified
        {
            get { return this.modified; }
            set
            {
                this.modified = value;
                this.UpdateUI();
            }
        }

        public LocalTermStore TermStore
        {
            get { return this.ctlListView.TermStore; }
        }

        #endregion

        internal void NotifyAdded(TabPage tabPage)
        {
            this.tabPage = tabPage;
            this.UpdateUI();
        }

        internal void NotifyRemoved()
        {
        }

        private void UpdateUI()
        {
            if (this.TabPage != null)
            {
                string title;

                if (string.IsNullOrEmpty(this.Filename))
                {
                    title = "Untitled";
                }
                else
                {
                    title = Path.GetFileName(this.Filename);
                }

                if (this.Modified)
                    title += "*";

                this.TabPage.Text = title;
                this.TabPage.ToolTipText = this.Filename;
            }
        }

        public void LoadFile(string filenameToLoad)
        {
            TaxmlLoader loader = new TaxmlLoader();
            LocalTermStore termStore = loader.LoadFromFile(filenameToLoad);
            this.ctlListView.TermStore = termStore;
            this.Filename = filenameToLoad;
            this.Modified = false;
            this.UpdateUI();
        }

        internal void LoadTermStore(LocalTermStore termStore)
        {
            this.ctlListView.TermStore = termStore;
            this.Filename = "Untitled";
            this.Modified = false;
            this.UpdateUI();
        }

        internal void SaveFile(string newFilename = null)
        {
            if (newFilename == null)
                newFilename = this.Filename;

            if (string.IsNullOrWhiteSpace(newFilename))
                throw new InvalidOperationException("Invalid filename");

            TaxmlSaver saver = new TaxmlSaver();
            LocalTermStore termStore = this.ctlListView.TermStore;
            if (termStore == null)
                throw new InvalidOperationException("No term store is loaded");

            saver.SaveToFile(newFilename, termStore);

            this.Filename = newFilename;
            this.Modified = false;
            this.UpdateUI();
        }

        private void mnuContextTest_Click(object sender, EventArgs e)
        {
            this.Modified = true;
        }
    }
}
