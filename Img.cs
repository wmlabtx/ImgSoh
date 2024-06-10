using System;
using System.Drawing;

namespace ImgSoh
{
    public class Img
    {
        public string Hash { get; }
        public string Path { get; set; }
        public string Ext { get; set; }
        public RotateFlipType Orientation { get; set; }
        public DateTime LastView { get; set; }
        public DateTime LastCheck { get; set; }
        public string Next { get; set; }
        public string Horizon { get; set; }
        public string Prev { get; set; }
        public bool Verified { get; set; }
        public int Counter { get; set; }
        public DateTime Taken { get; }
        public short Meta { get; }

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
            string path,
            string ext,
            float[] vector,
            DateTime lastview,
            RotateFlipType orientation,
            DateTime lastcheck,
            string next,
            string horizon,
            string prev,
            bool verified,
            int counter,
            DateTime taken,
            short meta
            )
        {
            Hash = hash;
            Path = path;
            Ext = ext;
            _vector = vector;
            Orientation = orientation;
            LastView = lastview;
            LastCheck = lastcheck;
            Next = next;
            Horizon = horizon;
            Prev = prev;
            Verified = verified;
            Counter = counter;
            Taken = taken;
            Meta = meta;
        }
    }
}