using System.Drawing;

namespace ImgSoh
{
    public class ImgPanel
    {
        public string Hash { get; }
        public long Size { get; }
        public Bitmap Bitmap { get; private set; }
        public string Format { get; }
        public bool IsVictim { get; private set; }

        public ImgPanel(string hash, long size, Bitmap bitmap, string format)
        {
            Hash = hash;
            Size = size;
            Bitmap = bitmap;
            Format = format;
            IsVictim = false;
        }

        public void SetVictim()
        {
            IsVictim = true;
        }
    }
}
