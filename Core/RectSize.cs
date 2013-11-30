using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace eas_lab
{
    /// <summary>
    /// Rectangular size
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RectSize
    {
        readonly int dim_x, dim_y; // order of these matters, for interop

        [ContractInvariantMethod]
        void ObjectInvariant()
        {
            Contract.Invariant(dim_x > 0);
            Contract.Invariant(dim_y > 0);
        }

        [Pure]
        public int DimX
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() == dim_x);
                return dim_x;
            }
        }

        [Pure]
        public int DimY
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() == dim_y);
                return dim_y;
            }
        }

        public RectSize(int dim_x, int dim_y)
            : this()
        {
            Contract.Requires<ArgumentOutOfRangeException>(dim_x >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(dim_y >= 0);
            Contract.Ensures(this.DimX == dim_x);
            Contract.Ensures(this.DimY == dim_y);
            Contract.EndContractBlock();

            if (!(dim_x > 0) || !(dim_y > 0))
                throw new ArgumentOutOfRangeException("Contracts aside, we require positive dimensions.");

            this.dim_x = dim_x;
            this.dim_y = dim_y;
        }

        [Pure]
        public override string ToString()
        {
            return string.Format("{0}x{1}", dim_x, dim_y);
        }

        [Pure]
        public static bool operator ==(RectSize a, RectSize b)
        {
            Contract.Ensures(Contract.Result<bool>() == ((a.DimX == b.DimX) & (a.DimY == b.DimY)));

            bool k1 = (a.DimX == b.DimX);
            bool k2 = (a.DimY == b.DimY);

            return (k1 & k2);
        }

        [Pure]
        public static bool operator !=(RectSize a, RectSize b)
        {
            Contract.Ensures(Contract.Result<bool>() == !(a == b));
            return !(a == b);
        }

        // vector ops for binning
        // 
        [Pure]
        public static RectSize operator *(int x, RectSize a)
        {
            Contract.Requires<ArgumentOutOfRangeException>(a.DimX * x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.DimY * x > 0);

            return new RectSize(a.DimX * x, a.DimY * x);
        }

        [Pure]
        public static RectSize operator *(RectSize a, int x)
        {
            Contract.Requires<ArgumentOutOfRangeException>(a.DimX * x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.DimY * x > 0);

            return x * a;
        }

        [Pure]
        public static RectSize operator /(RectSize a, int x)
        {
            Contract.Requires<ArgumentOutOfRangeException>(a.DimX / x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.DimY / x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(x > 0);

            return new RectSize(a.DimX / x, a.DimY / x);
        }

        [Pure]
        public bool Equals(RectSize x) { return (this == x); }

        [Pure]
        public override bool Equals(object obj)
        {
            if ((obj == null) || (obj.GetType() != typeof(RectSize)))
                return false;
            RectSize x = (RectSize)obj;
            return this.Equals(x);
        }

        [Pure]
        public override int GetHashCode()
        {
            return dim_x | (dim_y << 16);
        }

        [Pure]
        public bool IsValid
        {
            get
            {
                Contract.Ensures(Contract.Result<bool>() == (dim_x > 0 & dim_y > 0));
                return (dim_x > 0) & (dim_y > 0);
            }
        }

        [Pure]
        public int TotalArea
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                Contract.Ensures(Contract.Result<int>() == dim_x * dim_y);
                return dim_x * dim_y;
            }
        }

        public void Validate()
        {
            if (!IsValid)
                throw new Exception("Invalid RectSize!");
        }
    }

}