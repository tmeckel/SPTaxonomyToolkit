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

using System.Diagnostics;
using System.Management.Automation;

namespace TaxonomyToolkit.PowerShell
{
    public class AppLog
    {
        private PSCmdlet cmdlet;

        public AppLog(PSCmdlet cmdlet)
        {
            this.cmdlet = cmdlet;
        }

        public void WriteInfo(string message)
        {
            Debug.WriteLine(message);
            this.cmdlet.WriteObject(message);
        }

        public void WriteInfo(string format, params object[] args)
        {
            this.WriteInfo(string.Format(format, args));
        }

        public void WriteVerbose(string message)
        {
            Debug.WriteLine(message);
            this.cmdlet.WriteVerbose(message);
        }

        public void WriteVerbose(string format, params object[] args)
        {
            this.WriteVerbose(string.Format(format, args));
        }

        public void WriteLine()
        {
            this.WriteInfo("");
        }

        public bool IsVerbose
        {
            get
            {
                object value;
                if (this.cmdlet.MyInvocation.BoundParameters.TryGetValue("verbose", out value))
                {
                    var switchParameter = (SwitchParameter) value;
                    return switchParameter.IsPresent;
                }

                return false;
            }
        }
    }
}
