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
        public string Horizon { get; }
        public string Prev { get; }
        public bool Verified { get; }
        public int Counter { get; }

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
            string horizon,
            string prev,
            bool verified,
            int counter
            )
        {
            Hash = hash;
            Folder = folder;
            _vector = vector;
            Orientation = orientation;
            LastView = lastview;
            LastCheck = lastcheck;
            Next = next;
            Horizon = horizon;
            Prev = prev;
            Verified = verified;
            Counter = counter;
        }
    }
}