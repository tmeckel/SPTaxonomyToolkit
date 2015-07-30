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
    // TODO: Replace this with CredUIPromptForCredentials()
    public partial class LoginForm : Form
    {
        private LoginInfo loginInfo;

        public LoginForm(LoginInfo loginInfo)
        {
            this.loginInfo = loginInfo;

            this.InitializeComponent();

            this.WriteToFormControls();
        }

        private void WriteToFormControls()
        {
            if (this.loginInfo.UserName == "")
            {
                this.radWindowsCredentials.Checked = true;
                this.txtUserName.Text = "";
                this.txtPassword.Text = "";
                this.chkSavePassword.Checked = false;
            }
            else
            {
                this.radSpecifyCredentials.Checked = true;
                this.txtUserName.Text = this.loginInfo.UserName;
                this.txtPassword.Text = this.loginInfo.Password;
                this.chkSavePassword.Checked = this.loginInfo.SavePassword;
            }

            this.UpdateUI();
        }

        private void ReadFromFormControls()
        {
            if (this.radSpecifyCredentials.Checked)
            {
                this.loginInfo.UserName = this.txtUserName.Text;
                this.loginInfo.Password = this.txtPassword.Text;
                this.loginInfo.SavePassword = this.chkSavePassword.Checked;
            }
            else
            {
                this.loginInfo.Reset();
            }
        }

        private void UpdateUI()
        {
            this.txtUserName.Enabled = this.radSpecifyCredentials.Checked;
            this.txtPassword.Enabled = this.radSpecifyCredentials.Checked;
            this.chkSavePassword.Enabled = this.radSpecifyCredentials.Checked;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.ReadFromFormControls();
            this.Close();
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (this.radWindowsCredentials.Checked)
            {
                this.txtUserName.Text = "";
                this.txtPassword.Text = "";
                this.chkSavePassword.Checked = false;
            }

            this.UpdateUI();
        }

        private void chkSavePassword_CheckedChanged(object sender, EventArgs e)
        {
            if (!this.chkSavePassword.Checked)
            {
                // Clear the saved password immediately
                LoginInfo tempLoginInfo = new LoginInfo();
                tempLoginInfo.LoadFromSettings();
                if (tempLoginInfo.Password != "")
                {
                    tempLoginInfo.SavePassword = false;
                    tempLoginInfo.SaveToSettings();
                }

                this.txtPassword.Text = "";
            }

            this.UpdateUI();
        }
    }
}
