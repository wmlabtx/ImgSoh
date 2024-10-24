using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ImgSoh
{
    public class Img
    {
        public string Hash { get; }
        public string Name { get; }
        public DateTime Taken { get; }
        public int Meta { get; }
        public float[] Vector { get; }
        public float Magnitude { get; }
        public RotateFlipType Orientation { get; }
        public DateTime LastView { get; }
        public string Family { get; }
        public string History { get; }

        private readonly HashSet<string> _history;

        public string[] HistoryArray => _history.ToArray();
        public int Count => _history.Count;

        public bool IsInHistory (string hash) {
            if (_history.Count == 0) {
                return false;
            }

            return _history.Contains(hash);
        }

        public Img(
            string hash,
            string name,
            DateTime taken,
            int meta,
            float[] vector,
            float magnitude,
            RotateFlipType orientation,
            DateTime lastview,
            string family,
            string history
            )
        {
            Hash = hash;
            Name = name;
            Taken = taken;
            Meta = meta;
            Vector = vector;
            Magnitude = magnitude;
            Orientation = orientation;
            LastView = lastview;
            Family = family;
            History = history;

            _history = new HashSet<string>();
            if (History.Length > 0) {
                for (var offset = 0; offset < History.Length; offset += 32) {
                    _history.Add(History.Substring(offset, 32));
                }
            }
        }
    }
}