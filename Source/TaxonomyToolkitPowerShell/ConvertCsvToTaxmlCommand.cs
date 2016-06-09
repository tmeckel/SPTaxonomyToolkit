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

using System.IO;
using System.Management.Automation;
using TaxonomyToolkit.Taxml;

namespace TaxonomyToolkit.PowerShell
{
    [Cmdlet(VerbsData.Convert, "CsvToTaxml")]
    public class ConvertCsvToTaxmlCommand : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string CsvPath { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string TaxmlPath { get; set; }

        protected AppLog AppLog;

        public ConvertCsvToTaxmlCommand()
        {
            this.AppLog = new AppLog(this);
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            ExportImportHelpers.WriteStartupBanner(this.AppLog);

            string inputFilename = ExportImportHelpers.GetAbsolutePath(this.CsvPath, this.SessionState);
            string outputFilename = ExportImportHelpers.GetAbsolutePath(this.TaxmlPath, this.SessionState);

            // Validate parameters
            if (!File.Exists(inputFilename))
            {
                if (!File.Exists(inputFilename))
                {
                    throw new FileNotFoundException("The CSV input file does not exist: " + inputFilename);
                }
            }

            this.AppLog.WriteInfo("Reading CSV input from " + inputFilename);
            SharePointCsvLoader loader = new SharePointCsvLoader();
            LocalTermStore termStore = loader.LoadFromFile(inputFilename);

            this.AppLog.WriteInfo("Writing output to " + outputFilename);
            TaxmlSaver taxmlSaver = new TaxmlSaver();
            taxmlSaver.SaveToFile(outputFilename, termStore);

            this.AppLog.WriteLine();
            this.AppLog.WriteInfo("The operation completed successfully.");
            this.AppLog.WriteLine();
        }
    }
}
