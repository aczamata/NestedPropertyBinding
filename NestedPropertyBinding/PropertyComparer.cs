// Copyright (c) 2014 Kuan Bartel, All rights reserved.
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// See License.md in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Linq;

namespace NestedPropertyBinding
{
    // <summary>
    // Implements comparing for the <see cref="NestedBindingList{T}" /> implementation.
    // </summary>
    internal class PropertyComparer<T> : Comparer<T>
    {
        private readonly IComparer _comparer;
        private readonly ListSortDirection _direction;
        private readonly PropertyDescriptor _prop;
        private readonly bool _useToString;

        // <summary>
        // Initializes a new instance of the <see cref="SortableBindingList{T}.PropertyComparer" /> class
        // for sorting the list.
        // </summary>
        // <param name="prop"> The property to sort by. </param>
        // <param name="direction"> The sort direction. </param>
        public PropertyComparer(PropertyDescriptor prop, ListSortDirection direction)
        {
            if (!prop.ComponentType.IsAssignableFrom(typeof(T)))
            {
                throw new MissingMemberException(typeof(T).Name, prop.Name);
            }

            Debug.Assert(CanSort(prop.PropertyType), "Cannot use PropertyComparer unless it can be compared by IComparable or ToString");

            _prop = prop;
            _direction = direction;

            if (CanSortWithIComparable(prop.PropertyType))
            {
                var property = typeof(Comparer<>).MakeGenericType(new[] { prop.PropertyType }).GetDeclaredProperty("Default");
                _comparer = (IComparer)property.GetValue(null, null);
                _useToString = false;
            }
            else
            {
                Debug.Assert(
                    CanSortWithToString(prop.PropertyType),
                    "Cannot use PropertyComparer unless it can be compared by IComparable or ToString");

                _comparer = StringComparer.CurrentCultureIgnoreCase;
                _useToString = true;
            }
        }

        // <summary>
        // Compares two instances of items in the list.
        // </summary>
        // <param name="left"> The left item to compare. </param>
        // <param name="right"> The right item to compare. </param>
        public override int Compare(T left, T right)
        {
            var leftValue = _prop.GetValue(left);
            var rightValue = _prop.GetValue(right);

            if (_useToString)
            {
                leftValue = leftValue != null ? leftValue.ToString() : null;
                rightValue = rightValue != null ? rightValue.ToString() : null;
            }

            return _direction == ListSortDirection.Ascending
                       ? _comparer.Compare(leftValue, rightValue)
                       : _comparer.Compare(rightValue, leftValue);
        }

        // <summary>
        // Determines whether this instance can sort for the specified type.
        // </summary>
        // <param name="type"> The type. </param>
        // <returns>
        // <c>true</c> if this instance can sort for the specified type; otherwise, <c>false</c> .
        // </returns>
        public static bool CanSort(Type type)
        {
            return CanSortWithToString(type) || CanSortWithIComparable(type);
        }

        // <summary>
        // Determines whether this instance can sort for the specified type using IComparable.
        // </summary>
        // <param name="type"> The type. </param>
        // <returns>
        // <c>true</c> if this instance can sort for the specified type; otherwise, <c>false</c> .
        // </returns>
        private static bool CanSortWithIComparable(Type type)
        {
            return type.GetInterface("IComparable") != null ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        // <summary>
        // Determines whether this instance can sort for the specified type using ToString.
        // </summary>
        // <param name="type"> The type. </param>
        // <returns>
        // <c>true</c> if this instance can sort for the specified type; otherwise, <c>false</c> .
        // </returns>
        private static bool CanSortWithToString(Type type)
        {
            return type.Equals(typeof(XNode)) || type.IsSubclassOf(typeof(XNode));
        }
    }
}
