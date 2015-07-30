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

using System.Collections.Generic;

namespace TaxonomyToolkit.General
{
    /// <summary>
    ///     An adapter that converts a list to another list type by casting the individual elements.
    ///     This is useful because C# 3.0 lacks covariant conversions for parameterized types.
    /// </summary>
    public class CastedList<TTarget, TSource> : IList<TTarget>
        where TTarget : class
        where TSource : class
    {
        private readonly IList<TSource> Source;

        public CastedList(IList<TSource> source)
        {
            this.Source = source;
        }

        private TTarget AsTarget(TSource item)
        {
            return (TTarget) (object) item;
        }

        private TSource AsSource(TTarget item)
        {
            return (TSource) (object) item;
        }

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable) this.Source).GetEnumerator();
        }

        #endregion

        #region IEnumerable<TTarget> Members

        public IEnumerator<TTarget> GetEnumerator()
        {
            foreach (TSource item in this.Source)
                yield return this.AsTarget(item);
        }

        #endregion

        #region ICollection<TTarget> Members

        public void Add(TTarget item)
        {
            this.Source.Add(this.AsSource(item));
        }

        public void Clear()
        {
            this.Source.Clear();
        }

        public bool Contains(TTarget item)
        {
            return this.Source.Contains(this.AsSource(item));
        }

        public void CopyTo(TTarget[] array, int arrayIndex)
        {
            int count = this.Source.Count;
            for (int i = 0; i < count; ++i)
                array[arrayIndex + i] = this.AsTarget(this.Source[i]);
        }

        public int Count
        {
            get { return this.Source.Count; }
        }

        public bool IsReadOnly
        {
            get { return this.Source.IsReadOnly; }
        }

        public bool Remove(TTarget item)
        {
            return this.Source.Remove(this.AsSource(item));
        }

        #endregion

        #region IList<TTarget> Members

        public int IndexOf(TTarget item)
        {
            return this.Source.IndexOf(this.AsSource(item));
        }

        public void Insert(int index, TTarget item)
        {
            this.Source.Insert(index, this.AsSource(item));
        }

        public void RemoveAt(int index)
        {
            this.Source.RemoveAt(index);
        }

        public TTarget this[int index]
        {
            get { return this.AsTarget(this.Source[index]); }
            set { this.Source[index] = this.AsSource(value); }
        }

        #endregion
    }
}
