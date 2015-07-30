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
using System.Windows.Forms;
using TaxonomyToolkit.Taxml;

namespace TaxonomyToolkit.TaxmlManager
{
    public partial class ServerPane : UserControl
    {
        private LoginInfo loginInfo = new LoginInfo();
        private Client15Connector connector = null;

        public ServerPane()
        {
            this.InitializeComponent();

            this.txtInterface.SelectedIndex = 0;

            this.loginInfo.LoadFromSettings();

            this.LoadSettings();

            this.UpdateUI();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();

                this.Disconnect();
            }
            base.Dispose(disposing);
        }

        public bool IsConnected
        {
            get { return this.connector != null; }
        }

        public MainForm MainForm
        {
            get
            {
                for (Control control = this.Parent; control != null; control = control.Parent)
                {
                    MainForm mainForm = control as MainForm;
                    if (mainForm != null)
                        return mainForm;
                }
                return null;
            }
        }

        private void Disconnect()
        {
            if (this.connector == null)
                return;
            //this.connector.Dispose();
            this.connector = null;
            // (don't call UpdateUI() here because we may be disposing)
        }

        private void UpdateUI()
        {
            this.btnConnectDisconnect.Text = this.IsConnected ? "&Disconnect" : "&Connect";
            this.txtServerUrl.Enabled = !this.IsConnected;
            this.txtInterface.Enabled = !this.IsConnected;
            this.chkSkipLoginPrompt.Enabled = !this.IsConnected && !this.loginInfo.ShouldPromptForPassword;

            this.txtTermStore.Enabled = this.IsConnected;
            this.lblTermStoreManager.Enabled = this.IsConnected;
            this.btnDownload.Enabled = this.IsConnected;
            this.btnUpload.Enabled = this.IsConnected;
        }

        private void LoadSettings()
        {
            this.txtServerUrl.Text = Utilities.ReadAppSetting("ServerUrl", "");
            this.chkSkipLoginPrompt.Checked = Utilities.ReadAppSetting("SkipLoginPrompt", "false") == "true";
        }

        private void SaveSettings()
        {
            Utilities.WriteAppSetting("ServerUrl", this.txtServerUrl.Text);
            Utilities.WriteAppSetting("SkipLoginPrompt", this.chkSkipLoginPrompt.Checked ? "true" : "false");
        }

        private void btnConnectDisconnect_Click(object sender, EventArgs e)
        {
            if (this.IsConnected)
            {
                this.Disconnect();
                this.UpdateUI();
                return;
            }

            if (!this.chkSkipLoginPrompt.Checked)
            {
                using (LoginForm form = new LoginForm(this.loginInfo))
                {
                    if (form.ShowDialog() != DialogResult.OK)
                        return; // don't connect
                }
            }

            try
            {
                this.connector = new Client15Connector(this.txtServerUrl.Text);
                this.loginInfo.SetCredentials(this.connector);

                List<LocalTermStore> termStores = this.connector.FetchTermStores();

                this.txtTermStore.Items.Clear();
                this.txtTermStore.Items.AddRange(termStores.Cast<object>().ToArray());
                this.txtTermStore.SelectedIndex = 0;

                // Only save the credentials if the connection succeeded
                this.SaveSettings();
                this.loginInfo.SaveToSettings();
            }
            catch
            {
                try
                {
                    this.Disconnect();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Discarding exception: " + ex.Message);
                }
                throw;
            }
            finally
            {
                this.UpdateUI();
            }
        }

        private void lblTermStoreManager_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = Utilities.CombineUrl(this.txtServerUrl.Text, "/_layouts/TermStoreManager.aspx");

            LocalTermStore selectedTermStore = this.txtTermStore.SelectedItem as LocalTermStore;
            if (selectedTermStore != null)
            {
                url += "?tsid=" + selectedTermStore.Id.ToString().Replace("-", "");
            }

            Process.Start(url);
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            LocalTermStore selectedTermStore = this.txtTermStore.SelectedItem as LocalTermStore;
            if (selectedTermStore == null)
                throw new InvalidOperationException("No selection");

            MainForm mainForm = this.MainForm;
            if (mainForm == null)
                throw new InvalidOperationException("No main form");

            DocumentView documentView = null;

            using (DownloadTaxonomyForm downloadForm = new DownloadTaxonomyForm(selectedTermStore))
            {
                downloadForm.ShowDialog(this);

                using (DownloadProgressForm progressForm = new DownloadProgressForm())
                {
                    progressForm.ShowDialog(delegate()
                    {
                        // @@ Improve this
                        progressForm.ProgressBar.Style = ProgressBarStyle.Marquee;

                        ClientConnectorDownloadOptions options = downloadForm.GetTaxonomyReadOptions();

                        LocalTermStore termStoreCopy = new LocalTermStore(selectedTermStore.Id, selectedTermStore.Name);
                        this.connector.Download(termStoreCopy, options);

                        documentView = new DocumentView();
                        documentView.LoadTermStore(termStoreCopy);
                    }
                        );
                }
            }

            if (documentView != null)
                mainForm.AddTab(documentView);
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            MainForm mainForm = this.MainForm;
            if (mainForm == null)
                throw new InvalidOperationException("No main form");

            DocumentView documentView = mainForm.SelectedDocumentView;
            if (documentView == null || documentView.TermStore == null)
                throw new InvalidOperationException("No document is selected");

            LocalTermStore selectedTermStore = this.txtTermStore.SelectedItem as LocalTermStore;
            if (selectedTermStore == null)
                throw new InvalidOperationException("No selection");

            this.connector.Upload(documentView.TermStore, selectedTermStore.Id);
        }
    }
}
