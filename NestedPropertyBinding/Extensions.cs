// Copyright (c) 2014 Kuan Bartel, All rights reserved.
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace NestedPropertyBinding
{
    internal static class ListExtensions
    {
        public static T Last<T>(this IList<T> list)
        {
            if (list.Count == 0)
                throw new IndexOutOfRangeException();

            return list[list.Count - 1];
        }
    }

    internal static class TypeExtensions
    {
        public static PropertyInfo GetDeclaredProperty(this Type type, string name)
        {
            Debug.Assert(type != null);
            Debug.Assert(!string.IsNullOrEmpty(name));

            const BindingFlags bindingFlags
                    = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            return type.GetProperty(name, bindingFlags);
        }
    }
}
