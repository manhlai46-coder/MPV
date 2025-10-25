using System.Drawing;
using System.Windows.Forms;

namespace MPV.Services
{
    public static class ImageHelper
    {
        public static Rectangle GetDisplayedImageRectangle(PictureBox pb)
        {
            if (pb.Image == null) return Rectangle.Empty;

            int imgWidth = pb.Image.Width;
            int imgHeight = pb.Image.Height;
            int boxWidth = pb.Width;
            int boxHeight = pb.Height;

            float imgRatio = (float)imgWidth / imgHeight;
            float boxRatio = (float)boxWidth / boxHeight;

            int drawWidth, drawHeight, offsetX = 0, offsetY = 0;

            if (imgRatio > boxRatio)
            {
                drawWidth = boxWidth;
                drawHeight = (int)(boxWidth / imgRatio);
                offsetY = (boxHeight - drawHeight) / 2;
            }
            else
            {
                drawHeight = boxHeight;
                drawWidth = (int)(boxHeight * imgRatio);
                offsetX = (boxWidth - drawWidth) / 2;
            }

            return new Rectangle(offsetX, offsetY, drawWidth, drawHeight);
        }
    }
}
