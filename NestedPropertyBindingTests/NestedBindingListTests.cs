// Copyright (c) 2014 Kuan Bartel, All rights reserved.
// See License.md in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
            string propname = null;
            list.ListChanged += (s, a) =>
                                {
                                    raised = true;
                                    change = a.ListChangedType;
                                    if (a.PropertyDescriptor != null)
                                        propname = a.PropertyDescriptor.Name;
                                };

            // Add a new item
            var p = new Person() { Address = new Address() };
            list.Add(p);
            Assert.True(raised);
            Assert.Equal(ListChangedType.ItemAdded, change);
            raised = false;

            // Set a value
            p.Name = "Fred";
            Assert.True(raised);
            Assert.Equal(ListChangedType.ItemChanged, change);
            Assert.Equal("Name", propname);
            raised = false;
            propname = null;

            // Set nested properties
            p.Address.City = new City();
            Assert.True(raised);
            Assert.Equal(ListChangedType.ItemChanged, change);
            Assert.Equal("Address.City", propname);
            raised = false;
            propname = null;

            // Set a nested property deeper than the PropertyBindingDepth
            p.Address.City.Name = "Blahville";
            Assert.False(raised);

            // Remove the item
            list.Remove(p);
            Assert.True(raised);
            Assert.Equal(ListChangedType.ItemDeleted, change);
            raised = false;

            // Changing a property on the removed item won't raise event
            p.Name = "Fredrick";
            Assert.False(raised);

            // Add a few new items
            list.Add(new Person() { Address = new Address() { City = new City() } });
            list.Add(new Person());
            list.Add(null); // Null value
            raised = false;

            // Change the PropertyBindingDepth
            list.PropertyBindingDepth = 0;
            Assert.True(raised);
            Assert.Equal(ListChangedType.Reset, change);
            raised = false;

            // Change a property and make sure it still raises the event
            list[0].Name = "Bill";
            Assert.True(raised);
            Assert.Equal(ListChangedType.ItemChanged, change);
            Assert.Equal("Name", propname);
            raised = false;
            propname = null;

            // Set a nested property deeper than the new PropertyBindingDepth
            // but within the old depth shouldn't raise the event
            list[0].Address.Street = "23 Foo Ave";
            Assert.False(raised);

            // Remove a null value
            list.RemoveAt(2);
            Assert.True(raised);
            Assert.Equal(ListChangedType.ItemDeleted, change);
            raised = false;

            // Replace a value
            p = list[1];
            list[1] = new Person();
            Assert.True(raised);
            Assert.Equal(ListChangedType.ItemChanged, change);
            raised = false;

            // Old item doesn't raise events
            p.Name = "Bob";
            Assert.False(raised);

            // New item does
            list[1].Name = "Sherrie";
            Assert.True(raised);
            Assert.Equal(ListChangedType.ItemChanged, change);
            Assert.Equal("Name", propname);
            raised = false;
            propname = null;

            // Clear the list
            list.Clear();
            Assert.True(raised);
            Assert.Equal(ListChangedType.Reset, change);
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

            Assert.True(list.Select(f => f.Name)
                            .SequenceEqual(new string[] { null, "Fred", "Bill" }));

            // Sort the list on Name
            list.SortBy("Name");
            Assert.True(list.Select(f => f.Name)
                            .SequenceEqual(new string[] { null, "Bill", "Fred" }));

            // Adding a new item will sort the list
            list.Add(new Person() { Name = "Abigale" });
            Assert.True(list.Select(f => f.Name)
                            .SequenceEqual(new string[] { null, "Abigale", "Bill", "Fred" }));

            // Using AddNew will add a new item to the end of the list and won't sort
            var item = list.AddNew();
            Assert.Same(list[4], item);

            // Modifying the new item also won't sort the list
            item.Name = "Emily";
            Assert.Same(list[4], item);

            // Calling EndNew will sort
            list.EndNew(4);
            Assert.True(list.Select(f => f.Name)
                            .SequenceEqual(new string[] { null, "Abigale", "Bill", "Emily", "Fred" }));

            // Removing the sort leaves things where they were
            list.RemoveSort();
            Assert.True(list.Select(f => f.Name)
                            .SequenceEqual(new string[] { null, "Abigale", "Bill", "Emily", "Fred" }));

            // Sort the list on a nested property which has some
            // intermediate null values in the list - descending
            item = list[3];
            item.Address = new Address() { City = new City() { Name = "Brisbane" } };
            list.SortBy("Address.City.Name", false, false);
            Assert.Same(item, list[0]);

            // Setting nested properties will sort
            item = list[3];
            item.Address = new Address() { City = new City() { Name = "Melbourne" } };
            Assert.Same(item, list[0]);

            // Adding a new item will sort on the nested property
            item = new Person() { Name = "Gertrude", Address = new Address() { City = new City() { Name = "Perth" } } };
            list.Add(item);
            Assert.Same(item, list[0]);
        }
    }
}