// Copyright (c) 2014 Kuan Bartel, All rights reserved.
// See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace NestedPropertyBinding
{
    public class NestedPropertyDescriptor : PropertyDescriptor
    {
        private PropertyDescriptor _currentPD;
        private NestedPropertyDescriptor _parentPD;

        private NestedPropertyDescriptor(PropertyDescriptor pd, NestedPropertyDescriptor parent = null)
            : base(parent == null
                    ? DebugCheck.NotNull(pd, "pd").Name
                    : string.Format("{0}.{1}", parent.Name, DebugCheck.NotNull(pd, "pd").Name),
                    null)
        {
            _currentPD = pd;
            _parentPD = parent;
        }

        /// <summary>
        /// A <see cref="System.ComponentModel.PropertyDescriptor" /> for nested
        /// properties. That is for properties of properties.
        /// </summary>
        /// <param name="type">The component type for which the nested property belongs.</param>
        /// <param name="propertyName">
        /// The nested property name specified using dot notation. This should not contain
        /// the name of component itself.
        /// </param>
        public NestedPropertyDescriptor(Type type, string propertyName)
            : base(Check.NotEmpty(propertyName, "propertyName"), null)
        {
            Check.NotNull(type, "type");

            var propMap = propertyName.Split('.');

            Type currtype = type;
            _parentPD = null;

            int i = 0;
            do
            {
                _currentPD = TypeDescriptor.GetProperties(currtype).Find(propMap[i], false);
                if (_currentPD == null)
                    throw new ArgumentException("The specified nested property does not exist.", "propertyName");

                i++;
                if (i == propMap.Length)
                    break;

                _parentPD = new NestedPropertyDescriptor(_currentPD, _parentPD);

                currtype = _currentPD.PropertyType;
            }
            while (true);
        }

        #region Properties

        public override Type ComponentType
        {
            get
            {
                if (_parentPD != null)
                    return _parentPD.ComponentType;

                return _currentPD.ComponentType;
            }
        }

        public override bool IsReadOnly
        {
            get { return _currentPD.IsReadOnly; }
        }

        private int _Depth = -1;

        /// <summary>
        /// The property's depth from the component. Properties directly on an component
        /// have a depth of 0.
        /// </summary>
        public int PropertyDepth
        {
            get
            {
                if (_Depth == -1)
                {
                    if (_parentPD != null)
                        _Depth = _parentPD.PropertyDepth + 1;
                    else
                        _Depth = 0;
                }

                return _Depth;
            }
        }

        public override Type PropertyType
        {
            get { return _currentPD.PropertyType; }
        }

        #endregion Properties

        #region Methods

        public override bool CanResetValue(object component)
        {
            if (_parentPD != null)
                component = _parentPD.GetValue(component);

            return _currentPD.CanResetValue(component);
        }

        public override object GetValue(object component)
        {
            if (_parentPD != null)
                component = _parentPD.GetValue(component);

            return _currentPD.GetValue(component);
        }

        public override void ResetValue(object component)
        {
            if (_parentPD != null)
                component = _parentPD.GetValue(component);

            _currentPD.ResetValue(component);
            OnValueChanged(component, EventArgs.Empty);
        }

        public override void SetValue(object component, object value)
        {
            var obj = component;
            if (_parentPD != null)
                obj = _parentPD.GetValue(obj);

            _currentPD.SetValue(obj, value);
            OnValueChanged(component, EventArgs.Empty);
        }

        public override bool ShouldSerializeValue(object component)
        {
            if (_parentPD != null)
                component = _parentPD.GetValue(component);

            return _currentPD.ShouldSerializeValue(component);
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Gets all properties and nested properties for a component type up to the
        /// specified depth.  A depth of 0 is effectively the same as calling
        /// <see cref="System.ComponentModel.TypeDescriptor.GetProperties"/>.
        /// </summary>
        public static IEnumerable<NestedPropertyDescriptor> GetNestedProperties(Type type, int depth = 1, bool includeValueTypes = false)
        {
            Check.WithinRange(depth, "depth", d => d >= 0);

            return GetNestedProperties(type, depth, includeValueTypes, null);
        }

        private static IEnumerable<NestedPropertyDescriptor> GetNestedProperties(
            Type type, int depth = 1, bool includeValueTypes = false, NestedPropertyDescriptor parent = null)
        {
            Debug.Assert(depth >= 0);

            if (!includeValueTypes && (type.IsValueType || type == typeof(string)))
                yield break;

            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(type))
            {
                var npd = new NestedPropertyDescriptor(pd, parent);
                yield return npd;

                if (depth > 0)
                {
                    foreach (var childpd in GetNestedProperties(npd.PropertyType, depth - 1, includeValueTypes, npd))
                        yield return childpd;
                }
            }
        }

        #endregion Methods

#if SupportChangeEvents
        // Support for external change events
        // This sort of works, however a handler can only be added
        // if all objects in the tree are non-null and the handler will be
        // lost if an intermediate property is changed.

        public override bool SupportsChangeEvents
        {
            get { return _currentPD.SupportsChangeEvents; }
        }

        public override void AddValueChanged(object component, EventHandler handler)
        {
            if (_parentPD != null)
                component = _parentPD.GetValue(component);

            _currentPD.AddValueChanged(component, handler);
        }

        public override void RemoveValueChanged(object component, EventHandler handler)
        {
            if (_parentPD != null)
                component = _parentPD.GetValue(component);

            _currentPD.RemoveValueChanged(component, handler);
        }
#endif
    }
}