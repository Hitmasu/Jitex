using System;
using System.Collections.Generic;
using System.Text;

namespace Jitex.Utils.Comparer
{
    public class TypeComparer : IEqualityComparer<Type>
    {
        public static readonly TypeComparer Instance = new TypeComparer();

        public bool Equals(Type x, Type y)
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
