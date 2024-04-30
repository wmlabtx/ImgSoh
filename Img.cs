using System;
using System.Drawing;

namespace ImgSoh
{
    public class Img
    {
        public string Hash { get; }
        public string Folder { get; }
        public RotateFlipType Orientation { get; }
        public DateTime LastView { get; }
        public DateTime LastCheck { get; }
        public string Next { get; }
        public bool Verified { get; }
        public float Distance { get; }
        public string Prev { get; }
        public int Horizon { get; }

        private readonly float[] _vector;
        public float[] GetVector()
        {
            return _vector;
        }

        public Img(
            string hash,
            string folder,
            float[] vector,
            DateTime lastview,
            RotateFlipType orientation,
            DateTime lastcheck,
            string next,
            bool verified,
            float distance,
            int horizon,
            string prev
            )
        {
            Hash = hash;
            Folder = folder;
            _vector = vector;
            Orientation = orientation;
            LastView = lastview;
            LastCheck = lastcheck;
            Next = next;
            Verified = verified;
            Distance = distance;
            Horizon = horizon;
            Prev = prev;
        }
    }
}