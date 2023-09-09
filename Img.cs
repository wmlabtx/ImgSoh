using System;
using System.Drawing;

namespace ImgSoh
{
    public class Img
    {
        public string Hash { get; }
        public string Folder { get; }

        private readonly byte[] _vector;
        public byte[] GetVector()
        {
            return _vector;
        }

        public DateTime LastView { get; }
        public RotateFlipType Orientation { get; }
        public float Distance { get; }
        public DateTime LastCheck { get; }
        public string Next { get; }
        public bool Verified { get; }

        public Img(
            string hash,
            string folder,
            byte[] vector,
            DateTime lastview,
            RotateFlipType orientation,
            float distance,
            DateTime lastcheck,
            string next,
            bool verified
            )
        {
            Hash = hash;
            Folder = folder;
            _vector = vector;
            Orientation = orientation;
            LastView = lastview;
            Distance = distance;
            LastCheck = lastcheck;
            Next = next;
            Verified = verified;
        }
    }
}