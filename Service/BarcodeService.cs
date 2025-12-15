using MPV.Enums;
using System.Drawing;
using ZXing;

namespace MPV.Services
{
    public class BarcodeService
    {
        public string Decode(Bitmap cropped, BarcodeAlgorithm algorithm)
        {
            if (cropped == null) return null;

            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new ZXing.Common.DecodingOptions
                {
                    PossibleFormats = new[] { MapAlgorithmToFormat(algorithm) },
                    TryHarder = true
                }
            };

            // Use built-in luminance source for Bitmap
            var luminanceSource = new ZXing.BitmapLuminanceSource(cropped);
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
    }
}
