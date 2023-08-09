using System;
using System.Collections.Generic;

namespace Jitex.Utils.Comparer
{
    internal class TypeEqualityComparer : IEqualityComparer<Type>
    {
        public static readonly TypeEqualityComparer Instance = new TypeEqualityComparer();

        public bool Equals(Type? x, Type? y)
        {
            if (x == null && y == null)
                return true;

            if (x == null || y == null)
                return false;

            return x.MetadataToken == y.MetadataToken && x.Module.ModuleVersionId.Equals(y.Module.ModuleVersionId);
        }

        public int GetHashCode(Type obj)
        {
            return obj.GetHashCode();
        }
    }
}