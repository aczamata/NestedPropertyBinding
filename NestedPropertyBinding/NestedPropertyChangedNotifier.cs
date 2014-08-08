// Copyright (c) 2014 Kuan Bartel, All rights reserved.
// See License.md in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace NestedPropertyBinding
{
    /// <summary>
    /// A helper class which raises <see cref="PropertyChanged" /> events for an object
    /// and nested properties which implement <see cref="INotifyPropertyChanged" />.
    /// </summary>
    public class NestedPropertyChangedNotifier<T> : INotifyPropertyChanged
    {
        private int depth;
        private Dictionary<string, NestedPropertyDescriptor> properties;
        private Dictionary<string, InnerNotifier> notifiers;

        /// <summary>
        /// Create a new <see cref="NestedPropertyChangeNotifier{T}" /> for type T and the
        /// given depth of nested properties and assigns the specified object for
        /// listening and raising PropertyChanged events for.
        /// </summary>
        public NestedPropertyChangedNotifier(T obj, int depth = 1)
            : this(depth)
        {
            Object = obj;
        }

        /// <summary>
        /// Create a new <see cref="NestedPropertyChangeNotifier{T}" /> for type T and the
        /// given depth of nested properties.
        /// </summary>
        public NestedPropertyChangedNotifier(int depth = 1)
        {
            CascadeNotifications = true;

            this.depth = depth;
            var pds = NestedPropertyDescriptor.GetNestedProperties(typeof(T), depth);
            properties = pds.ToDictionary(pd => pd.Name);
            var npcPds = pds.Where(npd =>
                                typeof(INotifyPropertyChanged).IsAssignableFrom(npd.PropertyType)
                                && npd.PropertyDepth != depth);

            notifiers = new Dictionary<string, InnerNotifier>();

            foreach (var npd in npcPds)
            {
                var notifier = new InnerNotifier(npd);
                notifiers[npd.Name] = notifier;

                notifier.PropertyChanged += NestedPropertyChanged;
            }
        }

        /// <summary>
        /// Private copy constructor. Used by <see cref="CreateNew" />.
        /// </summary>
        private NestedPropertyChangedNotifier(NestedPropertyChangedNotifier<T> copyFrom, T obj)
        {
            CascadeNotifications = copyFrom.CascadeNotifications;
            depth = copyFrom.depth;
            properties = new Dictionary<string, NestedPropertyDescriptor>(copyFrom.properties);
            notifiers = new Dictionary<string, InnerNotifier>();

            var npcPds = properties.Select(p => p.Value).Where(npd =>
                    typeof(INotifyPropertyChanged).IsAssignableFrom(npd.PropertyType)
                    && npd.PropertyDepth != depth);

            foreach (var npd in npcPds)
            {
                var notifier = new InnerNotifier(npd);
                notifiers[npd.Name] = notifier;

                notifier.PropertyChanged += NestedPropertyChanged;
            }

            Object = obj;
        }

        private T _object;

        /// <summary>
        /// The object to raise <see cref="PropertyChanged" /> events for.
        /// </summary>
        public T Object
        {
            get { return _object; }
            set
            {
                var comparer = EqualityComparer<T>.Default;
                if (!comparer.Equals(_object, value))
                {
                    var npc = _object as INotifyPropertyChanged;
                    if (null != npc)
                        npc.PropertyChanged -= NestedPropertyChanged;

                    _object = value;

                    npc = _object as INotifyPropertyChanged;
                    if (null != npc)
                    {
                        npc.PropertyChanged += NestedPropertyChanged;
                    }

                    UpdateNotifiers();
                }
            }
        }

        /// <summary>
        /// Whether <see cref="PropertyChanged" /> events should be raised for nested
        /// properties when a higher level property is changed.
        /// </summary>
        public bool CascadeNotifications { get; set; }

        public NestedPropertyChangedNotifier<T> CreateNew(T obj)
        {
            return new NestedPropertyChangedNotifier<T>(this, obj);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        private void NestedPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateNotifiers(e.PropertyName);
            if (CascadeNotifications)
            {
                foreach (var prop in properties.Where(p => p.Key.StartsWith(e.PropertyName)).Select(p => p.Value))
                {
                    OnPropertyChanged(prop.Name);
                }
            }
            else
                OnPropertyChanged(e.PropertyName);
        }

        /// <summary>
        /// After the <see cref="Object" /> is set update all notifiers.
        /// </summary>
        private void UpdateNotifiers()
        {
            foreach (var notifier in notifiers.Values)
            {
                notifier.UpdateObject(Object);
            }
        }

        /// <summary>
        /// After a property changes then update all nested notifiers which will have changed.
        /// </summary>
        private void UpdateNotifiers(string PropertyName)
        {
            foreach (var notifier in notifiers.Where(p => p.Key.StartsWith(PropertyName)).Select(p => p.Value))
            {
                notifier.UpdateObject(Object);
            }
        }

        /// <summary>
        /// Class to wrap each nested INotifyPropertyChanged object and raise
        /// PropertyChanged events using nested property names.
        /// </summary>
        private class InnerNotifier
        {
            internal InnerNotifier(PropertyDescriptor property)
            {
                DebugCheck.NotNull(property, "property");
                DebugCheck.True(property, "property", pd => typeof(INotifyPropertyChanged).IsAssignableFrom(pd.PropertyType));

                Property = property;
            }

            internal PropertyDescriptor Property { get; private set; }

            private object _object;

            internal object Object
            {
                get { return _object; }
                private set
                {
                    if (value != _object)
                    {
                        var npc = _object as INotifyPropertyChanged;
                        if (null != npc)
                            npc.PropertyChanged -= Object_PropertyChanged;

                        _object = value;

                        npc = _object as INotifyPropertyChanged;
                        if (null != npc)
                        {
                            npc.PropertyChanged += Object_PropertyChanged;
                        }
                    }
                }
            }

            internal void UpdateObject(object component)
            {
                Object = Property.GetValue(component);
            }

            private void Object_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                OnPropertyChanged(string.Format("{0}.{1}", Property.Name, e.PropertyName));
            }

            internal event PropertyChangedEventHandler PropertyChanged;

            internal void OnPropertyChanged(string name)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }
            }
        }
    }
}