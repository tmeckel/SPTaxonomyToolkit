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
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using System.Xml.Schema;
using TaxonomyToolkit.General;

namespace TaxonomyToolkit.Taxml
{
    /// <summary>
    /// This class reads Taxonomy data in the TAXML XML format, as defined by TaxmlFile.xsd.
    /// </summary>
    public class TaxmlLoader
    {
        private static Version OldestSupportedVersion = new Version(2, 0, 0, 0);

        public TaxmlLoader()
        {
        }

        public LocalTermStore LoadFromFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException("File not found: \"" + fileName + "\"", fileName);
            }

            using (StreamReader streamReader = new StreamReader(fileName))
            {
                return this.LoadFromStream(streamReader);
            }
        }

        public LocalTermStore LoadFromStream(TextReader textReader)
        {
            XDocument document = XDocument.Load(textReader, LoadOptions.SetLineInfo);

            document.Validate(TaxmlSpec.TaxmlSchema,
                delegate(object sender, ValidationEventArgs e)
                {
                    throw new ParseException(e.Message, sender as XObject, e.Exception);
                }
                );

            LocalTermStore termStore = new LocalTermStore(Guid.NewGuid(), "Term Store");

            XElement rootElement = document.Root;
            XElement termStoreElement;

            if (rootElement.Name == TaxmlSpec.TermStoreToken)
            {
                // For backwards compatibility, if the root element is the "TermStore" element
                // then wrap it inside a TaxmlFile element
                termStoreElement = rootElement;
                rootElement = new XElement(TaxmlSpec.TaxmlFileToken, termStoreElement);
                rootElement.SetAttributeValue(TaxmlSpec.VersionToken, TaxmlSpec.Versions.V2_0.ToString());
            }
            else
            {
                termStoreElement = rootElement.Element(TaxmlSpec.TermStoreToken);
            }

            // Check the version
            string versionString = this.GetAttributeValue(rootElement, TaxmlSpec.VersionToken);
            Version fileVersion = Version.Parse(versionString);

            if (fileVersion < TaxmlSpec.Versions.OldestLoadable)
            {
                throw new InvalidOperationException(
                    "The TAXML file version is too old and is no longer supported by this tool");
            }
            if (fileVersion > TaxmlSpec.Versions.Current)
            {
                throw new InvalidOperationException(
                    "The TAXML file is using a newer version that is not supported; consider upgrading to a newer tool");
            }

            this.ProcessTermStore(termStoreElement, termStore);

            return termStore;
        }

        public void LoadFromResource(Type typeInAssembly, string expectedNamespace, string resourceName)
        {
            TaxmlSpec.ImportResource(typeInAssembly, expectedNamespace, resourceName,
                delegate(StreamReader reader) { this.LoadFromStream(reader); }
                );
        }

        public static LocalTermStore LoadFromString(string taxmlString)
        {
            using (var reader = new StringReader(taxmlString))
            {
                TaxmlLoader loader = new TaxmlLoader();
                return loader.LoadFromStream(reader);
            }
        }

        private void ProcessTermStore(XElement xmlNode, LocalTermStore termStore)
        {
            this.ReadTaxmlComments(xmlNode, termStore);

            // Stage 1: Create everything that can be immediately created
            foreach (XElement childNode in xmlNode.Elements())
            {
                switch (childNode.Name.LocalName)
                {
                    case TaxmlSpec.TermSetGroupToken:
                        this.ProcessTermSetGroup(childNode, termStore);
                        break;

                    case TaxmlSpec.SyncActionToken:
                        this.ProcessSyncAction(childNode, termStore);
                        break;

                    default:
                        throw new ParseException("Unimplemented XML tag \"" + childNode.Name.LocalName + "\"", childNode);
                }
            }
        }

        private void ProcessTermSetGroup(XElement xmlNode, LocalTermStore termStore)
        {
            string name = this.GetRequiredAttributeValue(xmlNode, TaxmlSpec.NameToken);

            if (TaxmlSpec.IsReservedName(name))
            {
                // TODO: Handle system groups here
                throw new NotImplementedException();
            }

            Guid id = this.GetGuidAttributeValue(xmlNode, TaxmlSpec.IdToken) ?? Guid.Empty;

            LocalTermGroup termGroup = new LocalTermGroup(id, name, termStore.DefaultLanguageLcid);
            this.ReadTaxmlComments(xmlNode, termGroup);

            string description = this.GetAttributeValue(xmlNode, TaxmlSpec.DescriptionToken);
            if (description != null)
            {
                termGroup.Description = description;
            }

            // Add the group to the term store
            termStore.AddTermGroup(termGroup);

            bool processedDescription = false;

            foreach (XElement childNode in xmlNode.Elements())
            {
                switch (childNode.Name.LocalName)
                {
                    case TaxmlSpec.DescriptionToken:
                        if (processedDescription)
                            throw new ParseException("The description cannot be specified more than once", childNode);
                        processedDescription = true;
                        termGroup.Description = childNode.Value;
                        break;

                    case TaxmlSpec.TermSetToken:
                        this.ProcessTermSet(childNode, termGroup);
                        break;

                    case TaxmlSpec.SyncActionToken:
                        this.ProcessSyncAction(childNode, termGroup);
                        break;

                    default:
                        throw new ParseException("Unimplemented XML tag \"" + childNode.Name.LocalName + "\"", childNode);
                }
            }
        }

        private void ProcessTermSet(XElement xmlNode, LocalTermGroup termGroup)
        {
            string name = this.GetRequiredAttributeValue(xmlNode, TaxmlSpec.NameToken);
            Guid id = this.GetGuidAttributeValue(xmlNode, TaxmlSpec.IdToken) ?? Guid.Empty;

            if (TaxmlSpec.IsReservedName(name))
            {
                if (id != Guid.Empty)
                {
                    throw new ParseException(
                        "The \"ID\" attribute may not be used with a reserved name such as \"" + name + "\"",
                        xmlNode);
                }

                if (!termGroup.IsSystemGroup)
                {
                    throw new ParseException(
                        "The reserved TermSet \"" + name + "\" should be used inside the TermSetGroup with name \""
                            + TaxmlSpec.SystemGroupReservedName + "\".",
                        xmlNode);
                }

                if (StringComparer.OrdinalIgnoreCase.Equals(name, TaxmlSpec.OrphanedTermsTermSetReservedName))
                {
                    // TODO: Handle system term sets
                    throw new NotImplementedException();
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(name, TaxmlSpec.KeywordsTermSetReservedName))
                {
                    // TODO: Handle system term sets
                    throw new NotImplementedException();
                }
                else
                {
                    throw new InvalidOperationException("Unrecognized reserved name \"" + name + "\"");
                }
            }

            if (id == null)
                id = Guid.NewGuid();

            LocalTermSet termSet = new LocalTermSet(id, name, termGroup.DefaultLanguageLcid);
            this.ReadTaxmlComments(xmlNode, termSet);

            bool? isAvailableForTaggning = this.GetBooleanAttributeValue(xmlNode, TaxmlSpec.IsAvailableForTaggingToken);
            if (isAvailableForTaggning.HasValue)
            {
                if (termGroup.IsSystemGroup)
                {
                    throw new ParseException("The " + TaxmlSpec.IsAvailableForTaggingToken
                        + " attribute is not supported for system term sets", xmlNode);
                }
                termSet.IsAvailableForTagging = isAvailableForTaggning.Value;
            }

            bool? isOpenForTermCreation = this.GetBooleanAttributeValue(xmlNode, TaxmlSpec.IsOpenForTermCreationToken);
            if (isOpenForTermCreation.HasValue)
            {
                if (termGroup.IsSystemGroup)
                {
                    throw new ParseException("The " + TaxmlSpec.IsOpenForTermCreationToken
                        + " attribute is not supported for system term sets", xmlNode);
                }
                termSet.IsOpenForTermCreation = isOpenForTermCreation.Value;
            }

            string owner = this.GetAttributeValue(xmlNode, TaxmlSpec.OwnerToken);
            if (owner != null)
            {
                termSet.Owner = owner;
            }

            string contact = this.GetAttributeValue(xmlNode, TaxmlSpec.ContactToken);
            if (contact != null)
            {
                termSet.Contact = contact;
            }

            // Add the term set to the group
            termGroup.AddTermSet(termSet);

            var inOrderList = new List<Guid>();

            foreach (XElement childNode in xmlNode.Elements())
            {
                switch (childNode.Name.LocalName)
                {
                    case TaxmlSpec.LocalizedNameToken:
                        int lcid = this.GetLcidFromLanguageAttribute(childNode, termSet);
                        termSet.SetName(childNode.Value, lcid);
                        break;

                    case TaxmlSpec.DescriptionToken:
                        // TermSet.Description is not localized
                        termSet.Description = childNode.Value;
                        break;

                    case TaxmlSpec.CustomPropertyToken:
                        this.ProcessCustomProperty(termSet.CustomProperties, childNode);
                        break;

                    case TaxmlSpec.CustomSortOrderToken:
                        this.ProcessCustomSortOrder(termSet.CustomSortOrder, childNode);
                        break;

                    case TaxmlSpec.StakeHolderToken:
                        termSet.AddStakeholder(childNode.Value);
                        break;

                    case TaxmlSpec.TermToken:
                        this.ProcessTerm(childNode, termSet, inOrderList, isTermLink: false);
                        break;

                    case TaxmlSpec.TermLinkToken:
                        this.ProcessTerm(childNode, termSet, inOrderList, isTermLink: true);
                        break;

                    case TaxmlSpec.SyncActionToken:
                        this.ProcessSyncAction(childNode, termSet);
                        break;

                    default:
                        throw new ParseException("Unimplemented XML tag \"" + childNode.Name.LocalName + "\"", childNode);
                }
            }

            this.ProcessInOrderList(termSet, inOrderList, xmlNode);
        }

        private void ProcessTerm(XElement xmlNode, LocalTermContainer parentTermContainer, List<Guid> parentInOrderList,
            bool isTermLink)
        {
            LocalTerm term;

            Guid id = this.GetGuidAttributeValue(xmlNode, TaxmlSpec.IdToken) ?? Guid.Empty;
            if (isTermLink)
            {
                string nameHint = this.GetAttributeValue(xmlNode, TaxmlSpec.NameHintToken) ?? "";
                string termLinkSourcePath = this.GetAttributeValue(xmlNode, TaxmlSpec.TermLinkSourcePathToken) ?? "";
                if (id == Guid.Empty && string.IsNullOrWhiteSpace(termLinkSourcePath))
                {
                    throw new ParseException(
                        "TermLink elements must have either the ID attribute or the TermLinkSourcePath attribute",
                        xmlNode);
                }

                if (termLinkSourcePath.Length > 0)
                {
                    term = LocalTerm.CreateTermLinkUsingPath(parentTermContainer.DefaultLanguageLcid, termLinkSourcePath);
                }
                else
                {
                    term = LocalTerm.CreateTermLinkUsingId(id, parentTermContainer.DefaultLanguageLcid, nameHint);
                }
            }
            else
            {
                string name = this.GetRequiredAttributeValue(xmlNode, TaxmlSpec.NameToken);
                term = LocalTerm.CreateTerm(id, name, parentTermContainer.DefaultLanguageLcid);
            }

            this.ReadTaxmlComments(xmlNode, term);

            bool? isAvailableForTagging = this.GetBooleanAttributeValue(xmlNode, TaxmlSpec.IsAvailableForTaggingToken);
            if (isAvailableForTagging.HasValue)
            {
                term.IsAvailableForTagging = isAvailableForTagging.Value;
            }

            if (isTermLink)
            {
                bool? isPinnedRoot = this.GetBooleanAttributeValue(xmlNode, TaxmlSpec.IsPinnedRootToken);
                if (isPinnedRoot.HasValue)
                {
                    term.IsPinnedRoot = isPinnedRoot.Value;
                }
            }
            else
            {
                string owner = this.GetAttributeValue(xmlNode, TaxmlSpec.OwnerToken);
                if (owner != null)
                {
                    term.Owner = owner;
                }

                bool? isDeprecated = this.GetBooleanAttributeValue(xmlNode, TaxmlSpec.IsDeprecatedToken);
                if (isDeprecated.HasValue)
                {
                    term.IsDeprecated = isDeprecated.Value;
                }
            }

            bool inOrder = this.GetBooleanAttributeValue(xmlNode, TaxmlSpec.InOrderToken) ?? false;
            if (inOrder)
            {
                if (term.Id == Guid.Empty)
                    throw new ParseException("The InOrder attribute cannot be used when the term ID is empty", xmlNode);
                parentInOrderList.Add(term.Id);
            }

            // Add the term set to the parent term / term set
            parentTermContainer.AddTerm(term);

            var inOrderList = new List<Guid>();

            foreach (XElement childNode in xmlNode.Elements())
            {
                switch (childNode.Name.LocalName)
                {
                    case TaxmlSpec.LocalizedDescriptionToken:
                        int descriptionLcid = this.GetLcidFromLanguageAttribute(childNode, term);
                        term.SetDescription(childNode.Value, descriptionLcid);
                        break;
                    case TaxmlSpec.CustomPropertyToken:
                        this.ProcessCustomProperty(term.CustomProperties, childNode);
                        break;
                    case TaxmlSpec.LocalCustomPropertyToken:
                        this.ProcessCustomProperty(term.LocalCustomProperties, childNode);
                        break;
                    case TaxmlSpec.CustomSortOrderToken:
                        this.ProcessCustomSortOrder(term.CustomSortOrder, childNode);
                        break;
                    case TaxmlSpec.LabelToken:
                        int labelLcid = this.GetLcidFromLanguageAttribute(childNode, term);
                        bool? setAsDefaultLabel = this.GetBooleanAttributeValue(childNode,
                            TaxmlSpec.IsDefaultForLanguageToken);
                        term.AddLabel(childNode.Value, labelLcid, setAsDefaultLabel ?? false);
                        break;

                    case TaxmlSpec.TermToken:
                        this.ProcessTerm(childNode, term, inOrderList, isTermLink: false);
                        break;

                    case TaxmlSpec.TermLinkToken:
                        this.ProcessTerm(childNode, term, inOrderList, isTermLink: true);
                        break;

                    case TaxmlSpec.SyncActionToken:
                        this.ProcessSyncAction(childNode, term);
                        break;

                    default:
                        throw new ParseException("Unimplemented XML tag \"" + childNode.Name.LocalName + "\"", childNode);
                }
            }

            this.ProcessInOrderList(term, inOrderList, xmlNode);
        }

        private void ProcessInOrderList(LocalTermContainer termContainer, List<Guid> inOrderList, XNode xmlNodeForError)
        {
            if (inOrderList.Count > 0)
            {
                if (termContainer.CustomSortOrder.Count > 0)
                {
                    throw new ParseException(
                        "The InOrder attribute cannot be used when the container has a CustomSortOrder element",
                        xmlNodeForError);
                }
                termContainer.CustomSortOrder.AssignFrom(inOrderList);
            }
        }

        private void ProcessCustomProperty(IDictionary<string, string> propertyBag, XElement xmlNode)
        {
            string name = this.GetRequiredAttributeValue(xmlNode, TaxmlSpec.NameToken);
            string value = xmlNode.Value;
            propertyBag.Add(name, value);
        }

        private void ProcessCustomSortOrder(CustomSortOrder customSortOrder, XElement customSortOrderElement)
        {
            customSortOrder.Clear();
            foreach (XElement itemElement in customSortOrderElement.Elements())
            {
                Guid? itemId = this.GetGuidAttributeValue(itemElement, TaxmlSpec.IdToken);
                if (itemId == null)
                    throw new ParseException("CustomSortOrder Id attribute is missing", itemElement);
                customSortOrder.Add(itemId.Value);
            }
        }

        private void ProcessSyncAction(XElement xmlNode, LocalTaxonomyItem taxonomyItem)
        {
            // If any of the attribute values is missing, preserve the setting from the
            // nearest ancestor
            SyncAction inheritedSyncAction = SyncAction.Default;
            if (taxonomyItem.ParentItem != null)
            {
                inheritedSyncAction = taxonomyItem.ParentItem.GetEffectiveSyncAction();
            }

            SyncAction syncAction = new SyncAction();

            syncAction.IfMissing = this.GetEnumAttributeValue<SyncActionIfMissing>(xmlNode,
                TaxmlSpec.IfMissingToken, inheritedSyncAction.IfMissing);
            syncAction.IfPresent = this.GetEnumAttributeValue<SyncActionIfPresent>(xmlNode,
                TaxmlSpec.IfPresentToken, inheritedSyncAction.IfPresent);
            syncAction.IfElsewhere = this.GetEnumAttributeValue<SyncActionIfElsewhere>(xmlNode,
                TaxmlSpec.IfElsewhereToken, inheritedSyncAction.IfElsewhere);
            syncAction.DeleteExtraChildItems = this.GetBooleanAttributeValue(xmlNode,
                TaxmlSpec.DeleteExtraChildItemsToken) ?? inheritedSyncAction.DeleteExtraChildItems;

            taxonomyItem.SyncAction = syncAction;
        }

        private T GetEnumAttributeValue<T>(XElement xmlNode, string attributeName, T defaultValue)
            where T : struct
        {
            string value = this.GetAttributeValue(xmlNode, attributeName);

            if (value == null)
                return defaultValue;

            T result;
            if (!Enum.TryParse<T>(value, out result))
                throw new ParseException("Invalid value for " + typeof (T).Name, xmlNode);

            return result;
        }

        #region Helper Methods

        private string GetRequiredAttributeValue(XElement xmlNode, string name)
        {
            string result = this.GetAttributeValue(xmlNode, name);
            if (result == null)
            {
                throw new InvalidOperationException("The \"" + name + "\" attribute is missing");
            }
            return result;
        }

        private string GetAttributeValue(XElement xmlNode, string name)
        {
            XAttribute attribute = xmlNode.Attribute(name);
            return (null == attribute) ? null : attribute.Value;
        }

        private bool? GetBooleanAttributeValue(XElement xmlNode, string name)
        {
            string text = this.GetAttributeValue(xmlNode, name);
            if (null != text)
            {
                return bool.Parse(text);
            }
            return null;
        }

        private int? GetIntegerAttributeValue(XElement xmlNode, string name)
        {
            string text = this.GetAttributeValue(xmlNode, name);
            if (null != text)
            {
                return int.Parse(text, (IFormatProvider) CultureInfo.InvariantCulture);
            }
            return null;
        }

        private Guid? GetGuidAttributeValue(XElement xmlNode, string name)
        {
            string text = this.GetAttributeValue(xmlNode, name);
            if (null != text)
            {
                return new Guid(text);
            }
            return null;
        }

        private int GetLcidFromLanguageAttribute(XElement xmlNode, LocalTaxonomyItem contextItem)
        {
            int? lcid = this.GetIntegerAttributeValue(xmlNode, TaxmlSpec.LanguageToken);
            if (lcid.HasValue)
            {
                return lcid.Value;
            }
            else
            {
                return contextItem.DefaultLanguageLcid;
            }
        }

        private void ReadTaxmlComments(XElement xmlNode, LocalTaxonomyItem taxonomyItem)
        {
            taxonomyItem.TaxmlComments.Clear();

            for (XNode node = xmlNode.PreviousNode;; node = node.PreviousNode)
            {
                if (node == null)
                    break; // stop if we get to the start of the container
                if (node is XElement) // stop if we get to a prior XElement
                    break;
                XComment comment = node as XComment;
                if (comment != null)
                {
                    taxonomyItem.TaxmlComments.Insert(0, comment.Value);
                }
            }
        }

        #endregion
    }
}
