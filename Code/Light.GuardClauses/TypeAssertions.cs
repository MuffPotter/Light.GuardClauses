﻿using System;
using System.Reflection;

namespace Light.GuardClauses
{
    /// <summary>
    ///     Provides assertion extension methods for the <see cref="Type" /> and <see cref="TypeInfo" /> types.
    /// </summary>
    public static class TypeAssertions
    {
        /// <summary>
        ///     Checks if the two specified types are equivalent. This is true when 1) both types are non-generic types and are equal
        ///     or when 2) one type is a constructed generic type and the other type is the corresponding generic type definition.
        /// </summary>
        /// <param name="type">The first type to be checked.</param>
        /// <param name="other">The other type to be checked.</param>
        /// <returns>
        ///     True if both types are null, or if both are non-generic types that are equal, or if one type
        ///     is a constructed generic type and the other one is the corresponding generic type definition, else false.
        /// </returns>
        public static bool IsEquivalentTo(this Type type, Type other)
        {
            if (ReferenceEquals(type, other)) return true;
            if (ReferenceEquals(type, null) || ReferenceEquals(other, null)) return false;

            if (type == other) return true;

            if (type.IsConstructedGenericType == other.IsConstructedGenericType)
                return false;

            if (type.IsConstructedGenericType)
                return type.GetGenericTypeDefinition() == other;
            return other.GetGenericTypeDefinition() == type;
        }
    }
}