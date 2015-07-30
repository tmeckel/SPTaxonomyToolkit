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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TaxonomyToolkit.General;

namespace TaxonomyToolkitTests
{
    [TestClass]
    public class PriorityQueueUnitTests
    {
        private class TestItem : IComparable<TestItem>
        {
            public int Value;

            public TestItem(int value)
            {
                this.Value = value;
            }

            public int CompareTo(TestItem other) // IComparable<TestItem>
            {
                return this.Value.CompareTo(other.Value);
            }
        }

        [TestMethod]
        public void PriorityQueueSortsCorrectly()
        {
            PriorityQueue<TestItem> queue = new PriorityQueue<TestItem>();

            Random random = new Random(11111);
            List<int> list = new List<int>();
            for (int i = 0; i < 1000; ++i)
            {
                int randomNumber = random.Next(1000);
                list.Add(randomNumber);
                queue.Add(new TestItem(randomNumber));
            }
            list.Sort();

            int index = 0;
            for (;;)
            {
                TestItem item = queue.RemoveTop();
                if (item == null)
                    break;
                Assert.AreEqual(list[index], item.Value);
                ++index;
            }
            Assert.AreEqual(index, list.Count);
        }

        [TestMethod]
        public void PriorityQueueEnumeratesCorrectly()
        {
            PriorityQueue<TestItem> queue = new PriorityQueue<TestItem>();

            Random random = new Random(22222);
            List<int> list = new List<int>();
            for (int i = 0; i < 1000; ++i)
            {
                int randomNumber = random.Next(1000);
                list.Add(randomNumber);
                queue.Add(new TestItem(randomNumber));
            }
            list.Sort();

            int index = 0;
            foreach (TestItem item in queue.EnumerateOrdered())
            {
                Assert.AreEqual(list[index], item.Value);
                ++index;
            }
            Assert.AreEqual(index, list.Count);
        }

        [TestMethod]
        public void MixedAddAndRemove()
        {
            PriorityQueue<TestItem> queue = new PriorityQueue<TestItem>();

            Random random = new Random(33333);
            List<int> list = new List<int>();
            for (int i = 0; i < 1000; ++i)
            {
                int randomNumber = random.Next(1000);

                if ((randomNumber%3) == 0 && queue.Count > 0)
                {
                    // Remove an item
                    int removedNumber = queue.RemoveTop().Value;
                    list.Remove(removedNumber);
                }
                else
                {
                    // Add an item
                    list.Add(randomNumber);
                    queue.Add(new TestItem(randomNumber));
                }
            }
            list.Sort();

            int index = 0;
            for (;;)
            {
                TestItem item = queue.RemoveTop();
                if (item == null)
                    break;
                Assert.AreEqual(list[index], item.Value);
                ++index;
            }
            Assert.AreEqual(index, list.Count);
        }
    }
}
