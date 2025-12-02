using System;
using System.Drawing;
using MPV.Models;

namespace MPV.Services
{
    public class HsvAutoService
    {
        public class HsvStats
        {
            public int MinH = 179, MaxH = 0;
            public int MinS = 255, MaxS = 0;
            public int MinV = 255, MaxV = 0;
            public long SumH = 0, SumS = 0, SumV = 0;
            public int Count = 0;
            public double AvgH => Count == 0 ? 0 : (double)SumH / Count;
            public double AvgS => Count == 0 ? 0 : (double)SumS / Count;
            public double AvgV => Count == 0 ? 0 : (double)SumV / Count;
        }

        /// <summary>
        /// Compute HSV statistics and derive Lower/Upper thresholds from pixel distribution.
        /// Strategy:
        ///  - H: use average hue with +/- huePadding (e.g., H=100 -> lower=85, upper=115 when huePadding=15)
        ///  - S,V: use min/max with optional svPadding.
        /// </summary>
        public (HsvValue lower, HsvValue upper, HsvStats stats) Compute(Bitmap roiBitmap, int huePadding = 0, int svPadding = 0)
        {
            var stats = new HsvStats();

            if (roiBitmap == null)
                return (new HsvValue(0,0,0), new HsvValue(0,0,0), stats);

            for (int y = 0; y < roiBitmap.Height; y++)
            {
                for (int x = 0; x < roiBitmap.Width; x++)
                {
                    var c = roiBitmap.GetPixel(x, y);
                    var hsv = RgbToHsv(c.R, c.G, c.B);

                    stats.Count++;
                    stats.SumH += hsv.h;
                    stats.SumS += hsv.s;
                    stats.SumV += hsv.v;

                    if (hsv.h < stats.MinH) stats.MinH = hsv.h;
                    if (hsv.h > stats.MaxH) stats.MaxH = hsv.h;
                    if (hsv.s < stats.MinS) stats.MinS = hsv.s;
                    if (hsv.s > stats.MaxS) stats.MaxS = hsv.s;
                    if (hsv.v < stats.MinV) stats.MinV = hsv.v;
                    if (hsv.v > stats.MaxV) stats.MaxV = hsv.v;
                }
            }

            // Center H bounds around average hue with +/- huePadding.
            int avgH = (int)Math.Round(stats.AvgH);
            int lowerH = Clamp(avgH - huePadding, 0, 179);
            int upperH = Clamp(avgH + huePadding, 0, 179);

            // Keep S,V using observed min/max with padding.
            int lowerS = Clamp(stats.MinS - svPadding, 0, 255);
            int upperS = Clamp(stats.MaxS + svPadding, 0, 255);
            int lowerV = Clamp(stats.MinV - svPadding, 0, 255);
            int upperV = Clamp(stats.MaxV + svPadding, 0, 255);

            var lower = new HsvValue(lowerH, lowerS, lowerV);
            var upper = new HsvValue(upperH, upperS, upperV);

            return (lower, upper, stats);
        }

        private (int h, int s, int v) RgbToHsv(byte r, byte g, byte b)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;

            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            double h = 0;
            if (delta > 0.00001)
            {
                if (max == rd) h = 60 * (((gd - bd) / delta) % 6);
                else if (max == gd) h = 60 * (((bd - rd) / delta) + 2);
                else h = 60 * (((rd - gd) / delta) + 4);
            }
            if (h < 0) h += 360;

            double s = (max == 0) ? 0 : delta / max;
            double v = max;

            return ((int)(h / 2), (int)(s * 255), (int)(v * 255)); // OpenCV scaling
        }

        private int Clamp(int val, int min, int max) => val < min ? min : (val > max ? max : val);
    }
}