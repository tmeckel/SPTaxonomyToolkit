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
using System.Globalization;
using System.Linq;
using TaxonomyToolkit.General;

namespace TaxonomyToolkit.Taxml
{
    /// <summary>
    /// Used with <see cref="LocalTaxonomyItem.Kind">LocalTaxonomyItem.Kind</see>
    /// </summary>
    public enum LocalTaxonomyItemKind
    {
        TermStore,
        TermGroup,
        TermSet,
        Term
    }

    /// <summary>
    /// Abstract base class for the generic class <see cref="LocalTaxonomyItem{T}" />.
    /// This class is analagous to the <b>TaxonomyItem</b> base class from
    /// Microsoft.SharePoint.Taxonomy.dll.
    /// <para />
    /// <see cref="LocalTaxonomyItem{T}" /> and <see cref="LocalTaxonomyItem{T}" />
    /// implement a generalized ParentItem/ChildItems pattern that simplifies traversal.
    /// </summary>
    public abstract class LocalTaxonomyItem
    {
        private readonly Guid id;

        private int defaultLanguageLcid;

        private readonly List<string> taxmlComments = new List<string>(0);

        internal LocalTaxonomyItem(Guid id, int defaultLanguageLcid)
        {
            this.id = id;

            // Validate the LCID
            CultureInfo.GetCultureInfo(defaultLanguageLcid);
            this.defaultLanguageLcid = defaultLanguageLcid;
        }

        #region Properties

        /// <summary>
        /// Indicates the type of object, which is useful when operating on the tree abstraction.
        /// </summary>
        public abstract LocalTaxonomyItemKind Kind { get; }

        public string Name
        {
            get { return this.GetName(); }
        }

        /// <summary>
        /// The globally unique identifier assigned by SharePoint for this object.
        /// If this is Guid.Empty, then the object is instead identified by its name.
        /// </summary>
        public Guid Id
        {
            get { return this.id; }
        }

        /// <summary>
        /// Indicates that some properties were omitted when this object was fetched from
        /// the server.  This flag does not refer to the ChildItems property; it is tracked
        /// separately by IncompleteChildItems.
        /// </summary>
        public bool IncompleteObject { get; set; }

        /// <summary>
        /// Indicates that objects in the ChildItems collection may have been omitted
        /// when this object was fetched from the server.
        /// </summary>
        public bool IncompleteChildItems { get; set; }

        /// <summary>
        /// For informational purposes, this preserves XML comments that occurred immediately
        /// before this object's element in the TAXML file format.
        /// </summary>
        public List<string> TaxmlComments
        {
            get { return this.taxmlComments; }
        }

        /// <summary>
        /// When syncing from LocalTermStore to the SharePoint service, this can be used
        /// to annotate items to indicate how they should be synced.  If null, the
        /// value is inherited from the parent item.
        /// </summary>
        public SyncAction SyncAction { get; set; }

        /// <summary>
        /// Multilingual properties such as Term.Name and Term.Description can store strings
        /// in various langauges.  If a string is not defined in a particular language,
        /// then the default language is used instead.  The data model guarantees that
        /// a string is always available in the default language when assigning a new
        /// default language; it does this by copying strings from the old default language
        /// if necessary.
        /// <para />
        /// When objects are joined to a tree (via LocalTaxonomyItem.ParentItem), their
        /// DefaultLanguageLcid matches the root of the tree (which is usually the
        /// LocalTermStore).  To avoid confusion, changes to the DefaultLanguageLcid must
        /// be performed on the root object.
        /// </summary>
        /// <remarks>
        /// The Taxonomy API also supports a TermStore.WorkingLanguage that specifies the language
        /// to be used by properties such as Term.Name and Term.Description.  (For example, changing
        /// the TermStore.WorkingLanguage may cause Term.Name to return different strings
        /// throughout the object tree, without any effect on the underlying data model.)
        /// This working language concept is NOT implemented by LocalTaxonomyItem; properties such
        /// as LocalTerm.Name will always read/write the term store's default language.  If you
        /// care about localization, consider using LocalTerm.GetNameWithDefault() and
        /// LocalTerm.SetName() instead of LocalTerm.Name.
        /// </remarks>
        public int DefaultLanguageLcid
        {
            get { return this.defaultLanguageLcid; }
            set
            {
                if (this.ParentItem != null)
                {
                    throw new InvalidOperationException("The DefaultLanguageLcid cannot be modified because "
                        + " it is inherited from the parent object; change the root of the tree instead.");
                }

                if (this.defaultLanguageLcid == value)
                    return;

                // Validate that a supported LCID code was provided
                CultureInfo.GetCultureInfo(value);

                this.defaultLanguageLcid = value;

                // Update all of the child objects
                foreach (LocalTaxonomyItem localTaxonomyItem in ToolkitUtilities.GetPreorder(this, x => x.ChildItems))
                {
                    if (localTaxonomyItem == this)
                        continue;
                    localTaxonomyItem.OnPrepareNewDefaultLanguageLcid(value);
                    localTaxonomyItem.defaultLanguageLcid = value;
                }
            }
        }

        public LocalTermStore GetTermStore()
        {
            for (LocalTaxonomyItem item = this; item != null; item = item.ParentItem)
            {
                if (item is LocalTermStore)
                    return (LocalTermStore) item;
            }
            return null;
        }

        #endregion

        #region #LocalTaxonomyItem Formalism

        protected abstract LocalTaxonomyItem GetParentItem();
        protected abstract void SetParentItem(LocalTaxonomyItem value);

        protected abstract void OnAddChildItem(LocalTaxonomyItem item);
        protected abstract void OnRemoveChildItem(LocalTaxonomyItem item);

        protected abstract ReadOnlyCollection<LocalTaxonomyItem> GetReadOnlyItems();

        /// <summary>
        /// The parent item in the tree, or null if there is no parent.
        /// </summary>
        public LocalTaxonomyItem ParentItem
        {
            get { return this.GetParentItem(); }
            set { this.SetParentItem(value); }
        }

        /// <summary>
        /// The child items in the tree.
        /// </summary>
        public ReadOnlyCollection<LocalTaxonomyItem> ChildItems
        {
            get { return this.GetReadOnlyItems(); }
        }

        protected void SetParentItem<T>(ref T parentItem, T newValue) where T : LocalTaxonomyItem
        {
            if (parentItem == newValue)
                return;

            if (newValue != null)
            {
                string objection = newValue.ExplainIsAllowableParentFor(this);
                if (objection != null)
                    throw new InvalidOperationException(objection);

                if (newValue.defaultLanguageLcid != this.defaultLanguageLcid)
                {
                    // We could do this automatically, but changing the default langauge can
                    // cause copying of term labels whose effects may be counterintuitive.
                    // In most cases, the child object should have been created with the same
                    // default language as its intended parent.
                    throw new InvalidOperationException("The child object cannot be attached"
                        + " unless its default language matches the parent.");
                }
            }

            if (parentItem != null)
            {
                LocalTermStore oldTermStore = parentItem.GetTermStore();
                if (oldTermStore != null)
                {
                    oldTermStore.OnBeforeRemoveSubtree(this);
                }

                parentItem.OnRemoveChildItem(this);
            }

            if (newValue != null)
            {
                LocalTermStore newTermStore = newValue.GetTermStore();
                if (newTermStore != null)
                {
                    newTermStore.OnBeforeAddSubtree(this);
                }
            }

            LocalTaxonomyItem oldValue = parentItem;
            parentItem = newValue;

            if (parentItem != null)
                parentItem.OnAddChildItem(this);

            this.OnParentItemChanged(oldValue, newValue);
        }

        protected virtual void OnParentItemChanged(LocalTaxonomyItem oldValue, LocalTaxonomyItem newValue)
        {
        }

        /// <summary>
        /// Ensures that multilingual properties will be defined for the specified newDefaultLanguageLcid,
        /// by possibly copying strings from the current DefaultLanguageLcid.
        /// </summary>
        protected virtual void OnPrepareNewDefaultLanguageLcid(int newDefaultLanguageLcid)
        {
        }

        /// <summary>
        /// Returns true if <paramref name="proposedChild" /> could be added as a child for this item.
        /// </summary>
        /// <remarks>
        /// This is mainly a topology check, e.g. to support a drag+drop user interface that
        /// uses the mouse cursor to indicate whether an item can be dragged to a new parent.
        /// The operation may fail for other reasons, e.g. the default language has not been reconciled.
        /// </remarks>
        public bool IsAllowableParentFor(LocalTaxonomyItem proposedChild)
        {
            return this.ExplainIsAllowableParentFor(proposedChild) == null;
        }

        /// <summary>
        /// Tests whether <paramref name="proposedChild" /> could be added as a child for this item.
        /// Null is returned if the operation is allowed; otherwise, an error message is returned.
        /// </summary>
        private string ExplainIsAllowableParentFor(LocalTaxonomyItem proposedChild)
        {
            if (proposedChild.Kind != this.ChildItemKind)
                return proposedChild.Kind + " cannot be a child of " + this.Kind;

            // Check for a circular reference
            if (proposedChild == this)
                return "Item cannot be a child of itself";

            if (this.IsDescendantOf(proposedChild))
                return "This operation would create a circular reference";

            return null; // no problem
        }

        /// <summary>
        /// Returns the LocalTaxonomyItemKind for child items.
        /// </summary>
        public LocalTaxonomyItemKind ChildItemKind
        {
            get { return LocalTaxonomyItem.GetChildItemKind(this.Kind); }
        }

        /// <summary>
        /// Returns the kind of child items for the specified parent item.
        /// </summary>
        public static LocalTaxonomyItemKind GetChildItemKind(LocalTaxonomyItemKind parentKind)
        {
            switch (parentKind)
            {
                case LocalTaxonomyItemKind.TermStore:
                    return LocalTaxonomyItemKind.TermGroup;
                case LocalTaxonomyItemKind.TermGroup:
                    return LocalTaxonomyItemKind.TermSet;
                case LocalTaxonomyItemKind.TermSet:
                case LocalTaxonomyItemKind.Term:
                    return LocalTaxonomyItemKind.Term;
            }
            throw new InvalidOperationException("Unsupported LocalTaxonomyItemKind value");
        }

        /// <summary>
        /// Returns true if this item is a (possibly recursive) child of <paramref name="parentItem" />
        /// </summary>
        public bool IsDescendantOf(LocalTaxonomyItem parentItem)
        {
            for (LocalTaxonomyItem ancestor = this.ParentItem; ancestor != null; ancestor = ancestor.ParentItem)
            {
                if (ancestor == parentItem)
                    return true;
            }
            return false;
        }

        protected T AddChildItem<T>(T child) where T : LocalTaxonomyItem
        {
            ToolkitUtilities.ConfirmNotNull(child, "child");
            if (child.ParentItem != null)
            {
                throw new InvalidOperationException("The " + child.Kind
                    + " cannot be added because it already belongs to another container.");
            }
            child.ParentItem = this;
            return child;
        }

        protected void RemoveChildItem<T>(T child) where T : LocalTaxonomyItem
        {
            ToolkitUtilities.ConfirmNotNull(child, "child");
            if (child.ParentItem != this)
            {
                throw new InvalidOperationException("The " + child.Kind
                    + " cannot be removed because it does not belong this container.");
            }
            child.ParentItem = null;
        }

        #endregion

        protected abstract string GetName();

        public void RemoveAllChildItems()
        {
            foreach (LocalTaxonomyItem childItem in this.ChildItems.ToArray())
            {
                childItem.ParentItem = null;
            }
        }

        public SyncAction GetEffectiveSyncAction()
        {
            if (this.SyncAction != null)
                return this.SyncAction;
            if (this.ParentItem != null)
                return this.ParentItem.GetEffectiveSyncAction();
            return SyncAction.Default;
        }

        public override string ToString()
        {
            string result = this.Kind.ToString() + ": ";
            if (this.Name != null)
                result += "\"" + this.Name + "\"";
            else
                result += "(null)";
            return result;
        }
    }

    /// <summary>
    /// Abstract base class for <see cref="LocalTermContainer" />,
    /// <see cref="LocalTermGroup" />, and <see cref="LocalTermStore" />.
    /// <para />
    /// <see cref="LocalTaxonomyItem{T}" /> and <see cref="LocalTaxonomyItem{T}" />
    /// implement a generalized ParentItem/ChildItems pattern that greatly simplifies
    /// the child classes.
    /// </summary>
    public abstract class LocalTaxonomyItem<TChild> : LocalTaxonomyItem
        where TChild : LocalTaxonomyItem
    {
        private readonly List<TChild> writableChildItems = new List<TChild>();

        internal LocalTaxonomyItem(Guid id, int defaultLanguageLcid)
            : base(id, defaultLanguageLcid)
        {
            this.ConstructReadOnlyCollection(this.writableChildItems);
        }

        protected abstract void ConstructReadOnlyCollection(List<TChild> writableChildItems);

        protected override sealed void OnAddChildItem(LocalTaxonomyItem item)
        {
            this.writableChildItems.Add((TChild) item);
        }

        protected override sealed void OnRemoveChildItem(LocalTaxonomyItem item)
        {
            this.writableChildItems.Remove((TChild) item);
        }

        protected override sealed ReadOnlyCollection<LocalTaxonomyItem> GetReadOnlyItems()
        {
            return new ReadOnlyCollection<LocalTaxonomyItem>(
                new CastedList<LocalTaxonomyItem, TChild>(this.writableChildItems)
                );
        }
    }
}
