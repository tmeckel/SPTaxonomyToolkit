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

using System.ComponentModel;
using System.Windows.Forms;

namespace TaxonomyToolkit.TaxmlManager
{
    public partial class AppIcons : Component
    {
        public static class Keys
        {
            public const string Error = "Error";

            // Taxonomy icons
            public const string TermStore = "TermStore";
            public const string TermGroup = "TermGroup";
            public const string TermSet = "TermSet";
            public const string TermSetSpecial = "TermSetSpecial";
            public const string Term = "Term";
            public const string TermReused = "TermReused";
        }

        public AppIcons()
        {
            this.InitializeComponent();
        }

        public AppIcons(IContainer container)
        {
            container.Add(this);

            this.InitializeComponent();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ImageList ImageList
        {
            get { return this.ctlIcons; }
        }
    }
}
