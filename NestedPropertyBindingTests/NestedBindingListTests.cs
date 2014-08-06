// Copyright (c) 2014 Kuan Bartel, All rights reserved.
// See License.md in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using TestModel;
using Xunit;

namespace NestedPropertyBinding.Tests
{
    public class NestedBindingListTests
    {
        [Fact()]
        public void NestedPropertyListChangedEventsTest()
        {
            // Create a NestedBindingList
            var list = new NestedBindingList<Person>(1);
            Assert.Equal(5, list.ItemProperties.Count);

            // Listen for change events
            var raised = false;
            ListChangedType change = default(ListChangedType);
            list.ListChanged += (s, a) =>
                                {
                                    raised = true;
                                    change = a.ListChangedType;
                                };

            // Add a new item
            var p = new Person();
            list.Add(p);
            Assert.True(raised);
            Assert.Equal(ListChangedType.ItemAdded, change);
            raised = false;

            // Set a value
            p.Name = "Fred";
            Assert.True(raised);
            Assert.Equal(ListChangedType.ItemChanged, change);
            raised = false;

            // Set nested properties
            p.Address = new Address() { City = new City() };
            Assert.True(raised);
            Assert.Equal(ListChangedType.ItemChanged, change);
            raised = false;

            // Set a nested property deeper than the PropertyBindingDepth
            p.Address.City.Name = "Blahville";
            Assert.False(raised);

            // Remove the item
            list.Remove(p);
            Assert.True(raised);
            Assert.Equal(ListChangedType.ItemDeleted, change);
        }

        [Fact()]
        public void ListChangedEventsOnExistingListTest()
        {
            // Create a list and add an item
            var list = new List<Person>();
            var p = new Person();
            list.Add(p);

            // Create a NestedBindingList wrapping the existing list
            var nlist = new NestedBindingList<Person>(list);

            // Listen for change events
            var raised = false;
            ListChangedType change = default(ListChangedType);
            nlist.ListChanged += (s, a) =>
            {
                raised = true;
                change = a.ListChangedType;
            };

            // Set a value
            p.Name = "Fred";
            Assert.True(raised);
            Assert.Equal(ListChangedType.ItemChanged, change);
        }

        [Fact()]
        public void ListSortTest()
        {
            // Create a NestedBindingList
            var list = new NestedBindingList<Person>(2);

            // Add some items
            list.Add(new Person());
            list.Add(new Person() { Name = "Fred" });
            list.Add(new Person() { Name = "Bill" });

            Assert.Null(list[0].Name);
            
            // Sort the list on Name
            var pd = list.ItemProperties.Find("Name", false);
            Assert.NotNull(pd);
            list.ApplySort(pd, ListSortDirection.Ascending);

            Assert.Equal("Bill", list[1].Name);

            // Adding a new item will sort the list
            list.Add(new Person() { Name = "Abigale" });
            Assert.Equal("Abigale", list[1].Name);

            // Removing the sort leaves things where they were
            list.RemoveSort();
            Assert.Equal("Abigale", list[1].Name);

            // Sort the list on a nested property which has
            // intermediate null values in the list
            pd = list.ItemProperties.Find("Address.City.Name", false);
            Assert.NotNull(pd);
            list.ApplySort(pd, ListSortDirection.Descending);

            // Set nested properties will sort with null values last
            list[3].Address = new Address() { City = new City() { Name = "Blah" } };
            Assert.Equal("Fred", list[0].Name);

            list[3].Address = new Address() { City = new City() { Name = "Kablah" } };
            Assert.Equal("Bill", list[0].Name);
        }
    }
}
