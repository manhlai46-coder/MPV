using MPV.Models;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MPV.Services
{
    public class HsvService
    {
        /// <summary>
        /// Ki?m tra xem ?nh có ch?a màu trong kho?ng HSV không
        /// </summary>
        public bool DetectColor(Bitmap image, HsvRange lower, HsvRange upper, out double matchPercentage)
        {
            if (image == null || lower == null || upper == null)
            {
                matchPercentage = 0;
                return false;
            }

            int matchCount = 0;
            int totalPixels = image.Width * image.Height;

            // Lock bitmap ?? truy c?p nhanh
            BitmapData bmpData = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            try
            {
                IntPtr ptr = bmpData.Scan0;
                int bytes = Math.Abs(bmpData.Stride) * image.Height;
                byte[] rgbValues = new byte[bytes];
                Marshal.Copy(ptr, rgbValues, 0, bytes);

                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        int index = y * bmpData.Stride + x * 3;
                        byte b = rgbValues[index];
                        byte g = rgbValues[index + 1];
                        byte r = rgbValues[index + 2];

                        // Convert RGB to HSV
                        var hsv = RgbToHsv(r, g, b);

                        // Check if in range
                        if (IsInRange(hsv, lower, upper))
                        {
                            matchCount++;
                        }
                    }
                }
            }
            finally
            {
                image.UnlockBits(bmpData);
            }

            matchPercentage = (double)matchCount / totalPixels * 100.0;

            // Ng??ng phát hi?n: > 1% pixel kh?p
            return matchPercentage > 1.0;
        }

        /// <summary>
        /// Convert RGB sang HSV
        /// </summary>
        private (int h, int s, int v) RgbToHsv(byte r, byte g, byte b)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;

            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            // Hue
            double h = 0;
            if (delta != 0)
            {
                if (max == rd)
                    h = 60 * (((gd - bd) / delta) % 6);
                else if (max == gd)
                    h = 60 * (((bd - rd) / delta) + 2);
                else
                    h = 60 * (((rd - gd) / delta) + 4);
            }
            if (h < 0) h += 360;

            // Saturation
            double s = (max == 0) ? 0 : (delta / max);

            // Value
            double v = max;

            // OpenCV format: H(0-179), S(0-255), V(0-255)
            return ((int)(h / 2), (int)(s * 255), (int)(v * 255));
        }

        /// <summary>
        /// Ki?m tra HSV có n?m trong range không
        /// </summary>
        private bool IsInRange((int h, int s, int v) hsv, HsvRange lower, HsvRange upper)
        {
            // X? lý tr??ng h?p Hue wrap around (0-179)
            bool hInRange;
            if (lower.HMin <= upper.HMax)
            {
                hInRange = hsv.h >= lower.HMin && hsv.h <= upper.HMax;
            }
            else
            {
                // Wrap around case (e.g., 170-10 means 170-179 and 0-10)
                hInRange = hsv.h >= lower.HMin || hsv.h <= upper.HMax;
            }

            bool sInRange = hsv.s >= lower.SMin && hsv.s <= upper.SMax;
            bool vInRange = hsv.v >= lower.VMin && hsv.v <= upper.VMax;

            return hInRange && sInRange && vInRange;
        }

        /// <summary>
        /// T?o mask ?nh theo HSV range (?? preview)
        /// </summary>
        public Bitmap CreateMask(Bitmap image, HsvRange lower, HsvRange upper)
        {
            if (image == null || lower == null || upper == null)
                return null;

            Bitmap mask = new Bitmap(image.Width, image.Height);

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    var hsv = RgbToHsv(pixel.R, pixel.G, pixel.B);

                    if (IsInRange(hsv, lower, upper))
                    {
                        mask.SetPixel(x, y, Color.White);
                    }
                    else
                    {
                        mask.SetPixel(x, y, Color.Black);
                    }
                }
            }

            return mask;
        }
    }
}