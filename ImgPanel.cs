using System.Drawing;

namespace ImgSoh
{
    public class ImgPanel
    {
        public string Hash { get; }
        public long Size { get; }
        public Bitmap Bitmap { get; private set; }
        public string Format { get; }
        public string[] FingerPrint { get; }
        public string DateTaken { get; }

        public ImgPanel(string hash, long size, Bitmap bitmap, string format, string[] fingerPrint, string dateTaken)
        {
            Hash = hash;
            Size = size;
            Bitmap = bitmap;
            Format = format;
            FingerPrint = fingerPrint;
            DateTaken = dateTaken;
        }
    }
}
