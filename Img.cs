 using System;
using System.Drawing;

namespace ImgSoh
{
    public class Img
    {
        public string Name { get; }
        public RotateFlipType Orientation { get; }
        public DateTime LastView { get; }
        public DateTime LastCheck { get; }
        public string Next { get; }
        public string Horizon { get; }
        public bool Verified { get; }
        public int Counter { get; }
        public DateTime Taken { get; }
        public int Meta { get; }
        public float Magnitude { get; }
        public int Viewed { get; }

        public Img(
            string name,
            DateTime lastview,
            RotateFlipType orientation,
            DateTime lastcheck,
            string next,
            string horizon,
            bool verified,
            int counter,
            DateTime taken,
            int meta,
            float magnitude,
            int viewed
            )
        {
            Name = name;
            Orientation = orientation;
            LastView = lastview;
            LastCheck = lastcheck;
            Next = next;
            Horizon = horizon;
            Verified = verified;
            Counter = counter;
            Taken = taken;
            Meta = meta;
            Magnitude = magnitude;
            Viewed = viewed;
        }
    }
}