// Copyright (c) 2014 Kuan Bartel, All rights reserved.
// See License.md in the project root for license information.

using System;
using System.Linq;
using TestModel;
using Xunit;

namespace NestedPropertyBinding.Tests
{
    public class NestedPropertyDescriptorTests
    {
        [Fact()]
        public void NestedPropertyDescriptorTest()
        {
            string propname = "Address.City.Name";
            var npd = new NestedPropertyDescriptor(typeof(Person), propname);
            Assert.Equal(propname, npd.Name);
            Assert.Equal(2, npd.PropertyDepth);
            Assert.Equal(typeof(Person), npd.ComponentType);
            Assert.Equal(typeof(string), npd.PropertyType);
        }

        [Fact()]
        public void GetSetValueTest()
        {
            string cityname = "Brisbane";
            var a = new Address()
            {
                City = new City() { Name = cityname, State = State.QLD }
            };
            Person p = new Person
            {
                Address = a
            };
            string propname = "Address.City.Name";
            var npd = new NestedPropertyDescriptor(typeof(Person), propname);

            // Get the value
            Assert.Equal(p.Address.City.Name, npd.GetValue(p));
            Assert.Equal(cityname, npd.GetValue(p));

            // Set the city name
            cityname = "Gold Coast";

            npd.SetValue(p, cityname);
            Assert.Equal(p.Address.City.Name, npd.GetValue(p));
            Assert.Equal(cityname, npd.GetValue(p));
        }

        [Fact()]
        public void GetNestedPropertiesTest()
        {
            // Test zero level properties with no value types - same as TypeDescriptor.GetProperties
            var props = NestedPropertyDescriptor.GetNestedProperties(typeof(Person), 0).ToList();
            Assert.Equal(3, props.Count);

            // Test single level properties with no value types
            props = NestedPropertyDescriptor.GetNestedProperties(typeof(Person), 1).ToList();
            Assert.Equal(5, props.Count);

            // Test two level properties with no value types
            props = NestedPropertyDescriptor.GetNestedProperties(typeof(Person), 2).ToList();
            Assert.Equal(8, props.Count);

            // Test zero level properties with value types - same as TypeDescriptor.GetProperties
            props = NestedPropertyDescriptor.GetNestedProperties(typeof(Person), 0, true).ToList();
            Assert.Equal(3, props.Count);

            // Test single level properties with value types
            props = NestedPropertyDescriptor.GetNestedProperties(typeof(Person), 1, true).ToList();
            Assert.Equal(6, props.Count);

            // Test two level properties with value types
            props = NestedPropertyDescriptor.GetNestedProperties(typeof(Person), 2, true).ToList();
            Assert.Equal(10, props.Count);
        }

#if SupportChangeEvents
        /// <summary>
        /// Note: This is probably wrong.
        /// A handler can only be added if all objects in the tree are non-null
        /// and the handler will be lost if an intermediate property is changed.
        /// </summary>
        [Fact()]
        public void ValueChangedEventHandlerTest()
        {
            // Create a person with null children
            var p = new Person();
            bool raised = false;

            // Create a NestedPropertyDescriptor
            var prop = "Address.City.Name";
            var pd = new NestedPropertyDescriptor(typeof(Person), prop);

            EventHandler handler = (s, a) => raised = true;
            // Try to add an eventhandler
            Assert.Throws<ArgumentNullException>(
                    () => pd.AddValueChanged(p, handler));

            // Set values so there's no nulls in the tree
            p.Address = new Address() { City = new City() };
            // Now the eventhandler can be added
            Assert.DoesNotThrow(
                    () => pd.AddValueChanged(p, handler));

            // Setting a different property directly on an
            // INotifyPropertyChanged object will not raise the event
            p.Address.City.PostCode = "1234";
            Assert.False(raised);

            // Setting the property directly on an INotifyPropertyChanged
            // object will raise the event
            p.Address.City.Name = "Blah";
            Assert.True(raised);
            raised = false;

            // Setting the property using the PropertyDescriptor
            // will raise the event
            pd.SetValue(p, "Blahville");
            Assert.True(raised);
            raised = false;

            // Setting an intermediate property will remove the handler
            p.Address.City = new City();
            Assert.False(raised);
            p.Address.City.Name = "Blahed";
            Assert.False(raised);
            pd.SetValue(p, "No Blah");
            Assert.False(raised);
        }
#else

        /// <summary>
        /// Note: This is also probably wrong.
        /// A handler can be added whether all objects in the tree are null or not.
        /// The handler will be called only when the property is set using the
        /// PropertyDescriptor.
        /// </summary>
        [Fact()]
        public void ValueChangedEventHandlerTest()
        {
            // Create a person with null children
            var p = new Person();
            bool raised = false;

            // Create a NestedPropertyDescriptor
            var prop = "Address.City.Name";
            var pd = new NestedPropertyDescriptor(typeof(Person), prop);

            EventHandler handler = (s, a) => raised = true;
            // Try to add an eventhandler
            Assert.DoesNotThrow(
                    () => pd.AddValueChanged(p, handler));

            // Set values so there's no nulls in the tree
            p.Address = new Address() { City = new City() };
            Assert.False(raised);

            // Setting a different property directly on an
            // INotifyPropertyChanged object will not raise the event
            p.Address.City.PostCode = "1234";
            Assert.False(raised);

            // Setting the property directly on an INotifyPropertyChanged
            // object will not raise the event
            p.Address.City.Name = "Blah";
            Assert.False(raised);

            // Setting the property using the PropertyDescriptor
            // will raise the event
            pd.SetValue(p, "Blahville");
            Assert.True(raised);
            raised = false;

            // Setting an intermediate property will not remove the handler
            p.Address.City = new City();
            Assert.False(raised);
            p.Address.City.Name = "Blahed";
            Assert.False(raised);
            pd.SetValue(p, "No Blah");
            Assert.True(raised);
        }

#endif
    }
}