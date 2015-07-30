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
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace TaxonomyToolkitTests
{
    public class TestDataFile
    {
        #region class TestDataSection

        private class TestDataSection
        {
            private const string NameAttribute = "name";

            private Dictionary<string, TestDataItem> testDataItemsByName = new Dictionary<string, TestDataItem>();
            private string name;

            public string Name
            {
                get { return this.name; }
            }

            public TestDataSection(XElement testDataSectionElement)
            {
                this.name = testDataSectionElement.Attribute(TestDataSection.NameAttribute).Value;
                foreach (XElement testDataItemElement in testDataSectionElement.Elements())
                {
                    TestDataItem testDataItem = new TestDataItem(testDataItemElement);
                    this.testDataItemsByName[testDataItem.Name] = testDataItem;
                }
            }

            public bool TryGetValue(string itemName, out TestDataItem testDataItem)
            {
                return this.testDataItemsByName.TryGetValue(itemName, out testDataItem);
            }
        }

        #endregion

        #region class TestDataItem

        private class TestDataItem
        {
            private const string NameAttribute = "name";

            private string name;
            private XElement testDataItemElement;

            public string Name
            {
                get { return this.name; }
            }

            public TestDataItem(XElement testDataItemElement)
            {
                this.name = testDataItemElement.Attribute(TestDataItem.NameAttribute).Value;
                this.testDataItemElement = testDataItemElement;
            }

            public string Xml
            {
                get
                {
                    XDocument document = new XDocument();
                    document.Add(this.testDataItemElement.Nodes());
                    using (var stringWriter = new StringWriter())
                    {
                        document.Save(stringWriter);
                        return stringWriter.ToString();
                    }
                }
                set
                {
                    XDocument document = XDocument.Parse(value);

                    this.testDataItemElement.RemoveNodes();
                    this.testDataItemElement.Add(document.Nodes());
                }
            }

            public bool IsXmlEqual(IEnumerable<XNode> otherNodes)
            {
                var nodeList = this.testDataItemElement.Nodes().ToList();
                var otherNodeList = otherNodes.ToList();

                if (nodeList.Count != otherNodeList.Count)
                    return false;

                for (int i = 0; i < nodeList.Count; ++i)
                {
                    if (!XNode.DeepEquals(nodeList[i], otherNodeList[i]))
                        return false;
                }
                return true;
            }

            public override string ToString()
            {
                return this.Xml;
            }
        }

        #endregion

        private readonly Dictionary<string, TestDataSection> testDataSectionsByName =
            new Dictionary<string, TestDataSection>();

        private XDocument documentXml = null;
        private string filename = null;
        public ITestDataNormalizer Normalizer { get; set; }

        public TestDataFile()
        {
        }

        public TestDataFile(string xmlFilename, ITestDataNormalizer normalizer = null)
        {
            this.Normalizer = normalizer;
            this.LoadFromXml(xmlFilename);
        }

        /// <summary>
        ///     The filename that was most recently used with LoadFromXml() or SaveToXml().
        /// </summary>
        public string Filename
        {
            get { return this.filename; }
        }

        /// <summary>
        ///     Loads the test data from the swpecified XML file.
        /// </summary>
        public void LoadFromXml(string xmlFilename)
        {
            using (StreamReader reader = new StreamReader(xmlFilename))
            {
                this.LoadFromXml(reader);
            }
            this.filename = xmlFilename;
        }

        public void LoadFromXml(TextReader textReader)
        {
            this.filename = null;
            this.testDataSectionsByName.Clear();

            this.documentXml = XDocument.Parse(textReader.ReadToEnd());

            if (this.Normalizer != null)
            {
                this.Normalizer.NormalizeDocument(this.documentXml);
                this.Normalizer.UnpackagePortableFormat(this.documentXml);
            }

            foreach (XElement testDataSectionElement in this.documentXml.Root.Elements())
            {
                TestDataSection testDataSection = new TestDataSection(testDataSectionElement);
                this.testDataSectionsByName[testDataSection.Name] = testDataSection;
            }
        }

        /// <summary>
        ///     Saves the test data to the specified XML file.  If xmlFilename is omitted,
        ///     SaveToXml() will use TestDataManager.Filename.
        /// </summary>
        /// <param name="xmlFilename"></param>
        public void SaveToXml(string xmlFilename = null)
        {
            string filenameToSave = xmlFilename ?? this.filename;
            if (filenameToSave == null)
                throw new InvalidOperationException("Save() must be called with an explicit filename");

            using (StreamWriter writer = new StreamWriter(filenameToSave))
            {
                this.SaveToXml(writer);
            }
            this.filename = filenameToSave;
        }

        public void SaveToXml(TextWriter textWriter)
        {
            var xmlWriterSettings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "    "
            };
            using (var xmlWriter = XmlWriter.Create(textWriter, xmlWriterSettings))
            {
                if (this.Normalizer != null)
                {
                    this.Normalizer.NormalizeDocument(this.documentXml);
                    this.Normalizer.PackagePortableFormat(this.documentXml);
                }

                this.documentXml.Save(xmlWriter);
            }
        }

        /// <summary>
        ///     Returns an test data item from the specified XML section.
        /// </summary>
        public string GetXml(string sectionName, string itemName)
        {
            TestDataItem testDataItem = this.GetTestDataItem(sectionName, itemName);
            return testDataItem.Xml;
        }

        /// <summary>
        ///     Assigns a test data item in the specified XML section.
        /// </summary>
        public void SetXml(string sectionName, string itemName, string newXml)
        {
            TestDataItem testDataItem = this.GetTestDataItem(sectionName, itemName);

            if (this.Normalizer != null)
                newXml = this.Normalizer.GetNormalized(newXml);

            testDataItem.Xml = newXml;
        }

        /// <summary>
        ///     Comparse the test data item in the specified XML section against the
        ///     provided otherXml value.
        /// </summary>
        public bool CompareXml(string sectionName, string itemName, string otherXml)
        {
            TestDataItem testDataItem = this.GetTestDataItem(sectionName, itemName);
            var otherDocument = XDocument.Parse(otherXml);

            if (this.Normalizer != null)
                this.Normalizer.NormalizeDocument(otherDocument);

            return testDataItem.IsXmlEqual(otherDocument.Nodes());
        }

        private TestDataItem GetTestDataItem(string sectionName, string itemName)
        {
            TestDataSection section;
            if (!this.testDataSectionsByName.TryGetValue(sectionName, out section))
                throw new KeyNotFoundException("The test section \"" + sectionName + "\" is not defined");
            TestDataItem testDataItem;
            if (!section.TryGetValue(itemName, out testDataItem))
            {
                throw new KeyNotFoundException("The test data item \"" + itemName
                    + "\" is not defined in section \"" + sectionName + "\"");
            }
            return testDataItem;
        }
    }
}
