using System;
using System.Drawing;

namespace ImgSoh
{
    public class ImgPanel
    {
        public string Hash { get; }
        public long Size { get; }
        public Bitmap Bitmap { get; private set; }
        public string Format { get; }
        public DateTime DateTaken { get; }

        public ImgPanel(string hash, long size, Bitmap bitmap, string format, DateTime dateTaken)
        {
            Hash = hash;
            Size = size;
            Bitmap = bitmap;
            Format = format;
            DateTaken = dateTaken;
        }
    }
}
