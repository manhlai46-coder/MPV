using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MPV.Models;
using MPV.Services;

namespace MPV.Renderers
{
    public class RoiRenderer
    {
        private readonly PictureBox _pictureBox;

        public RoiRenderer(PictureBox pictureBox)
        {
            _pictureBox = pictureBox ?? throw new ArgumentNullException(nameof(pictureBox));
        }

        public void DrawSelection(Graphics g, Rectangle selectionRect)
        {
            if (selectionRect.Width <= 0 || selectionRect.Height <= 0)
                return;

            using (Pen pen = new Pen(Color.Red, 2))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                g.DrawRectangle(pen, selectionRect);
            }
        }

        public void DrawRois(Graphics g, List<RoiRegion> roiList, Bitmap bitmap, int selectedIndex, bool showResults)
        {
            if (roiList == null || bitmap == null) return;

            Rectangle imgRect = ImageHelper.GetDisplayedImageRectangle(_pictureBox);
            float scaleX = (float)imgRect.Width / bitmap.Width;
            float scaleY = (float)imgRect.Height / bitmap.Height;

            for (int i = 0; i < roiList.Count; i++)
            {
                var roi = roiList[i];
                Rectangle displayRect = new Rectangle(
                    imgRect.X + (int)(roi.X * scaleX),
                    imgRect.Y + (int)(roi.Y * scaleY),
                    (int)(roi.Width * scaleX),
                    (int)(roi.Height * scaleY)
                );

                
                Color baseColor;
                if (roi.Mode == "HSV")
                {
   
                    baseColor = (roi.Lower != null && roi.Upper != null) ? Color.LimeGreen : Color.Red;
                }
                else 
                {
   
                    baseColor = Color.Red;
                }

                if (i == selectedIndex)
                    baseColor = Color.Orange;

                using (Pen pen = new Pen(baseColor, (i == selectedIndex) ? 3 : 2))
                {
                    g.DrawRectangle(pen, displayRect);
                }

                using (Font font = new Font("Arial", 9, FontStyle.Bold))
                using (SolidBrush brush = new SolidBrush(baseColor))
                {
                    g.DrawString($"ROI {i + 1}", font, brush, displayRect.X, displayRect.Y - 18);
                }
            }
        }
    }
}
