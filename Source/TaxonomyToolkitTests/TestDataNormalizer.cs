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
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TaxonomyToolkitTests
{
    public interface ITestDataNormalizer
    {
        void NormalizeDocument(XDocument document);
        string GetNormalized(string xml);

        void PackagePortableFormat(XDocument document);
        void UnpackagePortableFormat(XDocument document);
    }

    public class TestDataNormalizer : ITestDataNormalizer
    {
        // O15 Example: <Identity Id="1234" Name="00000000-0000-0000-0000-000000000001:st:sW1gELZgL06v3Mpxgsl6Og==" />
        // O16 Example: <Identity Id="1234" Name="00000000-0000-0000-0000-000000000001|fec14c62-0000-0000-0000-000000000002:st:sW1gELZgL06v3Mpxgsl6Og==" />
        private static Regex objectIdentityRegex = new Regex(@"^[0-9a-f|\-]+:(?<type>st|gr|se|te):(?<base64>.*)$",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        // Example: i:0#.w|domain\alias
        private static Regex domainUserRegex = new Regex(@"([^|\\]+\|)?(?<user>[^ \\]+\\[^ \\]+)$",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        private readonly Dictionary<string, string> packageMapping = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, string> unpackageMapping = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        public TestDataNormalizer()
        {
            int index = 0;
            foreach (string user in TestConfiguration.SharePointUserAccounts)
            {
                ++index;
                string genericName = @"[user " + index + "]";
                this.packageMapping.Add(user, genericName);
                this.unpackageMapping.Add(genericName, user);
            }
        }

        /// <summary>
        ///     Since the Term Store ID is randomly assigned for each farm, it cannot be controlled
        ///     as part of the test data set, so TestDataNormalizer needs to know what it looks like
        ///     so that it can normalize it.
        /// </summary>
        public Guid? TermStoreId { get; set; }

        public void NormalizeDocument(XDocument document) // ITestDataNormalizer
        {
            this.TraverseElements(document, (string elementName, XElement element) =>
            {
                switch (elementName)
                {
                    // Example:
                    // <ObjectPaths>
                    //     <Identity Id="1234" Name="00000000-0000-0000-0000-000000000001:st:sW1gELZgL06v3Mpxgsl6Og==" />
                    // </ObjectPaths>
                    case "Identity":
                    {
                        XAttribute idAttribute = this.GetAttribute(element, "Id");
                        XAttribute nameAttribute = this.GetAttribute(element, "Name");
                        if (idAttribute != null && nameAttribute != null)
                        {
                            nameAttribute.Value = this.GetNormalizedObjectId(nameAttribute.Value);
                        }
                    }
                        break;

                    // Example:
                    // <ObjectPaths>
                    //     <StaticMethod Id="1001" Name="GetTaxonomySession" TypeId="{981cbc68-9edc-4f8d-872f-71146fcbb84f}" />
                    //     <Property Id="1004" ParentId="1001" Name="TermStores" />
                    //     <Method Id="1010" ParentId="1004" Name="GetById">
                    //         <Parameters>
                    //             <Parameter Type="Guid">{00000000-0000-0000-0000-000000000001}</Parameter>
                    //         </Parameters>
                    //     </Method>
                    // </ObjectPaths>
                    case "Parameter":
                    {
                        XAttribute typeAttribute = this.GetAttribute(element, "Type");
                        if (typeAttribute != null && typeAttribute.Value == "Guid")
                        {
                            Guid guid;
                            if (Guid.TryParse(element.Value, out guid))
                            {
                                element.Value = this.GetNormalizedGuid(guid);
                            }
                        }
                    }
                        break;
                }
            });
        }

        public string GetNormalized(string xml) // ITestDataNormalizer
        {
            var document = XDocument.Parse(xml);
            this.NormalizeDocument(document);
            return document.ToString();
        }

        public void PackagePortableFormat(XDocument document) // ITestDataNormalizer
        {
            this.ConvertPortableFormat(document, true);
        }

        public void UnpackagePortableFormat(XDocument document) // ITestDataNormalizer
        {
            this.ConvertPortableFormat(document, false);
        }

        private void ConvertPortableFormat(XDocument document, bool package)
        {
            this.TraverseElements(document, (string elementName, XElement element) =>
            {
                switch (elementName)
                {
                    // Example:
                    // <TermSet Name="TermSetA" Id="{ffffffff-ffff-0001-000a-000000000000}" 
                    //     IsAvailableForTagging="false" IsOpenForTermCreation="true" 
                    //     Owner="domain\user4" Contact="domain\user3">
                    case "TermSet":
                    case "Term":
                    case "TermLink":
                    {
                        XAttribute ownerAttribute = this.GetAttribute(element, "Owner");
                        if (ownerAttribute != null)
                        {
                            ownerAttribute.Value = this.ConvertPortableFormatUser(ownerAttribute.Value, package);
                        }
                        XAttribute contactAttribute = this.GetAttribute(element, "Contact");
                        if (contactAttribute != null)
                        {
                            contactAttribute.Value = this.ConvertPortableFormatUser(contactAttribute.Value, package);
                        }
                    }
                        break;
                    case "Stakeholder":
                        element.Value = this.ConvertPortableFormatUser(element.Value, package);
                        break;
                }
            });
        }

        private string ConvertPortableFormatUser(string userValue, bool package)
        {
            var dictionary = package ? this.packageMapping : this.unpackageMapping;

            string mappedValue;
            if (dictionary.TryGetValue(userValue, out mappedValue))
                return mappedValue;

            return userValue;
        }

        private string GetNormalizedObjectId(string objectId)
        {
            var match = TestDataNormalizer.objectIdentityRegex.Match(objectId);
            if (match.Success)
            {
                string type = match.Groups["type"].Value;
                string base64 = match.Groups["base64"].Value;

                List<Guid> decodedGuids = TestDataNormalizer.GetDecodedBase64Guids(base64);

                string result = "type=" + type + ", guids=";
                bool needsComma = false;
                foreach (var guid in decodedGuids)
                {
                    if (needsComma)
                        result += ", ";

                    result += this.GetNormalizedGuid(guid);
                    needsComma = true;
                }

                return "[" + result + "]";
            }
            return objectId;
        }

        private string GetNormalizedGuid(Guid guid)
        {
            if (this.TermStoreId != null)
            {
                if (guid == this.TermStoreId)
                {
                    return "[term store]";
                }
            }
            return guid.ToString();
        }

        private static List<Guid> GetDecodedBase64Guids(string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            int numGuids = bytes.Length/16;
            List<Guid> guids = new List<Guid>(numGuids);
            for (int i = 0; i < numGuids; i++)
            {
                byte[] guidBytes = new byte[16];
                Array.Copy(bytes, i*16, guidBytes, 0, 16);
                guids.Add(new Guid(guidBytes));
            }
            return guids;
        }

        #region XML helpers

        private delegate void TraverseElementsAction(string elementName, XElement element);

        private void TraverseElements(XDocument document, TraverseElementsAction processor)
        {
            foreach (var element in document.Root
                .DescendantsAndSelf()
                .OfType<XElement>())
            {
                processor(element.Name.LocalName, element);
            }
        }

        private XAttribute GetAttribute(XElement element, string localName)
        {
            return element.Attributes().FirstOrDefault(x => x.Name.LocalName == localName);
        }

        #endregion
    }
}
