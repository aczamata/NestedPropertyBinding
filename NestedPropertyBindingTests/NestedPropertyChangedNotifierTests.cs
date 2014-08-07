// Copyright (c) 2014 Kuan Bartel, All rights reserved.
// See License.md in the project root for license information.

using System.ComponentModel;
using TestModel;
using Xunit;

namespace NestedPropertyBinding.Tests
{
    public class NestedPropertyChangedNotifierTests
    {
        /// <summary>
        /// Test <see cref="PropertyChanged"/> events for nested
        /// <see cref="INotifyPropertyChanged"/> objects.
        /// </summary>
        [Fact()]
        public void NestedINotifyPropertyChangedPropertiesTest()
        {
            var notifier = new NestedPropertyChangedNotifier<Person>(2);
            var p = new Person();
            notifier.Object = p;

            Assert.PropertyChanged(notifier,
                                   "ID",
                                   () => p.ID = 1);
            Assert.PropertyChanged(notifier,
                                   "Address",
                                   () => p.Address = new Address());
            Assert.PropertyChanged(notifier,
                                   "Address.City",
                                   () => p.Address.City = new City());
        }

        private class NoNotify
        {
            public Person Person { get; set; }

            public string Blah { get; set; }
        }

        /// <summary>
        /// Test <see cref="PropertyChanged"/> events for nested
        /// <see cref="INotifyPropertyChanged"/> objects, where the top
        /// level object doesn't implement <see cref="INotifyPropertyChanged"/>.
        /// </summary>
        [Fact()]
        public void LowerLevelPropertiesTest()
        {
            var myobj = new NoNotify() { Person = new Person(), Blah = "Blah" };
            var notifier = new NestedPropertyChangedNotifier<NoNotify>(3);
            notifier.Object = myobj;

            // Change some values and make sure that events are raised
            bool raised = false;
            notifier.PropertyChanged += ((s, a) => raised = true);

            myobj.Person.ID = 1;
            Assert.True(raised);
            raised = false;

            myobj.Person.Address = new Address();
            Assert.True(raised);
            raised = false;

            myobj.Person.Address.City = new City();
            Assert.True(raised);
            raised = false;

            // Changing a value on the top level object won't raise anything
            myobj.Blah = "No Blah";
            Assert.False(raised);
        }
    }
}