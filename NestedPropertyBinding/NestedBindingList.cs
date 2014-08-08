// Copyright (c) 2014 Kuan Bartel, All rights reserved.
// Copyright (c) 2007 Novell, Inc.
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace NestedPropertyBinding
{
    [SerializableAttribute]
    public class NestedBindingList<T> : Collection<T>,
        IBindingList, ICancelAddNew, IRaiseItemChangedEvents, ITypedList
    {
        #region Private fields

        private bool allow_edit = true;
        private bool allow_remove = true;
        private bool allow_new;
        private bool allow_new_set;

        private bool raise_list_changed_events = true;

        private bool type_has_default_ctor;
        private bool type_raises_item_changed_events;

        private bool adding_new;
        private bool add_pending;
        private int pending_add_index;

        private List<T> inner_list;
        private NestedPropertyChangedNotifier<T> notifier_default;
        private Dictionary<T, NestedPropertyChangedNotifier<T>> notifiers;

        #endregion Private fields

        #region Constructors

        public NestedBindingList(int depth = 1)
            : this(new List<T>(), depth)
        {
        }

        public NestedBindingList(List<T> list, int depth = 1)
            : base(Check.NotNull(list, "list"))
        {
            PropertyBindingDepth = Check.WithinRange(depth, "depth", d => d >= 0);
            inner_list = list;

            Initialise();
        }

        private void Initialise()
        {
            ConstructorInfo ci = typeof(T).GetConstructor(Type.EmptyTypes);
            type_has_default_ctor = (ci != null);
            type_raises_item_changed_events = typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(T));

            // Add PropertyChanged event handlers for existing items
            if (type_raises_item_changed_events)
            {
                UpdateNotifiers();
            }
        }

        #endregion Constructors

        #region Properties

        public bool AllowEdit
        {
            get { return allow_edit; }
            set
            {
                if (allow_edit != value)
                {
                    allow_edit = value;

                    if (raise_list_changed_events)
                        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1 /* XXX */));
                }
            }
        }

        public bool AllowNew
        {
            get
            {
                /* if the user explicitly set it, return that value */
                if (allow_new_set)
                    return allow_new;

                /* if the list type has a default constructor we allow new */
                if (type_has_default_ctor)
                    return true;

                /* if the user adds a delegate, we return true even if
                   the type doesn't have a default ctor */
                if (AddingNew != null)
                    return true;

                return false;
            }
            set
            {
                // this funky check (using AllowNew instead of allow_new allows us to keep
                // the logic for the 3 cases in one place (the getter) instead of spreading
                // them around the file (in the ctor, in the AddingNew add handler, etc).
                if (AllowNew != value)
                {
                    allow_new_set = true;

                    allow_new = value;

                    if (raise_list_changed_events)
                        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1 /* XXX */));
                }
            }
        }

        public bool AllowRemove
        {
            get { return allow_remove; }
            set
            {
                if (allow_remove != value)
                {
                    allow_remove = value;

                    if (raise_list_changed_events)
                        OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1 /* XXX */));
                }
            }
        }

        /// <summary>
        /// Gets whether the items in the list are sorted.
        /// </summary>
        public bool IsSorted
        {
            get { return SortProperty != null; }
        }

        private PropertyDescriptorCollection _ItemProperties;

        /// <summary>
        /// Gets a <see cref="PropertyDescriptorCollection"/> for the object properties which may be bound to.
        /// </summary>
        public PropertyDescriptorCollection ItemProperties
        {
            get
            {
                if (_ItemProperties == null)
                {
                    if (_bindingDepth == 0)
                        _ItemProperties = TypeDescriptor.GetProperties(typeof(T));
                    else
                        _ItemProperties = new PropertyDescriptorCollection(
                                NestedPropertyDescriptor.GetNestedProperties(typeof(T), _bindingDepth).ToArray());
                }

                return _ItemProperties;
            }
        }

        private int _bindingDepth = 0;

        /// <summary>
        /// The depth for which nested properties can be bound to. A depth of 0 indicates
        /// to only use the properties directly on the object with no nested properties.
        /// </summary>
        public int PropertyBindingDepth
        {
            get { return _bindingDepth; }
            set
            {
                if (value != _bindingDepth)
                {
                    if (value < 0)
                        throw new ArgumentOutOfRangeException("value");

                    _bindingDepth = value;
                    _ItemProperties = null;
                    notifier_default = new NestedPropertyChangedNotifier<T>(PropertyBindingDepth);

                    OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
                }
            }
        }

        public bool RaisesItemChangedEvents
        {
            get { return type_raises_item_changed_events; }
        }

        public bool RaiseListChangedEvents
        {
            get { return raise_list_changed_events; }
            set { raise_list_changed_events = value; }
        }

        /// <summary>
        /// Gets the direction of the sort.
        /// </summary>
        public ListSortDirection SortDirection { get; private set; }

        /// <summary>
        /// Gets the <see cref="PropertyDescriptor" /> that is being used for sorting.
        /// </summary>
        public PropertyDescriptor SortProperty { get; private set; }

        public bool SupportsChangeNotification
        {
            get { return true; }
        }

        public bool SupportsSearching
        {
            get { return true; }
        }

        public bool SupportsSorting
        {
            get { return true; }
        }

        #endregion Properties

        public event AddingNewEventHandler AddingNew;

        public event ListChangedEventHandler ListChanged;

        #region Methods

        #region Public methods

        public T AddNew()
        {
            if (!AllowNew)
                throw new InvalidOperationException();

            AddingNewEventArgs args = new AddingNewEventArgs();

            OnAddingNew(args);

            T new_obj = (T)args.NewObject;
            if (new_obj == null)
            {
                if (!type_has_default_ctor)
                    throw new InvalidOperationException();

                new_obj = (T)Activator.CreateInstance(typeof(T));
            }

            adding_new = true;

            // Add the item to the base list
            Add(new_obj);

            adding_new = false;

            pending_add_index = IndexOf(new_obj);
            add_pending = true;

            return new_obj;
        }

        public void ApplySort(PropertyDescriptor prop, ListSortDirection direction)
        {
            if (PropertyComparer<T>.CanSort(prop.PropertyType))
            {
                inner_list.Sort(new PropertyComparer<T>(prop, direction));
                SortDirection = direction;
                SortProperty = prop;
                OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            }
        }

        public void CancelNew(int itemIndex)
        {
            if (!add_pending)
                return;

            if (itemIndex != pending_add_index)
                return;

            add_pending = false;

            base.RemoveItem(itemIndex);

            if (raise_list_changed_events)
                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, itemIndex));
        }

        public void EndNew(int itemIndex)
        {
            if (add_pending && itemIndex == pending_add_index && IsSorted)
            {
                ApplySort(SortProperty, SortDirection);
                add_pending = false;
            }
        }

        public int Find(PropertyDescriptor property, object key)
        {
            return inner_list.FindIndex(x => property.GetValue(x) == key);
        }

        public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            return ItemProperties;
        }

        public string GetListName(PropertyDescriptor[] listAccessors)
        {
            return typeof(T).Name;
        }

        public void RemoveSort()
        {
            SortProperty = null;
        }

        /// <summary>
        /// Set the <see cref="SortProperty"/> and <see cref="SortDirection"/>
        /// which the list will be sorted by.
        /// </summary>
        public void SortBy(string propName, bool ignoreCase = false, bool ascending = true)
        {
            Check.NotEmpty(propName, "propName");

            var pd = ItemProperties.Find(propName, ignoreCase);
            Debug.Assert(pd != null);

            ApplySort(pd, ascending ? ListSortDirection.Ascending : ListSortDirection.Descending);
        }

        #endregion Public methods

        #region Protected Methods

        protected virtual void OnAddingNew(AddingNewEventArgs e)
        {
            if (AddingNew != null)
                AddingNew(this, e);
        }

        protected virtual void OnListChanged(ListChangedEventArgs e)
        {
            // If an item is changed then sort the list
            if (!add_pending && IsSorted && e.ListChangedType == ListChangedType.ItemChanged
                && (e.PropertyDescriptor == null || e.PropertyDescriptor == SortProperty))
            {
                ApplySort(SortProperty, SortDirection);
            }

            if (ListChanged != null)
                ListChanged(this, e);
        }

        #endregion Protected Methods

        #region Private methods

        private void UpdateNotifiers()
        {
            if (notifier_default == null)
                notifier_default = new NestedPropertyChangedNotifier<T>(PropertyBindingDepth);

            if (notifiers == null)
                notifiers = new Dictionary<T, NestedPropertyChangedNotifier<T>>();
            else
            {
                foreach (var n in notifiers)
                    n.Value.PropertyChanged -= Item_PropertyChanged;

                notifiers.Clear();
            }

            foreach (T item in base.Items)
            {
                AddPropertyChangedNotifier(item);
            }
        }

        private void Item_PropertyChanged(object item, PropertyChangedEventArgs args)
        {
            var notifier = item as NestedPropertyChangedNotifier<T>;
            Debug.Assert(notifier != null);

            int pos = base.IndexOf(notifier.Object);
            if (pos == -1)
            {
                Debug.Fail("Item not in list.");
                RemovePropertyChangedNotifier(notifier.Object);
                return;
            }

            var pd = ItemProperties.Find(args.PropertyName, true);
            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, pos, pd));
        }

        private void AddPropertyChangedNotifier(T item)
        {
            if (item == null)
                return;

            Debug.Assert(!notifiers.Keys.Contains(item));

            var notifier = notifier_default.CreateNew(item);
            notifier.PropertyChanged += Item_PropertyChanged;
            notifiers[item] = notifier;
        }

        private void RemovePropertyChangedNotifier(T item)
        {
            if (item == null)
                return;

            Debug.Assert(notifiers.Keys.Contains(item));

            var notifier = notifiers[item];
            notifier.PropertyChanged += Item_PropertyChanged;
            notifiers.Remove(item);
        }

        #endregion Private methods

        #region Overrides

        protected override void ClearItems()
        {
            EndNew(pending_add_index);
            if (type_raises_item_changed_events)
            {
                foreach (T item in base.Items)
                {
                    RemovePropertyChangedNotifier(item);
                }
            }
            base.ClearItems();

            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        /// <summary>
        /// Insert an item into the list. If the list is sorted then the actual index of
        /// the inserted item may differ from that specified.
        /// </summary>
        protected override void InsertItem(int index, T item)
        {
            EndNew(pending_add_index);

            base.InsertItem(index, item);

            if (IsSorted && !adding_new)
            {
                ApplySort(SortProperty, SortDirection);
                index = inner_list.IndexOf(item);
            }

            if (raise_list_changed_events)
                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, index));

            if (item != null && type_raises_item_changed_events)
                AddPropertyChangedNotifier(item);
        }

        protected override void RemoveItem(int index)
        {
            if (!AllowRemove)
                throw new NotSupportedException();

            EndNew(pending_add_index);

            if (type_raises_item_changed_events)
            {
                RemovePropertyChangedNotifier(base[index]);
            }

            base.RemoveItem(index);

            if (raise_list_changed_events)
                OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, index));
        }

        protected override void SetItem(int index, T item)
        {
            if (type_raises_item_changed_events)
            {
                RemovePropertyChangedNotifier(base[index]);
                AddPropertyChangedNotifier(item);
            }
            base.SetItem(index, item);

            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
        }

        #endregion Overrides

        #endregion Methods

        #region Explicit interface implementations

        void IBindingList.AddIndex(PropertyDescriptor index)
        {
            /* no implementation */
        }

        object IBindingList.AddNew()
        {
            return AddNew();
        }

        void IBindingList.RemoveIndex(PropertyDescriptor property)
        {
            /* no implementation */
        }

        #endregion Explicit interface implementations
    }
}