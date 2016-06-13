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
using System.Text.RegularExpressions;

namespace TaxonomyToolkit.General
{
    public static class ToolkitUtilities
    {
        private static Version toolkitVersion = null;
        private static Version sharePointClientVersion = null;

        /// <summary>
        /// The current release of the TaxonomyToolkit library.
        /// </summary>
        public static Version ToolkitVersion
        {
            get
            {
                if (ToolkitUtilities.toolkitVersion == null)
                {
                    var assembly = typeof (ToolkitUtilities).Assembly;
                    var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                    ToolkitUtilities.toolkitVersion = Version.Parse(fileVersionInfo.FileVersion);
                }
                return ToolkitUtilities.toolkitVersion;
            }
        }

        /// <summary>
        /// The version of the Microsoft.SharePoint.Client runtime that was loaded by
        /// the TaxonomyToolkit library.
        /// </summary>
        public static Version SharePointClientVersion
        {
            get
            {
                if (ToolkitUtilities.sharePointClientVersion == null)
                {
                    var assembly = typeof(Microsoft.SharePoint.Client.Taxonomy.TaxonomySession).Assembly;
                    var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                    ToolkitUtilities.sharePointClientVersion = Version.Parse(fileVersionInfo.FileVersion);
                }
                return ToolkitUtilities.sharePointClientVersion;
            }
        }

        /// <summary>
        /// Used with <see cref="GetPreorder{T}" />
        /// </summary>
        public delegate IEnumerable<T> GetChildren<T>(T node);

        /// <summary>
        /// This returns an enumerator that traverses a tree in the preorder sequence.
        /// The child nodes are specified by the <paramref name="getChildren" /> delegate.
        /// </summary>
        public static IEnumerable<T> GetPreorder<T>(T node, GetChildren<T> getChildren)
        {
            yield return node;
            foreach (T child in getChildren(node))
            {
                foreach (T item in ToolkitUtilities.GetPreorder(child, getChildren))
                    yield return item;
            }
        }

        public static IEnumerable<List<T>> GetBatches<T>(IEnumerable<T> collection, int batchSize)
        {
            List<T> batch = new List<T>(batchSize);
            foreach (T item in collection)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }
            if (batch.Count > 0)
                yield return batch;
        }

        public static void ConfirmNotNull<T>(T argument, string argumentName)
            where T : class
        {
            if (argument == null)
                throw new ArgumentNullException(argumentName);
        }

        // Based on Tuple.CombineHashCodes()
        public static int CombineHashCodes(int h1, int h2)
        {
            return unchecked(((h1 << 5) + h1) ^ h2);
        }

        /// <summary>
        /// This performs a similar operation as TaxonomyItem.NormalizeName(), and also checks
        /// that the name is valid.
        /// </summary>
        public static string GetNormalizedTaxonomyName(string name)
        {
            return ToolkitUtilities.GetNormalizedTaxonomyName(name, "name");
        }

        internal static string GetNormalizedTaxonomyName(string name, string argumentName)
        {
            ToolkitUtilities.ConfirmNotNull(name, argumentName);

            string normalizedName = Regex.Replace(name, @"\s+", " ");
            normalizedName = normalizedName.Replace('&', (char) 0xff06).Replace('"', (char) 0xff02);

            if (normalizedName.Length > 255)
                throw new ArgumentException("The name length cannot exceed 255 characters: \"" + normalizedName + "\"");

            normalizedName = normalizedName.Trim();
            if (normalizedName.Length == 0)
                throw new ArgumentException("The name cannot be empty");

            if (!ToolkitUtilities.validNameRegex.IsMatch(normalizedName))
                throw new ArgumentException("The name contains invalid characters: \"" + normalizedName + "\"");

            return normalizedName;
        }

        /// <summary>
        /// TaxonomyItem.NormalizeName() replaces ampersands with a Unicode character.  This form is
        /// used throughout the Taxonomy API and should be used in code, but when displayed in the
        /// UI it looks wrong in many fonts.  You can use GetDenormalizedTaxonomyNameForDisplay()
        /// to convert it back to the regular ampersand.
        /// </summary>
        public static string GetDenormalizedTaxonomyNameForDisplay(string name)
        {
            ToolkitUtilities.ConfirmNotNull(name, "name");
            return name.Replace((char) 0xff06, '&');
        }

        private static Regex validNameRegex = new Regex("^[^;\"<>|&\\t]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}
