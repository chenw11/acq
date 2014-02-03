using System;
using System.Diagnostics;
using System.Windows.Media;

namespace Lab.gui
{
    /// <summary>
    /// A BGR32 lookup table for non-negative values
    /// Does linear mapping to grayscale.
    /// </summary>
    public class LinearSaturatingLookupTable
    {
        public LinearSaturatingLookupTable(int bitsPerPixel)
        {
            if ((bitsPerPixel < 1) || (bitsPerPixel > 16))
                throw new ArgumentOutOfRangeException();
            this.bitsPerPixel = bitsPerPixel;

            if (bitsPerPixel <= 8)
                lutLength = (1 << 8);
            else
                lutLength = (1 << 16);

            maxValue = checked((ushort)((1 << bitsPerPixel) - 1));
            softMin = 0;
            softMax = maxValue;
        }

        readonly int bitsPerPixel;
        readonly int lutLength;
        readonly ushort maxValue;

        public ushort MaxValue { get { return maxValue; } }

        public int BitsPerPixel { get { return bitsPerPixel; } }

        readonly uint color_invalid = ToBGR32(Colors.Purple);
        readonly uint color_hardMax = ToBGR32(Colors.Red);
        readonly uint color_softMax = ToBGR32(Colors.Orange);
        readonly uint color_softMin = ToBGR32(Colors.LightBlue);
        readonly uint color_hardMin = ToBGR32(Colors.Blue);

        readonly object rangeLock = new object();
        ushort softMin = 0;
        ushort softMax;
        public float SoftMinimum
        {
            get { return softMin; }
            set
            {
                lock(rangeLock)
                {
                    if ((0 <= value) && (value < softMax))
                        softMin = (ushort)value;
                    else
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public float SoftMaximum
        {
            get { return softMax; }
            set
            {
                lock(rangeLock)
                {
                    if ((softMin < value) && (value <= maxValue))
                        softMax = (ushort)value;
                    else
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public static uint ToBGR32(byte b, byte g, byte r)
        {
            return ((uint)r << 16) | ((uint)g << 8) | (uint)b;
        }

        static uint ToBGR32(Color c)
        {
            return ToBGR32(c.B, c.G, c.R);
        }

        public void GetLatest(uint[] outLut)
        {
            if (outLut == null)
                throw new ArgumentNullException();
            if (outLut.Length != lutLength)
                throw new ArgumentException("Expecting array of length " + lutLength);

            if (bitsPerPixel <= 8)
            {
                if (lutLength != 256)
                    throw new Exception("Unexpected lutLength");
                outLut[0] = color_hardMin;
                for (uint i = 1; i < byte.MaxValue; i++)
                    outLut[i] = (i << 16) | (i << 8) | i;
                outLut[byte.MaxValue] = color_hardMax;
            }
            else
            {
                if (lutLength != 65536)
                    throw new Exception("Unepected lutLength");

                ushort m,M;
                lock(rangeLock)
                {
                    m = softMin;
                    M = softMax;
                }
                bool okRange = (m < M) && (M <= maxValue);
                if (!okRange)
                {
                    Trace.TraceInformation("Got invalid range.  Expecting m={0} < M={1} <= {2}", m, M, maxValue);
                    m = 0;
                    M = maxValue;
                }
                float h = 255.0f / (M - m);
                
                // v < 0 gives blue
                outLut[0] = color_hardMin;

                // 0 < r < m gives light blue
                for(uint i=1; i < m; i++)
                    outLut[i] = color_softMin;

                // m <= r <= M gives gray scale
                for (int i=m; i <= M; i++)
                {
                    uint v = (byte)((i - m) * h);
                    outLut[i] = (v << 16) | (v << 8) | v;
                }

                // M < r < maxValue gives orange
                for(int i=M+1; i<maxValue; i++)
                    outLut[i] = color_softMax;

                // maxValue gives red
                outLut[maxValue] = color_hardMax;

                // maxValue < r gives purple for error (beyond expected dynamic range)
                for(int i = maxValue+1; i <= ushort.MaxValue; i++)
                    outLut[i] = color_invalid;
            }
        }
    }
}
