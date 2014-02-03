using ProtoBuf;
using System.ComponentModel;

namespace Lab.Acq
{
    public enum DataSourceStatus : int
    {
        NotReady = 1,
        ReadyAndPaused = 4,
        Running = 9,
        Error = 14,
    }

    public enum BinningMode : int
    {
        Binning1x1 = 1,
        Binning2x2 = 2,
        Binning4x4 = 4,
        Binning8x8 = 8,
    }


    /// <summary>
    /// Attributes of the camera device which do not change during runtime
    /// </summary>
    [ProtoContract]
    public class CameraAttributes
    {
        [ProtoMember(1, IsPacked = true)]
        public BinningMode[] SupportedBinning { get; set; }

        [ProtoMember(2)]
        public int FullWidth { get; set; }

        [ProtoMember(3)]
        public int FullHeight { get; set; }

        [ProtoMember(4)]
        public int BitDepth { get; set; }

        [ProtoMember(12)]
        public string Model { get; set; }

        [ProtoMember(13)]
        public string SerialNumber { get; set; }

        public CameraAttributes Duplicate()
        {
            return ProtoBuf.Serializer.DeepClone<CameraAttributes>(this);
        }

        public override string ToString()
        {
            return string.Format("{0} {1} Area={2}x{3}, {4} bpp, supports [{5}]",
                Model, SerialNumber, FullWidth, FullHeight, BitDepth,
                string.Join("|", SupportedBinning));
        }
    }

    /// <summary>
    /// Video settings which cannot be adjusted during acquisition
    /// </summary>
    [ProtoContract]
    public class VideoSettingsStatic
    {
        BinningMode binning = BinningMode.Binning1x1;

        [DefaultValue(BinningMode.Binning1x1)]
        [ProtoMember(1)]
        public BinningMode Binning { get { return binning; } set { binning = value; } }

        [ProtoMember(2)]
        public int RoiX { get; set; }

        [ProtoMember(3)]
        public int RoiY { get; set; }

        [ProtoMember(4)]
        public int RoiWidth { get; set; }

        [ProtoMember(5)]
        public int RoiHeight { get; set; }


        public NaturalRect Roi
        {
            get { return new NaturalRect(RoiX, RoiY, RoiWidth, RoiHeight); }
            set
            {
                RoiX = value.Left;
                RoiY = value.Top;
                RoiWidth = value.Width;
                RoiHeight = value.Height;
            }
        }

        [ProtoMember(11)]
        public TriggeringMode Trigger { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", Roi, Binning, Trigger);
        }
    }

    public enum TriggeringMode : uint
    {
        Freerun = 0,
        Software = 1,
        HardwareEdgeHigh = 2,
        HardwareEdgeLow = 4
    }

    /// <summary>
    /// Video settings which may be adjusted during acquisition
    /// </summary>
    [ProtoContract]
    public class VideoSettingsDynamic
    {
        /// <summary>
        /// Analog gain, in decibels, before digitization.
        /// 0 is default, factory-optimized for maximum dynamic range
        /// </summary>
        [ProtoMember(4)]
        public float AnalogGain_dB { get; set; }


        /// <summary>
        /// Analog offset, before digitiazation.
        /// 0 is default, factory optimized for maximum dynamic range
        /// </summary>
        [ProtoMember(5)]
        public int AnalogOffset { get; set; }

    }

    [ProtoContract]
    public class VideoFrame
    {
        [ProtoMember(1, DataFormat = DataFormat.FixedSize)]
        public uint ErrorCode { get; set; }

        [ProtoMember(3, DataFormat = DataFormat.FixedSize)]
        public uint BitsPerPixel { get; set; }

        [ProtoMember(4, IsRequired = true, DataFormat = DataFormat.FixedSize)]
        public uint Width { get; set; }

        [ProtoMember(5, IsRequired = true, DataFormat = DataFormat.FixedSize)]
        public uint Height { get; set; }

        [ProtoMember(7, DataFormat=DataFormat.FixedSize)]
        public uint FrameNumber { get; set; }

        [ProtoMember(8, DataFormat=DataFormat.FixedSize)]
        public uint TimeStamp { get; set; }

        [ProtoMember(9, DataFormat=DataFormat.FixedSize)]
        public uint DataSizeBytes { get; set; }

        [ProtoMember(10)]
        public byte[] Data { get; set; }


        public bool IsValid
        {
            get
            {
                if ((BitsPerPixel < 1) || (BitsPerPixel > 30))
                    return false;
                if ((Width < 1) || (Height < 1) || (Width > 4096) || (Height > 4096))
                    return false;
                long expectedSize = (long)Utilities.BitsToBytes((int)BitsPerPixel) * Height * Width;
                if (expectedSize != DataSizeBytes)
                    return false;
                if (Data.Length != expectedSize)
                    return false;
                return true;
            }

        }

    }


}

