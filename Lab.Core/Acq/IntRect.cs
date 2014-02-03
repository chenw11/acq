using System;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;

namespace Lab.Acq
{

    /// <summary>
    /// Positioned rectangle with integer coordinates.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    [Serializable]
    public struct IntRect : IEquatable<IntRect>
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
                bool ok = width > 0;
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
        public int Left { get { return left; } }

        [Pure]
        public int Top { get { return top; } }

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
        public IntPoint2D TopLeft { get { return new IntPoint2D(left, top); } }

        public IntRect(IntPoint2D topleft, RectSize size)
            : this(topleft.X, topleft.Y, size)
        {
            Contract.Requires<ArgumentOutOfRangeException>(size.Width > 0);
            Contract.Requires<ArgumentOutOfRangeException>(size.Height > 0);
            Contract.Ensures(this.Width == size.Width);
            Contract.Ensures(this.Height == size.Height);
            Contract.Ensures(this.Left == topleft.X);
            Contract.Ensures(this.Top == topleft.Y);
        }

        public IntRect(int left, int top, int width, int height)
            : this()
        {
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

        public IntRect(int left, int top, RectSize size)
            : this(left, top, size.Width, size.Height)
        {
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
        public static IntRect operator *(IntRect a, int x)
        {
            Contract.Requires<ArgumentOutOfRangeException>(x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Width * x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Height * x > 0);

            return new IntRect(a.Left * x, a.Top * x, a.Width * x, a.Height * x);
        }

        [Pure]
        public static IntRect operator /(IntRect a, int x)
        {
            Contract.Requires<ArgumentOutOfRangeException>(x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Width / x > 0);
            Contract.Requires<ArgumentOutOfRangeException>(a.Height / x > 0);

            return new IntRect(a.Left / x, a.Top / x, a.Width / x, a.Height / x);
        }



        [Pure]
        public static bool operator ==(IntRect a, IntRect b)
        {
            Contract.Ensures(Contract.Result<bool>() == ((a.Left == b.Left) & (a.Top == b.Top) & (a.Size == b.Size)));
            return ((a.Left == b.Left) & (a.Top == b.Top) & (a.Size == b.Size));
        }

        [Pure]
        public static bool operator !=(IntRect a, IntRect b)
        {
            return !(a == b);
        }

        [Pure]
        public bool Equals(IntRect x) { return (this == x); }

        [Pure]
        public override bool Equals(object obj)
        {
            if ((obj == null) || (obj.GetType() != typeof(IntRect)))
                return false;
            IntRect x = (IntRect)obj;
            return this.Equals(x);
        }

        [Pure]
        public override int GetHashCode()
        {
            return Left | (Top << 8) | (Width << 16) | (Height << 24);
        }

        [Pure]
        public bool Contains(IntRect inner)
        {
            bool ok = this.Left <= inner.Left;
            ok &= this.Top <= inner.Top;
            ok &= this.Left + this.Width >= inner.Left + inner.Width;
            ok &= this.Top + this.Height >= inner.Top + inner.Height;
            return ok;
        }

    }
}
