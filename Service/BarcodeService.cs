using System.Drawing;
using ZXing;

namespace MPV.Services
{
    public class BarcodeService
    {
        public string Decode(Bitmap cropped)
        {
            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true
                }
            };

            var luminanceSource = new MyBitmapLuminanceSource(cropped);
            var result = reader.Decode(luminanceSource);
            return result?.Text;
        }

        
        private class MyBitmapLuminanceSource : LuminanceSource
        {
            private readonly byte[] _luminances;

            public MyBitmapLuminanceSource(Bitmap bitmap) : base(bitmap.Width, bitmap.Height)
            {
                _luminances = new byte[bitmap.Width * bitmap.Height];
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        var pixel = bitmap.GetPixel(x, y);
                        _luminances[y * bitmap.Width + x] = (byte)((pixel.R + pixel.G + pixel.B) / 3);
                    }
                }
            }

            public override byte[] Matrix => _luminances;
            public override byte[] getRow(int y, byte[] row)
            {
                if (row == null || row.Length < Width)
                    row = new byte[Width];
                System.Array.Copy(_luminances, y * Width, row, 0, Width);
                return row;
            }
        }
    }
}
