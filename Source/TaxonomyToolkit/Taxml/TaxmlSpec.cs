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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using TaxonomyToolkit.General;

namespace TaxonomyToolkit.Taxml
{
    /// <summary>
    /// Provides common definitions used for reading/writing the Taxml XML format.
    /// </summary>
    [SuppressMessage("Microsoft.StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented")]
    public static class TaxmlSpec
    {
        public class Versions
        {
            public static Version V2_0 = new Version(2, 0, 0, 0);
            public static Version V2_1 = new Version(2, 1, 0, 0);
            public static Version V2_2 = new Version(2, 2, 0, 0);

            // This should match the version number in TaxmlFile.xsd.
            public static Version Current = Versions.V2_2;
            public static Version OldestLoadable = Versions.V2_0;
        }

#pragma warning disable 1591

        // Elements
        public const string ContributorToken = "Contributor";
        public const string CustomPropertyToken = "Property";
        public const string CustomSortOrderToken = "CustomSortOrder";
        public const string LabelToken = "Label";
        public const string LocalCustomPropertyToken = "LocalProperty";
        public const string SyncActionToken = "SyncAction";
        public const string TermLinkToken = "TermLink";
        public const string TermSetGroupToken = "TermSetGroup";
        public const string TermSetToken = "TermSet";
        public const string TermStoreToken = "TermStore";
        public const string TermToken = "Term";
        public const string TaxmlFileToken = "TaxmlFile";

        // Attributes
        public const string ContactToken = "Contact";
        public const string DeleteExtraChildItemsToken = "DeleteExtraChildItems";
        public const string DefaultLanguageToken = "DefaultLanguage";
        public const string DescriptionToken = "Description";
        public const string IdToken = "Id";
        public const string IfElsewhereToken = "IfElsewhere";
        public const string IfMissingToken = "IfMissing";
        public const string IfPresentToken = "IfPresent";
        public const string InOrderToken = "InOrder";
        public const string IsAvailableForTaggingToken = "IsAvailableForTagging";
        public const string IsDefaultForLanguageToken = "IsDefaultForLanguage";
        public const string IsDeprecatedToken = "IsDeprecated";
        public const string IsOpenForTermCreationToken = "IsOpenForTermCreation";
        public const string IsPinnedRootToken = "IsPinnedRoot";
        public const string ItemToken = "Item";
        public const string LanguageToken = "Language";
        public const string LocalizedDescriptionToken = "LocalizedDescription";
        public const string LocalizedNameToken = "LocalizedName";
        public const string NameHintToken = "NameHint";
        public const string NameToken = "Name";
        public const string OpenToken = "Open";
        public const string OwnerToken = "Owner";
        public const string ReuseTermToken = "ReuseTerm";
        public const string ReuseTreeToken = "ReuseTree";
        public const string StakeHolderToken = "Stakeholder";
        public const string TermLinkSourcePathToken = "TermLinkSourcePath";
        public const string VersionToken = "Version";

        // Reserved names
        public const string KeywordsTermSetReservedName = "|KeywordsTermSet|";
        public const string OrphanedTermsTermSetReservedName = "|OrphanedTermsTermSet|";
        public const string SiteCollectionGroupReservedName = "|SiteCollectionGroup|";
        public const string SystemGroupReservedName = "|SystemGroup|";

        private static XmlSchemaSet taxmlSchema = null;

#pragma warning restore 1591

        /// <summary>
        /// Returns an XSD schema for the Taxml XML format.
        /// </summary>
        public static XmlSchemaSet TaxmlSchema
        {
            get
            {
                if (TaxmlSpec.taxmlSchema == null)
                {
                    XmlSchemaSet schemaSet = new XmlSchemaSet();
                    try
                    {
                        TaxmlSpec.ImportResource(
                            typeof (TaxmlSpec),
                            "TaxonomyToolkit.Taxml", "TaxmlFile.xsd",
                            delegate(StreamReader reader) { schemaSet.Add("", XmlReader.Create(reader)); }
                            );
                    }
                    catch (XmlSchemaException exception)
                    {
                        ParseException.RethrowWithLineInfo(exception);
                    }

                    TaxmlSpec.taxmlSchema = schemaSet;
                }
                return TaxmlSpec.taxmlSchema;
            }
        }

        /// <summary>
        /// Retrieves the named resource from the assembly where "typeInAssembly" is defined, and performs
        /// cleanup of the stream resources after the "action" is completed.  If preferredNamespace is not null
        /// then initially it will try to load the resource with the specified name, and failing that it
        /// will probe for a match.  For example, if preferredNamespace="a.b.c" and resourceName="d.e",
        /// then if "a.b.c.d.e" cannot be found, then e.g. "f.d.e" would be accepted as a match, provided
        /// there were not other conflicting matches such as "g.h.d.e".  This facilitates reuse of
        /// source files across different DLL projects.
        /// </summary>
        internal static void ImportResource(Type typeInAssembly, string preferredNamespace, string resourceName,
            Action<StreamReader> action)
        {
            Assembly assembly = Assembly.GetAssembly(typeInAssembly);

            Stream stream = null;

            string fullResourceName = string.IsNullOrEmpty(preferredNamespace)
                ? resourceName
                : preferredNamespace + "." + resourceName;

            // Try initially to load the expected full name
            stream = assembly.GetManifestResourceStream(fullResourceName);

            try
            {
                // If the initial attempt failed, then probe for an unambiguous match
                if (stream == null)
                {
                    Debug.WriteLine("Probing for resource \"" + resourceName + "\"");

                    string[] matches = assembly.GetManifestResourceNames()
                        .Where(name => name.EndsWith("." + resourceName, StringComparison.Ordinal)).ToArray();

                    if (matches.Length == 0)
                    {
                        // If the resource is not found, check that:
                        // 1. The TaxmlSpec.xsd file is added to your project and marked as "Embedded Resource"
                        //    in Visual Studio (or its equivalent "/resource:" line in the "sources" file)
                        // 2. The "typeof" line above refers to a class in the same DLL that contains the XSD resource
                        // 3. The resource name is correct -- to check the name, open your DLL using .NET Reflector 
                        //    and look in the "Resources" branch of the TreeView
                        throw new InvalidOperationException("Unable to find resource \"" + fullResourceName + "\"");
                    }

                    if (matches.Length > 1)
                    {
                        throw new InvalidOperationException("The resource \"" + fullResourceName
                            + "\" could not be resolved unambiguously");
                    }
                    string matchedName = matches[0];
                    Debug.WriteLine("Loading resource \"" + matchedName + "\"");

                    stream = assembly.GetManifestResourceStream(matchedName);

                    if (stream == null)
                    {
                        throw new InvalidOperationException("Failed to load resource \"" + matchedName + "\"");
                    }
                }


                using (StreamReader reader = new StreamReader(stream))
                {
                    action(reader);
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
            }
        }

        /// <summary>
        /// Returns true if the specified name is a system reserved name such
        /// as TaxmlSpec.KeywordsTermSetReservedName.
        /// </summary>
        public static bool IsReservedName(string name)
        {
            return name.StartsWith("|", StringComparison.Ordinal)
                && name.EndsWith("|", StringComparison.Ordinal);
        }
    }
}
