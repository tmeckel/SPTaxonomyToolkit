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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using System.Xml.Schema;
using TaxonomyToolkit.General;

namespace TaxonomyToolkit.Taxml
{
    /// <summary>
    /// This class reads Taxonomy data from a CSV file conforming to the "ImportTermSet.csv"
    /// template that is used with SharePoint's "Import Term Set" command.  To obtain
    /// this template, go to the Term Store Manager page, select the term store object,
    /// and then click the link for "View a sample import file".
    /// </summary>
    public class SharePointCsvLoader
    {
        public SharePointCsvLoader()
        {
        }

        public LocalTermStore LoadFromFile(string csvFileName)
        {
            LocalTermStore termStore = new LocalTermStore(Guid.NewGuid(), "Term Store");
            LocalTermGroup termGroup = termStore.AddTermGroup(Guid.NewGuid(), Path.GetFileNameWithoutExtension(csvFileName));

            using (CsvReader csvReader = new CsvReader(csvFileName))
            {
                csvReader.WrapExceptions(() =>
                {
                    ProcessCsvLines(termGroup, csvReader);
                });
            }

            return termStore;
        }

        private static void ProcessCsvLines(LocalTermGroup termGroup, CsvReader csvReader)
        {
            int columnTermSetName = csvReader.GetColumnIndex("Term Set Name");
            int columnTermSetDescription = csvReader.GetColumnIndex("Term Set Description");
            int columnLcid = csvReader.GetColumnIndex("LCID");
            int columnAvailableForTagging = csvReader.GetColumnIndex("Available for Tagging");
            int columnTermDescription = csvReader.GetColumnIndex("Term Description");

            int[] columnTermLabels = {
                csvReader.GetColumnIndex("Level 1 Term"),
                csvReader.GetColumnIndex("Level 2 Term"),
                csvReader.GetColumnIndex("Level 3 Term"),
                csvReader.GetColumnIndex("Level 4 Term"),
                csvReader.GetColumnIndex("Level 5 Term"),
                csvReader.GetColumnIndex("Level 6 Term"),
                csvReader.GetColumnIndex("Level 7 Term")
            };

            LocalTermSet termSet = null;
            while (csvReader.ReadNextLine())
            {
                // Is it a blank row?
                if (csvReader.Values.All(x => string.IsNullOrWhiteSpace(x)))
                {
                    // Yes, skip it
                    continue;
                }

                string termSetName = csvReader[columnTermSetName].Trim();
                string termSetDescription = csvReader[columnTermSetDescription].Trim();
                string lcidString = csvReader[columnLcid].Trim();
                int currentLcid;
                if (!int.TryParse(lcidString, out currentLcid))
                    currentLcid = termGroup.DefaultLanguageLcid;
                bool availableForTagging = true;
                if (!string.IsNullOrWhiteSpace(csvReader[columnAvailableForTagging]))
                    availableForTagging = bool.Parse(csvReader[columnAvailableForTagging]);
                string termDescription = csvReader[columnTermDescription].Trim();

                var termLabels = new List<string>();
                for (int i = 0; i < columnTermLabels.Length; ++i)
                {
                    int columnIndex = columnTermLabels[i];
                    if (columnIndex < csvReader.Values.Count)
                    {
                        string value = csvReader[columnIndex].Trim();
                        if (string.IsNullOrEmpty(value))
                            break;
                        termLabels.Add(value);
                    }
                }

                if (!string.IsNullOrEmpty(termSetName))
                {
                    termSet = termGroup.AddTermSet(Guid.NewGuid(), termSetName);
                    termSet.SetName(termSetName, currentLcid);
                    termSet.Description = termSetDescription;
                }

                if (termSet != null)
                {
                    LocalTermContainer parent = termSet;
                    if (termLabels.Count == 0)
                        throw new Exception("Missing term label");

                    foreach (string parentTermLabel in termLabels.Take(termLabels.Count - 1))
                    {
                        parent = parent.Terms
                            .FirstOrDefault(t => t.GetNameWithDefault(currentLcid) == parentTermLabel);
                        if (parent == null)
                        {
                            throw new Exception("No match found for parent term \"" + parentTermLabel + "\"");
                        }
                    }

                    string termLabel = termLabels.Last();
                    LocalTerm term = parent.AddTerm(Guid.NewGuid(), termLabel);
                    term.SetName(termLabel, currentLcid);
                    term.SetDescription(termDescription, currentLcid);
                    term.IsAvailableForTagging = availableForTagging;
                }
            }
        }
    }
}
