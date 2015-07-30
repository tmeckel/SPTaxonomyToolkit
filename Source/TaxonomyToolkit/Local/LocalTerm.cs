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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using TaxonomyToolkit.General;

namespace TaxonomyToolkit.Taxml
{
    /// <summary>
    ///     Used with <see cref="LocalTerm.Linkage">LocalTerm.Linkage</see>
    /// </summary>
    public enum LocalTermKind
    {
        /// <summary>
        ///     This is a regular term (that an act as the source term for term links).
        /// </summary>
        NormalTerm,

        /// <summary>
        ///     This is a term link whose source term is found using LocalTerm.Id,
        ///     which cannot be Guid.Empty.
        /// </summary>
        TermLinkUsingId,

        /// <summary>
        ///     This is a term link whose source term is found using
        ///     LocalTerm.TermLinkSourcePath, which cannot be empty.
        /// </summary>
        TermLinkUsingPath
    }

    /// <summary>
    ///     Represents a taxonomy term in the LocalTermStore object model.
    /// </summary>
    public sealed class LocalTerm : LocalTermContainer
    {
        #region class SharedData

        private sealed class SharedData
        {
            public readonly LocalPropertyBag CustomProperties = new LocalPropertyBag();

            public readonly SortedDictionary<int, string> DescriptionByLcid = new SortedDictionary<int, string>();

            // The first string in the list is the default label
            public readonly Dictionary<int, List<string>> LabelsByLcid = new Dictionary<int, List<string>>();

            public string Owner = "";
            public bool IsDeprecated = false;

            // The first item in the list is interpreted to be the "source term"
            public readonly List<LocalTerm> TermInstances = new List<LocalTerm>();

            public void CopyFrom(SharedData source)
            {
                this.Owner = source.Owner;
                this.IsDeprecated = source.IsDeprecated;

                SharedData.CopyDictionary(this.CustomProperties, source.CustomProperties);
                SharedData.CopyDictionary(this.DescriptionByLcid, source.DescriptionByLcid);
                SharedData.CopyDictionary(this.LabelsByLcid, source.LabelsByLcid);
            }

            private static void CopyDictionary<TKey, TValue>(IDictionary<TKey, TValue> target,
                IDictionary<TKey, TValue> source)
            {
                target.Clear();
                foreach (var pair in source)
                    target.Add(pair);
            }
        }

        #endregion

        #region class TermLinkData

        private class TermLinkData
        {
            public string TermLinkNameHint = "";
            public List<string> TermLinkSourcePathParts = null;
            public bool IsPinnedRoot = false;
        }

        #endregion

        private LocalTermContainer parentItem;

        // Either sharedDataIfSourceTerm is not null, or termLinkData is not null
        private SharedData sharedDataIfSourceTerm;
        private TermLinkData termLinkData;

        private readonly LocalPropertyBag localCustomProperties = new LocalPropertyBag();

        #region Constructors

        private LocalTerm(Guid id, string name, int defaultLanguageLcid)
            : base(id, defaultLanguageLcid)
        {
            this.sharedDataIfSourceTerm = new SharedData();
            this.termLinkData = null;

            this.AddLabel(name, defaultLanguageLcid, setAsDefaultLabel: true);

            Debug.Assert(this.TermKind == LocalTermKind.NormalTerm);
        }

        public static LocalTerm CreateTerm(Guid id, string name,
            int defaultLanguageLcid = LocalTermStore.EnglishLanguageLcid)
        {
            return new LocalTerm(id, name, defaultLanguageLcid);
        }

        private LocalTerm(Guid id, string nameHint, bool isPinnedRoot)
            : base(id)
        {
            ToolkitUtilities.ConfirmNotNull(nameHint, "nameHint");

            if (id == Guid.Empty)
                throw new ArgumentException("The term link requires a non-empty Guid for the Id", "id");

            this.sharedDataIfSourceTerm = null;
            this.termLinkData = new TermLinkData();
            Debug.Assert(this.TermKind == LocalTermKind.TermLinkUsingId);
            this.TermLinkNameHint = nameHint;
            this.IsPinnedRoot = isPinnedRoot;
        }

        public static LocalTerm CreateTermLinkUsingId(Guid id, string nameHint = "", bool isPinnedRoot = false)
        {
            return new LocalTerm(id, nameHint, isPinnedRoot);
        }

        public static LocalTerm CreateTermLinkUsingId(LocalTerm sourceTerm, bool isPinnedRoot = false)
        {
            if (!sourceTerm.IsSourceTerm)
                throw new InvalidOperationException("Cannot create a link to a term with IsSourceTerm=false");
            return LocalTerm.CreateTermLinkUsingId(sourceTerm.Id, sourceTerm.Name, isPinnedRoot);
        }

        private LocalTerm(string termLinkSourcePath, bool isPinnedRoot)
            : base(Guid.Empty)
        {
            ToolkitUtilities.ConfirmNotNull(termLinkSourcePath, "termLinkSourcePath");
            this.sharedDataIfSourceTerm = null;
            this.termLinkData = new TermLinkData();
            this.termLinkData.TermLinkSourcePathParts = LocalTerm.ParseTermLinkSourcePath(termLinkSourcePath);

            Debug.Assert(this.TermKind == LocalTermKind.TermLinkUsingPath);
        }

        public static LocalTerm CreateTermLinkUsingPath(string termLinkSourcePath, bool isPinnedRoot = false)
        {
            return new LocalTerm(termLinkSourcePath, isPinnedRoot);
        }

        #endregion

        #region LocalTermContainer Boilerplate

        public override LocalTaxonomyItemKind Kind
        {
            get { return LocalTaxonomyItemKind.Term; }
        }

        public new LocalTermContainer ParentItem
        {
            get { return this.parentItem; }
            set { this.SetParentItem(ref this.parentItem, value); }
        }

        protected override LocalTaxonomyItem GetParentItem()
        {
            return this.ParentItem;
        }

        protected override void SetParentItem(LocalTaxonomyItem value)
        {
            this.ParentItem = (LocalTermContainer) value;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     The SharePoint taxonomy allows a single term to be "reused" in multiple term sets,
        ///     with one of the instances designated as the "source term", and all of the instances
        ///     having the same Guid and sharing certain property data.  LocalTerm members such as
        ///     SourceTerm/IsReused/etc conform to the familiar semantics of the SharePoint API.
        ///     However unlike the SharePoint API, a LocalTerm may be disconnected from the  object graph.
        ///     To handle this case, we introduce a termonology that refers to the reused instances
        ///     as "term links" and distinguishes two different ways of referencing the source term.
        ///     (Note that the source term may be entirely absent from the LocalTermStore, and
        ///     in that case the shared state is undefined and inaccessible.)  The TAXML format
        ///     represents this via a special XML element for term links, which ensures that
        ///     the shared state is always expressed as a property of the source term.
        /// </summary>
        public LocalTermKind TermKind
        {
            get
            {
                if (this.sharedDataIfSourceTerm != null)
                {
                    Debug.Assert(this.termLinkData == null);
                    return LocalTermKind.NormalTerm;
                }
                Debug.Assert(this.termLinkData != null);
                if (this.Id != Guid.Empty)
                {
                    Debug.Assert(this.termLinkData.TermLinkSourcePathParts == null);
                    return LocalTermKind.TermLinkUsingId;
                }
                Debug.Assert(this.termLinkData.TermLinkSourcePathParts.Count > 2);
                return LocalTermKind.TermLinkUsingPath;
            }
        }

        /// <summary>
        ///     Indicates whether this term is reused in more than one term set.
        ///     If IsSourceTerm=true, then IsReused returns true if there are any other
        ///     terms in the LocalTermStore with the same LocalTerm.Id.
        ///     If IsSourceTerm=false, then IsReused always returns true.
        /// </summary>
        public bool IsReused
        {
            get
            {
                if (this.TermKind != LocalTermKind.NormalTerm)
                    return true;

                LocalTermStore termStore = this.GetTermStore();
                if (termStore == null)
                    return false;

                return termStore.GetTermsWithId(this.Id).Count > 1;
            }
        }

        /// <summary>
        ///     Returns true if this term is pinned and is the root of the pinned tree.
        ///     Since pinning is a kind of term reuse, the IsPinnedRoot property cannot
        ///     be true unless IsSourceTerm=false.
        /// </summary>
        public bool IsPinnedRoot
        {
            get
            {
                if (this.TermKind == LocalTermKind.NormalTerm)
                    return false;
                return this.termLinkData.IsPinnedRoot;
            }
            set
            {
                if (this.IsPinnedRoot == value)
                    return;
                this.RequireTermLink();
                this.termLinkData.IsPinnedRoot = value;
            }
        }

        /// <summary>
        ///     Returns the "source term" instance that is used for security permissions.
        ///     This returns the current object if it is the source term, or if the Term has
        ///     only one instance (i.e. IsReused=false).
        /// </summary>
        public LocalTerm SourceTerm
        {
            get
            {
                if (this.IsSourceTerm)
                    return this;

                LocalTermStore termStore = this.GetTermStore();
                if (termStore != null)
                {
                    var reusedTerms = termStore.GetTermsWithId(this.Id);
                    if (reusedTerms.Count > 0)
                    {
                        if (reusedTerms[0].IsSourceTerm)
                            return reusedTerms[0];
                    }
                }

                return null;
            }
        }

        /// <summary>
        ///     Specifies whether this term is the source term instance.
        /// </summary>
        public bool IsSourceTerm
        {
            get
            {
                return this.TermKind == LocalTermKind.NormalTerm;
            }
        }

        /// <summary>
        ///     When representing a reused term, typically the source term is matched using the LocalTerm.Id.
        ///     However, TermLinkSourcePath provides an alternative way to identify the source term
        ///     by specifying a chain of semicolon-delimited names (e.g. "My Group;My Term Set;My Source Term").
        ///     This is useful if the GUID for the source term is unknown or was randomly assigned.
        /// </summary>
        public string TermLinkSourcePath
        {
            get
            {
                if (this.TermKind != LocalTermKind.TermLinkUsingPath)
                    return "";
                return string.Join(";", this.termLinkData.TermLinkSourcePathParts);
            }
            set
            {
                if (value == this.TermLinkSourcePath)
                    return;
                if (this.TermKind != LocalTermKind.TermLinkUsingPath)
                {
                    throw new InvalidOperationException(
                        "The TermLinkSourcePath property cannot be used unless TermKind=TermLinkUsingPath");
                }
                this.termLinkData.TermLinkSourcePathParts = LocalTerm.ParseTermLinkSourcePath(value);
            }
        }

        /// <summary>
        ///     Returns the list of all instances of this term (i.e. itself and any reused instances).
        /// </summary>
        public ReadOnlyCollection<LocalTerm> TermInstances
        {
            get
            {
                LocalTermStore termStore = this.GetTermStore();
                if (termStore != null)
                {
                    return termStore.GetTermsWithId(this.Id);
                }
                return new ReadOnlyCollection<LocalTerm>(new LocalTerm[0]);
            }
        }

        /// <summary>
        ///     Returns the Name of the Term.
        /// </summary>
        public new string Name
        {
            get { return this.GetNameWithDefault(this.DefaultLanguageLcid); }
            set { this.SetName(value, this.DefaultLanguageLcid); }
        }

        /// <summary>
        ///     If the object is a term link, the name is determined by the source term.
        ///     If the source term is not available, LocalTerm can store an informational
        ///     "name hint" which is not used, but may be useful for diagnostic purposes
        ///     to give an idea which term is being linked.
        /// </summary>
        public string TermLinkNameHint
        {
            get
            {
                switch (this.TermKind)
                {
                    case LocalTermKind.TermLinkUsingId:
                        return this.termLinkData.TermLinkNameHint;
                    case LocalTermKind.TermLinkUsingPath:
                        return this.termLinkData.TermLinkSourcePathParts.Last();
                    case LocalTermKind.NormalTerm:
                    default:
                        return "";
                }
            }
            set
            {
                if (this.TermLinkNameHint == value)
                    return;
                if (this.TermKind != LocalTermKind.TermLinkUsingId)
                {
                    throw new InvalidOperationException(
                        "The TermLinkNameHint property can only be assigned when Kind=TermLinkUsingId");
                }

                string normalizedNameHint = "";
                if (value.Length > 0)
                    normalizedNameHint = ToolkitUtilities.GetNormalizedTaxonomyName(value, "TermLinkNameHint");
                this.termLinkData.TermLinkNameHint = normalizedNameHint;
            }
        }

        public override IDictionary<string, string> CustomProperties
        {
            get
            {
                SharedData sharedData = this.GetSharedDataFromSourceTerm(exceptionIfMissing: false);
                if (sharedData == null)
                    return null;
                return sharedData.CustomProperties;
            }
        }

        public IDictionary<string, string> LocalCustomProperties
        {
            get { return this.localCustomProperties; }
        }

        /// <summary>
        ///     This returns the Term description for each language LCID.  To modify the descriptions,
        ///     use SetDescription().
        /// </summary>
        public ReadOnlyCollection<LocalizedString> Descriptions
        {
            get
            {
                SharedData sharedData = this.GetSharedDataFromSourceTerm(exceptionIfMissing: false);
                if (sharedData == null)
                    return new ReadOnlyCollection<LocalizedString>(new LocalizedString[0]);

                return new ReadOnlyCollection<LocalizedString>(
                    sharedData.DescriptionByLcid.Select(
                        pair => new LocalizedString(pair.Key, pair.Value)
                        )
                        .OrderBy(description => description.Lcid)
                        .ToArray()
                    );
            }
        }

        /// <summary>
        ///     Returns all labels that are defined for all languages.  To modify the labels,
        ///     use AddLabel() or DeleteLabel().
        /// </summary>
        public ReadOnlyCollection<LocalTermLabel> Labels
        {
            get
            {
                SharedData sharedData = this.GetSharedDataFromSourceTerm(exceptionIfMissing: false);
                if (sharedData == null)
                    return new ReadOnlyCollection<LocalTermLabel>(new LocalTermLabel[0]);

                List<LocalTermLabel> list = new List<LocalTermLabel>(sharedData.LabelsByLcid.Sum(x => x.Value.Count));

                // The individual lists are already sorted, but we need to sort the lists themselves by LCID
                foreach (var pair in sharedData.LabelsByLcid.OrderBy(x => x.Key))
                {
                    bool isDefault = true; // the first item in the list is the default label
                    foreach (string label in pair.Value)
                    {
                        list.Add(new LocalTermLabel(pair.Key, label, isDefault));
                        isDefault = false;
                    }
                }

                return new ReadOnlyCollection<LocalTermLabel>(list);
            }
        }


        /// <summary>
        ///     Specifies the user account for the owner of the Term.
        /// </summary>
        public string Owner
        {
            get
            {
                SharedData sharedData = this.GetSharedDataFromSourceTerm(exceptionIfMissing: false);
                if (sharedData == null)
                    return "";
                return sharedData.Owner ?? "";
            }
            set
            {
                ToolkitUtilities.ConfirmNotNull(value, "value");
                SharedData sharedData = this.GetSharedDataFromSourceTerm(exceptionIfMissing: true);
                sharedData.Owner = value.Trim();
            }
        }

        /// <summary>
        ///     Indicates whether the Term has been marked as deprecated.
        /// </summary>
        public bool IsDeprecated
        {
            get
            {
                SharedData sharedData = this.GetSharedDataFromSourceTerm(exceptionIfMissing: false);
                if (sharedData == null)
                    return false;
                return sharedData.IsDeprecated;
            }
            set
            {
                SharedData sharedData = this.GetSharedDataFromSourceTerm(exceptionIfMissing: true);
                sharedData.IsDeprecated = value;
            }
        }

        #endregion

        private void RequireNormalTerm()
        {
            if (this.TermKind != LocalTermKind.NormalTerm)
            {
                throw new InvalidOperationException(
                    "This operation cannot be performed because the object is a term link (i.e. IsSourceTerm=true)");
            }
        }

        private void RequireTermLink()
        {
            if (this.TermKind == LocalTermKind.NormalTerm)
            {
                throw new InvalidOperationException(
                    "This operation requires the term to be a term link (i.e. IsSourceTerm=false)");
            }
        }

        private SharedData GetSharedDataFromSourceTerm(bool exceptionIfMissing)
        {
            LocalTerm sourceTerm = this.SourceTerm;
            if (sourceTerm == null)
            {
                if (exceptionIfMissing)
                {
                    throw new InvalidOperationException(
                        "This operation cannot be performed because the source term is not part of the tree");
                }
                return null;
            }

            Debug.Assert(sourceTerm.sharedDataIfSourceTerm != null);
            return sourceTerm.sharedDataIfSourceTerm;
        }

        protected override string GetName()
        {
            return this.Name;
        }

        public void AddLabel(string newLabel, int lcid, bool setAsDefaultLabel)
        {
            // Validate the LCID
            CultureInfo.GetCultureInfo(lcid);

            string normalizedName = ToolkitUtilities.GetNormalizedTaxonomyName(newLabel, "newLabel");

            SharedData sharedData = this.GetSharedDataFromSourceTerm(exceptionIfMissing: true);

            List<string> labelList = null;
            if (!sharedData.LabelsByLcid.TryGetValue(lcid, out labelList))
            {
                labelList = new List<string>();
                sharedData.LabelsByLcid.Add(lcid, labelList);
            }

            int existingIndex = labelList.IndexOf(normalizedName);
            if (existingIndex >= 0)
                labelList.RemoveAt(existingIndex);

            if (setAsDefaultLabel)
            {
                // TODO: Check for sibling terms that are already using this name
                // (this constraint only applies to the default label in the default language)

                labelList.Insert(0, normalizedName);
            }
            else
            {
                labelList.Add(normalizedName);
            }

            // Alphabetize the remaining labels
            labelList.Sort(1, labelList.Count - 1, StringComparer.Ordinal);
        }

        public void DeleteLabel(string label, int lcid)
        {
            string normalizedName = ToolkitUtilities.GetNormalizedTaxonomyName(label, "label");

            SharedData sharedData = this.GetSharedDataFromSourceTerm(exceptionIfMissing: true);

            List<string> labelList = null;
            if (!sharedData.LabelsByLcid.TryGetValue(lcid, out labelList))
                return;

            int index = labelList.IndexOf(normalizedName);
            if (index < 0)
                throw new InvalidOperationException("The specified label does not exist");

            if (lcid == this.DefaultLanguageLcid && labelList.Count == 1)
            {
                throw new InvalidOperationException(
                    "The label cannot be deleted because it is the default language label");
            }

            labelList.RemoveAt(index);
        }

        public void ClearLabels(string defaultLabel)
        {
            string normalizedName = ToolkitUtilities.GetNormalizedTaxonomyName(defaultLabel, "defaultLabel");

            SharedData sharedData = this.GetSharedDataFromSourceTerm(exceptionIfMissing: true);
            sharedData.LabelsByLcid.Clear();
            this.SetName(defaultLabel, this.DefaultLanguageLcid);
        }

        /// <summary>
        ///     Returns the default label for the specified language.  If there is no label
        ///     in the requested language, then the default language is used instead.
        /// </summary>
        public string GetNameWithDefault(int lcid)
        {
            SharedData sharedData = this.GetSharedDataFromSourceTerm(exceptionIfMissing: false);
            if (sharedData == null)
                return "<missing source term>";

            List<string> labelList = null;
            if (!sharedData.LabelsByLcid.TryGetValue(lcid, out labelList)
                || labelList.Count == 0)
            {
                lcid = this.DefaultLanguageLcid;

                if (!sharedData.LabelsByLcid.TryGetValue(lcid, out labelList)
                    || labelList.Count == 0)
                {
                    // This method is called by the property getter.  It is an invalid state,
                    // but we don't want to throw an exception because this could occur in a debugger
                    // tool tip
                    return "<invalid state>";
                }
            }
            return labelList[0];
        }

        /// <summary>
        ///     Assigns the default label for the specified language
        /// </summary>
        public void SetName(string name, int lcid)
        {
            this.AddLabel(name, lcid, setAsDefaultLabel: true);
        }

        public override string ToString()
        {
            switch (this.TermKind)
            {
                case LocalTermKind.TermLinkUsingId:
                {
                    string result = "TermLink: ";
                    if (this.TermLinkNameHint.Length > 0)
                        result += "Hint=\"" + this.TermLinkNameHint + "\" ";
                    if (this.IsPinnedRoot)
                        result += "IsPinnedRoot=true ";
                    result += this.Id;
                    return result;
                }
                case LocalTermKind.TermLinkUsingPath:
                {
                    string result = "TermLink: \"" + this.TermLinkSourcePath + "\"";
                    if (this.IsPinnedRoot)
                        result += " IsPinnedRoot=true";
                    return result;
                }
            }
            return base.ToString();
        }

        /// <summary>
        ///     Searches DescriptionByLcid for a description in the specified language; if not found,
        ///     then an empty string is returned.
        /// </summary>
        public string GetDescription(int lcid)
        {
            SharedData sharedData = this.GetSharedDataFromSourceTerm(exceptionIfMissing: false);
            if (sharedData == null)
                return "";

            string result;
            if (!sharedData.DescriptionByLcid.TryGetValue(lcid, out result))
                return "";

            return result;
        }

        /// <summary>
        ///     Assigns the description for the specified language.
        /// </summary>
        public void SetDescription(string description, int lcid)
        {
            // Validate the LCID
            CultureInfo.GetCultureInfo(lcid);

            ToolkitUtilities.ConfirmNotNull(description, "description");

            if (description.Contains('\t'))
                throw new ArgumentException("The description cannot contain tab characters", "description");

            if (description.Length > 0x3e8)
                throw new ArgumentException("The description exceeds the maximum allowable length");

            SharedData sharedData = this.GetSharedDataFromSourceTerm(exceptionIfMissing: true);

            if (description.Length == 0)
            {
                sharedData.DescriptionByLcid.Remove(lcid);
            }
            else
            {
                sharedData.DescriptionByLcid[lcid] = description;
            }
        }

        public void ClearDescriptions()
        {
            SharedData sharedData = this.GetSharedDataFromSourceTerm(exceptionIfMissing: true);
            sharedData.DescriptionByLcid.Clear();
        }

        public ReadOnlyCollection<string> GetTermLinkSourcePathParts()
        {
            if (this.TermKind != LocalTermKind.TermLinkUsingPath)
            {
                throw new InvalidOperationException(
                    "The TermLinkSourcePath is invalid because TermKind is not TermLinkUsingPath");
            }
            return new ReadOnlyCollection<string>(this.termLinkData.TermLinkSourcePathParts);
        }

        /// <summary>
        ///     Returns the concatenated names of this term's parents, followed by this term.
        ///     For example, if A has child term B, which has child term C, then the path is "A;B;C".
        /// </summary>
        public string GetPath()
        {
            LocalTerm parentTerm = this.ParentItem as LocalTerm;

            if (parentTerm == null)
                return this.Name;
            return parentTerm.GetPath() + ";" + this.Name;
        }

        protected override void OnParentItemChanged(LocalTaxonomyItem oldValue, LocalTaxonomyItem newValue)
        {
            base.OnParentItemChanged(oldValue, newValue);
        }

        protected override void OnPrepareNewDefaultLanguageLcid(int newDefaultLanguageLcid)
        {
            base.OnPrepareNewDefaultLanguageLcid(newDefaultLanguageLcid);

            this.SetName(this.GetNameWithDefault(newDefaultLanguageLcid), newDefaultLanguageLcid);
        }

        #region Static Helpers

        private static List<string> ParseTermLinkSourcePath(string value)
        {
            List<string> parts = new List<string>();
            var splitParts = value.Split(';');
            if (splitParts.Length < 3)
            {
                throw new ArgumentException(
                    "The TermLinkSourcePath must contain at least three semicolon-delimited names (group, term set, term).");
            }
            foreach (string splitPart in splitParts)
            {
                parts.Add(ToolkitUtilities.GetNormalizedTaxonomyName(splitPart, "part"));
            }
            return parts;
        }

        #endregion
    }

    /// <summary>
    ///     This class is used to represent the LocalTerm.Descriptions strings.
    ///     It is also the base class for LocalTermLabel.
    /// </summary>
    [DebuggerDisplay("{DebugText}")]
    public class LocalizedString : IEquatable<LocalizedString>
    {
        private readonly int lcid;
        private readonly string value;

        internal LocalizedString(int lcid, string value)
        {
            ToolkitUtilities.ConfirmNotNull(value, "value");
            this.lcid = lcid;
            this.value = value;
        }

        public int Lcid
        {
            get { return this.lcid; }
        }

        public string Value
        {
            get { return this.value; }
        }

        public bool Equals(LocalizedString other) // IEquatable<LocalizedString>
        {
            return this.lcid == other.lcid && this.value == other.value;
        }

        public override bool Equals(object other)
        {
            return this.Equals((LocalizedString) other);
        }

        public override int GetHashCode()
        {
            return ToolkitUtilities.CombineHashCodes(this.lcid.GetHashCode(), this.value.GetHashCode());
        }

        public override string ToString()
        {
            return this.value ?? "(null)";
        }

        protected virtual string DebugText
        {
            get { return string.Format("(lcid={0}) {1}", this.Lcid, this.Value); }
        }
    }

    /// <summary>
    ///     This class is used to represent the LocalTerm.Labels items.
    /// </summary>
    public class LocalTermLabel : LocalizedString, IEquatable<LocalTermLabel>
    {
        private readonly bool isDefault;

        internal LocalTermLabel(int lcid, string value, bool isDefault)
            : base(lcid, value)
        {
            this.isDefault = isDefault;
        }

        public bool IsDefault
        {
            get { return this.isDefault; }
        }

        public bool Equals(LocalTermLabel other) // IEquatable<LocalTermLabel>
        {
            if (!base.Equals(other))
                return false;

            return this.isDefault == other.isDefault;
        }

        public override bool Equals(object other)
        {
            return this.Equals((LocalTermLabel) other);
        }

        public override int GetHashCode()
        {
            return ToolkitUtilities.CombineHashCodes(this.isDefault.GetHashCode(), base.GetHashCode());
        }

        protected override string DebugText
        {
            get
            {
                if (this.isDefault)
                    return string.Format("(default, lcid={0}) {1}", this.Lcid, this.Value);
                else
                    return string.Format("(lcid={0}) {1}", this.Lcid, this.Value);
            }
        }
    }
}
