// Copyright (c) 2014 Kuan Bartel, All rights reserved.
// See License.md in the project root for license information.

using System;
using System.Diagnostics;

namespace NestedPropertyBinding
{
    /// <summary>
    /// Helper class for checking method arguments. The Checks can all be used inline in
    /// order to allow checking of arguments before passing to another constructor.
    /// </summary>
    internal class Check
    {
        /// <summary>
        /// Check if an argument satisfies a condition checked by a function.
        /// </summary>
        /// <exception cref="ArgumentException">If the function returns false.</exception>
        public static T True<T>(T value, string argName, Func<T, bool> check) where T : class
        {
            if (!check(value))
            {
#if DEBUG
                Debug.Fail(string.Format("Invalid argument: {0}", argName));
#else
                throw new ArgumentException(argName);
#endif
            }

            return value;
        }

        /// <summary>
        /// Check if value is null.
        /// </summary>
        /// <exception cref="ArgumentNullException">If the value is null.</exception>
        public static T NotNull<T>(T value, string argName) where T : class
        {
            if (value == null)
            {
#if DEBUG
                Debug.Fail(string.Format("Argument null: {0}", argName));
#else
                throw new ArgumentNullException(argName);
#endif
            }

            return value;
        }

        /// <summary>
        /// Check if value is null.
        /// </summary>
        /// <exception cref="ArgumentNullException">If the value is null.</exception>
        public static T? NotNull<T>(T? value, string argName) where T : struct
        {
            if (value == null)
            {
#if DEBUG
                Debug.Fail(string.Format("Argument null: {0}", argName));
#else
                throw new ArgumentNullException(argName);
#endif
            }

            return value;
        }

        /// <summary>
        /// Check if string value is null or white space.
        /// </summary>
        /// <exception cref="ArgumentException">If string.IsNullOrWhiteSpace(value) is true.</exception>
        public static string NotEmpty(string value, string argName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
#if DEBUG
                Debug.Fail(string.Format("Argument null or whitespace: {0}", argName));
#else
                throw new ArgumentException(argName);
#endif
            }

            return value;
        }

        /// <summary>
        /// Check if a primitive type value is within the specified range.
        /// The type must support the '&gt;' and '&lt;' operators.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If value < lowerBound || value > upperBound.</exception>
        public static T WithinRange<T>(T value, string argName, T lowerBound, T upperBound)
            where T : struct
        {
            Debug.Assert(typeof(T).IsPrimitive, "Type must be a primitive type.");

            return WithinRange(value, argName, v => (v >= (dynamic)lowerBound && v <= (dynamic)upperBound));
        }

        /// <summary>
        /// Check if a value satisfies a range check function.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If the function returns false.</exception>
        public static T WithinRange<T>(T value, string argName, Func<T, bool> check)
            where T : struct
        {
            if (!check(value))
            {
#if DEBUG
                Debug.Fail(string.Format("Argument out of range: {0}", argName));
#else
                throw new ArgumentOutOfRangeException(argName);
#endif
            }

            return value;
        }
    }

    /// <summary>
    /// DebugCheck wraps the <see cref="Check" /> class but will only perform the check
    /// when the DEBUG symbol is defined.
    /// </summary>
    internal class DebugCheck
    {
        public static T True<T>(T value, string argName, Func<T, bool> check) where T : class
        {
#if DEBUG
            return Check.True(value, argName, check);
#else
            return value;
#endif
        }

        /// <summary>
        /// Check if value is null.
        /// </summary>
        public static T NotNull<T>(T value, string argName) where T : class
        {
#if DEBUG
            return Check.NotNull(value, argName);
#else
            return value;
#endif
        }

        /// <summary>
        /// Check if value is null.
        /// </summary>
        public static T? NotNull<T>(T? value, string argName) where T : struct
        {
#if DEBUG
            return Check.NotNull(value, argName);
#else
            return value;
#endif
        }

        /// <summary>
        /// Check if string value is null or white space.
        /// </summary>
        public static string NotEmpty(string value, string argName)
        {
#if DEBUG
            return Check.NotEmpty(value, argName);
#else
            return value;
#endif
        }

        /// <summary>
        /// Check if a primitive type value is within the specified range.
        /// The type must support the '&gt;' and '&lt;' operators.
        /// </summary>
        public static T WithinRange<T>(T value, string argName, T lowerBound, T upperBound)
            where T : struct
        {
#if DEBUG
            return Check.WithinRange(value, argName, lowerBound, upperBound);
#else
            return value;
#endif
        }

        /// <summary>
        /// Use a check function to check if a value satisfies a range check function.
        /// </summary>
        public static T WithinRange<T>(T value, string argName, Func<T, bool> check)
            where T : struct
        {
#if DEBUG
            return Check.WithinRange(value, argName, check);
#else
            return value;
#endif
        }
    }
}