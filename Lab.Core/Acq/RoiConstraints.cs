using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;
using System.Diagnostics;

namespace Lab.Acq
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RoiConstraints : IEquatable<RoiConstraints>
    {
        public int HMax { get; private set; }				/* [out]			*/
        public int VMax { get; private set; }	            /* [out]			*/
        public int HPositionUnit { get; private set; }		/* [out]			*/
        public int VPositionUnit { get; private set; }		/* [out]			*/
        public int HSizeUnit { get; private set; }			/* [out]			*/
        public int VSizeUnit { get; private set; }			/* [out]			*/

        public RoiConstraints(int hMax, int vMax, int hPosUnit, int vPosUnit,
            int hSizeUnit, int vSizeUnit)
            :this()
        {
            Contract.Requires<ArgumentOutOfRangeException>(hMax > 0);
            Contract.Requires<ArgumentOutOfRangeException>(vMax > 0);
            Contract.Requires<ArgumentOutOfRangeException>(hPosUnit > 0);
            Contract.Requires<ArgumentOutOfRangeException>(vPosUnit > 0);
            Contract.Requires<ArgumentOutOfRangeException>(hSizeUnit > 0);
            Contract.Requires<ArgumentOutOfRangeException>(vSizeUnit > 0);
            Contract.Ensures(this.HMax == hMax);
            Contract.Ensures(this.VMax == vMax);
            Contract.Ensures(this.HPositionUnit == hPosUnit);
            Contract.Ensures(this.VPositionUnit == vPosUnit);
            Contract.Ensures(this.HSizeUnit == hSizeUnit);
            Contract.Ensures(this.VSizeUnit == vSizeUnit);
            Contract.Ensures(this.ConstraintsAreValid);

            this.HMax = hMax;
            this.VMax = vMax;
            this.HPositionUnit = hPosUnit;
            this.VPositionUnit = vPosUnit;
            this.HSizeUnit = hSizeUnit;
            this.VSizeUnit = vSizeUnit;
        }

        [Pure]
        public bool ConstraintsAreValid
        {
            get
            {
                Contract.Ensures(Contract.Result<bool>() ==
                    ((HPositionUnit > 0) & (VPositionUnit > 0)
                    & (HSizeUnit > 0) & (VSizeUnit > 0)
                    & (HMax > 0) & (VMax > 0)));

                return (HPositionUnit > 0) & (VPositionUnit > 0)
                    & (HSizeUnit > 0) & (VSizeUnit > 0)
                    & (HMax > 0) & (VMax > 0);
            }
        }

        [Pure]
        public override string ToString()
        {
            return string.Format("ROI_Constraints: MaxSize=({0},{1}); PosUnit=({2},{3}); SizeUnit=({4},{5})",
                HMax, VMax, HPositionUnit, VPositionUnit, HSizeUnit, VSizeUnit);
        }

        [Pure]
        public static bool operator ==(RoiConstraints a, RoiConstraints b)
        {
            return (a.HMax == b.HMax)
                && (a.VMax == b.VMax)
                && (a.HPositionUnit == b.HPositionUnit)
                && (a.VPositionUnit == b.VPositionUnit)
                && (a.HSizeUnit == b.HSizeUnit)
                && (a.VSizeUnit == b.VSizeUnit);
        }

        [Pure]
        public static bool operator !=(RoiConstraints a, RoiConstraints b)
        {
            return !(a == b);
        }

        [Pure]
        public static RoiConstraints operator / (RoiConstraints c, int s)
        {
            Contract.Requires<ArgumentOutOfRangeException>(c.HMax / s > 0);
            Contract.Requires<ArgumentOutOfRangeException>(c.VMax / s > 0);
            Contract.Requires<ArgumentOutOfRangeException>(c.HPositionUnit / s > 0);
            Contract.Requires<ArgumentOutOfRangeException>(c.VPositionUnit / s > 0);
            Contract.Requires<ArgumentOutOfRangeException>(c.HSizeUnit / s > 0);
            Contract.Requires<ArgumentOutOfRangeException>(c.VSizeUnit / s > 0);

            return new RoiConstraints(c.HMax / s, c.VMax / s,
                c.HPositionUnit / s, c.VPositionUnit / s,
                c.HSizeUnit / s, c.VSizeUnit / s);
        }

        [Pure]
        static int[] CountUp(int min, int max, int step)
        {
            int n = (max-min)/step + 1;
            int[] all = new int[n];
            for (int i = 0; i < n; i++)
                all[i] = min + i * step;
            return all;
        }

        [Pure]
        public IList<int> AllowedLeft { get { return CountUp(0, HMax - HSizeUnit, HPositionUnit); } }

        [Pure]
        public IList<int> AllowedTop { get { return CountUp(0, VMax - VSizeUnit, VPositionUnit); } }

        [Pure]
        public IList<int> AllowedWidth { get { return CountUp(HSizeUnit, HMax, HSizeUnit); } }

        [Pure]
        public IList<int> AllowedHeight { get { return CountUp(VSizeUnit, VMax, VSizeUnit); } }

        [Pure]
        public bool Equals(RoiConstraints x) { return (this == x); }

        [Pure]
        public override bool Equals(object obj)
        {
            if ((obj == null) || (obj.GetType() != typeof(RoiConstraints)))
                return false;
            RoiConstraints x = (RoiConstraints)obj;
            return this.Equals(x);
        }

        [Pure]
        public override int GetHashCode()
        {
            return HMax | (VMax << 5)
                | (HPositionUnit << 10) | (VPositionUnit << 15)
                | (HSizeUnit << 20) | (VSizeUnit << 25);
        }

        static bool Validate1D(int max, int posUnit, int sizeUnit, int pos, int size)
        {
            if ((max < 1) || (posUnit < 1) || (sizeUnit < 1))
                throw new ArgumentOutOfRangeException();

            bool in_range = (0 <= pos) && (0 < size) && (pos + size <= max);
            bool unit_ok = ((pos % posUnit) == 0) && ((size % sizeUnit) == 0);
            return in_range && unit_ok;
        }

        /// <summary>
        /// Indicates whether or not the given ROI is compatible with the constraints
        /// described by this object
        /// </summary>
        /// <param name="roi">An ROI to be validated</param>
        /// <returns>True if the given ROi is valid, false otherwise </returns>
        [Pure]
        public bool Validate(NaturalRect roi)
        {
            Contract.Requires<InvalidOperationException>(this.ConstraintsAreValid);
            bool h_ok = Validate1D(HMax, HPositionUnit, HSizeUnit, roi.Left, roi.Width);
            bool v_ok = Validate1D(VMax, VPositionUnit, VSizeUnit, roi.Top, roi.Height);
            return h_ok && v_ok;

            // we should really do this properly, perhaps spitting out informative
            // error messages.  and definitely doing either contracts, or
            // thorough unit tests with simulated and/or real hardware.
        }

        [Pure]
        public NaturalRect CoerceDown(NaturalRect rect)
        {
            Contract.Requires<InvalidOperationException>(this.ConstraintsAreValid);
            Contract.Requires<ArgumentOutOfRangeException>(rect.Left >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(rect.Top >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(rect.Width > 0);
            Contract.Requires<ArgumentOutOfRangeException>(rect.Height > 0);
            Contract.Requires<ArgumentOutOfRangeException>(rect.Width >= HSizeUnit);
            Contract.Requires<ArgumentOutOfRangeException>(rect.Height >= VSizeUnit);

            return new NaturalRect(RoundDownLeft(rect.Left), RoundDownTop(rect.Top),
                RoundDownWidth(rect.Width), RoundDownHeight(rect.Height));
        }

        [Pure]
        public int RoundUpWidth(int testWidth)
        {
            Contract.Requires<InvalidOperationException>(this.ConstraintsAreValid);
            Contract.Requires<ArgumentOutOfRangeException>(testWidth >= 0);
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() >= testWidth);
            return roundUp(testWidth, HSizeUnit);
        }

        [Pure]
        public int RoundDownWidth(int testWidth)
        {
            Contract.Requires<InvalidOperationException>(this.ConstraintsAreValid);
            Contract.Requires<ArgumentOutOfRangeException>(testWidth >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(testWidth >= HSizeUnit);
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() >= HSizeUnit);
            Contract.Ensures(Contract.Result<int>() <= testWidth);
            return roundDown(testWidth, HSizeUnit);
        }

        [Pure]
        public int RoundDownHeight(int testHeight)
        {
            Contract.Requires<InvalidOperationException>(this.ConstraintsAreValid);
            Contract.Requires<ArgumentOutOfRangeException>(testHeight >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(testHeight >= VSizeUnit);
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() >= VSizeUnit);
            Contract.Ensures(Contract.Result<int>() <= testHeight);
            return roundDown(testHeight, VSizeUnit);
        }

        [Pure]
        public int RoundDownLeft(int testLeft)
        {
            Contract.Requires<InvalidOperationException>(this.ConstraintsAreValid);
            Contract.Requires<ArgumentOutOfRangeException>(testLeft >= 0);
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= testLeft);
            return roundDown(testLeft, HPositionUnit);
        }

        [Pure]
        public int RoundDownTop(int testTop)
        {
            Contract.Requires<InvalidOperationException>(this.ConstraintsAreValid);
            Contract.Requires<ArgumentOutOfRangeException>(testTop >= 0);
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= testTop);
            return roundDown(testTop, VPositionUnit);
        }

        [Pure]
        static int roundDown(int x, int step)
        {
            Contract.Requires<ArgumentOutOfRangeException>(x >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(step > 0);
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= x);
            // this is an if-then statement: 
            // "if a then b" is equivalent to "!b or a"
            // so this line means "if x >= step, then result >= step"
            // we need this line to ensure that roundDown(width or height) > 0
            Contract.Ensures((x < step) || (Contract.Result<int>() >= step));
            return (x / step) * step;
        }

        [Pure]
        static int roundUp(int x, int step)
        {
            Contract.Requires<ArgumentOutOfRangeException>(x >= 0);
            Contract.Requires<ArgumentOutOfRangeException>(step > 0);
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() >= x);
            return ((x + step - 1) / step) * step;
        }

#if TEST

        public static void TestRoundingDown()
        {
            Xunit.Assert.Equal(0, roundDown(0, 1));
            Xunit.Assert.Equal(1, roundDown(1, 1));
            Xunit.Assert.Equal(0, roundDown(1, 2));
            Xunit.Assert.Equal(0, roundDown(3, 4));
            Xunit.Assert.Equal(2, roundDown(2, 2));
            Xunit.Assert.Equal(2, roundDown(3, 2));

            Xunit.Assert.Equal(16, roundDown(16, 8));
            Xunit.Assert.Equal(16, roundDown(17, 8));
            Xunit.Assert.Equal(16, roundDown(18, 8));
            Xunit.Assert.Equal(16, roundDown(23, 8));
            Xunit.Assert.Equal(24, roundDown(24, 8));
        }
        public static void TestRoundingUp()
        {
            Xunit.Assert.Equal(0, roundUp(0, 1));
            Xunit.Assert.Equal(1, roundUp(1, 1));
            Xunit.Assert.Equal(2, roundUp(1, 2));
            Xunit.Assert.Equal(2, roundUp(2, 2));
            Xunit.Assert.Equal(4, roundUp(3, 4));

            Xunit.Assert.Equal(16, roundUp(14, 8));
            Xunit.Assert.Equal(16, roundUp(15, 8));
            Xunit.Assert.Equal(16, roundUp(16, 8));
            Xunit.Assert.Equal(24, roundUp(17, 8));
            Xunit.Assert.Equal(24, roundDown(24, 8));
        }
#endif
    }
}