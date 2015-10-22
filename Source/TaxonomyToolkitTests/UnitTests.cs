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
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TaxonomyToolkit.Taxml;

namespace TaxonomyToolkitTests
{
    [TestClass]
    public class UnitTests
    {
        #region Test Framework

        private const string resourceManagerFilename = @"..\..\UnitTests.xml";

        private static TestDataFile resourceManager;

        private static bool ShouldOverwriteOutput = false;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            UnitTests.resourceManager = new TestDataFile(UnitTests.resourceManagerFilename);
#if OVERWRITE_OUTPUT
            ShouldOverwriteOutput = true;
#endif
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (UnitTests.ShouldOverwriteOutput)
            {
                UnitTests.resourceManager.SaveToXml(UnitTests.resourceManagerFilename);
            }
        }

        private void AssertXmlMatchesResource(string xmlToCompare, string resourceName,
            [CallerMemberName] string testGroupName = "")
        {
            if (UnitTests.ShouldOverwriteOutput)
            {
                UnitTests.resourceManager.SetXml(testGroupName, resourceName, xmlToCompare);
            }
            else
            {
                Assert.IsTrue(UnitTests.resourceManager.CompareXml(testGroupName, resourceName, xmlToCompare));
            }
        }

        private string GetTestResource(string resourceName, [CallerMemberName] string testGroupName = "")
        {
            return UnitTests.resourceManager.GetXml(testGroupName, resourceName);
        }

        private void RewriteTaxmlAndAssertOutput(string inputResourceName, string outputResourceName,
            [CallerMemberName] string testGroupName = "")
        {
            string inputTaxmlString = this.GetTestResource(inputResourceName, testGroupName);
            LocalTermStore termStore = TaxmlLoader.LoadFromString(inputTaxmlString);
            string outputTaxmlString = TaxmlSaver.SaveToString(termStore);
            this.AssertXmlMatchesResource(outputTaxmlString, outputResourceName, testGroupName);
        }

        #endregion

        [TestMethod]
        public void TaxmlRoundtripSyntax()
        {
            this.RewriteTaxmlAndAssertOutput("InputAndOutput", "InputAndOutput");
        }

        [TestMethod]
        public void TaxmlPreservedComments()
        {
            this.RewriteTaxmlAndAssertOutput("Input", "Output");
        }

        [TestMethod]
        public void SyncActionEnums()
        {
            this.RewriteTaxmlAndAssertOutput("Input", "Output");
        }

        [TestMethod]
        public void CreateObjectsWithoutId()
        {
            var termStore = new LocalTermStore(Guid.Empty, "Service");
            var termGroup = termStore.AddTermGroup(Guid.Empty, "Test Group");
            var termSet = termGroup.AddTermSet(Guid.Empty, "Test TermSet");
            var term = termSet.AddTerm(Guid.Empty, "Test Term");

            var reusedTerm = termSet.AddTermLinkUsingPath("My Group;My TermSet;MyTerm");
            Assert.AreEqual(reusedTerm.TermLinkNameHint, "MyTerm");

            string taxml = TaxmlSaver.SaveToString(termStore);
            this.AssertXmlMatchesResource(taxml, "ExpectedOutput");
        }

        [TestMethod]
        public void CreateObjectsWithId()
        {
            var termStore = new LocalTermStore(new Guid("11111111-2222-3333-4444-000000000001"), "Service");
            var termGroup = termStore.AddTermGroup(new Guid("11111111-2222-3333-4444-000000000002"), "Test Group");
            var termSet = termGroup.AddTermSet(new Guid("11111111-2222-3333-4444-000000000003"), "Test TermSet");
            var term = termSet.AddTerm(new Guid("11111111-2222-3333-4444-000000000004"), "Test Term");

            var reusedTerm = termSet.AddTermLinkUsingId(new Guid("11111111-2222-3333-4444-000000000005"), "NameHint");
            Assert.AreEqual(reusedTerm.TermLinkNameHint, "NameHint");

            string taxml = TaxmlSaver.SaveToString(termStore);
            this.AssertXmlMatchesResource(taxml, "ExpectedOutput");
        }

        [TestMethod]
        public void CustomSortOrderParsing()
        {
            var localTerm = LocalTerm.CreateTerm(new Guid("11111111-2222-3333-4444-000000000001"), "Term", 1033);
            localTerm.CustomSortOrder.AsText =
                " 11111111-2222-3333-4444-000000000001"
                    + ":11111111-2222-3333-4444-000000000002"
                    + ":11111111-2222-3333-4444-000000000003 ";
            localTerm.CustomSortOrder.Insert(0, new Guid("11111111-2222-3333-4444-000000000004"));

            Assert.AreEqual(localTerm.CustomSortOrder.Count, 4);

            Assert.AreEqual(localTerm.CustomSortOrder.AsText,
                "11111111-2222-3333-4444-000000000004"
                    + ":11111111-2222-3333-4444-000000000001"
                    + ":11111111-2222-3333-4444-000000000002"
                    + ":11111111-2222-3333-4444-000000000003"
                );

            localTerm.CustomSortOrder.Clear();

            Assert.AreEqual(localTerm.CustomSortOrder.AsText, "");
        }

        [TestMethod]
        public void CustomSortOrderParsingForServer()
        {
            var localTerm = LocalTerm.CreateTerm(new Guid("11111111-2222-3333-4444-000000000001"), "Term", 1033);
            localTerm.CustomSortOrder.AsTextForServer =
                " 11111111-2222-3333-4444-000000000001"
                    + ":11111111-2222-3333-4444-000000000002"
                    + ":: hello, world!"
                    + ":11111111-2222-3333-4444-000000000003 "
                    + ":11111111-2222-3333-4444-000000000004"
                    + ":11111111-2222-3333-4444-000000000003 ";
            localTerm.CustomSortOrder.Insert(0, new Guid("11111111-2222-3333-4444-000000000005"));

            Assert.AreEqual(localTerm.CustomSortOrder.Count, 5);

            Assert.AreEqual(localTerm.CustomSortOrder.AsText,
                "11111111-2222-3333-4444-000000000005"
                    + ":11111111-2222-3333-4444-000000000001"
                    + ":11111111-2222-3333-4444-000000000002"
                    + ":11111111-2222-3333-4444-000000000003"
                    + ":11111111-2222-3333-4444-000000000004"
                );

            try
            {
                localTerm.CustomSortOrder.Add(new Guid("11111111-2222-3333-4444-000000000005"));
                Assert.Fail("Exception not thrown");
            }
            catch (Exception ex)
            {
                Assert.AreEqual(ex.GetType(), typeof(ArgumentException));
                Assert.AreEqual(localTerm.CustomSortOrder.Count, 5);
            }

            localTerm.CustomSortOrder.Clear();

            Assert.AreEqual(localTerm.CustomSortOrder.AsText, "");
        }

        [TestMethod]
        public void CustomSortOrderNormalization()
        {
            this.RewriteTaxmlAndAssertOutput("Input", "Output");
        }

        [TestMethod]
        public void TaxonomyNameNormalization()
        {
            this.RewriteTaxmlAndAssertOutput("Input", "Output");
        }

        [TestMethod]
        public void TaxmlLoading_V2_1()
        {
            this.RewriteTaxmlAndAssertOutput("Input", "Output");
        }

        [TestMethod]
        public void DefaultLanguageMismatch()
        {
            var guids = new Guid[] {
                new Guid("ff000000-0000-0000-0000-000000000000"),
                new Guid("ff000000-0000-0000-0000-000000000001")
            };

            LocalTerm term0 = LocalTerm.CreateTerm(guids[0], "Name", 1030);
            LocalTerm term1 = LocalTerm.CreateTerm(guids[1], "Name", 1031);
            Assert.AreEqual(term0.DefaultLanguageLcid, 1030);
            Assert.AreEqual(term1.DefaultLanguageLcid, 1031);

            try
            {
                // should throw because the parent's default language doesn't match
                term1.ParentItem = term0;
                Assert.Fail("Exception not thrown");
            }
            catch (Exception ex)
            {
                Assert.AreEqual(ex.GetType(), typeof(InvalidOperationException));
            }
        }

        [TestMethod]
        public void DefaultLanguageInheritance()
        {
            var guids = new Guid[] {
                new Guid("ff000000-0000-0000-0000-000000000000"),
                new Guid("ff000000-0000-0000-0000-000000000001"),
                new Guid("ff000000-0000-0000-0000-000000000002"),
                new Guid("ff000000-0000-0000-0000-000000000003"),
                new Guid("ff000000-0000-0000-0000-000000000004")
            };

            var termStore = new LocalTermStore(guids[0], "MMS", 1030);
            var termGroup = termStore.AddTermGroup(guids[1], "Group");
            var termSet = termGroup.AddTermSet(guids[2], "TermSet");
            var term0 = termSet.AddTerm(guids[3], "term0");
            var term1 = term0.AddTerm(guids[4], "term1");

            Assert.AreEqual(termStore.DefaultLanguageLcid, 1030);
            Assert.AreEqual(termGroup.DefaultLanguageLcid, 1030);
            Assert.AreEqual(termSet.DefaultLanguageLcid, 1030);
            Assert.AreEqual(term0.DefaultLanguageLcid, 1030);
            Assert.AreEqual(term1.DefaultLanguageLcid, 1030);

            try
            {
                // cannot change termSet's default language because the
                // value is being inherited from the parent
                termSet.DefaultLanguageLcid = 1029;
                Assert.Fail("Exception not thrown");
            }
            catch (Exception ex)
            {
                Assert.AreEqual(ex.GetType(), typeof(InvalidOperationException));
            } 


            termStore.DefaultLanguageLcid = 1031;

            Assert.AreEqual(termStore.DefaultLanguageLcid, 1031);
            Assert.AreEqual(termGroup.DefaultLanguageLcid, 1031);
            Assert.AreEqual(termSet.DefaultLanguageLcid, 1031);
            Assert.AreEqual(term0.DefaultLanguageLcid, 1031);
            Assert.AreEqual(term1.DefaultLanguageLcid, 1031);

            termSet.ParentItem = null;
            termSet.DefaultLanguageLcid = 1032;

            Assert.AreEqual(termStore.DefaultLanguageLcid, 1031);
            Assert.AreEqual(termGroup.DefaultLanguageLcid, 1031);
            Assert.AreEqual(termSet.DefaultLanguageLcid, 1032);
            Assert.AreEqual(term0.DefaultLanguageLcid, 1032);
            Assert.AreEqual(term1.DefaultLanguageLcid, 1032);
        }
    }
}
