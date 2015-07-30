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
using System.Runtime.CompilerServices;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TaxonomyToolkit.Taxml;

namespace TaxonomyToolkitTests
{
    [TestClass]
    public class FunctionalTests
    {
        #region Test Framework

        private static TestDataNormalizer testDataNormalizer;
        private static TestDataFile testDataFile;

        private static bool ShouldOverwriteOutput = false;

        private int deterministicGuidCounter = 0;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            SampleData.DetectTermStore();

            FunctionalTests.testDataNormalizer = new TestDataNormalizer();
            FunctionalTests.testDataNormalizer.TermStoreId = SampleData.TermStoreId;

            FunctionalTests.testDataFile = new TestDataFile(@"..\..\FunctionalTests.xml",
                FunctionalTests.testDataNormalizer);

#if OVERWRITE_OUTPUT
            ShouldOverwriteOutput = true;
#endif
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (FunctionalTests.ShouldOverwriteOutput)
            {
                FunctionalTests.testDataFile.SaveToXml();
            }
        }

        private void AssertXmlMatchesResource(string xmlToCompare, string resourceName,
            [CallerMemberName] string testGroupName = "")
        {
            if (FunctionalTests.ShouldOverwriteOutput)
            {
                FunctionalTests.testDataFile.SetXml(testGroupName, resourceName, xmlToCompare);
            }
            else
            {
                Assert.IsTrue(FunctionalTests.testDataFile.CompareXml(testGroupName, resourceName, xmlToCompare));
            }
        }

        private void AssertXmlMatchesResourceWithoutOverwrite(string xmlToCompare, string resourceName,
            [CallerMemberName] string testGroupName = "")
        {
            Assert.IsTrue(FunctionalTests.testDataFile.CompareXml(testGroupName, resourceName, xmlToCompare));
        }

        private string GetTestResource(string resourceName, [CallerMemberName] string testGroupName = "")
        {
            return FunctionalTests.testDataFile.GetXml(testGroupName, resourceName);
        }

        private void RewriteTaxmlAndAssertOutput(string inputResourceName, string outputResourceName,
            [CallerMemberName] string testGroupName = "")
        {
            string inputTaxmlString = this.GetTestResource(inputResourceName, testGroupName);
            LocalTermStore termStore = TaxmlLoader.LoadFromString(inputTaxmlString);
            string outputTaxmlString = TaxmlSaver.SaveToString(termStore);
            this.AssertXmlMatchesResource(outputTaxmlString, outputResourceName, testGroupName);
        }

        /// <summary>
        /// Returns the name of the calling function as a string using the CallerMemberName attribute
        /// </summary>
        /// <param name="callerName"></param>
        /// <returns></returns>
        private string GetCurrentTestName([CallerMemberName] string callerName = "")
        {
            return callerName;
        }

        #endregion

        private Client15Connector connector;

        public FunctionalTests()
        {
            this.connector = new Client15Connector(TestConfiguration.SharePointSiteUrl);
            this.connector.GetNewGuid = this.GetDeterministicGuid;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Reset the GUID counter before each test
            this.deterministicGuidCounter = 1;
        }

        [TestMethod]
        public void DownloadAllProperties()
        {
            // Clean up any previous data
            SampleData.DeleteTestGroups();
            SampleData.CreateEverything();

            string outputTaxmlString = FunctionalTests.DownloadTestDataAsTaxml(this.connector);
            this.AssertXmlMatchesResource(outputTaxmlString, "Output");
        }

        [TestMethod]
        public void DownloadWithCsomTrace()
        {
            // Clean up any previous data
            SampleData.DeleteTestGroups();

            SampleData.CreateMinimal();

            ClientTestConnectorHook.ResetClientRequestSequenceId();
            using (var hook = new ClientTestConnectorHook(this.connector))
            {
                string outputTaxmlString = FunctionalTests.DownloadTestDataAsTaxml(this.connector);
                this.AssertXmlMatchesResource(outputTaxmlString, "Output");

                this.AssertXmlMatchesResource(hook.GetCsomRequestsAsXml(), "CsomRequests");
                Debug.WriteLine(hook.CsomRequests.Count);
            }
        }

        [TestMethod]
        public void UploadAllProperties()
        {
            Debug.WriteLine("START UploadAllProperties()");
            this.DoCreateUpdateModify(this.GetCurrentTestName());
            Debug.WriteLine("END UploadAllProperties()");
        }

        [TestMethod]
        public void CreateAndUpdateByName()
        {
            Debug.WriteLine("START CreateAndUpdateByName()");
            this.DoCreateUpdateModify(this.GetCurrentTestName());
            Debug.WriteLine("END CreateAndUpdateByName()");
        }

        private void DoCreateUpdateModify(string testGroupName)
        {
            // Clean up any previous data
            SampleData.DeleteTestGroups();

            var options = new ClientConnectorUploadOptions();
            // options.MaximumBatchSize = 1;

            //---------------------------------------------------------------
            // Initial creation of the objects
            Debug.WriteLine("Part 1: Upload that initially creates objects");

            string initialInputTaxml = this.GetTestResource("InitialInput", testGroupName);
            LocalTermStore initialTermStore = TaxmlLoader.LoadFromString(initialInputTaxml);

            initialTermStore.SyncAction = new SyncAction()
            {
                IfMissing = SyncActionIfMissing.Create,
                IfPresent = SyncActionIfPresent.Error,
                IfElsewhere = SyncActionIfElsewhere.Error
            };
            this.connector.Upload(initialTermStore, SampleData.TermStoreId, options);

            string initialOutputTaxml = FunctionalTests.DownloadTestDataAsTaxml(this.connector);
            this.AssertXmlMatchesResource(initialOutputTaxml, "InitialOutput", testGroupName);

            //---------------------------------------------------------------
            // Perform a second upload of the exact same content, to verify that
            // no changes were committed to the term store
            Debug.WriteLine("Part 2: Duplicate upload that doesn't change anything");

            initialTermStore.SyncAction = new SyncAction()
            {
                IfMissing = SyncActionIfMissing.Error,
                IfPresent = SyncActionIfPresent.Update,
                IfElsewhere = SyncActionIfElsewhere.Error
            };

            DateTime changeTimeBefore = this.GetChangeTimeForTermStore();
            this.connector.Upload(initialTermStore, SampleData.TermStoreId, options);
            DateTime changeTimeAfter = this.GetChangeTimeForTermStore();

            Assert.AreEqual(changeTimeAfter, changeTimeBefore);

            //---------------------------------------------------------------
            // Upload a modified input that changes every property
            Debug.WriteLine("Part 3: Upload that makes modifications");

            string modifiedInputTaxml = this.GetTestResource("ModifiedInput", testGroupName);
            LocalTermStore modifiedTermStore = TaxmlLoader.LoadFromString(modifiedInputTaxml);

            modifiedTermStore.SyncAction = new SyncAction()
            {
                IfMissing = SyncActionIfMissing.Error,
                IfPresent = SyncActionIfPresent.Update,
                IfElsewhere = SyncActionIfElsewhere.Error
            };
            this.connector.Upload(modifiedTermStore, SampleData.TermStoreId, options);

            string modifiedOutputTaxml = FunctionalTests.DownloadTestDataAsTaxml(this.connector);
            this.AssertXmlMatchesResource(modifiedOutputTaxml, "ModifiedOutput", testGroupName);
        }

        [TestMethod]
        public void ReassignSourceTerm()
        {
            Debug.WriteLine("START ReassignSourceTerm()");

            // Clean up any previous data
            SampleData.DeleteTestGroups();

            string startTaxml = this.GetTestResource("Start");
            LocalTermStore startTermStore = TaxmlLoader.LoadFromString(startTaxml);
            this.connector.Upload(startTermStore, SampleData.TermStoreId);

            string initialInputTaxml = this.GetTestResource("Input");
            LocalTermStore initialTermStore = TaxmlLoader.LoadFromString(initialInputTaxml);
            this.connector.Upload(initialTermStore, SampleData.TermStoreId);

            string initialOutputTaxml = FunctionalTests.DownloadTestDataAsTaxml(this.connector);
            this.AssertXmlMatchesResource(initialOutputTaxml, "Output");

            Debug.WriteLine("END ReassignSourceTerm()");
        }

        [TestMethod]
        public void UploadBatchingTest()
        {
            Debug.WriteLine("START UploadBatchingTest()");

            // Clean up any previous data
            SampleData.DeleteTestGroups();

            string initialInputTaxml = this.GetTestResource("Input");
            LocalTermStore initialTermStore = TaxmlLoader.LoadFromString(initialInputTaxml);
            this.connector.Upload(initialTermStore, SampleData.TermStoreId);

            string initialOutputTaxml = FunctionalTests.DownloadTestDataAsTaxml(this.connector);
            this.AssertXmlMatchesResource(initialOutputTaxml, "Output");

            Debug.WriteLine("END UploadBatchingTest()");
        }

        [TestMethod]
        public void UploadWithTermLinkSourcePath()
        {
            Debug.WriteLine("START UploadWithTermLinkSourcePath()");

            // Clean up any previous data
            SampleData.DeleteTestGroups();

            string startTaxml = this.GetTestResource("Start");
            LocalTermStore startTermStore = TaxmlLoader.LoadFromString(startTaxml);
            this.connector.Upload(startTermStore, SampleData.TermStoreId);

            string initialInputTaxml = this.GetTestResource("Input");
            LocalTermStore initialTermStore = TaxmlLoader.LoadFromString(initialInputTaxml);
            this.connector.Upload(initialTermStore, SampleData.TermStoreId);

            string initialOutputTaxml = FunctionalTests.DownloadTestDataAsTaxml(this.connector);
            this.AssertXmlMatchesResource(initialOutputTaxml, "Output");

            Debug.WriteLine("END UploadWithTermLinkSourcePath()");
        }

        [TestMethod]
        public void UploadWithTermLinkSourcePath_NotFoundError()
        {
            Debug.WriteLine("START UploadWithTermLinkSourcePath_NotFoundError()");

            // Clean up any previous data
            SampleData.DeleteTestGroups();
            SampleData.CreateMinimal();

            try
            {
                string initialInputTaxml = this.GetTestResource("Input");
                LocalTermStore initialTermStore = TaxmlLoader.LoadFromString(initialInputTaxml);
                this.connector.Upload(initialTermStore, SampleData.TermStoreId);

                Assert.Fail("The expected exception was not thrown");
            }
            catch (InvalidOperationException ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("Unable to find term specified by the TermLinkSourcePath:"));
            }


            Debug.WriteLine("END UploadWithTermLinkSourcePath_NotFoundError()");
        }

        #region Helpers

        private DateTime GetChangeTimeForTermStore()
        {
            TermStore clientTermStore = this.connector.FetchClientTermStore(SampleData.TermStoreId);

            var changeInformation = new ChangeInformation(this.connector.ClientContext);
            changeInformation.WithinTimeSpan = TimeSpan.FromMinutes(1.0);

            var changes = clientTermStore.GetChanges(changeInformation);
            this.connector.ClientContext.Load(changes, c => c.Include(x => x.ChangedTime));
            this.connector.ClientContext.ExecuteQuery();

            var maxChange = changes.ToList().LastOrDefault();
            if (maxChange == null)
                return DateTime.MinValue;
            return maxChange.ChangedTime;
        }

        /// <summary>
        /// Retrieve the taxonomy objects that were created in SharePoint,
        /// and return them as a TAXML string.
        /// </summary>
        private static string DownloadTestDataAsTaxml(Client15Connector connector)
        {
            Debug.WriteLine("DownloadTestDataAsTaxml()");
            ClientConnectorDownloadOptions options = new ClientConnectorDownloadOptions();
            options.EnableGroupIdFilter(SampleData.TestGroupIds);
            LocalTermStore outputTermStore = new LocalTermStore(SampleData.TermStoreId, "");

            connector.Download(outputTermStore, options);

            return TaxmlSaver.SaveToString(outputTermStore);
        }

        public Guid GetDeterministicGuid()
        {
            // Guid guid = new Guid()
            byte[] bytes = new byte[16]
            {
                0xDD, 0xDD, 0xDD, 0xDD,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            };
            bytes[12] = (byte) (this.deterministicGuidCounter >> 24);
            bytes[13] = (byte) (this.deterministicGuidCounter >> 16);
            bytes[14] = (byte) (this.deterministicGuidCounter >> 8);
            bytes[15] = (byte) this.deterministicGuidCounter;

            ++this.deterministicGuidCounter;

            return new Guid(bytes);
        }

        #endregion

        #region class SampleData

        private static class SampleData
        {
            public static readonly Guid GroupId_TestGroup1 = new Guid("FFFFFFFF-FFFF-0001-0000-000000000000");

            public static readonly Guid Id_TermSetA = new Guid("FFFFFFFF-FFFF-0001-000A-000000000000");
            public static readonly Guid Id_TermSetA_Term1 = new Guid("FFFFFFFF-FFFF-0001-000A-100000000000");
            public static readonly Guid Id_TermSetA_Term2 = new Guid("FFFFFFFF-FFFF-0001-000A-200000000000");
            public static readonly Guid Id_TermSetA_Term3 = new Guid("FFFFFFFF-FFFF-0001-000A-300000000000");
            public static readonly Guid Id_TermSetA_Term1_1 = new Guid("FFFFFFFF-FFFF-0001-000A-110000000000");
            public static readonly Guid Id_TermSetA_Term1_2 = new Guid("FFFFFFFF-FFFF-0001-000A-120000000000");
            public static readonly Guid Id_TermSetA_Term1_3 = new Guid("FFFFFFFF-FFFF-0001-000A-130000000000");

            public static readonly Guid GroupId_TestGroup2 = new Guid("FFFFFFFF-FFFF-0002-0000-000000000000");

            public static readonly Guid Id_TermSetB = new Guid("FFFFFFFF-FFFF-0002-000B-000000000000");
            public static readonly Guid Id_TermSetB_Term4 = new Guid("FFFFFFFF-FFFF-0002-000B-400000000000");

            public static readonly Guid Id_TermSetC = new Guid("FFFFFFFF-FFFF-0002-000C-000000000000");
            public static readonly Guid Id_TermSetC_Term5 = new Guid("FFFFFFFF-FFFF-0002-000C-500000000000");

            public static readonly Guid GroupId_TestGroup3 = new Guid("FFFFFFFF-FFFF-0003-0000-000000000000");

            public static readonly Guid Id_TermSetD = new Guid("FFFFFFFF-FFFF-0003-000D-000000000000");

            public static readonly Guid Id_TermSetE = new Guid("FFFFFFFF-FFFF-0003-000E-000000000000");
            public static readonly Guid Id_TermSetE_Term6 = new Guid("FFFFFFFF-FFFF-0003-000E-600000000000");
            public static readonly Guid Id_TermSetE_Term6_1 = new Guid("FFFFFFFF-FFFF-0003-000E-610000000000");
            public static readonly Guid Id_TermSetE_Term6_1_1 = new Guid("FFFFFFFF-FFFF-0003-000E-611000000000");

            public static readonly Guid[] TestGroupIds =
            {
                SampleData.GroupId_TestGroup1,
                SampleData.GroupId_TestGroup2,
                SampleData.GroupId_TestGroup3
            };

            private static Guid? termStoreId = null;

            public static Guid TermStoreId
            {
                get
                {
                    if (SampleData.termStoreId == null)
                        throw new InvalidOperationException("The sample data is not initialized yet");
                    return SampleData.termStoreId.Value;
                }
            }

            public static void DetectTermStore()
            {
                if (SampleData.termStoreId != null)
                    return;

                var clientContext = new ClientContext(TestConfiguration.SharePointSiteUrl);
                var taxonomySession = TaxonomySession.GetTaxonomySession(clientContext);
                clientContext.Load(taxonomySession, x => x.OfflineTermStoreNames);

                if (TestConfiguration.TestTermStoreId != null)
                {
                    // Check that the configured ID is valid
                    TermStore termStore = null;
                    var scope = new ExceptionHandlingScope(clientContext);
                    using (scope.StartScope())
                    {
                        using (scope.StartTry())
                        {
                            termStore = taxonomySession.TermStores.GetById(TestConfiguration.TestTermStoreId.Value);
                            clientContext.Load(termStore, x => x.Id);
                        }
                        using (scope.StartFinally())
                        {
                        }
                    }
                    clientContext.ExecuteQuery();

                    if (termStore.ServerObjectIsNull != false)
                    {
                        if (taxonomySession.OfflineTermStoreNames.Count() > 0)
                        {
                            throw new InvalidOperationException("The term store is offline");
                        }
                        else
                        {
                            throw new InvalidOperationException("A term store was not found with ID="
                                + TestConfiguration.TestTermStoreId.Value);
                        }
                    }
                    SampleData.termStoreId = termStore.Id;
                }
                else
                {
                    // Use the default term store ID
                    TermStore termStore = taxonomySession.GetDefaultSiteCollectionTermStore();
                    clientContext.Load(termStore, x => x.Id);
                    clientContext.ExecuteQuery();

                    if (termStore.ServerObjectIsNull == true)
                    {
                        if (taxonomySession.OfflineTermStoreNames.Count() > 0)
                        {
                            throw new InvalidOperationException("The term store is offline");
                        }
                        else
                        {
                            throw new InvalidOperationException("A default term store is not configured");
                        }
                    }
                    SampleData.termStoreId = termStore.Id;
                }
            }

            public static void DeleteTestGroups()
            {
                Debug.WriteLine("DeleteTestGroups()");
                var clientContext = new ClientContext(TestConfiguration.SharePointSiteUrl);
                var taxonomySession = TaxonomySession.GetTaxonomySession(clientContext);
                var termStore = taxonomySession.TermStores.GetById(SampleData.TermStoreId);
                var termGroups = new List<TermGroup>();

                foreach (Guid testGroupId in SampleData.TestGroupIds)
                {
                    termGroups.Add(termStore.GetGroup(testGroupId));
                }

                clientContext.ExecuteQuery();

                foreach (var termGroup in termGroups)
                {
                    if (termGroup.ServerObjectIsNull != false)
                        continue;

                    clientContext.Load(termGroup.TermSets);
                    clientContext.ExecuteQuery();

                    foreach (var termSet in termGroup.TermSets)
                    {
                        termSet.DeleteObject();
                        clientContext.ExecuteQuery(); // sic
                    }
                    termGroup.DeleteObject();
                    clientContext.ExecuteQuery();
                }
            }

            public static void CreateEverything()
            {
                // Populate new data
                var clientContext = new ClientContext(TestConfiguration.SharePointSiteUrl);
                var taxonomySession = TaxonomySession.GetTaxonomySession(clientContext);
                var termStore = taxonomySession.TermStores.GetById(SampleData.TermStoreId);

                clientContext.Load(termStore, ts => ts.Languages);
                clientContext.ExecuteQuery();

                // Add any missing languages
                var languages = termStore.Languages.ToArray();
                foreach (int lcid in new int[] {1033, 1036})
                {
                    if (!languages.Contains(lcid))
                    {
                        termStore.AddLanguage(lcid);
                        termStore.CommitAll();
                        clientContext.ExecuteQuery();
                    }
                }

                termStore.WorkingLanguage = 1033;

                clientContext.ExecuteQuery();

                // -------------------------
                var testGroup1 = termStore.CreateGroup("TaxonomyToolkit - TestGroup1", SampleData.GroupId_TestGroup1);
                testGroup1.Description = "Description for TestGroup1";

                var termSetA = testGroup1.CreateTermSet("TermSetA", SampleData.Id_TermSetA, 1033);
                termSetA.Contact = TestConfiguration.SharePointUserAccounts[1];
                termSetA.CustomSortOrder =
                    SampleData.Id_TermSetA_Term2 + ":" +
                        SampleData.Id_TermSetE_Term6 + ":" + // example of an unmatched GUID
                        SampleData.Id_TermSetA_Term1;

                termSetA.Description = "Description for TermSetA";
                termSetA.IsAvailableForTagging = false;
                termSetA.IsOpenForTermCreation = true;
                termStore.WorkingLanguage = 1036;
                termSetA.Name = "TermSetA - French";
                termStore.WorkingLanguage = 1033;
                termSetA.Owner = TestConfiguration.SharePointUserAccounts[2];
                termSetA.SetCustomProperty("Property1", "Value1");
                termSetA.SetCustomProperty("Property2", "Value2");
                termSetA.AddStakeholder(TestConfiguration.SharePointUserAccounts[3]);

                var termA1 = termSetA.CreateTerm("TermA1", 1033, SampleData.Id_TermSetA_Term1);
                termA1.CustomSortOrder = SampleData.Id_TermSetA_Term1_2.ToString();
                termA1.CreateLabel("TermA1 - Synonym", 1033, isDefault: false);
                termA1.CreateLabel("TermA1 - French", 1036, isDefault: true);
                termA1.SetDescription("Description for TermA1", 1033);
                termA1.SetDescription("Description for TermA1 - French", 1036);

                var termA1_1 = termA1.CreateTerm("TermA1_1", 1033, SampleData.Id_TermSetA_Term1_1);
                termA1.SetDescription("Description for TermA1_1", 1033);

                var termA1_2 = termA1.CreateTerm("TermA1_2", 1033, SampleData.Id_TermSetA_Term1_2);
                var termA1_3 = termA1.CreateTerm("TermA1_3", 1033, SampleData.Id_TermSetA_Term1_3);
                termA1_3.Deprecate(doDeprecate: true);

                var termA2 = termSetA.CreateTerm("TermA2", 1033, SampleData.Id_TermSetA_Term2);
                var termA3 = termSetA.CreateTerm("TermA3", 1033, SampleData.Id_TermSetA_Term3);

                clientContext.ExecuteQuery();

                // -------------------------
                var testGroup2 = termStore.CreateGroup("TaxonomyToolkit - TestGroup2", SampleData.GroupId_TestGroup2);
                testGroup2.Description = "Reused Term Scenarios";

                var termSetB = testGroup2.CreateTermSet("TermSetB", SampleData.Id_TermSetB, 1033);
                var termB4 = termSetB.CreateTerm("TermB4", 1033, SampleData.Id_TermSetB_Term4);

                var termSetC = testGroup2.CreateTermSet("TermSetC", SampleData.Id_TermSetC, 1033);
                var termC5 = termSetC.CreateTerm("TermC5", 1033, SampleData.Id_TermSetC_Term5);

                var reusedTermB4 = termSetC.ReuseTerm(termB4, reuseBranch: false);
                reusedTermB4.SetLocalCustomProperty("LocalProperty4", "Value4 in TermSetC");
                reusedTermB4.SetLocalCustomProperty("LocalProperty6", "Value6 in TermSetC");

                var reusedTermC5 = termSetB.ReuseTerm(termC5, reuseBranch: false);

                // The source term properties are assigned *after* the term is reused;
                // otherwise they would be copied to the reused term
                termB4.SetCustomProperty("Property3", "Value3");
                termB4.SetLocalCustomProperty("LocalProperty4", "Value4 in TermSetB");
                termB4.SetLocalCustomProperty("LocalProperty5", "Value5 in TermSetB");

                termB4.IsAvailableForTagging = false;
                reusedTermC5.IsAvailableForTagging = false;

                clientContext.ExecuteQuery();

                // -------------------------
                var testGroup3 = termStore.CreateGroup("TaxonomyToolkit - TestGroup3", SampleData.GroupId_TestGroup3);
                testGroup3.Description = "Pinned Term Scenarios";

                var termSetD = testGroup3.CreateTermSet("TermSetD", SampleData.Id_TermSetD, 1033);

                var termSetE = testGroup3.CreateTermSet("TermSetE", SampleData.Id_TermSetE, 1033);
                var termE6 = termSetE.CreateTerm("TermE6", 1033, SampleData.Id_TermSetE_Term6);
                var termE61 = termE6.CreateTerm("TermE6_1", 1033, SampleData.Id_TermSetE_Term6_1);
                var termE611 = termE6.CreateTerm("TermE6_1_1", 1033, SampleData.Id_TermSetE_Term6_1_1);

                termSetD.ReuseTermWithPinning(termE6);

                clientContext.ExecuteQuery();
            }

            public static void CreateMinimal()
            {
                // Populate new data
                var clientContext = new ClientContext(TestConfiguration.SharePointSiteUrl);
                var taxonomySession = TaxonomySession.GetTaxonomySession(clientContext);
                var termStore = taxonomySession.TermStores.GetById(SampleData.TermStoreId);
                termStore.WorkingLanguage = 1033;

                clientContext.ExecuteQuery();

                // -------------------------
                var testGroup1 = termStore.CreateGroup("TaxonomyToolkit - TestGroup1", SampleData.GroupId_TestGroup1);
                testGroup1.Description = "Description for TestGroup1";

                var termSetA = testGroup1.CreateTermSet("TermSetA", SampleData.Id_TermSetA, 1033);
                termStore.WorkingLanguage = 1036;
                termSetA.Name = "TermSetA - French";
                termStore.WorkingLanguage = 1033;

                var termA1 = termSetA.CreateTerm("TermA1", 1033, SampleData.Id_TermSetA_Term1);
                termA1.CreateLabel("TermA1 - Synonym", 1033, isDefault: false);
                termA1.CreateLabel("TermA1 - French", 1036, isDefault: true);
                termA1.SetDescription("Description for TermA1", 1033);
                termA1.SetDescription("Description for TermA1 - French", 1036);

                var termA1_1 = termA1.CreateTerm("TermA1_1", 1033, SampleData.Id_TermSetA_Term1_1);

                clientContext.ExecuteQuery();
            }
        }

        #endregion
    }
}
