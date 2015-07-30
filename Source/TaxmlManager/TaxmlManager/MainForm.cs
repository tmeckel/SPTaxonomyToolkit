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
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TaxonomyToolkit.TaxmlManager
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            this.InitializeComponent();

            this.ctlTabControl.TabPages.Clear();
            this.mnuFileNew_Click(null, EventArgs.Empty);

#if DEBUG
            this.ctlOpenFileDialog.InitialDirectory = Path.GetDirectoryName(Application.ExecutablePath);
            this.mnuDebug.Visible = true;
#endif
        }

        #region Properties

        public ReadOnlyCollection<DocumentView> DocumentViews
        {
            get
            {
                List<DocumentView> list = new List<DocumentView>(this.ctlTabControl.TabPages.Count);
                foreach (TabPage tabPage in this.ctlTabControl.TabPages)
                {
                    DocumentView documentView = MainForm.GetDocumentViewFromTab(tabPage);
                    if (documentView != null)
                        list.Add(documentView);
                }

                return new ReadOnlyCollection<DocumentView>(list);
            }
        }

        public DocumentView SelectedDocumentView
        {
            get
            {
                TabPage selectedTabPage = this.ctlTabControl.SelectedTab;
                if (selectedTabPage == null)
                    return null;
                return MainForm.GetDocumentViewFromTab(selectedTabPage);
            }
        }

        #endregion

        private void UpdateUI()
        {
            this.btnCloseTab.Visible = this.ctlTabControl.SelectedTab != null;
        }

        private void OpenFile(string filename)
        {
            // Is the file already open?
            foreach (DocumentView documentView in this.DocumentViews)
            {
                if (string.IsNullOrEmpty(documentView.Filename))
                    continue;

                if (StringComparer.OrdinalIgnoreCase.Equals(
                    Path.GetFullPath(documentView.Filename),
                    Path.GetFullPath(filename)))
                {
                    // Already open
                    this.ctlTabControl.SelectedTab = documentView.TabPage;
                    return;
                }
            }

            DocumentView newDocumentView = new DocumentView();
            newDocumentView.LoadFile(filename);
            this.AddTab(newDocumentView);
        }

        public void AddTab(DocumentView documentView)
        {
            TabPage tabPage = new TabPage();
            documentView.Dock = DockStyle.Fill;
            tabPage.Controls.Add(documentView);
            this.ctlTabControl.TabPages.Insert(0, tabPage);
            this.ctlTabControl.SelectedTab = tabPage;
            documentView.NotifyAdded(tabPage);
            this.UpdateUI();
        }

        private static DocumentView GetDocumentViewFromTab(TabPage tabPage)
        {
            return (DocumentView) tabPage.Controls[0];
        }

        private bool CloseTab(DocumentView documentView)
        {
            if (documentView.Modified)
            {
                this.ctlTabControl.SelectedTab = documentView.TabPage;

                string question;
                if (string.IsNullOrEmpty(documentView.Filename))
                {
                    question = "Save this file?";
                }
                else
                {
                    question = "Save changes to this file?\r\n\r\n" + documentView.Filename;
                }

                switch (MessageBox.Show(question, "Save Changes",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Cancel:
                        return false;
                    case DialogResult.Yes:
                        documentView.SaveFile();
                        break;
                }
            }

            this.ctlTabControl.TabPages.Remove(documentView.TabPage);
            documentView.NotifyRemoved();
            this.UpdateUI();
            return true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            // Ask about the modified files first
            foreach (DocumentView documentView in this.DocumentViews.OrderBy(x => x.Modified ? 0 : 1))
            {
                if (!this.CloseTab(documentView))
                {
                    e.Cancel = true;
                    break;
                }
            }
        }

        #region Form Events

        private void mnuFileNew_Click(object sender, EventArgs e)
        {
            DocumentView documentView = new DocumentView();
            this.AddTab(documentView);
        }

        private void mnuFileOpen_Click(object sender, EventArgs e)
        {
            if (this.ctlOpenFileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            this.OpenFile(this.ctlOpenFileDialog.FileName);
        }

        private void mnuFileSave_Click(object sender, EventArgs e)
        {
            if (this.SelectedDocumentView == null)
                return;

            this.SelectedDocumentView.SaveFile();
        }

        private void mnuFileSaveAs_Click(object sender, EventArgs e)
        {
            DocumentView documentView = this.SelectedDocumentView;
            if (documentView == null)
                return;

            try
            {
                this.ctlSaveFileDialog.FileName = Path.GetFileName(documentView.Filename);
                this.ctlSaveFileDialog.InitialDirectory = Path.GetDirectoryName(documentView.Filename);
            }
            catch {  }

            if (this.ctlSaveFileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            documentView.SaveFile(this.ctlSaveFileDialog.FileName);
        }

        private void btnCloseTab_Click(object sender, EventArgs e)
        {
            this.CloseTab(MainForm.GetDocumentViewFromTab(this.ctlTabControl.SelectedTab));
        }

        private void mnuFileExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void mnuDebugTest_Click(object sender, EventArgs e)
        {
        }

        #endregion
    }
}
