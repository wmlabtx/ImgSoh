using System;
using System.Drawing;

namespace ImgSoh
{
    public class Img
    {
        public string Hash { get; }
        public string Name { get; }
        public RotateFlipType Orientation { get; set; }
        public DateTime LastView { get; set; }
        public DateTime LastCheck { get; set; }
        public string Next { get; set; }
        public string Horizon { get; set; }
        public bool Verified { get; set; }
        public int Counter { get; set; }
        public DateTime Taken { get; }
        public int Meta { get; }
        public int Family { get; set; }
        public float Magnitude { get; set; }

        private float[] _vector;
        public float[] GetVector()
        {
            return _vector;
        }

        public void SetVector(float[] vector)
        {
            _vector = vector;
        }

        public Img(
            string hash,
            string name,
            float[] vector,
            DateTime lastview,
            RotateFlipType orientation,
            DateTime lastcheck,
            string next,
            string horizon,
            bool verified,
            int counter,
            DateTime taken,
            int meta,
            int family,
            float magnitude
            )
        {
            Hash = hash;
            Name = name;
            _vector = vector;
            Orientation = orientation;
            LastView = lastview;
            LastCheck = lastcheck;
            Next = next;
            Horizon = horizon;
            Verified = verified;
            Counter = counter;
            Taken = taken;
            Meta = meta;
            Family = family;
            Magnitude = magnitude;
        }
    }
}