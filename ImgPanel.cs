using System.Drawing;

namespace ImgSoh
{
    public class ImgPanel
    {
        public string Hash { get; }
        public Img Img { get; set; }
        public long Size { get; }
        public Bitmap Bitmap { get; private set; }
        public string Format { get; }
        public bool IsVictim { get; private set; }
        public int FamilySize { get; }

        public ImgPanel(string hash, Img img, long size, Bitmap bitmap, string format, int familysize)
        {
            Hash = hash;
            Img = img;
            Size = size;
            Bitmap = bitmap;
            Format = format;
            FamilySize = familysize;
            IsVictim = false;
        }

        public void SetVictim()
        {
            IsVictim = true;
        }
    }
}
