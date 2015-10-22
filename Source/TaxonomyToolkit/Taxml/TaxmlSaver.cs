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
using System.Xml.Schema;
using TaxonomyToolkit.General;

namespace TaxonomyToolkit.Taxml
{
    /// <summary>
    /// This class writes Taxonomy data in the TAXML XML format, as defined by TaxmlFile.xsd.
    /// </summary>
    public class TaxmlSaver
    {
        #region class CustomOrderedTerm

        private class CustomOrderedTerm
        {
            public readonly LocalTerm Term;
            public readonly bool WriteInOrderAttribute;

            public CustomOrderedTerm(LocalTerm term, bool writeInOrderAttribute)
            {
                this.Term = term;
                this.WriteInOrderAttribute = writeInOrderAttribute;
            }
        }

        #endregion

        public TaxmlSaver()
        {
        }

        public void SaveToFile(string fileName, LocalTermStore termStore)
        {
            using (StreamWriter streamWriter = new StreamWriter(fileName))
            {
                this.SaveToStream(streamWriter, termStore);
            }
        }

        public void SaveToStream(TextWriter textWriter, LocalTermStore termStore)
        {
            XElement rootElement = new XElement(TaxmlSpec.TaxmlFileToken);
            rootElement.SetAttributeValue(TaxmlSpec.VersionToken, TaxmlSpec.Versions.Current.ToString());

            XDocument xmlDocument = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                rootElement
                );

            this.ProcessTree(rootElement, termStore);

            var xmlWriterSettings = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "    "
            };
            using (var xmlWriter = XmlWriter.Create(textWriter, xmlWriterSettings))
            {
                xmlDocument.Save(xmlWriter);
            }

            textWriter.WriteLine(); // append a newline

#if DEBUG
            // Verify that the generated XML conforms to the schema
            xmlDocument.Validate(TaxmlSpec.TaxmlSchema,
                delegate(object sender, ValidationEventArgs e)
                {
                    throw new ParseException(e.Message, sender as XObject, e.Exception);
                }
                );
#endif
        }

        public static string SaveToString(LocalTermStore termStore)
        {
            using (var writer = new StringWriter())
            {
                TaxmlSaver saver = new TaxmlSaver();
                saver.SaveToStream(writer, termStore);
                return writer.ToString();
            }
        }

        private void ProcessTree(XElement rootElement, LocalTermStore termStore)
        {
            XElement termStoreElement = new XElement(TaxmlSpec.TermStoreToken);
            rootElement.Add(termStoreElement);

            termStoreElement.SetAttributeValue(TaxmlSpec.DefaultLanguageToken, termStore.DefaultLanguageLcid);

            this.ProcessTaxmlComments(termStoreElement, termStore);
            this.ProcessSyncActionElement(termStore, termStoreElement);

            foreach (LocalTermGroup termGroup in termStore.TermGroups)
            {
                // Replace e.g. "System" with the reserved symbol "|SystemGroup|"
                string adjustedGroupName = termGroup.Name;
                if (termGroup.IsSystemGroup)
                {
                    adjustedGroupName = TaxmlSpec.SystemGroupReservedName;
                }

                XElement groupElement = new XElement(TaxmlSpec.TermSetGroupToken,
                    new XAttribute(TaxmlSpec.NameToken, adjustedGroupName));
                termStoreElement.Add(groupElement);

                this.ProcessTaxmlComments(groupElement, termGroup);

                if (termGroup.Id != Guid.Empty)
                {
                    groupElement.Add(new XAttribute(TaxmlSpec.IdToken, termGroup.Id.ToString("B")));
                }

                this.ProcessSyncActionElement(termGroup, groupElement);

                if (!string.IsNullOrEmpty(termGroup.Description))
                {
                    groupElement.Add(new XElement(TaxmlSpec.DescriptionToken, termGroup.Description));
                }

                foreach (LocalTermSet termSet in termGroup.TermSets)
                {
                    bool isOrphanedTermsTermSet = false;
                    bool isKeywordsTermSet = false;

                    // Replace e.g. "Orphaned Terms" with the reserved symbol "|OrphanedTermsTermSet|"
                    string adjustedTermSetName = termSet.Name;
#if false
    // TODO
                    if (termGroup.IsSystemGroup)
                    {
                        if (termSet.Id == termStore.OrphanedTermsTermSet.Id)
                        {
                            adjustedTermSetName = TaxmlSpec.OrphanedTermsTermSetReservedName;
                            isOrphanedTermsTermSet = true;
                        }
                        else if (termSet.Id == this.termStoreAdapter.KeywordsTermSet.Id)
                        {
                            adjustedTermSetName = TaxmlSpec.KeywordsTermSetReservedName;
                            isKeywordsTermSet = true;
                        }
                    }
#endif
                    XElement termSetElement = new XElement(TaxmlSpec.TermSetToken,
                        new XAttribute(TaxmlSpec.NameToken, adjustedTermSetName));
                    groupElement.Add(termSetElement);

                    this.ProcessTaxmlComments(termSetElement, termSet);

                    this.ProcessSyncActionElement(termSet, termSetElement);


                    if (!isOrphanedTermsTermSet && !isKeywordsTermSet)
                    {
                        if (termSet.Id != Guid.Empty)
                        {
                            termSetElement.Add(new XAttribute(TaxmlSpec.IdToken, termSet.Id.ToString("B")));
                        }

                        if (!termSet.IsAvailableForTagging)
                        {
                            termSetElement.Add(new XAttribute(TaxmlSpec.IsAvailableForTaggingToken, false));
                        }

                        if (termSet.IsOpenForTermCreation)
                        {
                            termSetElement.Add(new XAttribute(TaxmlSpec.IsOpenForTermCreationToken, true));
                        }
                    }

                    if (!string.IsNullOrEmpty(termSet.Owner) && !isKeywordsTermSet)
                    {
                        termSetElement.Add(new XAttribute(TaxmlSpec.OwnerToken, termSet.Owner));
                    }

                    if (!string.IsNullOrEmpty(termSet.Contact) && !isKeywordsTermSet)
                    {
                        termSetElement.Add(new XAttribute(TaxmlSpec.ContactToken, termSet.Contact));
                    }

                    foreach (var localizedName in termSet.LocalizedNames)
                    {
                        if (localizedName.Lcid != termSet.DefaultLanguageLcid)
                        {
                            termSetElement.Add(
                                new XElement(TaxmlSpec.LocalizedNameToken, localizedName,
                                    new XAttribute(TaxmlSpec.LanguageToken, localizedName.Lcid))
                                );
                        }
                    }

                    // Technically the TermSet.Description property can be modified for the special "Keywords"
                    // term set, but it's not recommended, so we don't support that
                    if (!string.IsNullOrEmpty(termSet.Description) && !isOrphanedTermsTermSet && !isKeywordsTermSet)
                    {
                        termSetElement.Add(new XElement(TaxmlSpec.DescriptionToken, termSet.Description));
                    }

                    this.ProcessCustomProperties(termSet.CustomProperties, termSetElement, isLocal: false);

                    List<CustomOrderedTerm> orderedChildTerms = this.ProcessCustomSortOrder(termSet, termSetElement);

                    foreach (string stakeholder in termSet.Stakeholders)
                    {
                        termSetElement.Add(new XElement(TaxmlSpec.StakeHolderToken, stakeholder));
                    }

                    this.ProcessChildTerms(termSetElement, termSet, orderedChildTerms);
                }
            }
        }

        private void ProcessChildTerms(XElement termContainerElement,
            LocalTermContainer termContainer, List<CustomOrderedTerm> orderedTerms)
        {
            LocalTermStore termStore = termContainer.GetTermStore();

            foreach (CustomOrderedTerm customOrderedTerm in orderedTerms)
            {
                LocalTerm term = customOrderedTerm.Term;

                bool isTermLink = term.TermKind != LocalTermKind.NormalTerm;

                XElement termElement;
                if (isTermLink)
                {
                    termElement = new XElement(TaxmlSpec.TermLinkToken);
                    if (term.TermKind == LocalTermKind.TermLinkUsingId
                        && term.TermLinkNameHint.Length > 0)
                    {
                        termElement.Add(new XAttribute(TaxmlSpec.NameHintToken, term.TermLinkNameHint));
                    }
                }
                else
                {
                    termElement = new XElement(TaxmlSpec.TermToken, new XAttribute(TaxmlSpec.NameToken, term.Name));
                }

                termContainerElement.Add(termElement);
                this.ProcessTaxmlComments(termElement, term);

                if (term.Id != Guid.Empty)
                {
                    termElement.Add(new XAttribute(TaxmlSpec.IdToken, term.Id.ToString("B")));
                }

                if (customOrderedTerm.WriteInOrderAttribute)
                {
                    if (termContainer.CustomSortOrder.Contains(term.Id))
                    {
                        termElement.Add(new XAttribute(TaxmlSpec.InOrderToken, true));
                    }
                }

                if (!term.IsAvailableForTagging)
                {
                    termElement.Add(new XAttribute(TaxmlSpec.IsAvailableForTaggingToken, false));
                }

                this.ProcessSyncActionElement(term, termElement);

                if (isTermLink)
                {
                    if (!string.IsNullOrWhiteSpace(term.TermLinkSourcePath))
                    {
                        termElement.Add(new XAttribute(TaxmlSpec.TermLinkSourcePathToken, term.TermLinkSourcePath));
                    }
                    if (term.IsPinnedRoot)
                    {
                        termElement.Add(new XAttribute(TaxmlSpec.IsPinnedRootToken, true));
                    }
                }
                else
                {
                    // TODO: If the Term.Owner is the same as the parent object, can we omit it?
                    if (!string.IsNullOrEmpty(term.Owner))
                    {
                        termElement.Add(new XAttribute(TaxmlSpec.OwnerToken, term.Owner));
                    }

                    if (term.IsDeprecated)
                    {
                        termElement.Add(new XAttribute(TaxmlSpec.IsDeprecatedToken, true));
                    }

                    foreach (LocalizedString description in term.Descriptions)
                    {
                        XElement descriptionElement = new XElement(TaxmlSpec.LocalizedDescriptionToken, description);
                        termElement.Add(descriptionElement);

                        if (description.Lcid != term.DefaultLanguageLcid)
                        {
                            descriptionElement.Add(new XAttribute(TaxmlSpec.LanguageToken, description.Lcid));
                            descriptionElement.Value = description.Value;
                        }
                    }

                    this.ProcessCustomProperties(term.CustomProperties, termElement, isLocal: false);
                }

                this.ProcessCustomProperties(term.LocalCustomProperties, termElement, isLocal: true);

                List<CustomOrderedTerm> orderedChildTerms = this.ProcessCustomSortOrder(term, termElement);

                if (!isTermLink)
                {
                    foreach (LocalTermLabel label in term.Labels)
                    {
                        // If this is the Term.Name label, then it doesn't need to be specified explicitly
                        if (label.IsDefault && label.Lcid == term.DefaultLanguageLcid)
                            continue;

                        XElement labelElement = new XElement(TaxmlSpec.LabelToken, label.Value);
                        termElement.Add(labelElement);

                        if (label.Lcid != term.DefaultLanguageLcid)
                        {
                            labelElement.Add(new XAttribute(TaxmlSpec.LanguageToken, label.Lcid));
                        }

                        if (label.IsDefault)
                        {
                            labelElement.Add(new XAttribute(TaxmlSpec.IsDefaultForLanguageToken, true));
                        }
                    }
                }

                this.ProcessChildTerms(termElement, term, orderedChildTerms);
            }
        }

        private void ProcessCustomProperties(IEnumerable<KeyValuePair<string, string>> customProperties,
            XElement parentElement, bool isLocal)
        {
            foreach (KeyValuePair<string, string> pair in customProperties)
            {
                parentElement.Add(
                    new XElement(isLocal ? TaxmlSpec.LocalCustomPropertyToken : TaxmlSpec.CustomPropertyToken,
                        new XAttribute(TaxmlSpec.NameToken, pair.Key),
                        pair.Value)
                    );
            }
        }

        private List<CustomOrderedTerm> ProcessCustomSortOrder(LocalTermContainer termContainer,
            XElement containerElement)
        {
            // Can we use the InOrder attribute instead of the CustomSortOrder element?
            // To do that, we need to find a child term for each CustomSortOrder GUID
            // and return them, followed by the rest of the terms.  If we can't find 
            // a child term, then we fall back to writing the "CustomSortOrder" element
            // instead.
            var childTermsById = termContainer.Terms
                .Where(x => x.Id != Guid.Empty)
                .ToDictionary(x => x.Id);

            var resortedOrderedTerms = new List<CustomOrderedTerm>();

            foreach (var sortedId in termContainer.CustomSortOrder)
            {
                LocalTerm term;
                if (childTermsById.TryGetValue(sortedId, out term))
                {
                    resortedOrderedTerms.Add(new CustomOrderedTerm(term, writeInOrderAttribute: true));
                }
                else
                {
                    // An ID is missing, so fall back to using the "CustomSortOrder" element instead
                    var customSortOrderElement = new XElement(TaxmlSpec.CustomSortOrderToken);
                    containerElement.Add(customSortOrderElement);

                    foreach (Guid itemId in termContainer.CustomSortOrder)
                    {
                        customSortOrderElement.Add(
                            new XElement(TaxmlSpec.ItemToken,
                                new XAttribute(TaxmlSpec.IdToken, itemId.ToString("B"))
                                )
                            );
                    }
                    // Set writeInOrderAttribute=false for all terms, since we're 
                    // using the "CustomSortOrder" element
                    return termContainer.Terms
                        .Select(x => new CustomOrderedTerm(x, writeInOrderAttribute: false))
                        .ToList();
                }
            }

            var sortedIds = new HashSet<Guid>(termContainer.CustomSortOrder);
            foreach (var term in termContainer.Terms)
            {
                if (!sortedIds.Contains(term.Id))
                {
                    resortedOrderedTerms.Add(new CustomOrderedTerm(term, writeInOrderAttribute: false));
                }
            }
            return resortedOrderedTerms;
        }

        private void ProcessSyncActionElement(LocalTaxonomyItem taxonomyItem, XElement itemElement)
        {
            var syncAction = taxonomyItem.SyncAction;
            if (syncAction != null)
            {
                var syncActionElement = new XElement(TaxmlSpec.SyncActionToken);
                itemElement.Add(syncActionElement);
                syncActionElement.Add(new XAttribute(TaxmlSpec.IfMissingToken, syncAction.IfMissing.ToString()));
                syncActionElement.Add(new XAttribute(TaxmlSpec.IfPresentToken, syncAction.IfPresent.ToString()));
                syncActionElement.Add(new XAttribute(TaxmlSpec.IfElsewhereToken, syncAction.IfElsewhere.ToString()));
                syncActionElement.Add(new XAttribute(TaxmlSpec.DeleteExtraChildItemsToken,
                    syncAction.DeleteExtraChildItems));
            }
        }

        private void ProcessTaxmlComments(XElement xmlNode, LocalTaxonomyItem taxonomyItem)
        {
            foreach (string taxmlComment in taxonomyItem.TaxmlComments)
            {
                xmlNode.AddBeforeSelf(new XComment(taxmlComment));
            }
        }
    }
}
