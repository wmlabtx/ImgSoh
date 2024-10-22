using System;
using System.Drawing;

namespace ImgSoh
{
    public class Img
    {
        public string Hash { get; }
        public string Name { get; }
        public RotateFlipType Orientation { get; }
        public DateTime LastView { get; }
        public string Next { get; }
        public string Horizon { get; }
        public bool Verified { get; }
        public int Counter { get; }
        public DateTime Taken { get; }
        public int Meta { get; }
        public float[] Vector { get; }
        public float Magnitude { get; }
        public int Viewed { get; }
        public string Family { get; }
        public string History { get; }

        public Img(
            string hash,
            string name,
            DateTime lastview,
            RotateFlipType orientation,
            string next,
            string horizon,
            bool verified,
            int counter,
            DateTime taken,
            int meta,
            float[] vector,
            float magnitude,
            int viewed,
            string family,
            string history
            )
        {
            Hash = hash;
            Name = name;
            Orientation = orientation;
            LastView = lastview;
            Next = next;
            Horizon = horizon;
            Verified = verified;
            Counter = counter;
            Taken = taken;
            Meta = meta;
            Vector = vector;
            Magnitude = magnitude;
            Viewed = viewed;
            Family = family;
            History = history;
        }
    }
}