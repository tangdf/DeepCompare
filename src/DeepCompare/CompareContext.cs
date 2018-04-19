using System;
using System.Collections.Generic;
using System.Text;

namespace DeepCompare
{
    public sealed class CompareContext
    {
        private class CacheKey : IEquatable<CacheKey>
        {
            public CacheKey(object x, object y)
            {
                if (x == null)
                    throw new ArgumentNullException(nameof(x));
                if (y == null)
                    throw new ArgumentNullException(nameof(y));
                X = x;
                Y = y;
            }

            public object X { get; }

            public object Y { get; }

            public bool Equals(CacheKey other)
            {
                if (ReferenceEquals(null, other))
                    return false;
                if (ReferenceEquals(this, other))
                    return true;
                return ReferenceEquals(X, other.X) && ReferenceEquals(Y, other.Y);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != this.GetType())
                    return false;
                return Equals((CacheKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((X != null ? X.GetHashCode() : 0) * 397) ^ (Y != null ? Y.GetHashCode() : 0);
                }
            }
        }

        private readonly HashSet<CacheKey> _caches = new HashSet<CacheKey>();

        public bool Skip(object x, object y)
        {
            if (x == null || y == null)
                return false;

            CacheKey cacheKey = new CacheKey(x, y);

            return _caches.Add(cacheKey) == false;

        }

        internal void Reset()
        {
            _caches.Clear();
        }
    }
}