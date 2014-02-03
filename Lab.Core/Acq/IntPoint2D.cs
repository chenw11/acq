using System;
using System.Diagnostics.Contracts;

namespace Lab.Acq
{
    /// <summary>
    /// Location on a lattice.
    /// </summary>
    public struct IntPoint2D : IEquatable<IntPoint2D>
    {
        readonly int x, y;

        [Pure]
        public int X { get { return x; } }

        [Pure]
        public int Y { get { return y; } }

        public IntPoint2D(int x, int y)
            : this()
        {
            Contract.Ensures(this.X == x);
            Contract.Ensures(this.Y == y);

            this.x = x;
            this.y = y;
        }

        [Pure]
        public override string ToString()
        {
            return string.Format("({0},{1})", X, Y);
        }

        [Pure]
        public static IntPoint2D operator *(IntPoint2D a, int x)
        {
            return new IntPoint2D(a.X * x, a.Y * x);
        }

        [Pure]
        public static bool operator ==(IntPoint2D a, IntPoint2D b)
        {
            return (a.X == b.X) && (a.Y == b.Y);
        }

        [Pure]
        public static bool operator !=(IntPoint2D a, IntPoint2D b)
        {
            return !(a == b);
        }

        [Pure]
        public bool Equals(IntPoint2D x) { return (this == x); }

        [Pure]
        public override bool Equals(object obj)
        {
            if ((obj == null) || (obj.GetType() != typeof(IntPoint2D)))
                return false;
            IntPoint2D x = (IntPoint2D)obj;
            return this.Equals(x);
        }

        [Pure]
        public override int GetHashCode()
        {
            return x | (y << 8);
        }
    }
}