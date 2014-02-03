using System;
using System.Diagnostics.Contracts;

namespace Lab.Acq
{
    /// <summary>
    /// Location on a non-negative lattice.
    /// Useful for pixels in a CCD, bitmap, etc.
    /// </summary>
    public struct NaturalPoint2D : IEquatable<NaturalPoint2D>
    {
        readonly int x, y;

        [Pure]
        public int X
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return x;
            }
        }

        [Pure]
        public int Y
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return y;
            }
        }

        public NaturalPoint2D(int x, int y)
            : this()
        {
            Contract.Requires<ArgumentOutOfRangeException>(x >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(y >= 0);
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
        public static NaturalPoint2D operator *(NaturalPoint2D a, int x)
        {
            Contract.Requires<ArgumentOutOfRangeException>(x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.X * x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Y * x > 0);

            return new NaturalPoint2D(a.X * x, a.Y * x);
        }

        [Pure]
        public static bool operator ==(NaturalPoint2D a, NaturalPoint2D b)
        {
            return (a.X == b.X) && (a.Y == b.Y);
        }

        [Pure]
        public static bool operator !=(NaturalPoint2D a, NaturalPoint2D b)
        {
            return !(a == b);
        }

        [Pure]
        public bool Equals(NaturalPoint2D x) { return (this == x); }

        [Pure]
        public override bool Equals(object obj)
        {
            if ((obj == null) || (obj.GetType() != typeof(NaturalPoint2D)))
                return false;
            NaturalPoint2D x = (NaturalPoint2D)obj;
            return this.Equals(x);
        }

        [Pure]
        public override int GetHashCode()
        {
            return x | (y << 8);
        }
    }
}