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
using System.Windows.Forms;

namespace TaxonomyToolkit.TaxmlManager
{
    public partial class DownloadProgressForm : Form
    {
        private bool canceled = false;
        private Action action;

        public DownloadProgressForm()
        {
            this.InitializeComponent();
        }

        public bool Canceled
        {
            get { return this.canceled; }
        }

        public ProgressBar ProgressBar
        {
            get { return this.ctlProgressBar; }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (this.action != null)
            {
                Action actionCopy = this.action;
                this.action = null;
                actionCopy();
                this.Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.btnCancel.Enabled = false;
            this.canceled = true;
        }

        public DialogResult ShowDialog(Action action)
        {
            this.action = action;
            return this.ShowDialog();
        }
    }
}
