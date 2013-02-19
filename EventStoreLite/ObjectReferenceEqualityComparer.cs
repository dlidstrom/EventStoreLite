using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace EventStoreLite
{
    internal class ObjectReferenceEqualityComparer<T> : EqualityComparer<T> where T : class
    {
        public static new readonly IEqualityComparer<T> Default = new ObjectReferenceEqualityComparer<T>();

        public override bool Equals(T x, T y)
        {
            return ReferenceEquals(x, y);
        }

        public override int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
