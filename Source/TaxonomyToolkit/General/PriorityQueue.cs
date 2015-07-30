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

namespace TaxonomyToolkit.General
{
    /// <summary>
    /// A basic priority queue implemented using a pairing heap.  This handy
    /// little data structure is easy to implement and has very good performance:
    /// all operations are O(1) amortized, except for deletion which is O(log(n)) amortized.
    /// </summary>
    public class PriorityQueue<T>
        where T : class
    {
        private class Heap
        {
            public T Item;
            public Heap FirstChild = null;
            public Heap NextSibling = null;

            public Heap(T item, Heap firstChild)
            {
                this.Item = item;
                this.FirstChild = firstChild;
            }
        }

        private Heap root;
        private int count;
        private int version = 0;
        private IComparer<T> comparer;

        public PriorityQueue(IComparer<T> comparer = null)
        {
            this.comparer = comparer;
            if (this.comparer == null)
                this.comparer = Comparer<T>.Default;

            this.Clear();
        }

        public void Clear()
        {
            ++this.version;
            this.root = null;
            this.count = 0;
        }

        public int Count
        {
            get { return this.count; }
        }

        /// <summary>
        /// Adds the item to the heap.
        /// </summary>
        /// <remarks>This operation takes O(1) time amortized.</remarks>
        public void Add(T item)
        {
            ++this.version;
            this.root = this.MergeHeapPair(new Heap(item, null), this.root);
            ++this.count;
        }

        /// <summary>
        /// Gets the top item from the heap without removing it.
        /// The "top" item is the one whose value is smallest, as defined
        /// by the T.CompareTo() method.
        /// </summary>
        /// <remarks>This operation takes O(1) time.</remarks>
        public T PeekTop()
        {
            if (this.root == null)
            {
                Debug.Assert(this.count == 0);
                return null;
            }
            return this.root.Item;
        }

        /// <summary>
        /// Removes and returns the top item from the heap.
        /// The "top" item is the one whose value is smallest, as defined
        /// by the T.CompareTo() method.
        /// </summary>
        /// <remarks>This operation takes O(log(n)) time amortized.</remarks>
        public T RemoveTop()
        {
            ++this.version;
            if (this.root == null)
            {
                Debug.Assert(this.count == 0);
                return null;
            }
            T top = this.root.Item;

            this.root = this.MergeChildList(this.root.FirstChild);
            --this.count;
            return top;
        }

        /// <summary>
        /// Enumerates the items in random order.
        /// </summary>
        /// <remarks>This operation takes O(n) time.</remarks>
        public IEnumerable<T> EnumerateUnordered()
        {
            if (this.root == null)
                yield break;

            int enumeratedVersion = this.version;

            Stack<Heap> stack = new Stack<Heap>();
            stack.Push(this.root);

            while (stack.Count > 0)
            {
                Heap current = stack.Pop();
                do
                {
                    yield return current.Item;

                    if (this.version != enumeratedVersion)
                        throw new InvalidOperationException("The object was changed while enumerating");

                    if (current.FirstChild != null)
                        stack.Push(current.FirstChild);

                    current = current.NextSibling;
                } while (current != null);
            }
        }

        /// <summary>
        /// Enumerates the items in sorted order.
        /// </summary>
        /// <remarks>The algorithm uses heapsort and thus takes O(n*log(n)) time.</remarks>
        public IEnumerable<T> EnumerateOrdered()
        {
            int enumeratedVersion = this.version;

            T[] array = new T[this.Count];
            int index = 0;
            foreach (T item in this.EnumerateUnordered())
            {
                array[index++] = item;
            }
            Debug.Assert(index == this.Count);
            Array.Sort(array);

            for (int i = 0; i < array.Length; ++i)
            {
                yield return array[i];
                if (this.version != enumeratedVersion)
                    throw new InvalidOperationException("The object was changed while enumerating");
            }
        }

        #region Helpers

        private Heap MergeHeapPair(Heap heap1, Heap heap2)
        {
            if (heap1 == null)
                return heap2;
            if (heap2 == null)
                return heap1;

            if (this.comparer.Compare(heap1.Item, heap2.Item) < 0)
            {
                heap2.NextSibling = heap1.FirstChild;
                return new Heap(heap1.Item, firstChild: heap2);
            }
            else
            {
                heap1.NextSibling = heap2.FirstChild;
                return new Heap(heap2.Item, firstChild: heap1);
            }
        }

        private Heap MergeChildList(Heap heap)
        {
            Heap first = heap;
            if (first == null)
                return null;

            Heap second = heap.NextSibling;
            first.NextSibling = null;

            if (second == null)
                return first;

            Heap rest = second.NextSibling;
            second.NextSibling = null;

            Heap merged1 = this.MergeHeapPair(first, second);
            Heap merged2 = this.MergeChildList(rest);
            Heap merged3 = this.MergeHeapPair(merged1, merged2);
            return merged3;
        }

        #endregion
    }
}
