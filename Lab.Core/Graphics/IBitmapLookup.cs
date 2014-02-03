using System;
using System.Windows.Media.Imaging;
namespace Lab.gui
{
    public interface IBitmapProviderFront
    {
        BitmapSource Bitmap { get; }
    }

    public interface IDataBitmapBackside 
    {
        Array DataBuffer { get; }

        void Remap();

        void Invalidate();
    }

    public interface IBitmapLookup : IDataBitmapBackside, IBitmapProviderFront
    {
        uint[] LookupTable { get; }
    }
}
