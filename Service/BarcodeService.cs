using MPV.Enums;
using System.Drawing;
using ZXing;

namespace MPV.Services
{
    public class BarcodeService
    {
        public string Decode(Bitmap cropped, BarcodeAlgorithm algorithm)
        {
            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = false, 
                
                TryInverted = false,
                Options = new ZXing.Common.DecodingOptions
                {
                    PossibleFormats = new[] { MapAlgorithmToFormat(algorithm) },
                    TryHarder = true
                }
            };

            var luminanceSource = new MyBitmapLuminanceSource(cropped);
            var result = reader.Decode(luminanceSource);
            return result?.Text;

        }

        private BarcodeFormat MapAlgorithmToFormat(BarcodeAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case BarcodeAlgorithm.QRCode: return BarcodeFormat.QR_CODE;
                case BarcodeAlgorithm.Code128: return BarcodeFormat.CODE_128;
                case BarcodeAlgorithm.EAN13: return BarcodeFormat.EAN_13;
                case BarcodeAlgorithm.DataMatrix: return BarcodeFormat.DATA_MATRIX;
                case BarcodeAlgorithm.CODE_39: return BarcodeFormat.CODE_39;
                case BarcodeAlgorithm.EAN_8: return BarcodeFormat.EAN_8;
                case BarcodeAlgorithm.UPC_A: return BarcodeFormat.UPC_A;
                case BarcodeAlgorithm.PDF_417: return BarcodeFormat.PDF_417;
                case BarcodeAlgorithm.AZTEC: return BarcodeFormat.AZTEC;
                default: return BarcodeFormat.QR_CODE;
            }
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
