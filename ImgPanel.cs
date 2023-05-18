using System;
using System.Drawing;

namespace ImgSoh
{
    public class ImgPanel
    {
        public Img Img { get; }
        public long Size { get; }
        public Bitmap Bitmap { get; private set; }
        public string Format { get; }
        public DateTime DateTaken { get; }
        public double Blur { get; }

        public ImgPanel(Img img, long size, Bitmap bitmap, string format, DateTime dateTaken, double blur)
        {
            Img = img;
            Size = size;
            Bitmap = bitmap;
            Format = format;
            DateTaken = dateTaken;
            Blur = blur;
        }

        public void SetBitmap(Bitmap bmp)
        {
            Bitmap.Dispose();
            Bitmap = bmp;
        }
    }
}
