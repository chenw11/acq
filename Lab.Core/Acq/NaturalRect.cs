using System;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;

namespace Lab.Acq
{

    /// <summary>
    /// Positioned rectangle with natural number coordinates.
    /// Appropriate for defining subareas of CCDs or other 2D lattices
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    [Serializable]
    public struct NaturalRect : IEquatable<NaturalRect>
    {
        [FieldOffset(0)]
        readonly int left;

        [FieldOffset(4)]
        readonly int top;

        [FieldOffset(8)]
        readonly int width;

        [FieldOffset(12)]
        readonly int height;

        [Pure]
        public bool IsValid
        {
            get
            {
                bool ok = left >= 0;
                ok &= top >= 0;
                ok &= width > 0;
                ok &= height > 0;
                return ok;
            }
        }

        public void Validate() 
        {
            if (!IsValid)
                throw new ValidationException("Invalid rectangle.");
        }

        [Pure]
        public int Left
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return left;
            }
        }

        [Pure]
        public int Top
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return top;
            }
        }

        [Pure]
        public int Width
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return width;
            }
        }

        [Pure]
        public int Height
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return height;
            }
        }

        [Pure]
        public RectSize Size
        {
            get
            {
                Contract.Ensures(Contract.Result<RectSize>().Width == this.Width);
                Contract.Ensures(Contract.Result<RectSize>().Height == this.Height);
                return new RectSize(width, height);
            }
        }

        [Pure]
        public NaturalPoint2D TopLeft { get { return new NaturalPoint2D(left, top); } }

        public NaturalRect(NaturalPoint2D topleft, RectSize size)
            : this(topleft.X, topleft.Y, size)
        {
            Contract.Requires<ArgumentOutOfRangeException>(topleft.X >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(topleft.Y >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(size.Width > 0);
            Contract.Requires<ArgumentOutOfRangeException>(size.Height > 0);
            Contract.Ensures(this.Width == size.Width);
            Contract.Ensures(this.Height == size.Height);
            Contract.Ensures(this.Left == topleft.X);
            Contract.Ensures(this.Top == topleft.Y);
        }

        public NaturalRect(int left, int top, int width, int height)
            : this()
        {
            Contract.Requires<ArgumentOutOfRangeException>(left >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(top >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(width > 0);
            Contract.Requires<ArgumentOutOfRangeException>(height > 0);
            Contract.Ensures(this.Width == width);
            Contract.Ensures(this.Height == height);
            Contract.Ensures(this.Left == left);
            Contract.Ensures(this.Top == top);
            Contract.EndContractBlock();

            this.left = left;
            this.top = top;
            this.width = width;
            this.height = height;
        }

        public NaturalRect(int left, int top, RectSize size)
            : this(left, top, size.Width, size.Height)
        {
            Contract.Requires<ArgumentOutOfRangeException>(left >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(top >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(size.Width > 0);
            Contract.Requires<ArgumentOutOfRangeException>(size.Height > 0);
            Contract.Ensures(this.Width == size.Width);
            Contract.Ensures(this.Height == size.Height);
            Contract.Ensures(this.Left == left);
            Contract.Ensures(this.Top == top);
        }

        [Pure]
        public override string ToString()
        {
            return string.Format("({0},{1};{2})", Left, Top, Size.ToString());
        }

        [Pure]
        public static NaturalRect operator *(NaturalRect a, int x)
        {
            Contract.Requires<ArgumentOutOfRangeException>(x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Left * x >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Top * x >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Width * x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Height * x > 0);

            return new NaturalRect(a.Left * x, a.Top * x, a.Width * x, a.Height * x);
        }

        [Pure]
        public static NaturalRect operator /(NaturalRect a, int x)
        {
            Contract.Requires<ArgumentOutOfRangeException>(x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Left / x >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Top / x >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Width / x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Height / x > 0);

            return new NaturalRect(a.Left / x, a.Top / x, a.Width / x, a.Height / x);
        }



        [Pure]
        public static bool operator ==(NaturalRect a, NaturalRect b)
        {
            Contract.Ensures(Contract.Result<bool>() == ((a.Left == b.Left) & (a.Top == b.Top) & (a.Size == b.Size)));
            return ((a.Left == b.Left) & (a.Top == b.Top) & (a.Size == b.Size));
        }

        [Pure]
        public static bool operator !=(NaturalRect a, NaturalRect b)
        {
            return !(a == b);
        }

        [Pure]
        public bool Equals(NaturalRect x) { return (this == x); }

        [Pure]
        public override bool Equals(object obj)
        {
            if ((obj == null) || (obj.GetType() != typeof(NaturalRect)))
                return false;
            NaturalRect x = (NaturalRect)obj;
            return this.Equals(x);
        }

        [Pure]
        public override int GetHashCode()
        {
            return Left | (Top << 8) | (Width << 16) | (Height << 24);
        }

        [Pure]
        public bool Contains(NaturalRect inner)
        {
            bool ok = this.Left <= inner.Left;
            ok &= this.Top <= inner.Top;
            ok &= this.Left + this.Width >= inner.Left + inner.Width;
            ok &= this.Top + this.Height >= inner.Top + inner.Height;
            return ok;
        }

    }
}
