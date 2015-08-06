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
using System.Linq;
using System.Security;
using System.Text;
using Microsoft.Win32;
using TaxonomyToolkit.Sync;

namespace TaxonomyToolkit.TaxmlManager
{
    public enum AuthenticationMode
    {
        WindowsCredentials,
        FormsAuthentication,
        CloudAuthentication
    }

    public class LoginInfo
    {
        private const string VOLATILE_REGISTRY_PATH = @"HKEY_CURRENT_USER\Software\Microsoft\TaxmlManager\Volatile";

        public LoginInfo()
        {
            this.Reset();
        }

        public string UserName { get; set; }

        // TODO: Make this a SecureString and replace with CredUIPromptForCredentials()
        public string Password { get; set; }
        public bool SavePassword { get; set; }

        public AuthenticationMode AuthenticationMode
        {
            get
            {
                if (this.UserName == "")
                    return AuthenticationMode.WindowsCredentials;
                if (this.UserName.Contains('@'))
                    return AuthenticationMode.CloudAuthentication;
                return AuthenticationMode.FormsAuthentication;
            }
        }

        public bool ShouldPromptForPassword
        {
            get { return this.Password == ""; }
        }

        public void Reset()
        {
            this.UserName = "";
            this.Password = "";
            this.SavePassword = false;
        }

        public void LoadFromSettings()
        {
            this.Reset();

            this.UserName = Utilities.ReadAppSetting("LoginUserName", "");
            string encodedPassword = Registry.GetValue(LoginInfo.VOLATILE_REGISTRY_PATH,
                "LoginPassword", null) as string ?? "";

            if (encodedPassword != "")
            {
                // Encode the password
                this.Password = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPassword));
                this.SavePassword = true;
            }
        }

        public void SaveToSettings()
        {
            Utilities.WriteAppSetting("LoginUserName", this.UserName);

            // Encode the password
            string encodedPassword = "";

            if (this.SavePassword && this.Password != "")
            {
                encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.Password));
            }

            Registry.SetValue(LoginInfo.VOLATILE_REGISTRY_PATH,
                "LoginPassword", encodedPassword, RegistryValueKind.String);
        }

        public void SetCredentials(Client15Connector connector)
        {
            switch (this.AuthenticationMode)
            {
                case AuthenticationMode.WindowsCredentials:
                    return;
                case AuthenticationMode.FormsAuthentication:
                    connector.SetCredentialsForOnPrem(this.UserName, LoginInfo.MakeSecureString(this.Password));
                    return;
                case AuthenticationMode.CloudAuthentication:
                    connector.SetCredentialsForCloud(this.UserName, LoginInfo.MakeSecureString(this.Password));
                    return;
                default:
                    throw new NotImplementedException("Cloud login not implemented");
            }
        }

        // TODO: Eliminate this by using CredUIPromptForCredentials()
        private static SecureString MakeSecureString(string password)
        {
            SecureString result = new SecureString();
            foreach (char c in password)
                result.AppendChar(c);
            return result;
        }
    }
}
