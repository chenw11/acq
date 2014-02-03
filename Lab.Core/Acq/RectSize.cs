using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace Lab.Acq
{
    /// <summary>
    /// Rectangular size
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RectSize
    {
        readonly int width;
        readonly int height; // order of these matters, for interop

        [Pure]
        public int Width
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() == width);
                return width;
            }
        }

        [Pure]
        public int Height
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() == height);
                return height;
            }
        }

        public RectSize(int width, int height)
            : this()
        {
            Contract.Requires<ArgumentOutOfRangeException>(width >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(height >= 0);
            Contract.Ensures(this.Width == width);
            Contract.Ensures(this.Height == height);
            Contract.EndContractBlock();

            if (!(width > 0) || !(height > 0))
                throw new ArgumentOutOfRangeException("Contracts aside, we require positive dimensions.");

            this.width = width;
            this.height = height;
        }

        [Pure]
        public override string ToString()
        {
            return string.Format("{0}x{1}", width, height);
        }

        [Pure]
        public static bool operator ==(RectSize a, RectSize b)
        {
            Contract.Ensures(Contract.Result<bool>() == ((a.Width == b.Width) & (a.Height == b.Height)));

            bool k1 = (a.Width == b.Width);
            bool k2 = (a.Height == b.Height);

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
            Contract.Requires<ArgumentOutOfRangeException>(a.Width * x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Height * x > 0);

            return new RectSize(a.Width * x, a.Height * x);
        }

        [Pure]
        public static RectSize operator *(RectSize a, int x)
        {
            Contract.Requires<ArgumentOutOfRangeException>(a.Width * x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Height * x > 0);

            return x * a;
        }

        [Pure]
        public static RectSize operator /(RectSize a, int x)
        {
            Contract.Requires<ArgumentOutOfRangeException>(a.Width / x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Height / x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(x > 0);

            return new RectSize(a.Width / x, a.Height / x);
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
            return width | (height << 16);
        }

        [Pure]
        public bool IsValid
        {
            get
            {
                Contract.Ensures(Contract.Result<bool>() == (width > 0 & height > 0));
                return (width > 0) & (height > 0);
            }
        }

        [Pure]
        public int TotalArea
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return checked(width * height);
            }
        }

        public void Validate()
        {
            if (!IsValid)
                throw new Exception("Invalid RectSize!");
        }
    }

}