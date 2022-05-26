using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace UVIndicator2
{
    internal class UVBitmapGenerator
    {
        private UVBitmapGenerator() { }

        public static Bitmap GenerateNumberIcon(int uvi, float scale)
        {
            int width = (int)Math.Round(256 * scale);
            int height = (int)Math.Round(256 * scale);

            Bitmap iconBMP = new(width, height, PixelFormat.Format32bppArgb);

            RectangleF rectf = new RectangleF(10, 10, width - 20, height - 20);

            Graphics g = Graphics.FromImage(iconBMP);

            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Brush backgroundBrush, fontBrush;
            if (uvi <= 2)
            {
                backgroundBrush = Brushes.LightGreen;
                fontBrush = Brushes.Black;
            } else if (uvi <= 4)
            {
                backgroundBrush  = Brushes.Yellow;
                fontBrush = Brushes.Black;
            } else
            {
                backgroundBrush = Brushes.DarkRed;
                fontBrush = Brushes.White;
            }

            g.FillRectangle(backgroundBrush, new Rectangle(0, 0, width, height));
            g.DrawString(uvi.ToString(), new Font("Segoe UI", (uvi < 10)? 150 : 110,
                                                    FontStyle.Bold, GraphicsUnit.Point),
                         fontBrush, rectf);

            g.Flush();
            return iconBMP;

        }
    }
}
