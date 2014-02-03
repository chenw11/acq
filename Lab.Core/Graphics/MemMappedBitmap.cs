using System;
using System.IO.MemoryMappedFiles;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lab.gui
{
    public abstract unsafe class MemMappedBitmap : Disposable
    {
        public static readonly int MaxPixelsPerDimension = 4096;

        private readonly MemoryMappedFile section;
        private readonly MemoryMappedViewAccessor sectionView;

        protected readonly InteropBitmap interop;
        protected readonly uint* bgr32;

        public MemMappedBitmap(int width, int height)
        {
            if ((width < 1) || (width > MaxPixelsPerDimension))
                throw new ArgumentOutOfRangeException("width");
            if ((height < 1) || (height > MaxPixelsPerDimension))
                throw new ArgumentOutOfRangeException("height");

            section = MemoryMappedFile.CreateNew(null, width * height * sizeof(uint));
            sectionView = section.CreateViewAccessor();
            byte* ptr = null;
            sectionView.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            bgr32 = (uint*)ptr;

            Clear(bgr32, width * height);
            interop = (InteropBitmap)Imaging.CreateBitmapSourceFromMemorySection(
                section.SafeMemoryMappedFileHandle.DangerousGetHandle(), width, height,
                PixelFormats.Bgr32, width * sizeof(uint), 0);
        }

        static void Clear(uint* pBgr32, int length)
        {
            var bgr32End = pBgr32 + length;

            while (pBgr32 < bgr32End)
                *pBgr32++ = 0;
        }

        protected override void RunOnceDisposer()
        {
            if (bgr32 != null)
                sectionView.SafeMemoryMappedViewHandle.ReleasePointer();

            sectionView.TryDispose();
            section.TryDispose();
        }


        public BitmapSource Bitmap { get { return interop; } }
        public void Invalidate()
        {
            var i = interop;
            if (i != null)
                i.Invalidate();
        }
    }
}
