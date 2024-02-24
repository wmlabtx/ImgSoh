using System;
 using System.Collections.Generic;
 using System.Drawing;
 using System.Linq;

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
        public string FingerPrint { get; }

        private readonly float[] _vector;
        public float[] GetVector()
        {
            return _vector;
        }

        private readonly SortedSet<string> _history;
        public int HistoryCount => _history.Count;
        public string[] HistoryArray => _history.ToArray();
        public string History => Helper.SortedSetToString(_history);

        public bool IsInHistory(string hash)
        {
            return _history.Contains(hash);
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
            string history,
            string fingerprint
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
            _history = Helper.StringToSortedSet(history);
            FingerPrint = fingerprint;
        }
    }
}