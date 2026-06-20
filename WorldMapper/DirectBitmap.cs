using System;
using System.IO;
using SkiaSharp;

namespace WorldMapper
{
    public class DirectBitmap : IDisposable
    {
        public SKBitmap Bitmap { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }

        public DirectBitmap(int width, int height)
        {
            Width = width;
            Height = height;
            Bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        }

        public void SetPixel(int x, int y, SKColor colour)
        {
            Bitmap.SetPixel(x, y, colour);
        }

        public void Save(string path)
        {
            using var data = Bitmap.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(path);
            data.SaveTo(stream);
        }

        public void Dispose()
        {
            Bitmap?.Dispose();
        }
    }
}
